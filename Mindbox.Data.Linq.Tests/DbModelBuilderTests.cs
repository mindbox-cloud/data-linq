using System;
using System.Collections.Generic;
using System.Data.Linq;
using System.Data.Linq.Mapping;
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
		public void BooleanColumnViaAttribute()
		{
			var configuration = new MindboxMappingConfiguration();
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
	}
}
