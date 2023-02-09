using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Data.Common;
using System.Data.Linq.Mapping;
using System.Data.Linq.Provider;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Threading;
using System.Data.Linq.SqlClient.Implementation;
using Microsoft.Data.SqlClient;
using System.Diagnostics.CodeAnalysis;
using Mindbox.Data.Linq.Proxy;
using Mindbox.Expressions;

namespace System.Data.Linq.SqlClient 
{
    internal class ObjectReaderCompiler : IObjectReaderCompiler 
	{
		private static readonly LocalDataStoreSlot cacheSlot = Thread.AllocateDataSlot();
		private static int maxReaderCacheSize = 10;

        private readonly Type dataReaderType;
		private readonly IDataServices services;

		private readonly MethodInfo miDRisDBNull;
		private readonly MethodInfo miBRisDBNull;
		private readonly FieldInfo readerField;
		private readonly FieldInfo bufferReaderField;

		private readonly FieldInfo ordinalsField;
		private readonly FieldInfo globalsField;
		private readonly FieldInfo argsField;


        internal ObjectReaderCompiler(Type dataReaderType, IDataServices services) 
		{
            this.dataReaderType = dataReaderType;
            this.services = services;

            miDRisDBNull = dataReaderType.GetMethod(
				"IsDBNull", 
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            miBRisDBNull = typeof(DbDataReader).GetMethod(
				"IsDBNull", 
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            var orbType = typeof(ObjectMaterializer<>).MakeGenericType(this.dataReaderType);
            ordinalsField = orbType.GetField("Ordinals", BindingFlags.Instance | BindingFlags.Public);
            globalsField = orbType.GetField("Globals", BindingFlags.Instance | BindingFlags.Public);
            argsField = orbType.GetField("Arguments", BindingFlags.Instance | BindingFlags.Public);
            readerField = orbType.GetField("DataReader", BindingFlags.Instance | BindingFlags.Public);
            bufferReaderField = orbType.GetField("BufferReader", BindingFlags.Instance | BindingFlags.Public);

            Debug.Assert(
                miDRisDBNull != null &&
                miBRisDBNull != null &&
                readerField != null &&
                bufferReaderField != null &&
                ordinalsField != null &&
                globalsField != null &&
                argsField != null);
        }


        [ResourceExposure(ResourceScope.None)] // Consumed by Thread.AllocateDataSource result being unique.
        [ResourceConsumption(ResourceScope.AppDomain, ResourceScope.AppDomain)] // Thread.GetData method call.
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        public IObjectReaderFactory Compile(SqlExpression expression, Type elementType) {
            var mapping = services.Context.Mapping.Identity;
            var options = services.Context.LoadOptions;
            IObjectReaderFactory factory = null;
            ReaderFactoryCache cache = null;

            var canBeCompared = SqlProjectionComparer.CanBeCompared(expression);
            if (canBeCompared) 
			{
                cache = (ReaderFactoryCache)Thread.GetData(cacheSlot);
                if (cache == null) 
				{
                    cache = new ReaderFactoryCache(maxReaderCacheSize);
                    Thread.SetData(cacheSlot, cache);
                }
                factory = cache.GetFactory(elementType, dataReaderType, mapping, options, expression);
            }

            if (factory == null) 
			{
                var gen = new Generator(this, elementType);

                var dm = CompileDynamicMethod(gen, expression, elementType);
                var fnMatType = typeof(Func<,>)
					.MakeGenericType(
						typeof(ObjectMaterializer<>).MakeGenericType(dataReaderType), 
						elementType);
                var fnMaterialize = dm.CreateDelegate(fnMatType);

                var factoryType = typeof(ObjectReaderFactory<,>).MakeGenericType(dataReaderType, elementType);
                factory = (IObjectReaderFactory)Activator.CreateInstance(
                    factoryType, 
					BindingFlags.Instance | BindingFlags.NonPublic, 
					null,
                    new object[]
                    {
	                    fnMaterialize, 
						gen.NamedColumns, 
						gen.Globals, 
						gen.Locals
                    }, 
					null);

                if (canBeCompared) 
				{
                    expression = new SourceExpressionRemover().VisitExpression(expression);
                    cache.AddFactory(elementType, dataReaderType, mapping, options, expression, factory);
                }
            }
            return factory;
        }

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		public IObjectReaderSession CreateSession(
			DbDataReader reader, 
			IReaderProvider provider, 
			object[] parentArgs, 
			object[] userArgs, 
			ICompiledSubQuery[] subQueries)
		{
			var sessionType = typeof(ObjectReaderSession<>).MakeGenericType(dataReaderType);
			return (IObjectReaderSession)Activator.CreateInstance(
				sessionType, 
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
				null,
				new object[]
				{
					reader, 
					provider, 
					parentArgs, 
					userArgs, 
					subQueries
				}, 
				null);
		}

		private DynamicMethod CompileDynamicMethod(Generator gen, SqlExpression expression, Type elementType)
		{
			var objectReaderType = typeof(ObjectMaterializer<>).MakeGenericType(dataReaderType);
			var dm = new DynamicMethod(
				"Read_" + elementType.Name,
				elementType,
				new[]
                {
	                objectReaderType
                },
				true);
			gen.GenerateBody(dm.GetILGenerator(), expression);
			return dm;
		}


		internal class SqlProjectionComparer
		{
			internal static bool CanBeCompared(SqlExpression node)
			{
				if (node == null)
					return true;

				switch (node.NodeType)
				{
					case SqlNodeType.New:
						{
							var new1 = (SqlNew)node;
							for (int i = 0, n = new1.Args.Count; i < n; i++)
								if (!CanBeCompared(new1.Args[i]))
									return false;
							for (int i = 0, n = new1.Members.Count; i < n; i++)
								if (!CanBeCompared(new1.Members[i].Expression))
									return false;
							return true;
						}

					case SqlNodeType.ColumnRef:
					case SqlNodeType.Value:
					case SqlNodeType.UserColumn:
						return true;

					case SqlNodeType.Link:
						{
							var l1 = (SqlLink)node;
							for (int i = 0, c = l1.KeyExpressions.Count; i < c; ++i)
								if (!CanBeCompared(l1.KeyExpressions[i]))
									return false;
							return true;
						}

					case SqlNodeType.OptionalValue:
						return CanBeCompared(((SqlOptionalValue)node).Value);

					case SqlNodeType.ValueOf:
					case SqlNodeType.OuterJoinedValue:
						return CanBeCompared(((SqlUnary)node).Operand);

					case SqlNodeType.Lift:
						return CanBeCompared(((SqlLift)node).Expression);

					case SqlNodeType.Grouping:
						{
							var g1 = (SqlGrouping)node;
							return CanBeCompared(g1.Key) && CanBeCompared(g1.Group);
						}

					case SqlNodeType.ClientArray:
						{
							if (node.SourceExpression.NodeType != ExpressionType.NewArrayInit &&
									node.SourceExpression.NodeType != ExpressionType.NewArrayBounds)
								return false;
							var a1 = (SqlClientArray)node;
							for (int i = 0, n = a1.Expressions.Count; i < n; i++)
								if (!CanBeCompared(a1.Expressions[i]))
									return false;
							return true;
						}

					case SqlNodeType.ClientCase:
						{
							var c1 = (SqlClientCase)node;
							for (int i = 0, n = c1.Whens.Count; i < n; i++)
								if (!CanBeCompared(c1.Whens[i].Match) || !CanBeCompared(c1.Whens[i].Value))
									return false;
							return true;
						}

					case SqlNodeType.SearchedCase:
						{
							var c1 = (SqlSearchedCase)node;
							for (int i = 0, n = c1.Whens.Count; i < n; i++)
								if (!CanBeCompared(c1.Whens[i].Match) || !CanBeCompared(c1.Whens[i].Value))
									return false;
							return CanBeCompared(c1.Else);
						}

					case SqlNodeType.TypeCase:
						{
							var c1 = (SqlTypeCase)node;
							if (!CanBeCompared(c1.Discriminator))
								return false;

							foreach (var c1When in c1.Whens)
							{
								if (!CanBeCompared(c1When.Match))
									return false;
								if (!CanBeCompared(c1When.TypeBinding))
									return false;
							}
							return true;
						}

					case SqlNodeType.DiscriminatedType:
						return CanBeCompared(((SqlDiscriminatedType)node).Discriminator);

					case SqlNodeType.JoinedCollection:
						{
							var j1 = (SqlJoinedCollection)node;
							return CanBeCompared(j1.Count) && CanBeCompared(j1.Expression);
						}

					case SqlNodeType.Member:
						return CanBeCompared(((SqlMember)node).Expression);

					case SqlNodeType.MethodCall:
						{
							var mc = (SqlMethodCall)node;
							if (mc.Object != null && !CanBeCompared(mc.Object))
								return false;

							foreach (var argument in mc.Arguments)
								if (!CanBeCompared(argument))
									return false;
							return true;
						}

					case SqlNodeType.ClientQuery:
						return true;

					default:
						return false;
				}
			}

			internal static bool AreSimilar(SqlExpression node1, SqlExpression node2)
			{
				if (node1 == node2)
					return true;
				if (node1 == null || node2 == null)
					return false;
				if (node1.NodeType != node2.NodeType || node1.ClrType != node2.ClrType || node1.SqlType != node2.SqlType)
					return false;
				switch (node1.NodeType)
				{
					case SqlNodeType.New:
						{
							var new1 = (SqlNew)node1;
							var new2 = (SqlNew)node2;
							if (new1.Args.Count != new2.Args.Count || new1.Members.Count != new2.Members.Count)
								return false;
							for (var i = 0; i < new1.Args.Count; i++)
								if (!AreSimilar(new1.Args[i], new2.Args[i]))
									return false;
							for (var i = 0; i < new1.Members.Count; i++)
								if (!MetaPosition.AreSameMember(new1.Members[i].Member, new2.Members[i].Member) ||
										!AreSimilar(new1.Members[i].Expression, new2.Members[i].Expression))
									return false;
							return true;
						}

					case SqlNodeType.ColumnRef:
						{
							var cref1 = (SqlColumnRef)node1;
							var cref2 = (SqlColumnRef)node2;
							return cref1.Column.Ordinal == cref2.Column.Ordinal;
						}

					case SqlNodeType.Link:
						{
							var l1 = (SqlLink)node1;
							var l2 = (SqlLink)node2;
							if (!MetaPosition.AreSameMember(l1.Member.Member, l2.Member.Member))
								return false;
							if (l1.KeyExpressions.Count != l2.KeyExpressions.Count)
								return false;
							for (var i = 0; i < l1.KeyExpressions.Count; ++i)
								if (!AreSimilar(l1.KeyExpressions[i], l2.KeyExpressions[i]))
									return false;
							return true;
						}

					case SqlNodeType.Value:
						return Equals(((SqlValue)node1).Value, ((SqlValue)node2).Value);

					case SqlNodeType.OptionalValue:
						{
							var ov1 = (SqlOptionalValue)node1;
							var ov2 = (SqlOptionalValue)node2;
							return AreSimilar(ov1.Value, ov2.Value);
						}

					case SqlNodeType.ValueOf:
					case SqlNodeType.OuterJoinedValue:
						return AreSimilar(((SqlUnary)node1).Operand, ((SqlUnary)node2).Operand);

					case SqlNodeType.Lift:
						return AreSimilar(((SqlLift)node1).Expression, ((SqlLift)node2).Expression);

					case SqlNodeType.Grouping:
						{
							var g1 = (SqlGrouping)node1;
							var g2 = (SqlGrouping)node2;
							return AreSimilar(g1.Key, g2.Key) && AreSimilar(g1.Group, g2.Group);
						}

					case SqlNodeType.ClientArray:
						{
							var a1 = (SqlClientArray)node1;
							var a2 = (SqlClientArray)node2;
							if (a1.Expressions.Count != a2.Expressions.Count)
								return false;
							for (var i = 0; i < a1.Expressions.Count; i++)
								if (!AreSimilar(a1.Expressions[i], a2.Expressions[i]))
									return false;
							return true;
						}

					case SqlNodeType.UserColumn:
						return ((SqlUserColumn)node1).Name == ((SqlUserColumn)node2).Name;

					case SqlNodeType.ClientCase:
						{
							var c1 = (SqlClientCase)node1;
							var c2 = (SqlClientCase)node2;
							if (c1.Whens.Count != c2.Whens.Count)
								return false;
							for (var i = 0; i < c1.Whens.Count; i++)
								if (!AreSimilar(c1.Whens[i].Match, c2.Whens[i].Match) ||
										!AreSimilar(c1.Whens[i].Value, c2.Whens[i].Value))
									return false;
							return true;
						}

					case SqlNodeType.SearchedCase:
						{
							var c1 = (SqlSearchedCase)node1;
							var c2 = (SqlSearchedCase)node2;
							if (c1.Whens.Count != c2.Whens.Count)
								return false;
							for (var i = 0; i < c1.Whens.Count; i++)
								if (!AreSimilar(c1.Whens[i].Match, c2.Whens[i].Match) ||
										!AreSimilar(c1.Whens[i].Value, c2.Whens[i].Value))
									return false;
							return AreSimilar(c1.Else, c2.Else);
						}

					case SqlNodeType.TypeCase:
						{
							var c1 = (SqlTypeCase)node1;
							var c2 = (SqlTypeCase)node2;
							if (!AreSimilar(c1.Discriminator, c2.Discriminator))
								return false;
							if (c1.Whens.Count != c2.Whens.Count)
								return false;
							for (var i = 0; i < c1.Whens.Count; ++i)
							{
								if (!AreSimilar(c1.Whens[i].Match, c2.Whens[i].Match))
									return false;
								if (!AreSimilar(c1.Whens[i].TypeBinding, c2.Whens[i].TypeBinding))
									return false;
							}
							return true;
						}

					case SqlNodeType.DiscriminatedType:
						{
							var dt1 = (SqlDiscriminatedType)node1;
							var dt2 = (SqlDiscriminatedType)node2;
							return AreSimilar(dt1.Discriminator, dt2.Discriminator);
						}

					case SqlNodeType.JoinedCollection:
						{
							var j1 = (SqlJoinedCollection)node1;
							var j2 = (SqlJoinedCollection)node2;
							return AreSimilar(j1.Count, j2.Count) && AreSimilar(j1.Expression, j2.Expression);
						}

					case SqlNodeType.Member:
						{
							var m1 = (SqlMember)node1;
							var m2 = (SqlMember)node2;
							return m1.Member == m2.Member && AreSimilar(m1.Expression, m2.Expression);
						}

					case SqlNodeType.ClientQuery:
						{
							var cq1 = (SqlClientQuery)node1;
							var cq2 = (SqlClientQuery)node2;
							if (cq1.Arguments.Count != cq2.Arguments.Count)
								return false;
							for (var i = 0; i < cq1.Arguments.Count; i++)
								if (!AreSimilar(cq1.Arguments[i], cq2.Arguments[i]))
									return false;
							return true;
						}

					case SqlNodeType.MethodCall:
						{
							var mc1 = (SqlMethodCall)node1;
							var mc2 = (SqlMethodCall)node2;
							if (mc1.Method != mc2.Method || !AreSimilar(mc1.Object, mc2.Object))
								return false;
							if (mc1.Arguments.Count != mc2.Arguments.Count)
								return false;
							for (var i = 0; i < mc1.Arguments.Count; i++)
								if (!AreSimilar(mc1.Arguments[i], mc2.Arguments[i]))
									return false;
							return true;
						}

					default:
						return false;
				}
			}
		}


        private class SourceExpressionRemover : SqlDuplicator.DuplicatingVisitor 
		{
            internal SourceExpressionRemover()
                : base(true) 
			{
            }


            internal override SqlNode Visit(SqlNode node) 
			{
                node = base.Visit(node);

	            if (node != null)
		            node.ClearSourceExpression();
	            return node;
            }

            internal override SqlExpression VisitColumnRef(SqlColumnRef cref) 
			{
                var result = base.VisitColumnRef(cref);

                if (result != null && result == cref) 
				{
                    // reference to outer scope, don't propogate references to expressions or aliases
                    var col = cref.Column;
                    var newcol = new SqlColumn(col.ClrType, col.SqlType, col.Name, col.MetaMember, null, col.SourceExpression)
                    {
	                    Ordinal = col.Ordinal
                    };
					result = new SqlColumnRef(newcol);
                    newcol.ClearSourceExpression();
                }
                return result;
            }

            internal override SqlExpression VisitAliasRef(SqlAliasRef aref) 
			{
                var result = base.VisitAliasRef(aref);

                if (result != null && result == aref) 
				{
                    // reference to outer scope, don't propogate references to expressions or aliases
                    var newalias = new SqlAlias(new SqlNop(aref.ClrType, aref.SqlType, null));
                    return new SqlAliasRef(newalias);
                }
                return result;
            }
        }


        private class ReaderFactoryCache 
		{
            private readonly int maxCacheSize;
			private readonly LinkedList<CacheInfo> list;


			internal ReaderFactoryCache(int maxCacheSize)
			{
				this.maxCacheSize = maxCacheSize;
				list = new LinkedList<CacheInfo>();
			}


			internal IObjectReaderFactory GetFactory(
				Type elementType, 
				Type dataReaderType, 
				object mapping, 
				DataLoadOptions options, 
				SqlExpression projection)
			{
				for (var info = list.First; info != null; info = info.Next)
				{
					if (elementType == info.Value.elementType &&
						dataReaderType == info.Value.dataReaderType &&
						mapping == info.Value.mapping &&
						DataLoadOptions.ShapesAreEquivalent(options, info.Value.options) &&
						SqlProjectionComparer.AreSimilar(projection, info.Value.projection))
					{
						// move matching item to head of list to reset its lifetime
						list.Remove(info);
						list.AddFirst(info);
						return info.Value.factory;
					}
				}
				return null;
			}

			internal void AddFactory(
				Type elementType, 
				Type dataReaderType, 
				object mapping, 
				DataLoadOptions options, 
				SqlExpression projection, 
				IObjectReaderFactory factory)
			{
				list.AddFirst(new LinkedListNode<CacheInfo>(
					new CacheInfo(elementType, dataReaderType, mapping, options, projection, factory)));
				if (list.Count > maxCacheSize)
					list.RemoveLast();
			}

			
			private class CacheInfo 
			{
                internal readonly Type elementType;
                internal readonly Type dataReaderType;
                internal readonly object mapping;
                internal readonly DataLoadOptions options;
                internal readonly SqlExpression projection;
                internal readonly IObjectReaderFactory factory;


                public CacheInfo(
					Type elementType, 
					Type dataReaderType, 
					object mapping, 
					DataLoadOptions options, 
					SqlExpression projection, 
					IObjectReaderFactory factory) 
				{
                    this.elementType = elementType;
                    this.dataReaderType = dataReaderType;
                    this.options = options;
                    this.mapping = mapping;
                    this.projection = projection;
                    this.factory = factory;
                }
            }
        }


        private class SideEffectChecker : SqlVisitor 
		{
            private bool hasSideEffect;


            internal bool HasSideEffect(SqlNode node) 
			{
                hasSideEffect = false;
                Visit(node);
                return hasSideEffect;
            }

            internal override SqlExpression VisitJoinedCollection(SqlJoinedCollection jc) 
			{
                hasSideEffect = true;
                return jc;
            }

            internal override SqlExpression VisitClientQuery(SqlClientQuery cq) 
			{
                return cq;
            }
        }


        private class Generator 
		{
			/// <summary>
			/// Cannot use Call for virtual methods - it results in unverifiable code.  Ensure we're using the correct op code.
			/// </summary>
			private static OpCode GetMethodCallOpCode(MethodInfo mi)
			{
				return (mi.IsStatic || mi.DeclaringType.IsValueType) ? OpCodes.Call : OpCodes.Callvirt;
			}

			private static bool IsAssignable(MemberInfo member)
			{
				var fi = member as FieldInfo;
				if (fi != null)
					return true;
				var pi = member as PropertyInfo;
				return (pi != null) && pi.CanWrite;
			}


            private readonly ObjectReaderCompiler compiler;
			private ILGenerator gen;
			private List<object> globals;
			private List<NamedColumn> namedColumns;
			private LocalBuilder locDataReader;
			private readonly Type elementType;
			private int nLocals;
	        private readonly SideEffectChecker sideEffectChecker = new SideEffectChecker();

#if DEBUG

			private int stackDepth;

#endif


            internal Generator(ObjectReaderCompiler compiler, Type elementType) 
			{
                this.compiler = compiler;
                this.elementType = elementType;
            }


			internal object[] Globals
			{
				get { return globals.ToArray(); }
			}

			internal NamedColumn[] NamedColumns
			{
				get { return namedColumns.ToArray(); }
			}

			internal int Locals
			{
				get { return nLocals; }
			}


            internal void GenerateBody(ILGenerator generator, SqlExpression expression) 
			{
                gen = generator;
                globals = new List<object>();
                namedColumns = new List<NamedColumn>();

                // prepare locDataReader
                locDataReader = generator.DeclareLocal(compiler.dataReaderType);
                generator.Emit(OpCodes.Ldarg_0);
                generator.Emit(OpCodes.Ldfld, compiler.readerField);
                generator.Emit(OpCodes.Stloc, locDataReader);

                GenerateExpressionForType(expression, elementType);

                generator.Emit(OpCodes.Ret);
            }


			private Type Generate(SqlNode node)
			{
				return Generate(node, null);
			}

            private Type Generate(SqlNode node, LocalBuilder locInstance) 
			{
#if DEBUG
                try 
				{
                    stackDepth++;
                    Debug.Assert(stackDepth < 500);
#endif
                    switch (node.NodeType) 
					{
                        case SqlNodeType.New:
                            return GenerateNew((SqlNew)node);

                        case SqlNodeType.ColumnRef:
                            return GenerateColumnReference((SqlColumnRef)node);

                        case SqlNodeType.ClientQuery:
                            return GenerateClientQuery((SqlClientQuery)node);

                        case SqlNodeType.JoinedCollection:
                            return GenerateJoinedCollection((SqlJoinedCollection)node);

                        case SqlNodeType.Link:
                            return GenerateLink((SqlLink)node, locInstance);

                        case SqlNodeType.Value:
                            return GenerateValue((SqlValue)node);

                        case SqlNodeType.ClientParameter:
                            return GenerateClientParameter((SqlClientParameter)node);

                        case SqlNodeType.ValueOf:
                            return GenerateValueOf((SqlUnary)node);

                        case SqlNodeType.OptionalValue:
                            return GenerateOptionalValue((SqlOptionalValue)node);

                        case SqlNodeType.OuterJoinedValue:
                            return Generate(((SqlUnary)node).Operand);

                        case SqlNodeType.Lift:
                            return GenerateLift((SqlLift)node);

                        case SqlNodeType.Grouping:
                            return GenerateGrouping((SqlGrouping)node);

                        case SqlNodeType.ClientArray:
                            return GenerateClientArray((SqlClientArray)node);

                        case SqlNodeType.UserColumn:
                            return GenerateUserColumn((SqlUserColumn)node);

                        case SqlNodeType.ClientCase:
                            return GenerateClientCase((SqlClientCase)node, false, locInstance);

                        case SqlNodeType.SearchedCase:
                            return GenerateSearchedCase((SqlSearchedCase)node);

                        case SqlNodeType.TypeCase:
                            return GenerateTypeCase((SqlTypeCase)node);

                        case SqlNodeType.DiscriminatedType:
                            return GenerateDiscriminatedType((SqlDiscriminatedType)node);

                        case SqlNodeType.Member:
                            return GenerateMember((SqlMember)node);

                        case SqlNodeType.MethodCall:
                            return GenerateMethodCall((SqlMethodCall)node);

                        default:
                            throw Error.CouldNotTranslateExpressionForReading(node.SourceExpression);
                    }
#if DEBUG
                }
                finally 
				{
                    stackDepth--;
                }
#endif
            }

            private void GenerateAccessBufferReader() 
			{
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, compiler.bufferReaderField);
            }

            private void GenerateAccessDataReader() 
			{
                gen.Emit(OpCodes.Ldloc, locDataReader);
            }

            private void GenerateAccessOrdinals() 
			{
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, compiler.ordinalsField);
            }

            private void GenerateAccessGlobals() 
			{
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, compiler.globalsField);
            }

            private void GenerateAccessArguments() 
			{
                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Ldfld, compiler.argsField);
            }

            private Type GenerateValue(SqlValue value) 
			{
                return GenerateConstant(value.ClrType, value.Value);
            }

            private Type GenerateClientParameter(SqlClientParameter cp) 
			{
                var d = cp.Accessor.Compile();
                var iGlobal = AddGlobal(d.GetType(), d);
                GenerateGlobalAccess(iGlobal, d.GetType());
                GenerateAccessArguments();
                var miInvoke = d.GetType().GetMethod(
                    "Invoke",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                    null,
                    new[]
                    {
	                    typeof(object[])
                    },
                    null);
                Debug.Assert(miInvoke != null);
                gen.Emit(GetMethodCallOpCode(miInvoke), miInvoke);
                return d.Method.ReturnType;
            }

            private Type GenerateValueOf(SqlUnary u) 
			{
                Debug.Assert(TypeSystem.IsNullableType(u.Operand.ClrType));
                GenerateExpressionForType(u.Operand, u.Operand.ClrType);
                var loc = gen.DeclareLocal(u.Operand.ClrType);
                gen.Emit(OpCodes.Stloc, loc);
                gen.Emit(OpCodes.Ldloca, loc);
                GenerateGetValue(u.Operand.ClrType);
                return u.ClrType;
            }

            private Type GenerateOptionalValue(SqlOptionalValue opt) 
			{
                Debug.Assert(opt.HasValue.ClrType == typeof(int?));

                var labIsNull = gen.DefineLabel();
                var labExit = gen.DefineLabel();

                var actualType = Generate(opt.HasValue);
                Debug.Assert(TypeSystem.IsNullableType(actualType));
                var loc = gen.DeclareLocal(actualType);
                gen.Emit(OpCodes.Stloc, loc);
                gen.Emit(OpCodes.Ldloca, loc);
                GenerateHasValue(actualType);
                gen.Emit(OpCodes.Brfalse, labIsNull);

                GenerateExpressionForType(opt.Value, opt.ClrType);
                gen.Emit(OpCodes.Br_S, labExit);

                gen.MarkLabel(labIsNull);
                GenerateConstant(opt.ClrType, null);

                gen.MarkLabel(labExit);
                return opt.ClrType;
            }

            private Type GenerateLift(SqlLift lift) 
			{
                return GenerateExpressionForType(lift.Expression, lift.ClrType);
            }

            private Type GenerateClientArray(SqlClientArray ca) 
			{
	            if (!ca.ClrType.IsArray)
		            throw Error.CannotMaterializeList(ca.ClrType);

	            var elemType = TypeSystem.GetElementType(ca.ClrType);
                GenerateConstInt(ca.Expressions.Count);
                gen.Emit(OpCodes.Newarr, elemType);
                for (var i = 0; i < ca.Expressions.Count; i++) 
				{
                    gen.Emit(OpCodes.Dup);
                    GenerateConstInt(i);
                    GenerateExpressionForType(ca.Expressions[i], elemType);
                    GenerateArrayAssign(elemType);
                }
                return ca.ClrType;
            }

            private Type GenerateMember(SqlMember m)
            {
	            var fi = m.Member as FieldInfo;
	            if (fi == null)
	            {
		            var pi = (PropertyInfo)m.Member;
		            return GenerateMethodCall(
						new SqlMethodCall(m.ClrType, m.SqlType, pi.GetGetMethod(), m.Expression, null, m.SourceExpression));
	            }

	            GenerateExpressionForType(m.Expression, m.Expression.ClrType);
	            gen.Emit(OpCodes.Ldfld, fi);
	            return fi.FieldType;
            }

	        private Type GenerateMethodCall(SqlMethodCall mc) 
			{
                var pis = mc.Method.GetParameters();
                if (mc.Object != null) 
				{
                    var actualType = GenerateExpressionForType(mc.Object, mc.Object.ClrType);
                    if (actualType.IsValueType) 
					{
                        var loc = gen.DeclareLocal(actualType);
                        gen.Emit(OpCodes.Stloc, loc);
                        gen.Emit(OpCodes.Ldloca, loc);
                    }
                }
                for (var i = 0; i < mc.Arguments.Count; i++) 
				{
                    var pi = pis[i];
                    var pType = pi.ParameterType;
                    if (pType.IsByRef) 
					{
                        pType = pType.GetElementType();
                        GenerateExpressionForType(mc.Arguments[i], pType);
                        var loc = gen.DeclareLocal(pType);
                        gen.Emit(OpCodes.Stloc, loc);
                        gen.Emit(OpCodes.Ldloca, loc);
                    }
                    else 
					{
                        GenerateExpressionForType(mc.Arguments[i], pType);
                    }
                }
                var callOpCode = GetMethodCallOpCode(mc.Method);
		        if (mc.Object != null && TypeSystem.IsNullableType(mc.Object.ClrType) && callOpCode == OpCodes.Callvirt)
			        gen.Emit(OpCodes.Constrained, mc.Object.ClrType);
		        gen.Emit(callOpCode, mc.Method);

                return mc.Method.ReturnType;
            }

            private Type GenerateNew(SqlNew sn)
            {
				if (compiler.services.Model.ShouldEntityProxyBeCreated(sn.ClrType))
					return GenerateNewProxy(sn);

                var locInstance = gen.DeclareLocal(sn.ClrType);

                // read all arg values
                if (sn.Args.Count > 0) 
				{
                    var pis = sn.Constructor.GetParameters();
					for (var i = 0; i < sn.Args.Count; i++)
						GenerateExpressionForType(sn.Args[i], pis[i].ParameterType);
				}

                // construct the new instance
                if (sn.Constructor != null) 
				{
                    gen.Emit(OpCodes.Newobj, sn.Constructor);
                    gen.Emit(OpCodes.Stloc, locInstance);
                }
                else if (sn.ClrType.IsValueType) 
				{
                    gen.Emit(OpCodes.Ldloca, locInstance);
                    gen.Emit(OpCodes.Initobj, sn.ClrType);
                }
                else 
				{
                    var ci = sn.ClrType.GetConstructor(Type.EmptyTypes);
                    gen.Emit(OpCodes.Newobj, ci);
                    gen.Emit(OpCodes.Stloc, locInstance);
                }

	            return GenerateInitializeNew(sn, locInstance);
            }

			private Type GenerateInitializeNew(SqlNew sn, LocalBuilder locInstance)
	        {
				LocalBuilder locStoreInMember = null;
				var labNewExit = gen.DefineLabel();
				var labAlreadyCached = gen.DefineLabel();

				// read/write key bindings if there are any
				foreach (var ma in sn.Members.OrderBy(m => sn.MetaType.GetDataMember(m.Member).Ordinal))
				{
					var mm = sn.MetaType.GetDataMember(ma.Member);
					if (mm.IsPrimaryKey)
						GenerateMemberAssignment(mm, locInstance, ma.Expression, null);
				}

				var iMeta = 0;

				if (sn.MetaType.IsEntity)
				{
					var locCached = gen.DeclareLocal(sn.ClrType);
					locStoreInMember = gen.DeclareLocal(typeof(bool));
					var labExit = gen.DefineLabel();

					iMeta = AddGlobal(typeof(MetaType), sn.MetaType);
					var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);

					// this.InsertLookup(metaType, locInstance)
					gen.Emit(OpCodes.Ldarg_0);
					GenerateConstInt(iMeta);
					gen.Emit(OpCodes.Ldloc, locInstance);
					var miInsertLookup = orbType.GetMethod(
						"InsertLookup",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
						null,
						new[]
                        {
	                        typeof(int), 
							typeof(object)
                        },
						null);

					Debug.Assert(miInsertLookup != null);
					gen.Emit(GetMethodCallOpCode(miInsertLookup), miInsertLookup);
					gen.Emit(OpCodes.Castclass, sn.ClrType);
					gen.Emit(OpCodes.Stloc, locCached);

					// if cached != instance then already cached
					gen.Emit(OpCodes.Ldloc, locCached);
					gen.Emit(OpCodes.Ldloc, locInstance);
					gen.Emit(OpCodes.Ceq);
					gen.Emit(OpCodes.Brfalse, labAlreadyCached);

					GenerateConstInt(1);
					gen.Emit(OpCodes.Stloc, locStoreInMember);
					gen.Emit(OpCodes.Br_S, labExit);

					gen.MarkLabel(labAlreadyCached);
					gen.Emit(OpCodes.Ldloc, locCached);
					gen.Emit(OpCodes.Stloc, locInstance);

					// signal to not store loaded values in instance...
					GenerateConstInt(0);
					gen.Emit(OpCodes.Stloc, locStoreInMember);

					gen.MarkLabel(labExit);
				}

				// read/write non-key bindings
				foreach (var ma in sn.Members.OrderBy(m => sn.MetaType.GetDataMember(m.Member).Ordinal))
				{
					var mm = sn.MetaType.GetDataMember(ma.Member);
					if (!mm.IsPrimaryKey)
						GenerateMemberAssignment(mm, locInstance, ma.Expression, locStoreInMember);
				}

				if (sn.MetaType.IsEntity)
				{
					// don't call SendEntityMaterialized if we already had the instance cached
					gen.Emit(OpCodes.Ldloc, locStoreInMember);
					GenerateConstInt(0);
					gen.Emit(OpCodes.Ceq);
					gen.Emit(OpCodes.Brtrue, labNewExit);

					// send entity materialized event
					gen.Emit(OpCodes.Ldarg_0);
					GenerateConstInt(iMeta);
					gen.Emit(OpCodes.Ldloc, locInstance);
					var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);
					var miRaiseEvent = orbType.GetMethod(
						"SendEntityMaterialized",
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
						null,
						new[]
                        {
	                        typeof(int), 
							typeof(object)
                        },
						null);
					Debug.Assert(miRaiseEvent != null);
					gen.Emit(GetMethodCallOpCode(miRaiseEvent), miRaiseEvent);
				}

				gen.MarkLabel(labNewExit);
				gen.Emit(OpCodes.Ldloc, locInstance);

				return sn.ClrType;
			}

			private Type GenerateNewProxy(SqlNew sn)
			{
				if (sn.Args.Any())
					throw new InvalidOperationException("sn.Args.Any()");

				var locInstance = gen.DeclareLocal(sn.ClrType);

				// construct the new instance
				var factoryMethod = typeof(ObjectMaterializer<>)
					.MakeGenericType(compiler.dataReaderType)
					.GetMethod("CreateEntityProxy", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
					.MakeGenericMethod(sn.ClrType);
				gen.Emit(OpCodes.Ldarg_0);
				gen.Emit(GetMethodCallOpCode(factoryMethod), factoryMethod);
				gen.Emit(OpCodes.Stloc, locInstance);

				return GenerateInitializeNew(sn, locInstance);
			}

			private void GenerateMemberAssignment(
				MetaDataMember mm, 
				LocalBuilder locInstance, 
				SqlExpression expr, 
				LocalBuilder locStoreInMember) 
			{
				var memberType = TypeSystem.GetMemberType(mm.StorageMember ?? mm.Member);

                // check for deferrable member & deferred source expression
                if (IsDeferrableExpression(expr) &&
                    (compiler.services.Context.LoadOptions == null ||
                     !compiler.services.Context.LoadOptions.IsPreloaded(mm.Member))) 
				{
                    // we can only defer deferrable members 
                    if (mm.IsDeferred) 
					{
                        // determine at runtime if we are allowed to defer load 
                        gen.Emit(OpCodes.Ldarg_0);
                        var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);
                        var piCanDeferLoad = orbType.GetProperty("CanDeferLoad");
                        Debug.Assert(piCanDeferLoad != null);
                        var miCanDeferLoad = piCanDeferLoad.GetGetMethod();
                        gen.Emit(GetMethodCallOpCode(miCanDeferLoad), miCanDeferLoad);

                        // if we can't defer load then jump over the code that does the defer loading
                        var labEndDeferLoad = gen.DefineLabel();
                        gen.Emit(OpCodes.Brfalse, labEndDeferLoad);

                        // execute the defer load operation
                        if (memberType.IsGenericType) 
						{
                            var genType = memberType.GetGenericTypeDefinition();
	                        if (genType == typeof(EntitySet<>))
		                        GenerateAssignDeferredEntitySet(mm, locInstance, expr, locStoreInMember);
	                        else if (genType == typeof(EntityRef<>) || genType == typeof(Link<>))
		                        GenerateAssignDeferredReference(mm, locInstance, expr, locStoreInMember);
	                        else
		                        throw Error.DeferredMemberWrongType();
                        }
                        else 
						{
							GenerateAssignDeferredReferenceInProxy(mm, locInstance, expr, locStoreInMember);
                        }
                        gen.MarkLabel(labEndDeferLoad);
                    }
                    else 
					{
                        // behavior for non-deferred members w/ deferrable expressions is to load nothing
                    }
                }
                else if (memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(EntitySet<>)) 
				{
                    GenerateAssignEntitySet(mm, locInstance, expr, locStoreInMember);
                }
                else 
				{
                    GenerateAssignValue(mm, locInstance, expr, locStoreInMember);
                }
            }

            private void GenerateAssignValue(
				MetaDataMember mm, 
				LocalBuilder locInstance, 
				SqlExpression expr, 
				LocalBuilder locStoreInMember) 
			{
                var m = mm.StorageMember ?? mm.Member;
	            if (!IsAssignable(m))
		            throw Error.CannotAssignToMember(m.Name);
	            var memberType = TypeSystem.GetMemberType(m);

                var labExit = gen.DefineLabel();

                var hasSideEffect = HasSideEffect(expr);

                if (locStoreInMember != null && !hasSideEffect) 
				{
                    gen.Emit(OpCodes.Ldloc, locStoreInMember);
                    GenerateConstInt(0);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labExit);
                }

                GenerateExpressionForType(expr, memberType, mm.DeclaringType.IsEntity ? locInstance : null);
                var locValue = gen.DeclareLocal(memberType);

                gen.Emit(OpCodes.Stloc, locValue);

                if (locStoreInMember != null && hasSideEffect) 
				{
                    gen.Emit(OpCodes.Ldloc, locStoreInMember);
                    GenerateConstInt(0);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labExit);
                }

                GenerateLoadForMemberAccess(locInstance);
                gen.Emit(OpCodes.Ldloc, locValue);
                GenerateStoreMember(m);

                gen.MarkLabel(labExit);
            }

            private void GenerateAssignDeferredEntitySet(
				MetaDataMember mm, 
				LocalBuilder locInstance, 
				SqlExpression expr, 
				LocalBuilder locStoreInMember) 
			{
                var m = mm.StorageMember ?? mm.Member;
                var memberType = TypeSystem.GetMemberType(m);
                Debug.Assert(memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(EntitySet<>));
                var labExit = gen.DefineLabel();
                var argType = typeof(IEnumerable<>).MakeGenericType(memberType.GetGenericArguments());

                var hasSideEffect = HasSideEffect(expr);

                if (locStoreInMember != null && !hasSideEffect) 
				{
                    gen.Emit(OpCodes.Ldloc, locStoreInMember);
                    GenerateConstInt(0);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labExit);
                }

                var eType = GenerateDeferredSource(expr, locInstance);
                Debug.Assert(argType.IsAssignableFrom(eType));
                var locSource = gen.DeclareLocal(eType);
                gen.Emit(OpCodes.Stloc, locSource);

                if (locStoreInMember != null && hasSideEffect) 
				{
                    gen.Emit(OpCodes.Ldloc, locStoreInMember);
                    GenerateConstInt(0);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labExit);
                }

                // if member is directly writeable, check for null entityset
                if (m is FieldInfo || (m is PropertyInfo && ((PropertyInfo)m).CanWrite)) 
				{
                    var labFetch = gen.DefineLabel();
                    GenerateLoadForMemberAccess(locInstance);
                    GenerateLoadMember(m);
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brfalse, labFetch);

                    // create new entity set
                    GenerateLoadForMemberAccess(locInstance);
                    var ci = memberType.GetConstructor(Type.EmptyTypes);
                    Debug.Assert(ci != null);
                    gen.Emit(OpCodes.Newobj, ci);
                    GenerateStoreMember(m);

                    gen.MarkLabel(labFetch);
                }

                // set the source
                GenerateLoadForMemberAccess(locInstance);
                GenerateLoadMember(m);
                gen.Emit(OpCodes.Ldloc, locSource);
                var miSetSource = memberType.GetMethod(
					"SetSource", 
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
					null, 
					new[]
					{
						argType
					}, 
					null);
                Debug.Assert(miSetSource != null);
                gen.Emit(GetMethodCallOpCode(miSetSource), miSetSource);

                gen.MarkLabel(labExit);
            }

            private bool HasSideEffect(SqlNode node) 
			{
                return sideEffectChecker.HasSideEffect(node);
            }

            private void GenerateAssignEntitySet(
				MetaDataMember mm, 
				LocalBuilder locInstance, 
				SqlExpression expr, 
				LocalBuilder locStoreInMember) 
			{
                var m = mm.StorageMember ?? mm.Member;
                var memberType = TypeSystem.GetMemberType(m);
                Debug.Assert(memberType.IsGenericType && memberType.GetGenericTypeDefinition() == typeof(EntitySet<>));
                var labExit = gen.DefineLabel();
                var argType = typeof(IEnumerable<>).MakeGenericType(memberType.GetGenericArguments());

                var hasSideEffect = HasSideEffect(expr);

                if (locStoreInMember != null && !hasSideEffect) 
				{
                    gen.Emit(OpCodes.Ldloc, locStoreInMember);
                    GenerateConstInt(0);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labExit);
                }

                var eType = Generate(expr, mm.DeclaringType.IsEntity ? locInstance : null);
                Debug.Assert(argType.IsAssignableFrom(eType));
                var locSource = gen.DeclareLocal(eType);
                gen.Emit(OpCodes.Stloc, locSource);

                if (locStoreInMember != null && hasSideEffect) 
				{
                    gen.Emit(OpCodes.Ldloc, locStoreInMember);
                    GenerateConstInt(0);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labExit);
                }

                // if member is directly writeable, check for null entityset
                if (m is FieldInfo || (m is PropertyInfo && ((PropertyInfo)m).CanWrite)) 
				{
                    var labFetch = gen.DefineLabel();
                    GenerateLoadForMemberAccess(locInstance);
                    GenerateLoadMember(m);
                    gen.Emit(OpCodes.Ldnull);
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brfalse, labFetch);

                    // create new entity set
                    GenerateLoadForMemberAccess(locInstance);
                    var ci = memberType.GetConstructor(Type.EmptyTypes);
                    Debug.Assert(ci != null);
                    gen.Emit(OpCodes.Newobj, ci);
                    GenerateStoreMember(m);

                    gen.MarkLabel(labFetch);
                }

                // set the source
                GenerateLoadForMemberAccess(locInstance);
                GenerateLoadMember(m);
                gen.Emit(OpCodes.Ldloc, locSource);
                var miAssign = memberType.GetMethod(
					"Assign", 
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
					null, 
					new[]
					{
						argType
					}, 
					null);
                Debug.Assert(miAssign != null);
                gen.Emit(GetMethodCallOpCode(miAssign), miAssign);

                gen.MarkLabel(labExit);
            }

            private void GenerateAssignDeferredReference(
				MetaDataMember mm, 
				LocalBuilder locInstance, 
				SqlExpression expr, 
				LocalBuilder locStoreInMember) 
			{
                var m = mm.StorageMember ?? mm.Member;
                var memberType = TypeSystem.GetMemberType(m);
                Debug.Assert(
                    memberType.IsGenericType &&
                    (memberType.GetGenericTypeDefinition() == typeof(EntityRef<>) ||
						memberType.GetGenericTypeDefinition() == typeof(Link<>)));
				
                var labExit = gen.DefineLabel();

				Action entityRefConstruction;
				GeneratePrepareToAssignDeferredReference(
					memberType,
					locInstance,
					expr,
					locStoreInMember,
					labExit,
					out entityRefConstruction);

                GenerateLoadForMemberAccess(locInstance);
	            entityRefConstruction();
                GenerateStoreMember(m);

                gen.MarkLabel(labExit);
            }

			private void GenerateAssignDeferredReferenceInProxy(
				MetaDataMember mm,
				LocalBuilder locInstance,
				SqlExpression expr,
				LocalBuilder locStoreInMember)
			{
				var attributedMetaDataMember = (AttributedMetaDataMember)mm;
				if (!attributedMetaDataMember.DoesRequireProxy)
					throw new InvalidOperationException("!attributedMetaDataMember.DoesRequireProxy");

				var memberType = typeof(EntityRef<>).MakeGenericType(mm.Type);
				var labExit = gen.DefineLabel();

				Action entityRefConstruction;
				GeneratePrepareToAssignDeferredReference(
					memberType, 
					locInstance, 
					expr, 
					locStoreInMember, 
					labExit, 
					out entityRefConstruction);

				GenerateLoadForMemberAccess(locInstance);

				gen.Emit(OpCodes.Castclass, typeof(IEntityProxy));

				gen.Emit(OpCodes.Ldtoken, ((PropertyInfo)mm.Member).GetGetMethod(nonPublic: true));
				var handleConvertionMethod = ReflectionExpressions.GetMethodInfo(() =>
					MethodBase.GetMethodFromHandle(default(RuntimeMethodHandle)));
				gen.Emit(GetMethodCallOpCode(handleConvertionMethod), handleConvertionMethod);

				entityRefConstruction();

				var meth = ReflectionExpressions
					.GetMethodInfo<IEntityProxy>(proxy => proxy.SetEntityRef(default(MemberInfo), default(EntityRef<object>)))
					.GetGenericMethodDefinition()
					.MakeGenericMethod(mm.Type);
				gen.Emit(GetMethodCallOpCode(meth), meth);

				gen.MarkLabel(labExit);
			}

	        private void GeneratePrepareToAssignDeferredReference(
				Type memberType,
				LocalBuilder locInstance,
				SqlExpression expr,
				LocalBuilder locStoreInMember,
				Label labExit,
				out Action entityRefConstruction)
	        {
		        if (memberType == null)
			        throw new ArgumentNullException("memberType");
		        if (locInstance == null)
			        throw new ArgumentNullException("locInstance");
		        if (expr == null)
			        throw new ArgumentNullException("expr");

		        var hasSideEffect = HasSideEffect(expr);

				if (locStoreInMember != null && !hasSideEffect)
				{
					gen.Emit(OpCodes.Ldloc, locStoreInMember);
					GenerateConstInt(0);
					gen.Emit(OpCodes.Ceq);
					gen.Emit(OpCodes.Brtrue, labExit);
				}

				var argType = typeof(IEnumerable<>).MakeGenericType(memberType.GetGenericArguments());
				var eType = GenerateDeferredSource(expr, locInstance);
				if (!argType.IsAssignableFrom(eType))
					throw Error.CouldNotConvert(argType, eType);

				var locSource = gen.DeclareLocal(eType);
				gen.Emit(OpCodes.Stloc, locSource);

				if (locStoreInMember != null && hasSideEffect)
				{
					gen.Emit(OpCodes.Ldloc, locStoreInMember);
					GenerateConstInt(0);
					gen.Emit(OpCodes.Ceq);
					gen.Emit(OpCodes.Brtrue, labExit);
				}

		        entityRefConstruction = () =>
		        {
					gen.Emit(OpCodes.Ldloc, locSource);
					var ci = memberType.GetConstructor(
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
						null,
						new[]
						{
							argType
						},
						null);
					Debug.Assert(ci != null);
					gen.Emit(OpCodes.Newobj, ci);
				};
	        }

			private void GenerateLoadForMemberAccess(LocalBuilder loc) 
			{
	            if (loc.LocalType.IsValueType)
		            gen.Emit(OpCodes.Ldloca, loc);
	            else
		            gen.Emit(OpCodes.Ldloc, loc);
			}

            private bool IsDeferrableExpression(SqlExpression expr) 
			{
	            if (expr.NodeType == SqlNodeType.Link)
		            return true;
	            if (expr.NodeType == SqlNodeType.ClientCase)
	            {
		            var c = (SqlClientCase)expr;
		            foreach (var when in c.Whens)
			            if (!IsDeferrableExpression(when.Value))
				            return false;
		            return true;
	            }
	            return false;
            }

            private Type GenerateGrouping(SqlGrouping grp) 
			{
                var typeArgs = grp.ClrType.GetGenericArguments();

                GenerateExpressionForType(grp.Key, typeArgs[0]);
                Generate(grp.Group);

                var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);
                var miCreateGroup = TypeSystem.FindStaticMethod(
					orbType, 
					"CreateGroup", 
					new[]
					{
						typeArgs[0], 
						typeof(IEnumerable<>).MakeGenericType(typeArgs[1])
					}, 
					typeArgs);
                Debug.Assert(miCreateGroup != null);
                gen.Emit(OpCodes.Call, miCreateGroup);

                return miCreateGroup.ReturnType;
            }

            private Type GenerateLink(SqlLink link, LocalBuilder locInstance) 
			{
                gen.Emit(OpCodes.Ldarg_0);

                // iGlobalLink arg
                var iGlobalLink = AddGlobal(typeof(MetaDataMember), link.Member);
                GenerateConstInt(iGlobalLink);

                // iLocalFactory arg
                var iLocalFactory = AllocateLocal();
                GenerateConstInt(iLocalFactory);

                var elemType = link.Member.IsAssociation && link.Member.Association.IsMany
                    ? TypeSystem.GetElementType(link.Member.Type)
                    : link.Member.Type;

                MethodInfo mi;
	            if (locInstance == null)
	            {
		            // create array of key values for 'keyValues' arg
		            GenerateConstInt(link.KeyExpressions.Count);
		            gen.Emit(OpCodes.Newarr, typeof(object));

		            // intialize key values
		            for (var i = 0; i < link.KeyExpressions.Count; i++)
		            {
			            gen.Emit(OpCodes.Dup);
			            GenerateConstInt(i);
			            GenerateExpressionForType(link.KeyExpressions[i], typeof(object));
			            GenerateArrayAssign(typeof(object));
		            }

		            // call GetLinkSource on ObjectReaderBase
		            mi = typeof(ObjectMaterializer<>).MakeGenericType(this.compiler.dataReaderType)
			            .GetMethod("GetLinkSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		            Debug.Assert(mi != null);
		            var miGLS = mi.MakeGenericMethod(elemType);
		            gen.Emit(GetMethodCallOpCode(miGLS), miGLS);
	            }
	            else
	            {
		            // load instance for 'instance' arg
		            gen.Emit(OpCodes.Ldloc, locInstance);

		            // call GetNestedLinkSource on ObjectReaderBase
		            mi = typeof(ObjectMaterializer<>)
			            .MakeGenericType(compiler.dataReaderType)
			            .GetMethod("GetNestedLinkSource", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		            Debug.Assert(mi != null);
		            var miGLS = mi.MakeGenericMethod(elemType);
		            gen.Emit(GetMethodCallOpCode(miGLS), miGLS);
	            }

	            return typeof(IEnumerable<>).MakeGenericType(elemType);
            }

            private Type GenerateDeferredSource(SqlExpression expr, LocalBuilder locInstance)
            {
	            if (expr.NodeType == SqlNodeType.ClientCase)
		            return GenerateClientCase((SqlClientCase)expr, true, locInstance);
	            if (expr.NodeType == SqlNodeType.Link)
		            return GenerateLink((SqlLink)expr, locInstance);
	            throw Error.ExpressionNotDeferredQuerySource();
            }

	        private Type GenerateClientQuery(SqlClientQuery cq) 
			{
                var clientElementType = cq.Query.NodeType == SqlNodeType.Multiset ? 
					TypeSystem.GetElementType(cq.ClrType) : 
					cq.ClrType;

                gen.Emit(OpCodes.Ldarg_0); // ObjectReaderBase
                GenerateConstInt(cq.Ordinal); // iSubQuery
                
                // create array of subquery parent args
                GenerateConstInt(cq.Arguments.Count);
                gen.Emit(OpCodes.Newarr, typeof(object));

                // intialize arg values
                for (var i = 0; i < cq.Arguments.Count; i++) 
				{
                    gen.Emit(OpCodes.Dup);
                    GenerateConstInt(i);
                    var clrType = cq.Arguments[i].ClrType;
                    if (cq.Arguments[i].NodeType == SqlNodeType.ColumnRef) 
					{
                        var cref = (SqlColumnRef)cq.Arguments[i];
						if (clrType.IsValueType && !TypeSystem.IsNullableType(clrType))
							clrType = typeof(Nullable<>).MakeGenericType(clrType);
						GenerateColumnAccess(clrType, cref.SqlType, cref.Column.Ordinal, null);
                    }
                    else 
					{
                        GenerateExpressionForType(cq.Arguments[i], cq.Arguments[i].ClrType);
                    }
					if (clrType.IsValueType)
						gen.Emit(OpCodes.Box, clrType);
					GenerateArrayAssign(typeof(object));
                }

                var miExecute = typeof(ObjectMaterializer<>)
					.MakeGenericType(compiler.dataReaderType)
                    .GetMethod("ExecuteSubQuery", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                Debug.Assert(miExecute != null);
                gen.Emit(GetMethodCallOpCode(miExecute), miExecute);

                var actualType = typeof(IEnumerable<>).MakeGenericType(clientElementType);
                gen.Emit(OpCodes.Castclass, actualType);

                var resultType = typeof(List<>).MakeGenericType(clientElementType);
                GenerateConvertToType(actualType, resultType);

                return resultType;
            }

            private Type GenerateJoinedCollection(SqlJoinedCollection jc) 
			{
                var locCount = gen.DeclareLocal(typeof(int));
                var locHasRows = gen.DeclareLocal(typeof(bool));
                var joinElementType = jc.Expression.ClrType;
                var listType = typeof(List<>).MakeGenericType(joinElementType);
                var locList = gen.DeclareLocal(listType);

                // count = xxx
                GenerateExpressionForType(jc.Count, typeof(int));
                gen.Emit(OpCodes.Stloc, locCount);

                // list = new List<T>(count)
                gen.Emit(OpCodes.Ldloc, locCount);
                var ci = listType.GetConstructor(new[]
                {
	                typeof(int)
                });
                Debug.Assert(ci != null);
                gen.Emit(OpCodes.Newobj, ci);
                gen.Emit(OpCodes.Stloc, locList);

                // hasRows = true
                gen.Emit(OpCodes.Ldc_I4_1);
                gen.Emit(OpCodes.Stloc, locHasRows);

                // start loop
                var labLoopTest = gen.DefineLabel();
                var labLoopTop = gen.DefineLabel();
                var locI = gen.DeclareLocal(typeof(int));
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Stloc, locI);
                gen.Emit(OpCodes.Br, labLoopTest);

                gen.MarkLabel(labLoopTop);
                // loop interior

                // if (i > 0 && hasRows) { hasRows = this.Read(); }
                gen.Emit(OpCodes.Ldloc, locI);
                gen.Emit(OpCodes.Ldc_I4_0);
                gen.Emit(OpCodes.Cgt);
                gen.Emit(OpCodes.Ldloc, locHasRows);
                gen.Emit(OpCodes.And);
                var labNext = gen.DefineLabel();
                gen.Emit(OpCodes.Brfalse, labNext);

                // this.Read()
                gen.Emit(OpCodes.Ldarg_0);
                var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);
                var miRead = orbType.GetMethod(
					"Read", 
					BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
					null, 
					Type.EmptyTypes, 
					null);
                Debug.Assert(miRead != null);
                gen.Emit(GetMethodCallOpCode(miRead), miRead);
                gen.Emit(OpCodes.Stloc, locHasRows);

                gen.MarkLabel(labNext);
                // if (hasRows) { list.Add(expr); }
                var labNext2 = gen.DefineLabel();
                gen.Emit(OpCodes.Ldloc, locHasRows);
                gen.Emit(OpCodes.Brfalse, labNext2);
                gen.Emit(OpCodes.Ldloc, locList);
                GenerateExpressionForType(jc.Expression, joinElementType);
                var miAdd = listType.GetMethod(
					"Add", 
					BindingFlags.Instance | BindingFlags.Public, 
					null, 
					new[]
					{
						joinElementType
					}, 
					null);
                Debug.Assert(miAdd != null);
                gen.Emit(GetMethodCallOpCode(miAdd), miAdd);

                gen.MarkLabel(labNext2);
                // loop bottom
                // i = i + 1
                gen.Emit(OpCodes.Ldloc, locI);
                gen.Emit(OpCodes.Ldc_I4_1);
                gen.Emit(OpCodes.Add);
                gen.Emit(OpCodes.Stloc, locI);

                // loop test
                // i < count && hasRows
                gen.MarkLabel(labLoopTest);
                gen.Emit(OpCodes.Ldloc, locI);
                gen.Emit(OpCodes.Ldloc, locCount);
                gen.Emit(OpCodes.Clt);
                gen.Emit(OpCodes.Ldloc, locHasRows);
                gen.Emit(OpCodes.And);
                gen.Emit(OpCodes.Brtrue, labLoopTop);

                // return list;
                gen.Emit(OpCodes.Ldloc, locList);

                return listType;
            }

            private Type GenerateExpressionForType(SqlExpression expr, Type type) 
			{
                return GenerateExpressionForType(expr, type, null);
            }

            private Type GenerateExpressionForType(SqlExpression expr, Type type, LocalBuilder locInstance) 
			{
                var actualType = Generate(expr, locInstance);
                GenerateConvertToType(actualType, type);
                return type;
            }

            private void GenerateConvertToType(Type actualType, Type expectedType, Type readerMethodType) 
			{
                GenerateConvertToType(readerMethodType, actualType);
                GenerateConvertToType(actualType, expectedType);
            }

            private void GenerateConvertToType(Type actualType, Type expectedType) 
			{
	            if ((expectedType == actualType) || (!actualType.IsValueType && actualType.IsSubclassOf(expectedType)))
		            return;

	            var genExpectedType = expectedType.IsGenericType ? expectedType.GetGenericTypeDefinition() : null;
	            var genExpectedTypeArgs = genExpectedType != null ? expectedType.GetGenericArguments() : null;

	            var elemType = TypeSystem.GetElementType(actualType);
	            var seqType = TypeSystem.GetSequenceType(elemType);
	            var actualIsSequence = seqType.IsAssignableFrom(actualType);

	            if (expectedType == typeof(object) && actualType.IsValueType) 
				{
		            gen.Emit(OpCodes.Box, actualType);
	            }
	            else if (actualType == typeof(object) && expectedType.IsValueType) 
				{
		            gen.Emit(OpCodes.Unbox_Any, expectedType);
	            }
	            else if ((actualType.IsSubclassOf(expectedType) || expectedType.IsSubclassOf(actualType)) && 
					!actualType.IsValueType && 
					!expectedType.IsValueType) 
				{
						// is one type an explicit subtype of the other?
			            // (T)expr
			            gen.Emit(OpCodes.Castclass, expectedType);
		        }
	            else if (genExpectedType == typeof(IEnumerable<>) && actualIsSequence) 
				{
					// do we expected a sequence of a different element type?
					if (elementType.IsInterface ||
			            genExpectedTypeArgs[0].IsInterface ||
			            elementType.IsSubclassOf(genExpectedTypeArgs[0]) ||
			            genExpectedTypeArgs[0].IsSubclassOf(elementType) ||
			            TypeSystem.GetNonNullableType(elementType) == TypeSystem.GetNonNullableType(genExpectedTypeArgs[0])) 
					{
				            // reference or nullable conversion use seq.Cast<E>()
				            var miCast = TypeSystem.FindSequenceMethod(
								"Cast", 
								new[]
								{
									seqType
								}, 
								genExpectedTypeArgs[0]);
				            Debug.Assert(miCast != null);
				            gen.Emit(OpCodes.Call, miCast);
			        }
		            else 
					{
			            // otherwise use orb.Convert<E>(sequence)
			            var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);
			            var miConvert = TypeSystem.FindStaticMethod(
							orbType, 
							"Convert", 
							new[]
							{
								seqType
							}, 
							genExpectedTypeArgs[0]);
			            Debug.Assert(miConvert != null);
			            gen.Emit(OpCodes.Call, miConvert);
		            }
	            }
	            else if (expectedType == elemType && actualIsSequence) 
				{
					// Do we have a sequence where we wanted a singleton?
					// seq.SingleOrDefault()
		            var miFirst = TypeSystem.FindSequenceMethod(
						"SingleOrDefault", 
						new[]
						{
							seqType
						}, 
						expectedType);
		            Debug.Assert(miFirst != null);
		            gen.Emit(OpCodes.Call, miFirst);
	            }
	            else if (TypeSystem.IsNullableType(expectedType) &&
		            TypeSystem.GetNonNullableType(expectedType) == actualType) 
				{
					// do we have a non-nullable value where we want a nullable value?
					// new Nullable<T>(expr)
			        var ci = expectedType.GetConstructor(new[]
			        {
				        actualType
			        });
			        gen.Emit(OpCodes.Newobj, ci);
		        }
	            else if (TypeSystem.IsNullableType(actualType) &&
		            TypeSystem.GetNonNullableType(actualType) == expectedType) 
				{
					// do we have a nullable value where we want a non-nullable value?
					// expr.GetValueOrDefault()
			        var loc = gen.DeclareLocal(actualType);
			        gen.Emit(OpCodes.Stloc, loc);
			        gen.Emit(OpCodes.Ldloca, loc);
			        GenerateGetValueOrDefault(actualType);
		        }
	            else if (genExpectedType == typeof(EntityRef<>) || genExpectedType == typeof(Link<>)) 
				{
					// do we have a value when we want an EntityRef or Link of that value
					if (actualType.IsAssignableFrom(genExpectedTypeArgs[0]))
					{
			            // new T(expr)
						// Ensure that the actual runtime type of the value is
						// compatible.  For example, in inheritance scenarios
						// the Type of the value can vary from row to row.
						if (actualType != genExpectedTypeArgs[0])
							GenerateConvertToType(actualType, genExpectedTypeArgs[0]);
						var ci = expectedType.GetConstructor(new[]
						{
							genExpectedTypeArgs[0]
						});
			            Debug.Assert(ci != null);
			            gen.Emit(OpCodes.Newobj, ci);
		            }
		            else if (seqType.IsAssignableFrom(actualType)) 
					{
			            // new T(seq.SingleOrDefault())
			            var miFirst = TypeSystem.FindSequenceMethod(
							"SingleOrDefault", 
							new[]
							{
								seqType
							}, 
							elemType);
			            Debug.Assert(miFirst != null);
			            gen.Emit(OpCodes.Call, miFirst);
			            var ci = expectedType.GetConstructor(new[]
			            {
				            elemType
			            });
			            Debug.Assert(ci != null);
			            gen.Emit(OpCodes.Newobj, ci);
		            }
		            else 
					{
			            throw Error.CannotConvertToEntityRef(actualType);
		            }
	            }
	            else if ((expectedType == typeof(IQueryable) ||
		            expectedType == typeof(IOrderedQueryable))
		            && typeof(IEnumerable).IsAssignableFrom(actualType)) 
				{
					// do we have a sequence when we want IQueryable/IOrderedQueryable?
					// seq.AsQueryable()
			        var miAsQueryable = TypeSystem.FindQueryableMethod(
						"AsQueryable", 
						new[]
						{
							typeof(IEnumerable)
						});
			        Debug.Assert(miAsQueryable != null);
			        gen.Emit(OpCodes.Call, miAsQueryable);
					if (genExpectedType == typeof(IOrderedQueryable))
						gen.Emit(OpCodes.Castclass, expectedType);
				}
	            else if ((genExpectedType == typeof(IQueryable<>) || genExpectedType == typeof(IOrderedQueryable<>)) &&
		            actualIsSequence) 
				{
					// do we have a sequence when we want IQuerayble<T>/IOrderedQueryable<T>?
					if (elemType != genExpectedTypeArgs[0])
					{
				        seqType = typeof(IEnumerable<>).MakeGenericType(genExpectedTypeArgs);
				        GenerateConvertToType(actualType, seqType);
				        elemType = genExpectedTypeArgs[0];
			        }
			        // seq.AsQueryable()
			        var miAsQueryable = TypeSystem.FindQueryableMethod(
						"AsQueryable", 
						new[]
						{
							seqType
						}, 
						elemType);
			        Debug.Assert(miAsQueryable != null);
			        gen.Emit(OpCodes.Call, miAsQueryable);
					if (genExpectedType == typeof(IOrderedQueryable<>))
						gen.Emit(OpCodes.Castclass, expectedType);
				}
	            else if (genExpectedType == typeof(IOrderedEnumerable<>) && actualIsSequence) 
				{
					// do we have a sequence when we want IOrderedEnumerable?
					if (elemType != genExpectedTypeArgs[0])
					{
			            seqType = typeof(IEnumerable<>).MakeGenericType(genExpectedTypeArgs);
			            GenerateConvertToType(actualType, seqType);
			            elemType = genExpectedTypeArgs[0];
		            }
		            // new OrderedResults<E>(seq)
		            var orbType = typeof(ObjectMaterializer<>).MakeGenericType(compiler.dataReaderType);
		            var miCreateOrderedEnumerable = TypeSystem.FindStaticMethod(
						orbType, 
						"CreateOrderedEnumerable", 
						new[]
						{
							seqType
						}, 
						elemType);
		            Debug.Assert(miCreateOrderedEnumerable != null);
		            gen.Emit(OpCodes.Call, miCreateOrderedEnumerable);
	            }
	            else if (genExpectedType == typeof(EntitySet<>) && actualIsSequence) 
				{
					// do we have a sequence when we want EntitySet<T> ?
					if (elemType != genExpectedTypeArgs[0])
					{
			            seqType = typeof(IEnumerable<>).MakeGenericType(genExpectedTypeArgs);
			            GenerateConvertToType(actualType, seqType);
			            actualType = seqType;
		            }
		            // loc = new EntitySet<E>(); loc.Assign(seq); loc
		            var locSeq = gen.DeclareLocal(actualType);
		            gen.Emit(OpCodes.Stloc, locSeq);

		            var ci = expectedType.GetConstructor(Type.EmptyTypes);
		            Debug.Assert(ci != null);
		            gen.Emit(OpCodes.Newobj, ci);
		            var locEs = gen.DeclareLocal(expectedType);
		            gen.Emit(OpCodes.Stloc, locEs);

		            gen.Emit(OpCodes.Ldloc, locEs);
		            gen.Emit(OpCodes.Ldloc, locSeq);
		            var miAssign = expectedType.GetMethod(
						"Assign", 
						BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, 
						null, 
						new[]
						{
							seqType
						}, 
						null);
		            Debug.Assert(miAssign != null);
		            gen.Emit(GetMethodCallOpCode(miAssign), miAssign);

		            gen.Emit(OpCodes.Ldloc, locEs);
	            }
	            else if (typeof(IEnumerable).IsAssignableFrom(expectedType) &&
		            actualIsSequence &&
		            expectedType.IsAssignableFrom(typeof(List<>).MakeGenericType(elemType))) 
				{
					// do we have a sequence when we want something assignable from List<T>?
					// new List<E>(seq)
			        var listType = typeof(List<>).MakeGenericType(elemType);
			        var ci = listType.GetConstructor(new[]
			        {
				        seqType
			        });
			        Debug.Assert(ci != null);
			        gen.Emit(OpCodes.Newobj, ci);
		        }
	            else if (expectedType.IsArray && 
					expectedType.GetArrayRank() == 1 &&
		            !actualType.IsArray && 
					seqType.IsAssignableFrom(actualType) &&
		            expectedType.GetElementType().IsAssignableFrom(elemType)) 
				{
					// do we have a sequence when we want T[]?
					// seq.ToArray()
			        var miToArray = TypeSystem.FindSequenceMethod(
						"ToArray", 
						new[]
						{
							seqType
						}, 
						elemType);
			        Debug.Assert(miToArray != null);
			        gen.Emit(OpCodes.Call, miToArray);
		        }
	            else if (expectedType.IsClass &&
		            typeof(ICollection<>).MakeGenericType(elemType).IsAssignableFrom(expectedType) &&
		            expectedType.GetConstructor(Type.EmptyTypes) != null &&
		            seqType.IsAssignableFrom(actualType)) 
				{
					// do we have a sequence when we want some other collection type?
					throw Error.GeneralCollectionMaterializationNotSupported();
		        }
	            else if (expectedType == typeof(bool) && actualType == typeof(int)) 
				{
					// do we have an int when we want a bool?
					// expr != 0
		            var labZero = gen.DefineLabel();
		            var labExit = gen.DefineLabel();
		            gen.Emit(OpCodes.Ldc_I4_0);
		            gen.Emit(OpCodes.Ceq);
		            gen.Emit(OpCodes.Brtrue_S, labZero);
		            gen.Emit(OpCodes.Ldc_I4_1);
		            gen.Emit(OpCodes.Br_S, labExit);
		            gen.MarkLabel(labZero);
		            gen.Emit(OpCodes.Ldc_I4_0);
		            gen.MarkLabel(labExit);
	            }
	            else 
				{
		            // last-ditch attempt: convert at runtime using DBConvert
		            // DBConvert.ChangeType(type, expr)
					if (actualType.IsValueType)
						gen.Emit(OpCodes.Box, actualType);
					gen.Emit(OpCodes.Ldtoken, expectedType);
		            var miGetTypeFromHandle = typeof(Type).GetMethod(
						"GetTypeFromHandle", 
						BindingFlags.Static | BindingFlags.Public);
		            Debug.Assert(miGetTypeFromHandle != null);
		            gen.Emit(OpCodes.Call, miGetTypeFromHandle);
		            var miChangeType = typeof(DBConvert).GetMethod(
						"ChangeType", 
						BindingFlags.Static | BindingFlags.Public, 
						null, 
						new[]
						{
							typeof(object), 
							typeof(Type)
						}, 
						null);
		            Debug.Assert(miChangeType != null);
		            gen.Emit(OpCodes.Call, miChangeType);
					if (expectedType.IsValueType)
						gen.Emit(OpCodes.Unbox_Any, expectedType);
					else if (expectedType != typeof(object))
						gen.Emit(OpCodes.Castclass, expectedType);
				}
			}

            private Type GenerateColumnReference(SqlColumnRef cref) 
			{
                GenerateColumnAccess(cref.ClrType, cref.SqlType, cref.Column.Ordinal, null);
                return cref.ClrType;
            }

            private Type GenerateUserColumn(SqlUserColumn suc) 
			{
                // if the user column is not named, it must be the only one!
                if (string.IsNullOrEmpty(suc.Name)) {
                    GenerateColumnAccess(suc.ClrType, suc.SqlType, 0, null);
                    return suc.ClrType;
                }

                var iName = namedColumns.Count;
                namedColumns.Add(new NamedColumn(suc.Name, suc.IsRequired));

                var labNotDefined = gen.DefineLabel();
                var labExit = gen.DefineLabel();
                var locOrdinal = gen.DeclareLocal(typeof(int));

                // ordinal = session.ordinals[i]
                GenerateAccessOrdinals();
                GenerateConstInt(iName);
                GenerateArrayAccess(typeof(int), false);
                gen.Emit(OpCodes.Stloc, locOrdinal);

                // if (ordinal < 0) goto labNotDefined
                gen.Emit(OpCodes.Ldloc, locOrdinal);
                GenerateConstInt(0);
                gen.Emit(OpCodes.Clt);
                gen.Emit(OpCodes.Brtrue, labNotDefined);

                // access column at ordinal position
                GenerateColumnAccess(suc.ClrType, suc.SqlType, 0, locOrdinal);
                gen.Emit(OpCodes.Br_S, labExit);

                // not defined?
                gen.MarkLabel(labNotDefined);
                GenerateDefault(suc.ClrType, false);

                gen.MarkLabel(labExit);

                return suc.ClrType;
            }

            private void GenerateColumnAccess(Type cType, ProviderType pType, int ordinal, LocalBuilder locOrdinal)
            {
                var rType = pType.GetClosestRuntimeType();
                var readerMethod = GetReaderMethod(compiler.dataReaderType, rType);
                var bufferMethod = GetReaderMethod(typeof(DbDataReader), rType);

                var labIsNull = gen.DefineLabel();
                var labExit = gen.DefineLabel();
                var labReadFromBuffer = gen.DefineLabel();

                // if (buffer != null) goto ReadFromBuffer
                GenerateAccessBufferReader();
                gen.Emit(OpCodes.Ldnull);
                gen.Emit(OpCodes.Ceq);
                gen.Emit(OpCodes.Brfalse, labReadFromBuffer);

                // read from DataReader
                // this.reader.IsNull?
                GenerateAccessDataReader();
                if (locOrdinal != null)
                    gen.Emit(OpCodes.Ldloc, locOrdinal);
                else
                    GenerateConstInt(ordinal);
                gen.Emit(GetMethodCallOpCode(this.compiler.miDRisDBNull), this.compiler.miDRisDBNull);
                gen.Emit(OpCodes.Brtrue, labIsNull);

                // Special case handling. Allow to read Int32 value if rType is Int64
                if (rType == typeof(long))
                {
                    var labUseGetInt32 = gen.DefineLabel();
                    var fieldTypeMethod = GetFieldTypeMethod(compiler.dataReaderType);
                    var readerInt32Method = GetReaderMethod(compiler.dataReaderType, typeof(int));

                    // this.reader.GetFieldType(i) 
                    GenerateAccessDataReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(fieldTypeMethod), fieldTypeMethod);

                    // if (fieldType == typeof(int)) goto labUseGetInt32
                    gen.Emit(OpCodes.Ldtoken, typeof(int));
                    gen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labUseGetInt32);

                    // this.reader.GetInt64
                    GenerateAccessDataReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(readerMethod), readerMethod);
                    GenerateConvertToType(rType, cType, readerMethod.ReturnType);
                    gen.Emit(OpCodes.Br_S, labExit);

                    // this.reader.GetInt32
                    gen.MarkLabel(labUseGetInt32);
                    GenerateAccessDataReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(readerInt32Method), readerInt32Method);
                    gen.Emit(OpCodes.Conv_I8); // (long)%value%
                    gen.Emit(OpCodes.Br_S, labExit);
                }
                else
                {
                    // this.reader.GetXXX()
                    GenerateAccessDataReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(readerMethod), readerMethod);
                    GenerateConvertToType(rType, cType, readerMethod.ReturnType);
                    gen.Emit(OpCodes.Br_S, labExit);
                }

                // read from BUFFER
                gen.MarkLabel(labReadFromBuffer);

                // this.bufferReader.IsNull?
                GenerateAccessBufferReader();
                if (locOrdinal != null)
                    gen.Emit(OpCodes.Ldloc, locOrdinal);
                else
                    GenerateConstInt(ordinal);
                gen.Emit(GetMethodCallOpCode(compiler.miBRisDBNull), compiler.miBRisDBNull);
                gen.Emit(OpCodes.Brtrue, labIsNull);

                // Special case handling. Allow to read Int32 value if rType is Int64
                if (rType == typeof(long))
                {
                    var labBufferUseGetInt32 = gen.DefineLabel();
                    var bufferFieldTypeMethod = GetFieldTypeMethod(typeof(DbDataReader));
                    var bufferReaderInt32Method = GetReaderMethod(typeof(DbDataReader), typeof(int));

                    // this.reader.GetFieldType(i) 
                    GenerateAccessBufferReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(bufferFieldTypeMethod), bufferFieldTypeMethod);

                    // if (fieldType == typeof(int)) goto labUseGetInt32
                    gen.Emit(OpCodes.Ldtoken, typeof(int));
                    gen.Emit(OpCodes.Call, typeof(Type).GetMethod("GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));
                    gen.Emit(OpCodes.Ceq);
                    gen.Emit(OpCodes.Brtrue, labBufferUseGetInt32);

                    // this.reader.GetInt64
                    GenerateAccessBufferReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(bufferMethod), bufferMethod);
                    GenerateConvertToType(rType, cType, bufferMethod.ReturnType);
                    gen.Emit(OpCodes.Br_S, labExit);

                    // this.reader.GetInt32
                    gen.MarkLabel(labBufferUseGetInt32);
                    GenerateAccessBufferReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(bufferReaderInt32Method), bufferReaderInt32Method);
                    gen.Emit(OpCodes.Conv_I8); // (long)%value%
                    gen.Emit(OpCodes.Br_S, labExit);
                }
                else
                {
                    // this.bufferReader.GetXXX()
                    GenerateAccessBufferReader();
                    if (locOrdinal != null)
                        gen.Emit(OpCodes.Ldloc, locOrdinal);
                    else
                        GenerateConstInt(ordinal);
                    gen.Emit(GetMethodCallOpCode(bufferMethod), bufferMethod);
                    GenerateConvertToType(rType, cType, bufferMethod.ReturnType);
                    gen.Emit(OpCodes.Br_S, labExit);
                }

                // return NULL
                gen.MarkLabel(labIsNull);
                GenerateDefault(cType);

                gen.MarkLabel(labExit);
            }

            private Type GenerateClientCase(SqlClientCase scc, bool isDeferred, LocalBuilder locInstance) 
			{
                var locDiscriminator = gen.DeclareLocal(scc.Expression.ClrType);
                GenerateExpressionForType(scc.Expression, scc.Expression.ClrType);
                gen.Emit(OpCodes.Stloc, locDiscriminator);

                var labNext = gen.DefineLabel();
                var labEnd = gen.DefineLabel();
                for (var i = 0; i < scc.Whens.Count; i++) 
				{
                    if (i > 0) 
					{
                        gen.MarkLabel(labNext);
                        labNext = gen.DefineLabel();
                    }
                    var when = scc.Whens[i];
                    if (when.Match != null) 
					{
                        gen.Emit(OpCodes.Ldloc, locDiscriminator);
                        GenerateExpressionForType(when.Match, scc.Expression.ClrType);
                        GenerateEquals(locDiscriminator.LocalType);
                        gen.Emit(OpCodes.Brfalse, labNext);
                    }
					if (isDeferred)
						GenerateDeferredSource(when.Value, locInstance);
					else
						GenerateExpressionForType(when.Value, scc.ClrType);
					gen.Emit(OpCodes.Br, labEnd);
                }
                gen.MarkLabel(labEnd);

                return scc.ClrType;
            }

            private Type GenerateTypeCase(SqlTypeCase stc) {
                LocalBuilder locDiscriminator = gen.DeclareLocal(stc.Discriminator.ClrType);
                this.GenerateExpressionForType(stc.Discriminator, stc.Discriminator.ClrType);
                gen.Emit(OpCodes.Stloc, locDiscriminator);

                Label labNext = gen.DefineLabel();
                Label labEnd = gen.DefineLabel();
                bool hasDefault = false;

                for (int i = 0, n = stc.Whens.Count; i < n; i++) {
                    if (i > 0) {
                        gen.MarkLabel(labNext);
                        labNext = gen.DefineLabel();
                    }
                    SqlTypeCaseWhen when = stc.Whens[i];
                    if (when.Match != null) {
                        gen.Emit(OpCodes.Ldloc, locDiscriminator);
                        SqlValue vMatch = when.Match as SqlValue;
                        System.Diagnostics.Debug.Assert(vMatch != null);
                        this.GenerateConstant(locDiscriminator.LocalType, vMatch.Value);
                        this.GenerateEquals(locDiscriminator.LocalType);
                        gen.Emit(OpCodes.Brfalse, labNext);
                    }
                    else {
                        System.Diagnostics.Debug.Assert(i == n - 1);
                        hasDefault = true;
                    }
                    this.GenerateExpressionForType(when.TypeBinding, stc.ClrType);
                    gen.Emit(OpCodes.Br, labEnd);
                }
                gen.MarkLabel(labNext);
                if (!hasDefault) {
                    this.GenerateConstant(stc.ClrType, null);
                }
                gen.MarkLabel(labEnd);

                return stc.ClrType;
            }

            private Type GenerateDiscriminatedType(SqlDiscriminatedType dt) {
                System.Diagnostics.Debug.Assert(dt.ClrType == typeof(Type));

                LocalBuilder locDiscriminator = gen.DeclareLocal(dt.Discriminator.ClrType);
                this.GenerateExpressionForType(dt.Discriminator, dt.Discriminator.ClrType);
                gen.Emit(OpCodes.Stloc, locDiscriminator);

                return this.GenerateDiscriminatedType(dt.TargetType, locDiscriminator, dt.Discriminator.SqlType);
            }

            private Type GenerateDiscriminatedType(MetaType targetType, LocalBuilder locDiscriminator, ProviderType discriminatorType) {
                System.Diagnostics.Debug.Assert(targetType != null && locDiscriminator != null);

                MetaType defType = null;
                Label labNext = gen.DefineLabel();
                Label labEnd = gen.DefineLabel();
                foreach (MetaType imt in targetType.InheritanceTypes) {
                    if (imt.InheritanceCode != null) {
                        if (imt.IsInheritanceDefault) {
                            defType = imt;
                        }
                        // disc == code?
                        gen.Emit(OpCodes.Ldloc, locDiscriminator);
                        object code = InheritanceRules.InheritanceCodeForClientCompare(imt.InheritanceCode, discriminatorType);
                        this.GenerateConstant(locDiscriminator.LocalType, code);
                        this.GenerateEquals(locDiscriminator.LocalType);
                        gen.Emit(OpCodes.Brfalse, labNext);

                        this.GenerateConstant(typeof(Type), imt.Type);
                        gen.Emit(OpCodes.Br, labEnd);

                        gen.MarkLabel(labNext);
                        labNext = gen.DefineLabel();
                    }
                }
                gen.MarkLabel(labNext);
                if (defType != null) {
                    this.GenerateConstant(typeof(Type), defType.Type);
                }
                else {
                    this.GenerateDefault(typeof(Type));
                }

                gen.MarkLabel(labEnd);

                return typeof(Type);
            }

            private Type GenerateSearchedCase(SqlSearchedCase ssc) {
                Label labNext = gen.DefineLabel();
                Label labEnd = gen.DefineLabel();
                for (int i = 0, n = ssc.Whens.Count; i < n; i++) {
                    if (i > 0) {
                        gen.MarkLabel(labNext);
                        labNext = gen.DefineLabel();
                    }
                    SqlWhen when = ssc.Whens[i];
                    if (when.Match != null) {
                        this.GenerateExpressionForType(when.Match, typeof(bool)); // test
                        this.GenerateConstInt(0);
                        gen.Emit(OpCodes.Ceq);
                        gen.Emit(OpCodes.Brtrue, labNext);
                    }
                    this.GenerateExpressionForType(when.Value, ssc.ClrType);
                    gen.Emit(OpCodes.Br, labEnd);
                }
                gen.MarkLabel(labNext);
                if (ssc.Else != null) {
                    this.GenerateExpressionForType(ssc.Else, ssc.ClrType);
                }
                gen.MarkLabel(labEnd);
                return ssc.ClrType;
            }

            private void GenerateEquals(Type type) {
                switch (Type.GetTypeCode(type)) {
                    case TypeCode.Object:
                    case TypeCode.String:
                    case TypeCode.DBNull:
                        if (type.IsValueType) {
                            LocalBuilder locLeft = gen.DeclareLocal(type);
                            LocalBuilder locRight = gen.DeclareLocal(type);
                            gen.Emit(OpCodes.Stloc, locRight);
                            gen.Emit(OpCodes.Stloc, locLeft);
                            gen.Emit(OpCodes.Ldloc, locLeft);
                            gen.Emit(OpCodes.Box, type);
                            gen.Emit(OpCodes.Ldloc, locRight);
                            gen.Emit(OpCodes.Box, type);
                        }
                        MethodInfo miEquals = typeof(object).GetMethod("Equals", BindingFlags.Static | BindingFlags.Public);
                        System.Diagnostics.Debug.Assert(miEquals != null);
                        gen.Emit(GetMethodCallOpCode(miEquals), miEquals);
                        break;
                    default:
                        gen.Emit(OpCodes.Ceq);
                        break;
                }
            }

            private void GenerateDefault(Type type) {
                this.GenerateDefault(type, true);
            }

            private void GenerateDefault(Type type, bool throwIfNotNullable) {
                if (type.IsValueType) {
                    if (!throwIfNotNullable || TypeSystem.IsNullableType(type)) {
                        LocalBuilder loc = gen.DeclareLocal(type);
                        gen.Emit(OpCodes.Ldloca, loc);
                        gen.Emit(OpCodes.Initobj, type);
                        gen.Emit(OpCodes.Ldloc, loc);
                    }
                    else {
                        gen.Emit(OpCodes.Ldtoken, type);
                        gen.Emit(OpCodes.Call, typeof(Type).GetMethod(
                            "GetTypeFromHandle", BindingFlags.Static | BindingFlags.Public));

                        MethodInfo mi = typeof(ObjectMaterializer<>)
                            .MakeGenericType(this.compiler.dataReaderType)
                            .GetMethod("ErrorAssignmentToNull", BindingFlags.Static | BindingFlags.Public);
                        System.Diagnostics.Debug.Assert(mi != null);
                        gen.Emit(OpCodes.Call, mi);
                        gen.Emit(OpCodes.Throw);
                    }
                }
                else {
                    gen.Emit(OpCodes.Ldnull);
                }
            }

            private static Type[] readMethodSignature = new Type[] { typeof(int) };

            private MethodInfo GetFieldTypeMethod(Type readerType)
            {
                return readerType.GetMethod(
                   "GetFieldType",
                   BindingFlags.Instance | BindingFlags.Public,
                   null,
                   readMethodSignature,
                   null);
            }

            [SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic", Justification = "Unknown reason.")]
            private MethodInfo GetReaderMethod(Type readerType, Type valueType) {
                if (valueType.IsEnum)
                    valueType = valueType.BaseType;

                TypeCode tc = Type.GetTypeCode(valueType);
                string name;
                if (tc == TypeCode.Single) {
                    name = "GetFloat";
                }
                else {
                    name = "Get" + valueType.Name;
                }

                MethodInfo readerMethod = readerType.GetMethod(
                   name,
                   BindingFlags.Instance | BindingFlags.Public,
                   null,
                   readMethodSignature,
                   null
                   );

                if (readerMethod == null) {
                    readerMethod = readerType.GetMethod(
                        "GetValue",
                        BindingFlags.Instance | BindingFlags.Public,
                        null,
                        readMethodSignature,
                        null
                        );
                }
                System.Diagnostics.Debug.Assert(readerMethod != null);
                return readerMethod;
            }

            private void GenerateHasValue(Type nullableType) {
                MethodInfo mi = nullableType.GetMethod("get_HasValue", BindingFlags.Instance | BindingFlags.Public);
                gen.Emit(OpCodes.Call, mi);
            }

            private void GenerateGetValue(Type nullableType) {
                MethodInfo mi = nullableType.GetMethod("get_Value", BindingFlags.Instance | BindingFlags.Public);
                gen.Emit(OpCodes.Call, mi);
            }

            private void GenerateGetValueOrDefault(Type nullableType) {
                MethodInfo mi = nullableType.GetMethod("GetValueOrDefault", System.Type.EmptyTypes);
                gen.Emit(OpCodes.Call, mi);
            }

            private Type GenerateGlobalAccess(int iGlobal, Type type) {
                this.GenerateAccessGlobals();
                if (type.IsValueType) {
                    this.GenerateConstInt(iGlobal);
                    gen.Emit(OpCodes.Ldelem_Ref);
                    Type varType = typeof(StrongBox<>).MakeGenericType(type);
                    gen.Emit(OpCodes.Castclass, varType);
                    FieldInfo fi = varType.GetField("Value", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                    gen.Emit(OpCodes.Ldfld, fi);
                }
                else {
                    this.GenerateConstInt(iGlobal);
                    gen.Emit(OpCodes.Ldelem_Ref);
                    this.GenerateConvertToType(typeof(object), type);
                    gen.Emit(OpCodes.Castclass, type);
                }
                return type;
            }

            private int AddGlobal(Type type, object value) {
                int iGlobal = this.globals.Count;
                if (type.IsValueType) {
                    this.globals.Add(Activator.CreateInstance(typeof(StrongBox<>).MakeGenericType(type), new object[] { value }));
                }
                else {
                    this.globals.Add(value);
                }
                return iGlobal;
            }

            private int AllocateLocal() {
                return this.nLocals++;
            }

            private void GenerateStoreMember(MemberInfo mi) {
                FieldInfo fi = mi as FieldInfo;
                if (fi != null) {
                    gen.Emit(OpCodes.Stfld, fi);
                }
                else {
                    PropertyInfo pi = (PropertyInfo)mi;
                    MethodInfo meth = pi.GetSetMethod(true);
                    System.Diagnostics.Debug.Assert(meth != null);
                    gen.Emit(GetMethodCallOpCode(meth), meth);
                }
            }

            private void GenerateLoadMember(MemberInfo mi) {
                FieldInfo fi = mi as FieldInfo;
                if (fi != null) {
                    gen.Emit(OpCodes.Ldfld, fi);
                }
                else {
                    PropertyInfo pi = (PropertyInfo)mi;
                    MethodInfo meth = pi.GetGetMethod(true);
                    gen.Emit(GetMethodCallOpCode(meth), meth);
                }
            }

            [SuppressMessage("Microsoft.Performance", "CA1804:RemoveUnusedLocals", Justification = "[....]: The variable tc for which the rule fires is used in both a Debug.Assert and in a switch statement")]
            private void GenerateArrayAssign(Type type) {
                // This method was copied out of the expression compiler codebase.  
                // Since DLINQ doesn't currently consume array indexers most of this 
                // function goes unused. Currently, the DLINQ materializer only 
                // accesses only ararys of objects and array of integers.
                // The code is comment out to improve code coverage test.
                // If you see one of the following assert fails, try to enable 
                // the comment out code.

                if (type.IsEnum) {
                    gen.Emit(OpCodes.Stelem, type);
                }
                else {
                    TypeCode tc = Type.GetTypeCode(type);

                    switch (tc) {
                        case TypeCode.SByte:
                        case TypeCode.Byte:
                             gen.Emit(OpCodes.Stelem_I1);
                             break;
                        case TypeCode.Int16:
                        case TypeCode.UInt16:
                             gen.Emit(OpCodes.Stelem_I2);
                             break;
                        case TypeCode.Int32:
                        case TypeCode.UInt32:
                             gen.Emit(OpCodes.Stelem_I4);
                             break;
                        case TypeCode.Int64:
                        case TypeCode.UInt64:
                             gen.Emit(OpCodes.Stelem_I8);
                             break;
                        case TypeCode.Single:
                             gen.Emit(OpCodes.Stelem_R4);
                             break;
                        case TypeCode.Double:
                             gen.Emit(OpCodes.Stelem_R8);
                             break;
                        default:
                            if (type.IsValueType) {
                                gen.Emit(OpCodes.Stelem, type);
                            }
                            else {
                                gen.Emit(OpCodes.Stelem_Ref);
                            }
                            break;
                    }
                }
            }

            [SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "address", Justification = "[....]: See comments in source. Usage commented out to improve code coverage test")]
            private Type GenerateArrayAccess(Type type, bool address) {
                // This method was copied out of the expression compiler codebase.  
                // Since DLINQ doesn't currently consume array indexers most of this 
                // function goes unused. Currently, the DLINQ materializer only 
                // accesses arrays of objects and array of integers.
                // The code is comment out to improve code coverage test.
                // If you see one of the following asserts fails, try to enable 
                // the comment out code.

                System.Diagnostics.Debug.Assert(address == false);

                // if (address)
                // {
                //    gen.Emit(OpCodes.Ldelema);
                //    return type.MakeByRefType();
                // }
                // else
                {
                    if (type.IsEnum) {
                        System.Diagnostics.Debug.Assert(false);
                        // gen.Emit(OpCodes.Ldelem, type);
                    }
                    else {
                        TypeCode tc = Type.GetTypeCode(type);
                        System.Diagnostics.Debug.Assert(tc == TypeCode.Int32);

                        switch (tc) {
                            //case TypeCode.SByte:
                            //     gen.Emit(OpCodes.Ldelem_I1);
                            //     break;
                            //case TypeCode.Int16:
                            //     gen.Emit(OpCodes.Ldelem_I2);
                            //     break;
                            case TypeCode.Int32:
                                gen.Emit(OpCodes.Ldelem_I4);
                                break;
                            //case TypeCode.Int64:
                            //     gen.Emit(OpCodes.Ldelem_I8);
                            //     break;
                            //case TypeCode.Single:
                            //     gen.Emit(OpCodes.Ldelem_R4);
                            //     break;
                            //case TypeCode.Double:
                            //     gen.Emit(OpCodes.Ldelem_R8);
                            //     break;
                            //default:
                            //     if (type.IsValueType) {
                            //        gen.Emit(OpCodes.Ldelem, type);
                            //     }
                            //     else {
                            //        gen.Emit(OpCodes.Ldelem_Ref);
                            //     }
                            //     break;
                        }
                    }
                    return type;
                }
            }

            private Type GenerateConstant(Type type, object value) {
                if (value == null) {
                    if (type.IsValueType) {
                        LocalBuilder loc = gen.DeclareLocal(type);
                        gen.Emit(OpCodes.Ldloca, loc);
                        gen.Emit(OpCodes.Initobj, type);
                        gen.Emit(OpCodes.Ldloc, loc);
                    }
                    else {
                        gen.Emit(OpCodes.Ldnull);
                    }
                }
                else {
                    TypeCode tc = Type.GetTypeCode(type);
                    switch (tc) {
                        case TypeCode.Boolean:
                            this.GenerateConstInt((bool)value ? 1 : 0);
                            break;
                        case TypeCode.SByte:
                            this.GenerateConstInt((SByte)value);
                            gen.Emit(OpCodes.Conv_I1);
                            break;
                        case TypeCode.Int16:
                            this.GenerateConstInt((Int16)value);
                            gen.Emit(OpCodes.Conv_I2);
                            break;
                        case TypeCode.Int32:
                            this.GenerateConstInt((Int32)value);
                            break;
                        case TypeCode.Int64:
                            gen.Emit(OpCodes.Ldc_I8, (Int64)value);
                            break;
                        case TypeCode.Single:
                            gen.Emit(OpCodes.Ldc_R4, (float)value);
                            break;
                        case TypeCode.Double:
                            gen.Emit(OpCodes.Ldc_R8, (double)value);
                            break;
                        default:
                            int iGlobal = this.AddGlobal(type, value);
                            return this.GenerateGlobalAccess(iGlobal, type);
                    }
                }
                return type;
            }


            private void GenerateConstInt(int value) {
                switch (value) {
                    case 0:
                        gen.Emit(OpCodes.Ldc_I4_0);
                        break;
                    case 1:
                        gen.Emit(OpCodes.Ldc_I4_1);
                        break;
                    case 2:
                        gen.Emit(OpCodes.Ldc_I4_2);
                        break;
                    case 3:
                        gen.Emit(OpCodes.Ldc_I4_3);
                        break;
                    case 4:
                        gen.Emit(OpCodes.Ldc_I4_4);
                        break;
                    case 5:
                        gen.Emit(OpCodes.Ldc_I4_5);
                        break;
                    case 6:
                        gen.Emit(OpCodes.Ldc_I4_6);
                        break;
                    case 7:
                        gen.Emit(OpCodes.Ldc_I4_7);
                        break;
                    case 8:
                        gen.Emit(OpCodes.Ldc_I4_8);
                        break;
                    default:
                        if (value == -1) {
                            gen.Emit(OpCodes.Ldc_I4_M1);
                        }
                        else if (value >= -127 && value < 128) {
                            gen.Emit(OpCodes.Ldc_I4_S, (sbyte)value);
                        }
                        else {
                            gen.Emit(OpCodes.Ldc_I4, value);
                        }
                        break;
                }
            }
        }

        struct NamedColumn {
            string name;
            bool isRequired;
            internal NamedColumn(string name, bool isRequired) {
                this.name = name;
                this.isRequired = isRequired;
            }
            internal string Name {
                get { return this.name; }
            }
            internal bool IsRequired {
                get { return this.isRequired; }
            }
        }

        class ObjectReaderFactory<TDataReader, TObject> : IObjectReaderFactory
            where TDataReader : DbDataReader {
            Func<ObjectMaterializer<TDataReader>, TObject> fnMaterialize;
            NamedColumn[] namedColumns;
            object[] globals;
            int nLocals;

            internal ObjectReaderFactory(
                Func<ObjectMaterializer<TDataReader>, TObject> fnMaterialize,
                NamedColumn[] namedColumns,
                object[] globals,
                int nLocals
                ) {
                this.fnMaterialize = fnMaterialize;
                this.namedColumns = namedColumns;
                this.globals = globals;
                this.nLocals = nLocals;
            }

            public IObjectReader Create(DbDataReader dataReader, bool disposeDataReader, IReaderProvider provider, object[] parentArgs, object[] userArgs, ICompiledSubQuery[] subQueries) {
                ObjectReaderSession<TDataReader> session = new ObjectReaderSession<TDataReader>((TDataReader)dataReader, provider, parentArgs, userArgs, subQueries);
                return session.CreateReader<TObject>(this.fnMaterialize, this.namedColumns, this.globals, this.nLocals, disposeDataReader);
            }

            public IObjectReader GetNextResult(IObjectReaderSession session, bool disposeDataReader) {
                ObjectReaderSession<TDataReader> ors = (ObjectReaderSession<TDataReader>)session;
                IObjectReader reader = ors.GetNextResult<TObject>(this.fnMaterialize, this.namedColumns, this.globals, this.nLocals, disposeDataReader);
                if (reader == null && disposeDataReader) {
                    ors.Dispose();
                }
                return reader;
            }
        }

        private abstract class ObjectReaderBase<TDataReader> : ObjectMaterializer<TDataReader>
            where TDataReader : DbDataReader 
		{
            protected readonly ObjectReaderSession<TDataReader> session;

			private bool hasReadAtLeastOneRow;
            private bool hasRead;
			private bool hasCurrentRow;
			private bool isFinished;
			private readonly IDataServices services;


            protected ObjectReaderBase(
                ObjectReaderSession<TDataReader> session,
                NamedColumn[] namedColumns,
                object[] globals,
                object[] arguments,
                int nLocals)
			{
                this.session = session;
                services = session.Provider.Services;
                DataReader = session.DataReader;
                Globals = globals;
                Arguments = arguments;
	            if (nLocals > 0)
		            Locals = new object[nLocals];
	            if (this.session.IsBuffered)
		            Buffer();
	            Ordinals = GetColumnOrdinals(namedColumns);
            }


			public override bool CanDeferLoad
			{
				get { return services.Context.DeferredLoadingEnabled; }
			}


			internal bool IsBuffered
			{
				get { return BufferReader != null; }
			}

			// This method is called from within this class's constructor (through a call to Buffer()) so it is sealed to prevent
			// derived classes from overriding it. See FxCop rule CA2214 for more information on why this is necessary.
			public override sealed bool Read()
			{
				if (isFinished)
					return false;

				var dataContext = services.Context;

				var hasRows = !DataReader.IsClosed && DataReader.HasRows;

				hasCurrentRow = BufferReader == null ? DataReader.Read() : BufferReader.Read();

				if (!hasCurrentRow)
				{
					if (dataContext.ShouldThrowReaderRowsPresenceMismatchException && 
						BufferReader == null && 
						!hasReadAtLeastOneRow && 
						hasRows)
						throw new ReaderRowsPresenceMismatchException(
							"Reader told that it has rows, but it didn't read anything");

					isFinished = true;
					session.Finish(this);
				}

				hasReadAtLeastOneRow = true;
				hasRead = true;
				return hasCurrentRow;
			}

			public override object InsertLookup(int iMetaType, object instance)
			{
				var mType = (MetaType)Globals[iMetaType];
				return services.InsertLookupCachedObject(mType, instance);
			}

			public override void SendEntityMaterialized(int iMetaType, object instance)
			{
				var mType = (MetaType)Globals[iMetaType];
				services.OnEntityMaterialized(mType, instance);
			}

			public override IEnumerable ExecuteSubQuery(int iSubQuery, object[] parentArgs)
			{
				if (session.ParentArguments != null)
				{
					// Create array to accumulate args, and add both parent
					// args and the supplied args to the array
					var nParent = session.ParentArguments.Length;
					var tmp = new object[nParent + parentArgs.Length];
					Array.Copy(session.ParentArguments, tmp, nParent);
					Array.Copy(parentArgs, 0, tmp, nParent, parentArgs.Length);
					parentArgs = tmp;
				}
				var subQuery = session.SubQueries[iSubQuery];
				var results = (IEnumerable)subQuery.Execute(session.Provider, parentArgs, session.UserArguments).ReturnValue;
				return results;
			}

			public override IEnumerable<T> GetLinkSource<T>(int iGlobalLink, int iLocalFactory, object[] keyValues)
			{
				var factory = (IDeferredSourceFactory)Locals[iLocalFactory];
				if (factory == null)
				{
					var member = (MetaDataMember)Globals[iGlobalLink];
					factory = services.GetDeferredSourceFactory(member);
					Locals[iLocalFactory] = factory;
				}
				return (IEnumerable<T>)factory.CreateDeferredSource(keyValues);
			}

			public override IEnumerable<T> GetNestedLinkSource<T>(int iGlobalLink, int iLocalFactory, object instance)
			{
				var factory = (IDeferredSourceFactory)Locals[iLocalFactory];
				if (factory == null)
				{
					var member = (MetaDataMember)Globals[iGlobalLink];
					factory = services.GetDeferredSourceFactory(member);
					Locals[iLocalFactory] = factory;
				}
				return (IEnumerable<T>)factory.CreateDeferredSource(instance);
			}

	        public override T CreateEntityProxy<T>()
	        {
		        return (T)services.Model.CreateEntityProxy(typeof(T));
	        }


	        internal void Buffer() 
			{
                if (BufferReader == null && (hasCurrentRow || !hasRead)) 
				{
                    if (session.IsBuffered) 
					{
                        BufferReader = session.GetNextBufferedReader();
                    }
                    else 
					{
                        var ds = new DataSet
                        {
	                        EnforceConstraints = false
                        };
						var bufferTable = new DataTable();
                        ds.Tables.Add(bufferTable);
                        var names = session.GetActiveNames();
                        bufferTable.Load(new Rereader(DataReader, hasCurrentRow, null), LoadOption.OverwriteChanges);
                        BufferReader = new Rereader(bufferTable.CreateDataReader(), false, names);
                    }
					if (hasCurrentRow)
						Read();
				}
            }


            private int[] GetColumnOrdinals(NamedColumn[] namedColumns) 
			{
	            var reader = BufferReader ?? DataReader;
	            if (namedColumns == null || namedColumns.Length == 0)
		            return null;
	            var columnOrdinals = new int[namedColumns.Length];
                var lookup = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                //we need to compare the quoted names on both sides
                //because the designer might quote the name unnecessarily
	            for (int i = 0, n = reader.FieldCount; i < n; i++)
		            lookup[SqlIdentifier.QuoteCompoundIdentifier(reader.GetName(i))] = i;
	            for (int i = 0, n = namedColumns.Length; i < n; i++) 
				{
                    int ordinal;
					if (lookup.TryGetValue(SqlIdentifier.QuoteCompoundIdentifier(namedColumns[i].Name), out ordinal))
						columnOrdinals[i] = ordinal;
					else if (namedColumns[i].IsRequired)
						throw Error.RequiredColumnDoesNotExist(namedColumns[i].Name);
					else
						columnOrdinals[i] = -1;
				}
                return columnOrdinals;
            }
        }

        class ObjectReader<TDataReader, TObject>
            : ObjectReaderBase<TDataReader>, IEnumerator<TObject>, IObjectReader, IDisposable
            where TDataReader : DbDataReader {
            Func<ObjectMaterializer<TDataReader>, TObject> fnMaterialize;
            TObject current;
            bool disposeSession;

            internal ObjectReader(
                ObjectReaderSession<TDataReader> session,
                NamedColumn[] namedColumns,
                object[] globals,
                object[] arguments,
                int nLocals,
                bool disposeSession,
                Func<ObjectMaterializer<TDataReader>, TObject> fnMaterialize
                )
                : base(session, namedColumns, globals, arguments, nLocals) {
                this.disposeSession = disposeSession;
                this.fnMaterialize = fnMaterialize;
            }

            public IObjectReaderSession Session {
                get { return this.session; }
            }

            public void Dispose() {
                // Technically, calling GC.SuppressFinalize is not required because the class does not
                // have a finalizer, but it does no harm, protects against the case where a finalizer is added
                // in the future, and prevents an FxCop warning.
                GC.SuppressFinalize(this);
                if (this.disposeSession) {
                    this.session.Dispose();
                }
            }

	        public bool MoveNext()
	        {
		        var dataContext = session.Provider.Services.Context;

		        var readResult = this.Read();

		        if (readResult)
		        {
			        this.current = this.fnMaterialize(this);

					return true;
		        }
		        else
		        {
			        this.current = default(TObject);
			        this.Dispose();
			        return false;
		        }
	        }

	        public TObject Current {
                get { return this.current; }
            }

            public void Reset() {
            }

            object IEnumerator.Current {
                get {
                    return this.Current;
                }
            }
        }

        class ObjectReaderSession<TDataReader> : IObjectReaderSession, IDisposable, IConnectionUser
            where TDataReader : DbDataReader {
            TDataReader dataReader;
            ObjectReaderBase<TDataReader> currentReader;
            IReaderProvider provider;
            List<DbDataReader> buffer;
            int iNextBufferedReader;
            bool isDisposed;
            bool isDataReaderDisposed;
            bool hasResults;
            object[] parentArgs;
            object[] userArgs;
            ICompiledSubQuery[] subQueries;

            internal ObjectReaderSession(
                TDataReader dataReader,
                IReaderProvider provider,
                object[] parentArgs,
                object[] userArgs,
                ICompiledSubQuery[] subQueries
                ) {
                this.dataReader = dataReader;
                this.provider = provider;
                this.parentArgs = parentArgs;
                this.userArgs = userArgs;
                this.subQueries = subQueries;
                this.hasResults = true;
            }

            internal ObjectReaderBase<TDataReader> CurrentReader {
                get { return this.currentReader; }
            }

            internal TDataReader DataReader {
                get { return this.dataReader; }
            }

            internal IReaderProvider Provider {
                get { return this.provider; }
            }

            internal object[] ParentArguments {
                get { return this.parentArgs; }
            }

            internal object[] UserArguments {
                get { return this.userArgs; }
            }

            internal ICompiledSubQuery[] SubQueries {
                get { return this.subQueries; }
            }

            internal void Finish(ObjectReaderBase<TDataReader> finishedReader) {
                if (this.currentReader == finishedReader) {
                    this.CheckNextResults();
                }
            }

            private void CheckNextResults() {
                this.hasResults = !this.dataReader.IsClosed && this.dataReader.NextResult();
                this.currentReader = null;
                if (!this.hasResults) {
                    this.Dispose();
                }
            }

            internal DbDataReader GetNextBufferedReader() {
                if (this.iNextBufferedReader < this.buffer.Count) {
                    return this.buffer[this.iNextBufferedReader++];
                }
                System.Diagnostics.Debug.Assert(false);
                return null;
            }

            public bool IsBuffered {
                get { return this.buffer != null; }
            }

            [SuppressMessage("Microsoft.Globalization", "CA1306:SetLocaleForDataTypes", Justification = "[....]: Used only as a buffer and never used for string comparison.")]
            public void Buffer() {
                if (this.buffer == null) {
                    if (this.currentReader != null && !this.currentReader.IsBuffered) {
                        this.currentReader.Buffer();
                        this.CheckNextResults();
                    }
                    // buffer anything remaining in the session
                    this.buffer = new List<DbDataReader>();
                    while (this.hasResults) {
                        DataSet ds = new DataSet();
                        ds.EnforceConstraints = false;
                        DataTable tb = new DataTable();
                        ds.Tables.Add(tb);
                        string[] names = this.GetActiveNames();
                        tb.Load(new Rereader(this.dataReader, false, null), LoadOption.OverwriteChanges);
                        this.buffer.Add(new Rereader(tb.CreateDataReader(), false, names));
                        this.CheckNextResults();
                    }
                }
            }

            internal string[] GetActiveNames() {
                string[] names = new string[this.DataReader.FieldCount];
                for (int i = 0, n = this.DataReader.FieldCount; i < n; i++) {
                    names[i] = this.DataReader.GetName(i);
                }
                return names;
            }

            public void CompleteUse() {
                this.Buffer();
            }

            public void Dispose() {
                if (!this.isDisposed) {
                    // Technically, calling GC.SuppressFinalize is not required because the class does not
                    // have a finalizer, but it does no harm, protects against the case where a finalizer is added
                    // in the future, and prevents an FxCop warning.
                    GC.SuppressFinalize(this);
                    this.isDisposed = true;
                    if (!this.isDataReaderDisposed) {
                        this.isDataReaderDisposed = true;
                        this.dataReader.Dispose();
                    }
                    this.provider.ConnectionManager.ReleaseConnection(this);
                }
            }

            internal ObjectReader<TDataReader, TObject> CreateReader<TObject>(
                Func<ObjectMaterializer<TDataReader>, TObject> fnMaterialize,
                NamedColumn[] namedColumns,
                object[] globals,
                int nLocals,
                bool disposeDataReader
                ) {
                ObjectReader<TDataReader, TObject> objectReader =
                    new ObjectReader<TDataReader, TObject>(this, namedColumns, globals, this.userArgs, nLocals, disposeDataReader, fnMaterialize);
                this.currentReader = objectReader;
                return objectReader;
            }

            internal ObjectReader<TDataReader, TObject> GetNextResult<TObject>(
                Func<ObjectMaterializer<TDataReader>, TObject> fnMaterialize,
                NamedColumn[] namedColumns,
                object[] globals,
                int nLocals,
                bool disposeDataReader
                ) {
                // skip forward to next results
                if (this.buffer != null) {
                    if (this.iNextBufferedReader >= this.buffer.Count) {
                        return null;
                    }
                }
                else {
                    if (this.currentReader != null) {
                        // buffer current reader
                        this.currentReader.Buffer();
                        this.CheckNextResults();
                    }
                    if (!this.hasResults) {
                        return null;
                    }
                }

                ObjectReader<TDataReader, TObject> objectReader =
                    new ObjectReader<TDataReader, TObject>(this, namedColumns, globals, this.userArgs, nLocals, disposeDataReader, fnMaterialize);

                this.currentReader = objectReader;
                return objectReader;
            }
        }

        class Rereader : DbDataReader, IDisposable {
            bool first;
            DbDataReader reader;
            string[] names;

            internal Rereader(DbDataReader reader, bool hasCurrentRow, string[] names) {
                this.reader = reader;
                this.first = hasCurrentRow;
                this.names = names;
            }

            public override bool Read() {
                if (this.first) {
                    this.first = false;
                    return true;
                }
                return this.reader.Read();
            }

            public override string GetName(int i) {
                if (this.names != null) {
                    return this.names[i];
                }
                return reader.GetName(i);
            }

            public override void Close() { }
            public override bool NextResult() { return false; }

            public override int Depth { get { return reader.Depth; } }
            public override bool IsClosed { get { return reader.IsClosed; } }
            public override int RecordsAffected { get { return reader.RecordsAffected; } }
            public override DataTable GetSchemaTable() { return reader.GetSchemaTable(); }

            public override int FieldCount { get { return reader.FieldCount; } }
            public override object this[int i] { get { return reader[i]; } }
            public override object this[string name] { get { return reader[name]; } }
            public override bool GetBoolean(int i) { return reader.GetBoolean(i); }
            public override byte GetByte(int i) { return reader.GetByte(i); }
            public override long GetBytes(int i, long fieldOffset, byte[] buffer, int bufferOffset, int length) { return reader.GetBytes(i, fieldOffset, buffer, bufferOffset, length); }
            public override char GetChar(int i) { return reader.GetChar(i); }
            public override long GetChars(int i, long fieldOffset, char[] buffer, int bufferOffset, int length) { return reader.GetChars(i, fieldOffset, buffer, bufferOffset, length); }
            public override string GetDataTypeName(int i) { return reader.GetDataTypeName(i); }
            public override DateTime GetDateTime(int i) { return reader.GetDateTime(i); }
            public override decimal GetDecimal(int i) { return reader.GetDecimal(i); }
            public override double GetDouble(int i) { return reader.GetDouble(i); }
            public override Type GetFieldType(int i) { return reader.GetFieldType(i); }
            public override float GetFloat(int i) { return reader.GetFloat(i); }
            public override Guid GetGuid(int i) { return reader.GetGuid(i); }
            public override short GetInt16(int i) { return reader.GetInt16(i); }
            public override int GetInt32(int i) { return reader.GetInt32(i); }
            public override long GetInt64(int i) { return reader.GetInt64(i); }
            public override int GetOrdinal(string name) { return reader.GetOrdinal(name); }
            public override string GetString(int i) { return reader.GetString(i); }
            public override object GetValue(int i) { return reader.GetValue(i); }
            public override int GetValues(object[] values) { return reader.GetValues(values); }
            public override bool IsDBNull(int i) { return reader.IsDBNull(i); }

            public override IEnumerator GetEnumerator() {
                return this.reader.GetEnumerator();
            }
            public override bool HasRows {
                get { return this.first || this.reader.HasRows; }
            }
        }

        internal class Group<K, T> : IGrouping<K, T>, IEnumerable<T>, IEnumerable {
            K key;
            IEnumerable<T> items;

            internal Group(K key, IEnumerable<T> items) {
                this.key = key;
                this.items = items;
            }

            K IGrouping<K, T>.Key {
                get { return this.key; }
            }

            IEnumerator IEnumerable.GetEnumerator() {
                return (IEnumerator)this.GetEnumerator();
            }

            public IEnumerator<T> GetEnumerator() {
                return this.items.GetEnumerator();
            }
        }

        internal class OrderedResults<T> : IOrderedEnumerable<T>, IEnumerable<T> {
            List<T> values;
            internal OrderedResults(IEnumerable<T> results) {
                this.values = results as List<T>;
                if (this.values == null)
                    this.values = new List<T>(results);
            }
            IOrderedEnumerable<T> IOrderedEnumerable<T>.CreateOrderedEnumerable<K>(Func<T, K> keySelector, IComparer<K> comparer, bool descending) {
                throw Error.NotSupported();
            }
            IEnumerator IEnumerable.GetEnumerator() {
                return ((IEnumerable)this.values).GetEnumerator();
            }
            IEnumerator<T> IEnumerable<T>.GetEnumerator() {
                return this.values.GetEnumerator();
            }
        }
    }
}
