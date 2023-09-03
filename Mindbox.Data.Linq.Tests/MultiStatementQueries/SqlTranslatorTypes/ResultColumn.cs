using System;

namespace Mindbox.Data.Linq.Tests.MultiStatementQueries.SqlTranslatorTypes;

class ResultColumn
{
    public Type ValueType { get; private set; }
    public string Name { get; private set; }

    public ResultColumn(string name, Type valueType)
    {
        Name = name;
        ValueType = valueType;
    }

    public static bool IsFloatNumeric(Type type) => type == typeof(decimal);

    public static bool IsIntegerNumeric(Type type) => type == typeof(int) || type == typeof(long);
}
