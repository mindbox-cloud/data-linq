using System;
using System.Data.Linq;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
    [TestClass]
    public class DataContextStatementsLabelTests
    {
        private const string Label = "some statement label";
        private const string LabelWithCommentSymbols = "-- " + Label + " --";

        [TestMethod]
        public void SelectWithSubSelectInWhereClause_LabelIsNotNull_LabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = Label})
            {
                var query = context.GetTable<SimpleEntity>()
                    .Where(t1 => context.GetTable<SimpleEntity>().Any(t2 => t2.X == t1.Id));
                using (var command = context.GetCommand(query))
                {
                    var expectedFirstPart = 
                        "SELECT " + Environment.NewLine + $"{LabelWithCommentSymbols}" + Environment.NewLine;
                    
                    Assert.IsTrue(command.CommandText.StartsWith(expectedFirstPart), command.CommandText);
                    
                    AssertThatLabelPresentedInCommandTextOnce(command.CommandText);
                }
            }
        }

        [TestMethod]
        public void SelectWithSubSelectInFromClause_LabelIsNotNull_LabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = Label})
            {
                var query = context.GetTable<SimpleEntity>()
                    .Select(t1 => context.GetTable<SimpleEntity>().FirstOrDefault(t2 => t2.X == t1.Id));
                using (var command = context.GetCommand(query))
                {
                    var expectedFirstPart = 
                        "SELECT " + Environment.NewLine + $"{LabelWithCommentSymbols}" + Environment.NewLine;
                    
                    Assert.IsTrue(command.CommandText.StartsWith(expectedFirstPart), command.CommandText);
                    
                    AssertThatLabelPresentedInCommandTextOnce(command.CommandText);
                }
            }
        }

        [TestMethod]
        public void Select_LabelIsNotNull_LabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = Label})
            {
                var query = context.GetTable<SimpleEntity>().Where(t => t.Id == 1);
                using (var command = context.GetCommand(query))
                {
                    var expectedFirstPart = 
                        "SELECT " + Environment.NewLine + $"{LabelWithCommentSymbols}" + Environment.NewLine;
                    
                    Assert.IsTrue(command.CommandText.StartsWith(expectedFirstPart), command.CommandText);
                    
                    AssertThatLabelPresentedInCommandTextOnce(command.CommandText);
                }
            }
        }
        
        [TestMethod]
        public void Select_LabelIsNull_NoLabelComment()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = null})
            {
                var query = context.GetTable<SimpleEntity>().Where(t => t.Id == 1);
                using (var command = context.GetCommand(query))
                {
                    var expectedFirstPart =
                        "SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine;
                    
                    Assert.IsTrue(command.CommandText.StartsWith(expectedFirstPart), command.CommandText);
                }
            }
        }

        [TestMethod]
        public void Insert_LabelIsNotNull_LabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = Label})
            {
                context.GetTable<SimpleEntity>().InsertOnSubmit(new SimpleEntity());
                
                var commandText = context.GetChangeText();
                
                var expectedFirstPart = 
                    "INSERT INTO " + Environment.NewLine + $"{LabelWithCommentSymbols}" + Environment.NewLine;
                    
                Assert.IsTrue(commandText.StartsWith(expectedFirstPart), commandText);
                    
                AssertThatLabelPresentedInCommandTextOnce(commandText);
            }
        }
        
        [TestMethod]
        public void Insert_LabelIsNull_NoLabelComment()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = null})
            {
                context.GetTable<SimpleEntity>().InsertOnSubmit(new SimpleEntity());
                
                var commandText = context.GetChangeText();

                var expectedFirstPart =
                    "INSERT INTO [SimpleTable]([Id], [Discriminator], [X])"
                    + Environment.NewLine
                    + "VALUES (@p0, @p1, @p2)";
                    
                Assert.IsTrue(commandText.StartsWith(expectedFirstPart), commandText);
            }
        }

        [TestMethod]
        public void Delete_LabelIsNotNull_LabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = Label})
            {
                var table = context.GetTable<SimpleEntity>();
                var entity = new SimpleEntity();

                table.Attach(entity);
                table.DeleteOnSubmit(entity);
                
                var commandText = context.GetChangeText();
                
                var expectedFirstPart = 
                    "DELETE FROM " + Environment.NewLine + $"{LabelWithCommentSymbols}" + Environment.NewLine;
                    
                Assert.IsTrue(commandText.StartsWith(expectedFirstPart), commandText);
                    
                AssertThatLabelPresentedInCommandTextOnce(commandText);
            }
        }
        
        [TestMethod]
        public void Delete_LabelIsNull_NoLabelComment()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = null})
            {
                var table = context.GetTable<SimpleEntity>();
                var entity = new SimpleEntity();

                table.Attach(entity);
                table.DeleteOnSubmit(entity);
                
                var commandText = context.GetChangeText();

                var expectedFirstPart = "DELETE FROM [SimpleTable]" + Environment.NewLine;
                    
                Assert.IsTrue(commandText.StartsWith(expectedFirstPart), commandText);
            }
        }

        [TestMethod]
        public void Update_LabelIsNotNull_LabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = Label})
            {
                var table = context.GetTable<SimpleEntity>();
                var entity = new SimpleEntity();

                table.Attach(entity);
                entity.X = 10;
                
                var commandText = context.GetChangeText();
                
                var expectedFirstPart = 
                    "UPDATE " + Environment.NewLine + $"{LabelWithCommentSymbols}" + Environment.NewLine;
                    
                Assert.IsTrue(commandText.StartsWith(expectedFirstPart), commandText);
                    
                AssertThatLabelPresentedInCommandTextOnce(commandText);
            }
        }
        
        [TestMethod]
        public void Update_LabelIsNull_NoLabelComment()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = null})
            {
                var table = context.GetTable<SimpleEntity>();
                var entity = new SimpleEntity();

                table.Attach(entity);
                entity.X = 10;
                
                var commandText = context.GetChangeText();
                
                var expectedFirstPart = 
                    "UPDATE [SimpleTable]" + Environment.NewLine;
                    
                Assert.IsTrue(commandText.StartsWith(expectedFirstPart), commandText);
            }
        }

        private void AssertThatLabelPresentedInCommandTextOnce(string commandText)
        {
            var occurrencesCount = Regex.Matches(commandText, Label).Count;

            Assert.AreEqual(1, occurrencesCount, commandText);
        }
    }
}