using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Department
    {
        public int Id { get; set; }

        [ForeignKey(nameof(TheOrganization))]

        public int OrganizationId { get; set; }

        public virtual Organization TheOrganization { get; set; }

        [ForeignKey(nameof(TheHead))]
        public int? HeadId { get; set; }

        public virtual Employee TheHead { get; set; }

        [ForeignKey(nameof(Parent))]
        public int? ParentId { get; set; }

        public virtual Department Parent { get; set; }

        [InverseProperty(nameof(Employee.TheDepartment))]
        public virtual ICollection<Employee> TheEmployees { get; set; }

        [Required]
        public string Name { get; set; }


    }
}