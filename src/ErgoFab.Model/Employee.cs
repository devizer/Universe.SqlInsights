using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        public int EmpId { get; set; }

        [ForeignKey(nameof(Organization))]
        [Required]
        public int OrganizationId { get; set; }

        public virtual Organization Organization { get; set; }

        [Required]
        [MaxLength(300)]
        public string Name { get; set; }

        [Required]
        [MaxLength(400)]
        public string SurName { get; set; }


        [ForeignKey(nameof(Department))]
        [Required]
        public int DepartmentId { get; set; }
        public Department Department { get; set; }

        [ForeignKey(nameof(Country))]
        public short? CountryId { get; set; }
        public Country Country { get; set; }


    }
}