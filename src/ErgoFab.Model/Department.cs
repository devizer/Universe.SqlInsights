using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Department
    {
        public int Id { get; set; }

        [ForeignKey("Organization")]

        public int OrganizationId { get; set; }

        public virtual Organization Organization { get; set; }

        [ForeignKey("Head")]
        public int? HeadId { get; set; }

        public virtual Employee Head { get; set; }

        [ForeignKey("Parent")]
        public int? ParentId { get; set; }

        public virtual Department Parent { get; set; }

        [InverseProperty("Department")]
        public virtual ICollection<Employee> Employees { get; set; }

        [Required]
        public string Name { get; set; }


    }
}