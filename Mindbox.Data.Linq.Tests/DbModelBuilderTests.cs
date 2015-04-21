using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Mindbox.Data.Linq.Mapping;

namespace Mindbox.Data.Linq.Tests
{
	[TestClass]
	public class DbModelBuilderTests
	{
		[TestMethod]
		public void TableAttributeViaAttribute()
		{
			var incompatibilityDetected = false;
			var configuration = new MindboxMappingConfiguration();
			configuration.EntityFrameworkIncompatibility += (sender, incompatibility) =>
			{
				if (incompatibility == EntityFrameworkIncompatibility.TableAttribute)
					incompatibilityDetected = true;
			};
			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity4));

			Assert.IsNotNull(metaTable);
			Assert.AreEqual("administration.Staff", metaTable.TableName);
			Assert.IsTrue(incompatibilityDetected);
		}

		[TestMethod]
		public void TableAttributeViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity6));

			Assert.IsNotNull(metaTable);
			Assert.AreEqual("administration.Staff", metaTable.TableName);
		}

		[TestMethod]
		public void TableAttributeViaBuilderWithAlreadyFrozenConfiguration()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());
			configuration.Freeze();

			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity6));

			Assert.IsNotNull(metaTable);
			Assert.AreEqual("administration.Staff", metaTable.TableName);
		}

		[TestMethod]
		public void BooleanColumnViaAttribute()
		{
			var incompatibilityDetected = false;
			var configuration = new MindboxMappingConfiguration();
			configuration.EntityFrameworkIncompatibility += (sender, incompatibility) =>
			{
				if (incompatibility == EntityFrameworkIncompatibility.ColumnAttribute)
					incompatibilityDetected = true;
			};
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "IsBlocked");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("IsBlocked", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("bit not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(bool), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
			Assert.IsTrue(incompatibilityDetected);
		}

		[TestMethod]
		public void BooleanColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "IsBlocked");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("IsBlocked", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("bit not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(bool), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void NvarcharNotNullColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "UserName");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("UserName", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("nvarchar(100) not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(string), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void NvarcharNotNullColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "UserName");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("UserName", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("nvarchar(100) not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(string), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void CharNullColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "PasswordHash");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("PasswordHash", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsTrue(metaDataMember.CanBeNull);
			Assert.AreEqual("char(40) null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(string), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void CharNullColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "PasswordHash");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("PasswordHash", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsTrue(metaDataMember.CanBeNull);
			Assert.AreEqual("char(40) null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(string), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void NvarcharMaxNotNullColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Comment");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("Comment", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("nvarchar(max) not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(string), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void NvarcharMaxNotNullColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Comment");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("Comment", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("nvarchar(max) not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(string), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void DateTimeColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "CreationDateTimeUtc");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("CreationDateTimeUtc", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("datetime not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(DateTime), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void DateTimeColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "CreationDateTimeUtc");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("CreationDateTimeUtc", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("datetime not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(DateTime), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void NullableDateTimeColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "AccountExpirationDateTimeUtc");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("AccountExpirationDateTimeUtc", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsTrue(metaDataMember.CanBeNull);
			Assert.AreEqual("datetime null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(DateTime?), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void NullableDateTimeColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "AccountExpirationDateTimeUtc");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("AccountExpirationDateTimeUtc", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Never, metaDataMember.AutoSync);
			Assert.IsTrue(metaDataMember.CanBeNull);
			Assert.AreEqual("datetime null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsFalse(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(DateTime?), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void VersionColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "RowVersion");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("RowVersion", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Always, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("rowversion not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsTrue(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsTrue(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(Binary), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void VersionColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "RowVersion");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("RowVersion", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.Always, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("rowversion not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsTrue(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsFalse(metaDataMember.IsPrimaryKey);
			Assert.IsTrue(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(byte[]), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void BinaryColumnViaBuilderRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity8.TestEntity8Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTableCommand = new SqlCommand(
						"create table Test8 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTableCommand.ExecuteNonQuery();

					var insertCommand = new SqlCommand("insert into Test8 (Value) values (0x1122334455)", connection);
					insertCommand.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item = context.GetTable<TestEntity8>().Single();
						Assert.IsNotNull(item.Value);
						Assert.AreEqual(5, item.Value.Length);
						Assert.AreEqual(0x11, item.Value[0]);
						Assert.AreEqual(0x22, item.Value[1]);
						Assert.AreEqual(0x33, item.Value[2]);
						Assert.AreEqual(0x44, item.Value[3]);
						Assert.AreEqual(0x55, item.Value[4]);

						item.Value = new byte[]
						{
							1,
							2,
							3,
							4,
							5
						};
						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item = context.GetTable<TestEntity8>().Single();
						Assert.IsNotNull(item.Value);
						Assert.AreEqual(5, item.Value.Length);
						Assert.AreEqual(1, item.Value[0]);
						Assert.AreEqual(2, item.Value[1]);
						Assert.AreEqual(3, item.Value[2]);
						Assert.AreEqual(4, item.Value[3]);
						Assert.AreEqual(5, item.Value[4]);
					}
				});
		}

		[TestMethod]
		public void IdentityColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity4))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Id");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("Id", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.OnInsert, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("int identity not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsTrue(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsTrue(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(int), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void IdentityColumnViaBuilder()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaDataMember = mappingSource
				.GetModel(typeof(DataContext))
				.GetMetaType(typeof(TestEntity6))
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Id");

			Assert.IsNotNull(metaDataMember);
			Assert.AreEqual("Id", metaDataMember.MappedName);
			Assert.IsNull(metaDataMember.Association);
			Assert.AreEqual(AutoSync.OnInsert, metaDataMember.AutoSync);
			Assert.IsFalse(metaDataMember.CanBeNull);
			Assert.AreEqual("int identity not null", metaDataMember.DbType);
			Assert.IsNull(metaDataMember.Expression);
			Assert.IsFalse(metaDataMember.IsAssociation);
			Assert.IsTrue(metaDataMember.IsDbGenerated);
			Assert.IsFalse(metaDataMember.IsDeferred);
			Assert.IsFalse(metaDataMember.IsDiscriminator);
			Assert.IsTrue(metaDataMember.IsPersistent);
			Assert.IsTrue(metaDataMember.IsPrimaryKey);
			Assert.IsFalse(metaDataMember.IsVersion);
			Assert.AreEqual(typeof(int), metaDataMember.Type);
			Assert.AreEqual(UpdateCheck.Always, metaDataMember.UpdateCheck);
		}

		[TestMethod]
		public void RequiredOneWayOneToManyAssociationViaAttribute()
		{
			var incompatibilityDetected = false;
			var configuration = new MindboxMappingConfiguration();
			configuration.EntityFrameworkIncompatibility += (sender, incompatibility) =>
			{
				if (incompatibility == EntityFrameworkIncompatibility.AssociationAttribute)
					incompatibilityDetected = true;
			};
			var mappingSource = new MindboxMappingSource(configuration);
			var metaModel = mappingSource.GetModel(typeof(DataContext));
			var entityMetaType = metaModel.GetMetaType(typeof(TestEntity4));

			var foreignKeyMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "CreatorId");
			Assert.IsNotNull(foreignKeyMember);
			Assert.AreEqual("CreatorId", foreignKeyMember.MappedName);
			Assert.IsNull(foreignKeyMember.Association);
			Assert.AreEqual(AutoSync.Never, foreignKeyMember.AutoSync);
			Assert.IsFalse(foreignKeyMember.CanBeNull);
			Assert.AreEqual("int not null", foreignKeyMember.DbType);
			Assert.IsNull(foreignKeyMember.Expression);
			Assert.IsFalse(foreignKeyMember.IsAssociation);
			Assert.IsFalse(foreignKeyMember.IsDbGenerated);
			Assert.IsFalse(foreignKeyMember.IsDeferred);
			Assert.IsFalse(foreignKeyMember.IsDiscriminator);
			Assert.IsTrue(foreignKeyMember.IsPersistent);
			Assert.IsFalse(foreignKeyMember.IsPrimaryKey);
			Assert.IsFalse(foreignKeyMember.IsVersion);
			Assert.AreEqual(typeof(int), foreignKeyMember.Type);
			Assert.AreEqual(UpdateCheck.Always, foreignKeyMember.UpdateCheck);

			var associationMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Creator");
			Assert.IsNotNull(associationMember);
			Assert.IsNotNull(associationMember.MappedName);
			Assert.IsNotNull(associationMember.Association);
			Assert.AreEqual(AutoSync.Never, associationMember.AutoSync);
			Assert.IsTrue(associationMember.CanBeNull);
			Assert.IsNull(associationMember.DbType);
			Assert.IsNull(associationMember.Expression);
			Assert.IsTrue(associationMember.IsAssociation);
			Assert.IsFalse(associationMember.IsDbGenerated);
			Assert.IsTrue(associationMember.IsDeferred);
			Assert.IsFalse(associationMember.IsDiscriminator);
			Assert.IsTrue(associationMember.IsPersistent);
			Assert.IsFalse(associationMember.IsPrimaryKey);
			Assert.IsFalse(associationMember.IsVersion);
			Assert.AreEqual(typeof(TestEntity4), associationMember.Type);
			Assert.AreEqual(UpdateCheck.Never, associationMember.UpdateCheck);
			Assert.IsFalse(associationMember.Association.DeleteOnNull);
			Assert.IsNull(associationMember.Association.DeleteRule);
			Assert.IsTrue(associationMember.Association.IsForeignKey);
			Assert.IsFalse(associationMember.Association.IsMany);
			Assert.IsFalse(associationMember.Association.IsNullable);
			Assert.IsFalse(associationMember.Association.IsUnique);
			Assert.IsTrue(associationMember.Association.OtherKeyIsPrimaryKey);
			Assert.IsFalse(associationMember.Association.ThisKeyIsPrimaryKey);
			Assert.IsNotNull(associationMember.Association.OtherKey);
			Assert.AreEqual(1, associationMember.Association.OtherKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherKey[0].DeclaringType);
			Assert.AreEqual("Id", associationMember.Association.OtherKey[0].Name);
			Assert.IsNotNull(associationMember.Association.ThisKey);
			Assert.AreEqual(1, associationMember.Association.ThisKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.ThisKey[0].DeclaringType);
			Assert.AreEqual("CreatorId", associationMember.Association.ThisKey[0].Name);
			Assert.IsNull(associationMember.Association.OtherMember);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherType);
			Assert.AreEqual(associationMember, associationMember.Association.ThisMember);

			Assert.IsTrue(incompatibilityDetected);
		}

		[TestMethod]
		public void RequiredOneWayOneToManyAssociationViaBuilderWithoutDeferredLoading()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaModel = mappingSource.GetModel(typeof(DataContext));
			var entityMetaType = metaModel.GetMetaType(typeof(TestEntity6));

			var foreignKeyMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "CreatorId");
			Assert.IsNotNull(foreignKeyMember);
			Assert.AreEqual("CreatorId", foreignKeyMember.MappedName);
			Assert.IsNull(foreignKeyMember.Association);
			Assert.AreEqual(AutoSync.Never, foreignKeyMember.AutoSync);
			Assert.IsFalse(foreignKeyMember.CanBeNull);
			Assert.AreEqual("int not null", foreignKeyMember.DbType);
			Assert.IsNull(foreignKeyMember.Expression);
			Assert.IsFalse(foreignKeyMember.IsAssociation);
			Assert.IsFalse(foreignKeyMember.IsDbGenerated);
			Assert.IsFalse(foreignKeyMember.IsDeferred);
			Assert.IsFalse(foreignKeyMember.IsDiscriminator);
			Assert.IsTrue(foreignKeyMember.IsPersistent);
			Assert.IsFalse(foreignKeyMember.IsPrimaryKey);
			Assert.IsFalse(foreignKeyMember.IsVersion);
			Assert.AreEqual(typeof(int), foreignKeyMember.Type);
			Assert.AreEqual(UpdateCheck.Always, foreignKeyMember.UpdateCheck);

			var associationMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Creator");
			Assert.IsNotNull(associationMember);
			Assert.IsNotNull(associationMember.MappedName);
			Assert.IsNotNull(associationMember.Association);
			Assert.AreEqual(AutoSync.Never, associationMember.AutoSync);
			Assert.IsTrue(associationMember.CanBeNull);
			Assert.IsNull(associationMember.DbType);
			Assert.IsNull(associationMember.Expression);
			Assert.IsTrue(associationMember.IsAssociation);
			Assert.IsFalse(associationMember.IsDbGenerated);
			Assert.IsFalse(associationMember.IsDiscriminator);
			Assert.IsTrue(associationMember.IsPersistent);
			Assert.IsFalse(associationMember.IsPrimaryKey);
			Assert.IsFalse(associationMember.IsVersion);
			Assert.AreEqual(typeof(TestEntity6), associationMember.Type);
			Assert.AreEqual(UpdateCheck.Never, associationMember.UpdateCheck);
			Assert.IsFalse(associationMember.Association.DeleteOnNull);
			Assert.IsNull(associationMember.Association.DeleteRule);
			Assert.IsTrue(associationMember.Association.IsForeignKey);
			Assert.IsFalse(associationMember.Association.IsMany);
			Assert.IsFalse(associationMember.Association.IsNullable);
			Assert.IsFalse(associationMember.Association.IsUnique);
			Assert.IsTrue(associationMember.Association.OtherKeyIsPrimaryKey);
			Assert.IsFalse(associationMember.Association.ThisKeyIsPrimaryKey);
			Assert.IsNotNull(associationMember.Association.OtherKey);
			Assert.AreEqual(1, associationMember.Association.OtherKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherKey[0].DeclaringType);
			Assert.AreEqual("Id", associationMember.Association.OtherKey[0].Name);
			Assert.IsNotNull(associationMember.Association.ThisKey);
			Assert.AreEqual(1, associationMember.Association.ThisKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.ThisKey[0].DeclaringType);
			Assert.AreEqual("CreatorId", associationMember.Association.ThisKey[0].Name);
			Assert.IsNull(associationMember.Association.OtherMember);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherType);
			Assert.AreEqual(associationMember, associationMember.Association.ThisMember);
		}

		[TestMethod]
		public void RequiredOneWayOneToManyAssociationViaBuilderDeferredLoading()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity6.TestEntity6Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaModel = mappingSource.GetModel(typeof(DataContext));
			var entityMetaType = metaModel.GetMetaType(typeof(TestEntity6));

			var foreignKeyMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Creator");
			Assert.IsNotNull(foreignKeyMember);
			Assert.IsTrue(foreignKeyMember.IsDeferred);
		}

		[TestMethod]
		public void RequiredOneWayOneToManyAssociationViaBuilderDeferredLoadingRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity10.TestEntity10Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity9.TestEntity9Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable10Command = new SqlCommand(
						"create table Test10 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTable10Command.ExecuteNonQuery();

					var createTable9Command = new SqlCommand(
						"create table Test9 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int not null foreign key references Test10 (Id))",
						connection);
					createTable9Command.ExecuteNonQuery();

					var insert10ACommand = new SqlCommand(
						"insert into Test10 (Value) values (0x1122334455); select scope_identity()",
						connection);
					var id10A = Convert.ToInt32(insert10ACommand.ExecuteScalar());

					var insert10BCommand = new SqlCommand(
						"insert into Test10 (Value) values (0x6655443322)",
						connection);
					insert10BCommand.ExecuteNonQuery();

					var insert9Command = new SqlCommand("insert into Test9 (OtherId) values (@OtherId)", connection);
					insert9Command.Parameters.AddWithValue("OtherId", id10A);
					insert9Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item9 = context.GetTable<TestEntity9>().Single();
						var proxyType = item9.GetType();
						Assert.AreNotEqual(typeof(TestEntity9), proxyType);
						Assert.IsNotNull(item9.Other);

						Assert.AreEqual(5, item9.Other.Value.Length);
						Assert.AreEqual(0x11, item9.Other.Value[0]);
						Assert.AreEqual(0x22, item9.Other.Value[1]);
						Assert.AreEqual(0x33, item9.Other.Value[2]);
						Assert.AreEqual(0x44, item9.Other.Value[3]);
						Assert.AreEqual(0x55, item9.Other.Value[4]);

						var newItem10 = context.GetTable<TestEntity10>().OrderByDescending(x => x.Id).First();
						item9.Other = newItem10;

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item9 = context.GetTable<TestEntity9>().Single();
						Assert.IsNotNull(item9.Other);

						Assert.AreEqual(5, item9.Other.Value.Length);
						Assert.AreEqual(0x66, item9.Other.Value[0]);
						Assert.AreEqual(0x55, item9.Other.Value[1]);
						Assert.AreEqual(0x44, item9.Other.Value[2]);
						Assert.AreEqual(0x33, item9.Other.Value[3]);
						Assert.AreEqual(0x22, item9.Other.Value[4]);
					}
				});
		}

		[TestMethod]
		public void OptionalOneWayOneToManyAssociationViaAttribute()
		{
			var incompatibilityDetected = false;
			var configuration = new MindboxMappingConfiguration();
			configuration.EntityFrameworkIncompatibility += (sender, incompatibility) =>
			{
				if (incompatibility == EntityFrameworkIncompatibility.AssociationAttribute)
					incompatibilityDetected = true;
			};
			var mappingSource = new MindboxMappingSource(configuration);
			var metaModel = mappingSource.GetModel(typeof(DataContext));
			var entityMetaType = metaModel.GetMetaType(typeof(TestEntity20));

			var foreignKeyMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "CreatorId");
			Assert.IsNotNull(foreignKeyMember);
			Assert.AreEqual("CreatorId", foreignKeyMember.MappedName);
			Assert.IsNull(foreignKeyMember.Association);
			Assert.AreEqual(AutoSync.Never, foreignKeyMember.AutoSync);
			Assert.IsTrue(foreignKeyMember.CanBeNull);
			Assert.AreEqual("int null", foreignKeyMember.DbType);
			Assert.IsNull(foreignKeyMember.Expression);
			Assert.IsFalse(foreignKeyMember.IsAssociation);
			Assert.IsFalse(foreignKeyMember.IsDbGenerated);
			Assert.IsFalse(foreignKeyMember.IsDeferred);
			Assert.IsFalse(foreignKeyMember.IsDiscriminator);
			Assert.IsTrue(foreignKeyMember.IsPersistent);
			Assert.IsFalse(foreignKeyMember.IsPrimaryKey);
			Assert.IsFalse(foreignKeyMember.IsVersion);
			Assert.AreEqual(typeof(int?), foreignKeyMember.Type);
			Assert.AreEqual(UpdateCheck.Always, foreignKeyMember.UpdateCheck);

			var associationMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Creator");
			Assert.IsNotNull(associationMember);
			Assert.IsNotNull(associationMember.MappedName);
			Assert.IsNotNull(associationMember.Association);
			Assert.AreEqual(AutoSync.Never, associationMember.AutoSync);
			Assert.IsTrue(associationMember.CanBeNull);
			Assert.IsNull(associationMember.DbType);
			Assert.IsNull(associationMember.Expression);
			Assert.IsTrue(associationMember.IsAssociation);
			Assert.IsFalse(associationMember.IsDbGenerated);
			Assert.IsTrue(associationMember.IsDeferred);
			Assert.IsFalse(associationMember.IsDiscriminator);
			Assert.IsTrue(associationMember.IsPersistent);
			Assert.IsFalse(associationMember.IsPrimaryKey);
			Assert.IsFalse(associationMember.IsVersion);
			Assert.AreEqual(typeof(TestEntity20), associationMember.Type);
			Assert.AreEqual(UpdateCheck.Never, associationMember.UpdateCheck);
			Assert.IsFalse(associationMember.Association.DeleteOnNull);
			Assert.IsNull(associationMember.Association.DeleteRule);
			Assert.IsTrue(associationMember.Association.IsForeignKey);
			Assert.IsFalse(associationMember.Association.IsMany);
			Assert.IsTrue(associationMember.Association.IsNullable);
			Assert.IsFalse(associationMember.Association.IsUnique);
			Assert.IsTrue(associationMember.Association.OtherKeyIsPrimaryKey);
			Assert.IsFalse(associationMember.Association.ThisKeyIsPrimaryKey);
			Assert.IsNotNull(associationMember.Association.OtherKey);
			Assert.AreEqual(1, associationMember.Association.OtherKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherKey[0].DeclaringType);
			Assert.AreEqual("Id", associationMember.Association.OtherKey[0].Name);
			Assert.IsNotNull(associationMember.Association.ThisKey);
			Assert.AreEqual(1, associationMember.Association.ThisKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.ThisKey[0].DeclaringType);
			Assert.AreEqual("CreatorId", associationMember.Association.ThisKey[0].Name);
			Assert.IsNull(associationMember.Association.OtherMember);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherType);
			Assert.AreEqual(associationMember, associationMember.Association.ThisMember);

			Assert.IsTrue(incompatibilityDetected);
		}

		[TestMethod]
		public void OptionalOneWayOneToManyAssociationViaBuilderWithoutDeferredLoading()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity21.TestEntity21Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaModel = mappingSource.GetModel(typeof(DataContext));
			var entityMetaType = metaModel.GetMetaType(typeof(TestEntity21));

			var foreignKeyMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "CreatorId");
			Assert.IsNotNull(foreignKeyMember);
			Assert.AreEqual("CreatorId", foreignKeyMember.MappedName);
			Assert.IsNull(foreignKeyMember.Association);
			Assert.AreEqual(AutoSync.Never, foreignKeyMember.AutoSync);
			Assert.IsTrue(foreignKeyMember.CanBeNull);
			Assert.AreEqual("int null", foreignKeyMember.DbType);
			Assert.IsNull(foreignKeyMember.Expression);
			Assert.IsFalse(foreignKeyMember.IsAssociation);
			Assert.IsFalse(foreignKeyMember.IsDbGenerated);
			Assert.IsFalse(foreignKeyMember.IsDeferred);
			Assert.IsFalse(foreignKeyMember.IsDiscriminator);
			Assert.IsTrue(foreignKeyMember.IsPersistent);
			Assert.IsFalse(foreignKeyMember.IsPrimaryKey);
			Assert.IsFalse(foreignKeyMember.IsVersion);
			Assert.AreEqual(typeof(int?), foreignKeyMember.Type);
			Assert.AreEqual(UpdateCheck.Always, foreignKeyMember.UpdateCheck);

			var associationMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Creator");
			Assert.IsNotNull(associationMember);
			Assert.IsNotNull(associationMember.MappedName);
			Assert.IsNotNull(associationMember.Association);
			Assert.AreEqual(AutoSync.Never, associationMember.AutoSync);
			Assert.IsTrue(associationMember.CanBeNull);
			Assert.IsNull(associationMember.DbType);
			Assert.IsNull(associationMember.Expression);
			Assert.IsTrue(associationMember.IsAssociation);
			Assert.IsFalse(associationMember.IsDbGenerated);
			Assert.IsFalse(associationMember.IsDiscriminator);
			Assert.IsTrue(associationMember.IsPersistent);
			Assert.IsFalse(associationMember.IsPrimaryKey);
			Assert.IsFalse(associationMember.IsVersion);
			Assert.AreEqual(typeof(TestEntity21), associationMember.Type);
			Assert.AreEqual(UpdateCheck.Never, associationMember.UpdateCheck);
			Assert.IsFalse(associationMember.Association.DeleteOnNull);
			Assert.IsNull(associationMember.Association.DeleteRule);
			Assert.IsTrue(associationMember.Association.IsForeignKey);
			Assert.IsFalse(associationMember.Association.IsMany);
			Assert.IsTrue(associationMember.Association.IsNullable);
			Assert.IsFalse(associationMember.Association.IsUnique);
			Assert.IsTrue(associationMember.Association.OtherKeyIsPrimaryKey);
			Assert.IsFalse(associationMember.Association.ThisKeyIsPrimaryKey);
			Assert.IsNotNull(associationMember.Association.OtherKey);
			Assert.AreEqual(1, associationMember.Association.OtherKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherKey[0].DeclaringType);
			Assert.AreEqual("Id", associationMember.Association.OtherKey[0].Name);
			Assert.IsNotNull(associationMember.Association.ThisKey);
			Assert.AreEqual(1, associationMember.Association.ThisKey.Count);
			Assert.AreEqual(entityMetaType, associationMember.Association.ThisKey[0].DeclaringType);
			Assert.AreEqual("CreatorId", associationMember.Association.ThisKey[0].Name);
			Assert.IsNull(associationMember.Association.OtherMember);
			Assert.AreEqual(entityMetaType, associationMember.Association.OtherType);
			Assert.AreEqual(associationMember, associationMember.Association.ThisMember);
		}

		[TestMethod]
		public void OptionalOneWayOneToManyAssociationViaBuilderDeferredLoading()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity21.TestEntity21Configuration());

			var mappingSource = new MindboxMappingSource(configuration);
			var metaModel = mappingSource.GetModel(typeof(DataContext));
			var entityMetaType = metaModel.GetMetaType(typeof(TestEntity21));

			var foreignKeyMember = entityMetaType
				.DataMembers
				.SingleOrDefault(aMetaDataMember => aMetaDataMember.Name == "Creator");
			Assert.IsNotNull(foreignKeyMember);
			Assert.IsTrue(foreignKeyMember.IsDeferred);
		}

		[TestMethod]
		public void OptionalOneWayOneToManyAssociationViaBuilderDeferredLoadingRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity22.TestEntity22Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity23.TestEntity23Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable23Command = new SqlCommand(
						"create table Test23 (Id int identity(1,1) not null primary key, Value int not null)",
						connection);
					createTable23Command.ExecuteNonQuery();

					var createTable22Command = new SqlCommand(
						"create table Test22 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int null foreign key references Test23 (Id))",
						connection);
					createTable22Command.ExecuteNonQuery();

					var insert23ACommand = new SqlCommand(
						"insert into Test23 (Value) values (6); select scope_identity()",
						connection);
					var id23A = Convert.ToInt32(insert23ACommand.ExecuteScalar());

					var insert23BCommand = new SqlCommand("insert into Test23 (Value) values (8)", connection);
					insert23BCommand.ExecuteNonQuery();

					var insert22Command = new SqlCommand("insert into Test22 (OtherId) values (@OtherId)", connection);
					insert22Command.Parameters.AddWithValue("OtherId", id23A);
					insert22Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item22 = context.GetTable<TestEntity22>().Single();
						var proxyType = item22.GetType();
						Assert.AreNotEqual(typeof(TestEntity22), proxyType);
						Assert.IsNotNull(item22.Other);

						Assert.AreEqual(6, item22.Other.Value);

						var newItem23 = context.GetTable<TestEntity23>().OrderByDescending(x => x.Id).First();
						item22.Other = newItem23;

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item22 = context.GetTable<TestEntity22>().Single();
						Assert.IsNotNull(item22.Other);

						Assert.AreEqual(8, item22.Other.Value);
					}
				});
		}

		[TestMethod]
		public void OptionalOneWayOneToManyAssociationViaBuilderDeferredLoadingRealDatabaseWasNull()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity22.TestEntity22Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity23.TestEntity23Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable23Command = new SqlCommand(
						"create table Test23 (Id int identity(1,1) not null primary key, Value int not null)",
						connection);
					createTable23Command.ExecuteNonQuery();

					var createTable22Command = new SqlCommand(
						"create table Test22 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int null foreign key references Test23 (Id))",
						connection);
					createTable22Command.ExecuteNonQuery();

					var insert23Command = new SqlCommand("insert into Test23 (Value) values (8)", connection);
					insert23Command.ExecuteNonQuery();

					var insert22Command = new SqlCommand("insert into Test22 (OtherId) values (null)", connection);
					insert22Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item22 = context.GetTable<TestEntity22>().Single();
						var proxyType = item22.GetType();
						Assert.AreNotEqual(typeof(TestEntity22), proxyType);
						Assert.IsNull(item22.Other);

						var newItem23 = context.GetTable<TestEntity23>().OrderByDescending(x => x.Id).First();
						item22.Other = newItem23;

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item22 = context.GetTable<TestEntity22>().Single();
						Assert.IsNotNull(item22.Other);

						Assert.AreEqual(8, item22.Other.Value);
					}
				});
		}

		[TestMethod]
		public void OptionalOneWayOneToManyAssociationViaBuilderDeferredLoadingRealDatabaseSetNull()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity22.TestEntity22Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity23.TestEntity23Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable23Command = new SqlCommand(
						"create table Test23 (Id int identity(1,1) not null primary key, Value int not null)",
						connection);
					createTable23Command.ExecuteNonQuery();

					var createTable22Command = new SqlCommand(
						"create table Test22 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int null foreign key references Test23 (Id))",
						connection);
					createTable22Command.ExecuteNonQuery();

					var insert23Command = new SqlCommand(
						"insert into Test23 (Value) values (6); select scope_identity()",
						connection);
					var id23 = Convert.ToInt32(insert23Command.ExecuteScalar());

					var insert22Command = new SqlCommand("insert into Test22 (OtherId) values (@OtherId)", connection);
					insert22Command.Parameters.AddWithValue("OtherId", id23);
					insert22Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item22 = context.GetTable<TestEntity22>().Single();
						var proxyType = item22.GetType();
						Assert.AreNotEqual(typeof(TestEntity22), proxyType);
						Assert.IsNotNull(item22.Other);

						Assert.AreEqual(6, item22.Other.Value);

						item22.Other = null;

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item22 = context.GetTable<TestEntity22>().Single();
						Assert.IsNull(item22.Other);
					}
				});
		}

		[TestMethod]
		public void ProxyInEntityRefDeferredLoadingRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity9.TestEntity9Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity10.TestEntity10Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity11.TestEntity11Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable10Command = new SqlCommand(
						"create table Test10 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTable10Command.ExecuteNonQuery();

					var createTable9Command = new SqlCommand(
						"create table Test9 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int not null foreign key references Test10 (Id))",
						connection);
					createTable9Command.ExecuteNonQuery();

					var createTable11Command = new SqlCommand(
						"create table Test11 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int not null foreign key references Test9 (Id))",
						connection);
					createTable11Command.ExecuteNonQuery();

					var insert10ACommand = new SqlCommand(
						"insert into Test10 (Value) values (0x1122334455); select scope_identity()",
						connection);
					var id10A = Convert.ToInt32(insert10ACommand.ExecuteScalar());

					var insert10BCommand = new SqlCommand(
						"insert into Test10 (Value) values (0x6655443322); select scope_identity()",
						connection);
					var id10B = Convert.ToInt32(insert10BCommand.ExecuteScalar());

					var insert9ACommand = new SqlCommand(
						"insert into Test9 (OtherId) values (@OtherId); select scope_identity()", 
						connection);
					insert9ACommand.Parameters.AddWithValue("OtherId", id10A);
					var id9A = Convert.ToInt32(insert9ACommand.ExecuteScalar());

					var insert9BCommand = new SqlCommand("insert into Test9 (OtherId) values (@OtherId)", connection);
					insert9BCommand.Parameters.AddWithValue("OtherId", id10B);
					insert9BCommand.ExecuteNonQuery();

					var insert11Command = new SqlCommand("insert into Test11 (OtherId) values (@OtherId)", connection);
					insert11Command.Parameters.AddWithValue("OtherId", id9A);
					insert11Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item11 = context.GetTable<TestEntity11>().Single();
						var item9 = item11.Other;
						Assert.IsNotNull(item9);
						var proxyType = item9.GetType();
						Assert.AreNotEqual(typeof(TestEntity9), proxyType);

						var item10 = item9.Other;
						Assert.IsNotNull(item10);

						Assert.AreEqual(5, item10.Value.Length);
						Assert.AreEqual(0x11, item10.Value[0]);
						Assert.AreEqual(0x22, item10.Value[1]);
						Assert.AreEqual(0x33, item10.Value[2]);
						Assert.AreEqual(0x44, item10.Value[3]);
						Assert.AreEqual(0x55, item10.Value[4]);

						var newItem9 = context.GetTable<TestEntity9>().OrderByDescending(x => x.Id).First();

						var newItem10 = newItem9.Other;
						Assert.IsNotNull(newItem10);

						Assert.AreEqual(5, newItem10.Value.Length);
						Assert.AreEqual(0x66, newItem10.Value[0]);
						Assert.AreEqual(0x55, newItem10.Value[1]);
						Assert.AreEqual(0x44, newItem10.Value[2]);
						Assert.AreEqual(0x33, newItem10.Value[3]);
						Assert.AreEqual(0x22, newItem10.Value[4]);

						item11.Other = newItem9;

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item11 = context.GetTable<TestEntity11>().Single();
						var item9 = item11.Other;
						Assert.IsNotNull(item9);
						var item10 = item9.Other;
						Assert.IsNotNull(item10);

						Assert.AreEqual(5, item10.Value.Length);
						Assert.AreEqual(0x66, item10.Value[0]);
						Assert.AreEqual(0x55, item10.Value[1]);
						Assert.AreEqual(0x44, item10.Value[2]);
						Assert.AreEqual(0x33, item10.Value[3]);
						Assert.AreEqual(0x22, item10.Value[4]);
					}
				});
		}

		[TestMethod]
		public void NewEntityInsteadOfProxyRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity9.TestEntity9Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity10.TestEntity10Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable10Command = new SqlCommand(
						"create table Test10 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTable10Command.ExecuteNonQuery();

					var createTable9Command = new SqlCommand(
						"create table Test9 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int not null foreign key references Test10 (Id))",
						connection);
					createTable9Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item9 = new TestEntity9
						{
							Other = new TestEntity10
							{
								Value = new byte[]
								{
									0x11,
									0x22,
									0x33,
									0x44,
									0x55
								}
							}
						};
						context.GetTable<TestEntity9>().InsertOnSubmit(item9);

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item9 = context.GetTable<TestEntity9>().Single();
						Assert.IsNotNull(item9.Other);

						Assert.AreEqual(5, item9.Other.Value.Length);
						Assert.AreEqual(0x11, item9.Other.Value[0]);
						Assert.AreEqual(0x22, item9.Other.Value[1]);
						Assert.AreEqual(0x33, item9.Other.Value[2]);
						Assert.AreEqual(0x44, item9.Other.Value[3]);
						Assert.AreEqual(0x55, item9.Other.Value[4]);
					}
				});
		}

		[TestMethod]
		public void VirtualStringPropertyIsNotDeferredViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity12));

			var member = (AttributedMetaDataMember)metaTable.RowType.PersistentDataMembers.Single(aMember => 
				aMember.Name == "Value");
			Assert.IsFalse(member.DoesRequireProxy);
		}

		[TestMethod]
		public void InterfaceImplementationStringPropertyIsNotDeferredViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity13));

			var member = (AttributedMetaDataMember)metaTable.RowType.PersistentDataMembers.Single(aMember =>
				aMember.Name == "Value");
			Assert.IsFalse(member.DoesRequireProxy);
		}

		[TestMethod]
		public void UnmappedTypeSelectionRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity10.TestEntity10Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable10Command = new SqlCommand(
						"create table Test10 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTable10Command.ExecuteNonQuery();

					var insert10Command = new SqlCommand(
						"insert into Test10 (Value) values (0x1122334455)",
						connection);
					insert10Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item10 = context
							.GetTable<TestEntity10>()
							.Select(entity => new
							{
								entity.Id,
								entity.Value
							})
							.Single();

						Assert.AreEqual(5, item10.Value.Length);
						Assert.AreEqual(0x11, item10.Value[0]);
						Assert.AreEqual(0x22, item10.Value[1]);
						Assert.AreEqual(0x33, item10.Value[2]);
						Assert.AreEqual(0x44, item10.Value[3]);
						Assert.AreEqual(0x55, item10.Value[4]);
					}
				});
		}

		[TestMethod]
		public void ProtectedSetterViaAttributeWithoutStorage()
		{
			var configuration = new MindboxMappingConfiguration();
			var mappingSource = new MindboxMappingSource(configuration);
			var metaTable = mappingSource.GetModel(typeof(DataContext)).GetTable(typeof(TestEntity14));

			var member = (AttributedMetaDataMember)metaTable.RowType.PersistentDataMembers.Single(aMember =>
				aMember.Name == "Id");
			Assert.IsTrue(member.IsPersistent);
		}

		[TestMethod]
		public void ProtectedSetterViaAttributeWithoutStorageRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable14Command = new SqlCommand(
						"create table TestEntity14 (Id int identity(1,1) not null primary key, Value int not null)",
						connection);
					createTable14Command.ExecuteNonQuery();

					var insert14Command = new SqlCommand(
						"insert into TestEntity14 (Value) values (1)",
						connection);
					insert14Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item14 = context
							.GetTable<TestEntity14>()
							.Single();

						Assert.AreNotEqual(default(int), item14.Id);
						Assert.AreEqual(1, item14.Value);
					}
				});
		}

		[TestMethod]
		public void PrivateSetterViaAttributeWithoutStorageRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable15Command = new SqlCommand(
						"create table TestEntity15 (Id int identity(1,1) not null primary key, Value int not null)",
						connection);
					createTable15Command.ExecuteNonQuery();

					var insert15Command = new SqlCommand(
						"insert into TestEntity15 (Value) values (1)",
						connection);
					insert15Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item15 = context
							.GetTable<TestEntity15>()
							.Single();

						Assert.AreNotEqual(default(int), item15.Id);
						Assert.AreEqual(1, item15.Value);
					}
				});
		}

		[TestMethod]
		public void CompletelyVirtualProxyHasChangeTrackingInterfacesRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity10.TestEntity10Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity16.TestEntity16Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable10Command = new SqlCommand(
						"create table Test10 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTable10Command.ExecuteNonQuery();

					var createTable16Command = new SqlCommand(
						"create table Test16 " +
							"(Id int identity(1,1) not null primary key, " +
							"OtherId int not null foreign key references Test10 (Id))",
						connection);
					createTable16Command.ExecuteNonQuery();

					var insert10Command = new SqlCommand(
						"insert into Test10 (Value) values (0x1122334455); select scope_identity()",
						connection);
					var id10 = Convert.ToInt32(insert10Command.ExecuteScalar());

					var insert16Command = new SqlCommand("insert into Test16 (OtherId) values (@OtherId)", connection);
					insert16Command.Parameters.AddWithValue("OtherId", id10);
					insert16Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item16 = context.GetTable<TestEntity16>().Single();
						Assert.IsInstanceOfType(item16, typeof(INotifyPropertyChanging));
						Assert.IsInstanceOfType(item16, typeof(INotifyPropertyChanged));

						var wasChangingNotified = false;
						var wasChangedNotified = false;

						var notifyPropertyChanging = (INotifyPropertyChanging)item16;
						notifyPropertyChanging.PropertyChanging += (sender, e) => wasChangingNotified = true;

						var notifyPropertyChanged = (INotifyPropertyChanged)item16;
						notifyPropertyChanged.PropertyChanged += (sender, e) => wasChangedNotified = true;

						item16.Id = -100;
						Assert.IsTrue(wasChangingNotified);
						Assert.IsTrue(wasChangedNotified);
					}
				});
		}

		[TestMethod]
		public void CompletelyVirtualClassWithoutChangeTrackingRequiresProxyRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity17.TestEntity17Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable17Command = new SqlCommand(
						"create table Test17 (Id int identity(1,1) not null primary key, Value binary(5) not null)",
						connection);
					createTable17Command.ExecuteNonQuery();

					var insert17Command = new SqlCommand(
						"insert into Test17 (Value) values (0x1122334455)",
						connection);
					insert17Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item17 = context.GetTable<TestEntity17>().Single();
						Assert.IsInstanceOfType(item17, typeof(INotifyPropertyChanging));
						Assert.IsInstanceOfType(item17, typeof(INotifyPropertyChanged));
					}
				});
		}

		[TestMethod]
		public void ProxyWorksOnManyEndOfOneToManyRelationshipRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
					var createTable19Command = new SqlCommand(
						"create table Test19 (Id int identity(1,1) not null primary key)",
						connection);
					createTable19Command.ExecuteNonQuery();

					var createTable18Command = new SqlCommand(
						"create table Test18 (" +
							"Id int identity(1,1) not null primary key, " +
							"OtherId int not null foreign key references Test19 (Id))",
						connection);
					createTable18Command.ExecuteNonQuery();

					var insert19Command = new SqlCommand(
						"insert into Test19 default values",
						connection);
					insert19Command.ExecuteNonQuery();
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item19 = context.GetTable<TestEntity19>().Single();
						Assert.IsInstanceOfType(item19, typeof(INotifyPropertyChanging));
						Assert.IsInstanceOfType(item19, typeof(INotifyPropertyChanged));

						new TestEntity18
						{
							Other = item19
						};

						context.SubmitChanges();
					}

					using (var context = dataContextFactory())
					{
						var item19 = context.GetTable<TestEntity19>().Single();
						Assert.AreEqual(1, item19.Others.Count);
					}
				});
		}

		[TestMethod]
		public void EntityRefSetsNullableForeignKeyRealDatabase()
		{
			var configuration = new MindboxMappingConfiguration();
			configuration.ModelBuilder.Configurations.Add(new TestEntity24.TestEntity24Configuration());
			configuration.ModelBuilder.Configurations.Add(new TestEntity23.TestEntity23Configuration());

			RunRealDatabaseTest(
				configuration,
				connection =>
				{
				},
				dataContextFactory =>
				{
					using (var context = dataContextFactory())
					{
						var item24 = context.CreateObject<TestEntity24>();
						var item23 = new TestEntity23
						{
							Id = 14,
							Value = 18
						};
						item24.Other = item23;
						Assert.AreEqual(item23, item24.Other);
						Assert.AreEqual(14, item24.OtherId);

						item24.Other = null;
						Assert.IsNull(item24.Other);
						Assert.IsNull(item24.OtherId);
					}
				});
		}


		private void RunRealDatabaseTest(
			MindboxMappingConfiguration configuration, 
			Action<SqlConnection> tableInitializer,
			Action<Func<DataContext>> body)
		{
			if (configuration == null)
				throw new ArgumentNullException("configuration");
			if (tableInitializer == null)
				throw new ArgumentNullException("tableInitializer");
			if (body == null)
				throw new ArgumentNullException("body");

			using (var masterConnection = new SqlConnection("Server=(local);Integrated Security=SSPI;Pooling=false"))
			{
				masterConnection.Open();

				var databaseName = "MindboxDataLinqTest_" + Guid.NewGuid().ToString("N");
				var createDatabaseCommand = new SqlCommand("create database [" + databaseName + "]", masterConnection);
				createDatabaseCommand.ExecuteNonQuery();
				try
				{
					var databaseConnectionString =
						"Server=(local);Integrated Security=SSPI;Database=" + databaseName + ";Pooling=false";

					using (var initializationConnection = new SqlConnection(databaseConnectionString))
					{
						initializationConnection.Open();
						tableInitializer(initializationConnection);
					}

					var mappingSource = new MindboxMappingSource(configuration);
					body(() => new DataContext(databaseConnectionString, mappingSource));
				}
				finally
				{
					var dropDatabaseCommand = new SqlCommand("drop database [" + databaseName + "]", masterConnection);
					dropDatabaseCommand.ExecuteNonQuery();
				}
			}
		}
	}
}
