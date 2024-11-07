using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model;

[Table("RegionalDivision")]
public class RegionalDivision : Organization
{
    [ForeignKey(nameof(ParentOrganization))]
    public int IdParent { get; set; }

    public Organization ParentOrganization { get; set; }

    public string RegionDescription { get; set; }

}