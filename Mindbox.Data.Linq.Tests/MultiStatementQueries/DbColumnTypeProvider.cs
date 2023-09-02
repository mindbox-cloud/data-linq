using Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;
using System;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries;

public class DbColumnTypeProvider : IDbColumnTypeProvider
{
    public string[] GetPKFields(string tableName)
    {
        return tableName switch
        {
            "directcrm.Customers" => new[] { "Id" },
            "directcrm.CustomerActions" => new[] { "Id" },
            "directcrm.CustomerCustomFieldValues" => new[] { "Id" },
            "directcrm.CustomerActionCustomFieldValues" => new[] { "Id" },
            "directcrm.Areas" => new[] { "Id" },
            "directcrm.ActionTemplates" => new[] { "Id" },
            "directcrm.SubAreas" => new[] { "Id" },
            "directcrm.RetailOrders" => new[] { "Id" },
            "directcrm.RetailOrderHistoryItems" => new[] { "Id" },
            "directcrm.RetailOrderPurchases" => new[] { "Id" },
            _ => throw new NotSupportedException()
        };
    }

    public string GetSqlType(string tableName, string columnName)
    {
        return (tableName, columnName) switch
        {
            ("directcrm.Customers", "Id") => "int not null",
            ("directcrm.Customers", "PasswordHash") => "nvarchar(32) not null",
            ("directcrm.Customers", "PasswordHashSalt") => "varbinary(16) null",
            ("directcrm.Customers", "TempPasswordHash") => "nvarchar(32) not null",
            ("directcrm.Customers", "TempPasswordHashSalt") => "varbinary(16) null",
            ("directcrm.Customers", "IsDeleted") => "bit not null",
            ("directcrm.Customers", "TempPasswordEmail") => "nvarchar(256) not null",
            ("directcrm.Customers", "TempPasswordMobilePhone") => "bigint null",
            ("directcrm.Customers", "AreaId") => "int not null",
            ("directcrm.CustomerActions", "Id") => "bigint not null",
            ("directcrm.CustomerActions", "DateTimeUtc") => "datetime2(7) not null",
            ("directcrm.CustomerActions", "CreationDateTimeUtc") => "datetime2(7) not null",
            ("directcrm.CustomerActions", "PointOfContactId") => "int not null",
            ("directcrm.CustomerActions", "ActionTemplateId") => "int not null",
            ("directcrm.CustomerActions", "CustomerId") => "int not null",
            ("directcrm.CustomerActions", "StaffId") => "int null",
            ("directcrm.CustomerActions", "OriginalCustomerId") => "int not null",
            ("directcrm.CustomerActions", "TransactionalId") => "bigint null",
            ("directcrm.CustomerActionCustomFieldValues", "Id") => "int not null",
            ("directcrm.CustomerActionCustomFieldValues", "CustomerActionId") => "bigint not null",
            ("directcrm.CustomerActionCustomFieldValues", "FieldName") => "nvarchar(32) not null",
            ("directcrm.CustomerActionCustomFieldValues", "FieldValue") => "nvarchar(32) not null",
            ("directcrm.CustomerCustomFieldValues", "Id") => "int not null",
            ("directcrm.CustomerCustomFieldValues", "CustomerId") => "int not null",
            ("directcrm.CustomerCustomFieldValues", "FieldName") => "nvarchar(32) not null",
            ("directcrm.CustomerCustomFieldValues", "FieldValue") => "nvarchar(32) not null",
            ("directcrm.Areas", "Id") => "int not null",
            ("directcrm.Areas", "Name") => "nvarchar(32) not null",
            ("directcrm.Areas", "SubAreaId") => "int null",
            ("directcrm.ActionTemplates", "Id") => "int not null",
            ("directcrm.ActionTemplates", "Name") => "nvarchar(32) not null",
            ("directcrm.SubAreas", "Id") => "int not null",
            ("directcrm.SubAreas", "Name") => "nvarchar(64) not null",
            ("directcrm.RetailOrders", "Id") => "int not null",
            ("directcrm.RetailOrders", "CustomerId") => "int null",
            ("directcrm.RetailOrders", "TotalSum") => "float not null",
            ("directcrm.RetailOrderHistoryItems", "Id") => "int not null",
            ("directcrm.RetailOrderHistoryItems", "IsCurrentOtherwiseNull") => "bit null",
            ("directcrm.RetailOrderHistoryItems", "RetailOrderId") => "int not null",
            ("directcrm.RetailOrderHistoryItems", "Amount") => "decimal(18,2) null",
            ("directcrm.RetailOrderPurchases", "Count") => "decimal(18,2) not null",
            ("directcrm.RetailOrderPurchases", "PriceForCustomerOfLine") => "decimal(18,2) not null",
            ("directcrm.RetailOrderPurchases", "RetailOrderHistoryItemId") => "bigint not null",
            _ => throw new NotSupportedException()
        };
    }
}
