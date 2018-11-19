using System;
using System.Data.Linq;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
    [TestClass]
    public class DataContextTests
    {
        private const string StatementsLabel = "someLabel";

        [TestMethod]
        public void Select_DataContextHasStatementsLabel_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = StatementsLabel})
            {
                var query = context.GetTable<SimpleEntity>()
                    .Where(t1 => context.GetTable<SimpleEntity>().Any(t2 => t2.X == t1.Id));
                using (var command = context.GetCommand(query))
                {
                    var expectedText = "SELECT " + Environment.NewLine +
                                       $"-- {StatementsLabel} --" + Environment.NewLine +
                                       "[t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
                                       "FROM [SimpleTable] AS [t0]" + Environment.NewLine +
                                       "WHERE EXISTS(" + Environment.NewLine +
                                       "    SELECT NULL AS [EMPTY]" + Environment.NewLine +
                                       "    FROM [SimpleTable] AS [t1]" + Environment.NewLine +
                                       "    WHERE [t1].[X] = [t0].[Id]" + Environment.NewLine +
                                       "    )";
                    Assert.AreEqual(expectedText, command.CommandText);
                }
            }
        }

        [TestMethod]
        public void Select_StatementsLabelNotNullFromHasSubSelect_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = StatementsLabel})
            {
                var query = context.GetTable<SimpleEntity>()
                    .Select(t1 => context.GetTable<SimpleEntity>().FirstOrDefault(t2 => t2.X == t1.Id));
                using (var command = context.GetCommand(query))
                {
                    var expectedText =
                        "SELECT " + Environment.NewLine +
                        $"-- {StatementsLabel} --" + Environment.NewLine +
                        "[t2].[test], [t2].[Id], [t2].[Discriminator], [t2].[X]" + Environment.NewLine +
                        "FROM [SimpleTable] AS [t0]" + Environment.NewLine +
                        "OUTER APPLY (" + Environment.NewLine +
                        "    SELECT TOP (1) 1 AS [test], [t1].[Id], [t1].[Discriminator], [t1].[X]" +
                        Environment.NewLine +
                        "    FROM [SimpleTable] AS [t1]" + Environment.NewLine +
                        "    WHERE [t1].[X] = [t0].[Id]" + Environment.NewLine +
                        "    ) AS [t2]";

                    Assert.AreEqual(expectedText, command.CommandText);
                }
            }
        }

        [TestMethod]
        public void Insert_StatementsLabelNotNull_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = StatementsLabel})
            {
                context.GetTable<SimpleEntity>().InsertOnSubmit(new SimpleEntity());

                var expectedText = "INSERT INTO " + Environment.NewLine +
                                   $"-- {StatementsLabel} --" + Environment.NewLine +
                                   "[SimpleTable]([Id], [Discriminator], [X])" + Environment.NewLine +
                                   "VALUES (@p0, @p1, @p2)" + Environment.NewLine +
                                   "-- @p0: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [0]" +
                                   Environment.NewLine +
                                   "-- @p1: Input AnsiString (Size = 4000; Prec = 0; Scale = 0) []" +
                                   Environment.NewLine +
                                   "-- @p2: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [0]";
                var commandText = context.GetChangeText();

                Assert.IsTrue(commandText.StartsWith(expectedText));
            }
        }

        [TestMethod]
        public void Delete_StatementsLabelNotNull_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = StatementsLabel})
            {
                var table = context.GetTable<SimpleEntity>();
                var entity = new SimpleEntity();

                table.Attach(entity);
                table.DeleteOnSubmit(entity);

                var expectedText =
                    "DELETE FROM " + Environment.NewLine +
                    $"-- {StatementsLabel} --" + Environment.NewLine +
                    "[SimpleTable] WHERE ([Id] = @p0) AND ([Discriminator] IS NULL) AND ([X] = @p1)" +
                    Environment.NewLine +
                    "-- @p0: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [0]" + Environment.NewLine +
                    "-- @p1: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [0]";
                var commandText = context.GetChangeText();

                Assert.IsTrue(commandText.StartsWith(expectedText));
            }
        }

        [TestMethod]
        public void Update_StatementsLabelNotNull_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            using (var context = new DataContext(connection) {StatementsLabel = StatementsLabel})
            {
                var table = context.GetTable<SimpleEntity>();
                var entity = new SimpleEntity();

                table.Attach(entity);
                entity.X = 10;

                var expectedText =
                    "UPDATE " + Environment.NewLine +
                    $"-- {StatementsLabel} --" + Environment.NewLine +
                    "[SimpleTable]" + Environment.NewLine +
                    "SET [X] = @p2" + Environment.NewLine +
                    "WHERE ([Id] = @p0) AND ([Discriminator] IS NULL) AND ([X] = @p1)" + Environment.NewLine +
                    "-- @p0: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [0]" + Environment.NewLine +
                    "-- @p1: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [0]" + Environment.NewLine +
                    "-- @p2: Input AnsiString (Size = -1; Prec = 0; Scale = 0) [10]";
                var commandText = context.GetChangeText();

                Assert.IsTrue(commandText.StartsWith(expectedText));
            }
        }
    }
}