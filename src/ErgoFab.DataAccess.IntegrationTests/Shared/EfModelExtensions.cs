using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Universe.GenericTreeTable;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public static class EfModelExtensions
{
    public static string BuildModelDescription(this IModel model)
    {
        IEnumerable<IEntityType> entityTypes = model.GetEntityTypes();
        ConsoleTable ct = new ConsoleTable("Entity Name", "ClrType", "Base Name", "Keys");
        foreach (IEntityType entityType in entityTypes)
        {
            var humanKeys = entityType.GetDeclaredKeys().Select(x => $"{x.GetKeyType()} {x.GetName()}");
            var humanProperties = entityType.GetProperties().Select(x => $"{x.ClrType}: {x.Name}");
            IProperty? organizationId1Property = entityType.GetProperties().FirstOrDefault(x => x.Name == "OrganizationId1");
            if (organizationId1Property != null && Debugger.IsAttached) Debugger.Break();
            ct.AddRow(
                entityType.Name,
                entityType.ClrType,
                entityType.BaseType?.Name,
                string.Join(", ", humanKeys)
            );
        }

        StringBuilder ret = new StringBuilder();
        ret.AppendLine(ct.ToString());

        string nl = Environment.NewLine;
        foreach (IEntityType entityType in entityTypes)
        {
            IProperty[] properties = entityType.GetProperties().ToArray();
            var max1stColumnLength = Math.Max(properties.Select(x => $"{FormatClrType(x.ClrType)}".Length).Max(), 1);
            Func<IProperty, string> twoColumns= x => string.Format("{0,-" + max1stColumnLength + "} → \"{1}\"", FormatClrType(x.ClrType), x.Name);
            var maxTwoColumnsLength = Math.Max(properties.Select(x => twoColumns(x).Length).Max(), 1);

            /*
            IProperty p = null;
            var firstPrincipal = p.GetPrincipals().FirstOrDefault();
            firstPrincipal.
            */


            var humanProperties = entityType
                .GetProperties()
                .Select(x => string.Format("  {0,-" + maxTwoColumnsLength + "} {1}", twoColumns(x), "| " + x));

            ret
                .AppendLine()
                .AppendLine()
                .AppendLine($"• Entity {entityType.FormatEntityTypeParentsChain()}")
                .AppendLine(string.Join(nl, humanProperties));
        }

        return ret.ToString();
    }

    public static IEnumerable<IEntityType> GetEntityTypeParentsChain(this IEntityType entityType)
    {
        while (entityType != null)
        {
            yield return entityType;
            entityType = entityType.BaseType;
        }
    }

    public static string FormatEntityTypeParentsChain(this IEntityType entityType, string separator = " ➛ ")
    {
        return string.Join(separator, entityType.GetEntityTypeParentsChain().Select(x => $"'{x.Name}'"));
    }


    static string FormatClrType(Type type)
    {
        var u = Nullable.GetUnderlyingType(type);
        if (u == null)
            return type.ToString();
        else return $"{u}?";
    }

    /*
        public static IEnumerable<IKey> GetDeepKeys(this IEntityType entityType)
        {
            List<IKey> ret = new List<IKey>();

            ret.AddRange(entityType.GetKeys());

        }
        */

}