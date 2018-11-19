using System;
using System.Data.Linq;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Mindbox.Data.Linq.Tests.SqlGeneration
{
    [TestClass]
    public class DataContextTests
    {
        const string StatementsLabel = "someLabel";
        
        [TestMethod]
        public void Select_DataContextHasStatementsLabel_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            {
                using (var context = new DataContext(connection)
                {
                    StatementsLabel = StatementsLabel
                })
                {
                    var query = context.GetTable<SimpleEntity>()
                        .Where(t1 => context.GetTable<SimpleEntity>().Any(t2 => t2.X == t1.Id));
                    using (var command = context.GetCommand(query))
                    {
                        var expectedText = "SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
                                           $"/* {StatementsLabel} */"  + Environment.NewLine +
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
        }
        
        [TestMethod]
        public void Select_StatementsLabelNotNullFromHasSubSelect_StatementsLabelAddedToSqlTextOnce()
        {
            using (var connection = new DbConnectionStub())
            {
                using (var context = new DataContext(connection)
                {
                    StatementsLabel = StatementsLabel
                })
                {
                    var query = context.GetTable<SimpleEntity>()
                        .Select(t1 => context.GetTable<SimpleEntity>().FirstOrDefault(t2 => t2.X == t1.Id));
                    using (var command = context.GetCommand(query))
                    {
                        var expectedText = "SELECT [t0].[Id], [t0].[Discriminator], [t0].[X]" + Environment.NewLine +
                                           $"/* {StatementsLabel} */"  + Environment.NewLine +
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
        }
    }
}