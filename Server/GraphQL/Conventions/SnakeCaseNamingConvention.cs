// Custom HotChocolate naming convention that converts member names to snake_case for the GraphQL schema.
using System.Reflection;
using System.Text.RegularExpressions;
using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Descriptors;

namespace Server.GraphQL.Conventions;

public class SnakeCaseNamingConvention : DefaultNamingConventions
{
    public override string GetMemberName(MemberInfo member, MemberKind kind)
    {
        var camel = base.GetMemberName(member, kind);
        return ToSnakeCase(camel);
    }

    public override string GetArgumentName(ParameterInfo parameter)
    {
        var camel = base.GetArgumentName(parameter);
        return ToSnakeCase(camel);
    }

    public override string GetEnumValueName(object value)
    {
        return base.GetEnumValueName(value).ToUpper();
    }

    public override string GetTypeName(Type type, TypeKind kind)
    {
        var attr = type.GetCustomAttribute<GraphQLNameAttribute>();
        if (attr is not null)
            return attr.Name;

        return type.IsGenericType
            ? type.Name[..type.Name.IndexOf('`')]
            : type.Name;
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return Regex.Replace(input, "([A-Z])", "_$1")
                    .ToLower()
                    .TrimStart('_');
    }
}
