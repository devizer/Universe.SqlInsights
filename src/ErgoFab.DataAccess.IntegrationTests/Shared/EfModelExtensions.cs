using System.Diagnostics;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Universe.GenericTreeTable;

namespace ErgoFab.DataAccess.IntegrationTests.Shared;

public static class EfModelExtensions
{
    public static string BuildModelDescription(this IModel model)
    {
        IEnumerable<IEntityType> entityTypes = model.GetEntityTypes().ToList();
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
        Func<IEntityType, string> entityOrderProperty = entityType =>
        {
            var arr = entityType.Name.Split('.');
            if (entityType.IsOwned() && arr.Length >= 2)
            {
                return arr[arr.Length - 2] + "." + arr.Last();
            }

            return arr.Last();
        };

        entityTypes = entityTypes.OrderBy(x => entityOrderProperty(x)).ToList();
        foreach (IEntityType entityType in entityTypes)
        {
            IProperty[] properties = entityType.GetProperties().ToArray();
            var max1stColumnLength = Math.Max(properties.Select(x => $"{FormatClrType(x.ClrType)}".Length).Max(), 1);
            Func<IProperty, string> twoColumns= x => string.Format("{0,-" + max1stColumnLength + "} → \"{1}\"", FormatClrType(x.ClrType), x.Name);
            var maxTwoColumnsLength = Math.Max(properties.Select(x => twoColumns(x).Length).Max(), 1);

            /*
            TODO: 1. Where FK Index point to?
                  2. Derivatives for entities
            */

            var principalOwnerName = entityType.FindOwnership()?.PrincipalEntityType?.Name;
            var humanForeignKeys = entityType.GetForeignKeys().Select(x => $"  {x}").ToList();

            Func<INavigation, string> navigationToString = navigation =>
            {
                var raw = Convert.ToString(navigation);
                var ret = raw.Replace(" ToDependent ", " ← Dependent ");
                ret = ret.Replace(" ToPrincipal ", " → Principal ");
                return ret;
            };
            var humanNavigations = entityType.GetNavigations().Select(x => $"  {navigationToString(x)}").ToList();

            var humanProperties = entityType
                .GetProperties()
                .Select(x => string.Format("  {0,-" + maxTwoColumnsLength + "} {1}", twoColumns(x), "| " + x))
                .ToList();

            var skipNav = entityType.GetSkipNavigations().Select(x => $"  {x}").ToList();

            ret
                .AppendLine()
                .AppendLine()
                .AppendLine($"• Entity {entityType.FormatEntityTypeParentsChain()}{(principalOwnerName == null ? "": $", owned by '{principalOwnerName}'")}, {entityType}");

            if (humanProperties.Count > 0) ret.AppendLine(string.Join(nl, humanProperties));
            if (humanNavigations.Count > 0) ret.AppendLine(string.Join(nl, humanNavigations));
            if (skipNav.Count > 0) ret.AppendLine(string.Join(nl, skipNav));
            if (humanForeignKeys.Count > 0) ret.AppendLine(string.Join(nl, humanForeignKeys));

            if (entityType.Name == "ErgoFab.Model.Expert" && Debugger.IsAttached) Debugger.Break();
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