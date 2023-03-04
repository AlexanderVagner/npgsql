using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Npgsql.Internal.TypeHandlers;
using Npgsql.Internal.TypeHandling;
using Npgsql.PostgresTypes;
using NpgsqlTypes;

namespace Npgsql.Internal.TypeMapping;

sealed class UserEnumTypeMapping<TEnum> : IUserTypeMapping
    where TEnum : struct, Enum
{
    public string PgTypeName { get; }
    public Type ClrType => typeof(TEnum);

    readonly Dictionary<TEnum, string> _enumToLabel = new();
    readonly Dictionary<string, TEnum> _labelToEnum = new();

    public UserEnumTypeMapping(string pgTypeName, INpgsqlNameTranslator nameTranslator)
    {
        PgTypeName = pgTypeName;

        foreach (var field in typeof(TEnum).GetFields(BindingFlags.Static | BindingFlags.Public))
        {
            var attribute = (PgNameAttribute?)field.GetCustomAttributes(typeof(PgNameAttribute), false).FirstOrDefault();
            var enumName = attribute is null
                ? nameTranslator.TranslateMemberName(field.Name)
                : attribute.PgName;
            var enumValue = (TEnum)field.GetValue(null)!;

            _enumToLabel[enumValue] = enumName;
            _labelToEnum[enumName] = enumValue;
        }
    }

    public NpgsqlTypeHandler CreateHandler(PostgresType postgresType, NpgsqlConnector connector)
        => new EnumHandler<TEnum>((PostgresEnumType)postgresType, _enumToLabel, _labelToEnum);
}