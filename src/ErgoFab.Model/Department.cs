using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Department
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Organization))]

        public int OrganizationId { get; set; }

        public virtual Organization Organization { get; set; }

        [ForeignKey(nameof(Head))]
        public int? HeadId { get; set; }

        public virtual Employee Head { get; set; }

        [ForeignKey(nameof(Parent))]
        public int? ParentId { get; set; }

        public virtual Department Parent { get; set; }

        [InverseProperty(nameof(Employee.Department))]
        public virtual ICollection<Employee> Employees { get; set; }

        [Required]
        public string Name { get; set; }


    }
}