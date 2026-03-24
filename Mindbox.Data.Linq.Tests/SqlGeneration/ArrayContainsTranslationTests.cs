using System;
using System.Data.Linq;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mindbox.Data.Linq.Tests
{
	/// <summary>
	/// Tests that array.Contains(value) in LINQ queries is correctly translated to SQL IN clause.
	/// In .NET 10, the C# compiler generates MemoryExtensions.Contains(ReadOnlySpan, value)
	/// for this pattern instead of Enumerable.Contains — this must produce the same SQL.
	/// Two compiler patterns exist:
	/// 1. MethodCallExpression(op_Implicit, array) — simple closure capture
	/// 2. InvocationExpression(ConstantExpression(delegate), []) — pre-compiled closure
	/// Both must be handled.
	/// </summary>
	[TestClass]
	public class ArrayContainsTranslationTests
	{
		[TestMethod]
		public void ArrayContains_TranslatesToSqlIn()
		{
			var ids = new[] { 1, 2, 3 };

			using var connection = new DbConnectionStub();
			using var context = new DataContext(connection);

			var query = context.GetTable<SimpleEntity>().Where(t => ids.Contains(t.Id));

			using var command = context.GetCommand(query);

			Assert.AreEqual(
				"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
				"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
				"WHERE [t0].[Id] IN (@p0, @p1, @p2)",
				command.CommandText);
		}

		/// <summary>
		/// Directly constructs the InvocationExpression(ConstantExpression(delegate), []) pattern
		/// using Expression API — guaranteed to exercise Pattern 2 regardless of compiler version.
		/// The MemoryExtensions.Contains call is built by hand with a pre-compiled zero-arg delegate
		/// as the span argument, matching exactly what .NET 10 generates in nested closure contexts.
		/// </summary>
		[TestMethod]
		public void ArrayContains_InvocationExpressionPattern_TranslatesToSqlIn()
		{
			var ids = new[] { 1, 2, 3 };

			// Build MemoryExtensions.Contains<int>(ReadOnlySpan<int>, int) method
			var containsMethod = typeof(MemoryExtensions)
				.GetMethods(BindingFlags.Public | BindingFlags.Static)
				.Single(m => m.Name == "Contains"
					&& m.IsGenericMethod
					&& m.GetParameters() is [{ ParameterType: var p0 }, { ParameterType: { IsGenericParameter: true } }]
					&& p0.IsGenericType
					&& p0.GetGenericTypeDefinition() == typeof(ReadOnlySpan<>))
				.MakeGenericMethod(typeof(int));

			// Build op_Implicit: int[] → ReadOnlySpan<int>
			var opImplicit = typeof(ReadOnlySpan<int>)
				.GetMethod("op_Implicit", new[] { typeof(int[]) });

			// Pattern 2: the compiler pre-compiles the implicit conversion into a zero-arg delegate
			// InvocationExpression(ConstantExpression(Func<ReadOnlySpan<int>>), [])
			// Build: Func<ReadOnlySpan<int>> spanFactory = () => op_Implicit(ids)
			var spanFactoryBody = Expression.Call(opImplicit, Expression.Constant(ids));
			var spanFactory = Expression.Lambda(spanFactoryBody).Compile();
			var spanExpr = Expression.Invoke(Expression.Constant(spanFactory));

			// Build: WHERE t.Id IN (ids) via MemoryExtensions.Contains(spanExpr, t.Id)
			var tParam = Expression.Parameter(typeof(SimpleEntity), "t");
			var idMember = Expression.Property(tParam, nameof(SimpleEntity.Id));
			var containsCall = Expression.Call(containsMethod, spanExpr, idMember);
			var predicate = Expression.Lambda<Func<SimpleEntity, bool>>(containsCall, tParam);

			using var connection = new DbConnectionStub();
			using var context = new DataContext(connection);

			var query = context.GetTable<SimpleEntity>().Where(predicate);

			using var command = context.GetCommand(query);

			Assert.AreEqual(
				"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
				"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
				"WHERE [t0].[Id] IN (@p0, @p1, @p2)",
				command.CommandText);
		}

		[TestMethod]
		public void ArrayContains_EmptyArray_TranslatesToFalse()
		{
			var ids = Array.Empty<int>();

			using var connection = new DbConnectionStub();
			using var context = new DataContext(connection);

			var query = context.GetTable<SimpleEntity>().Where(t => ids.Contains(t.Id));

			using var command = context.GetCommand(query);

			Assert.AreEqual(
				"SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
				"FROM [SimpleTable] AS [t0]" + Environment.NewLine +
				"WHERE 0 = 1",
				command.CommandText);
		}
	}
}
