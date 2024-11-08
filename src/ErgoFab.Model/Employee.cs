using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    [Table("Employee")]
    public class Employee
    {
        [Key]
        public int EmpId { get; set; }

        [ForeignKey(nameof(TheOrganization))]
        [Required]
        public int OrganizationId { get; set; }

        public virtual Organization TheOrganization { get; set; }

        [Required]
        [MaxLength(300)]
        public string Name { get; set; }

        [Required]
        [MaxLength(400)]
        public string SurName { get; set; }

        public virtual Duration TheEnrollment { get; set; }


        [ForeignKey(nameof(TheDepartment))]
        [Required]
        public int DepartmentId { get; set; }
        public Department TheDepartment { get; set; }

        [ForeignKey(nameof(TheCountry))]
        public short? CountryId { get; set; }
        public Country TheCountry { get; set; }


    }
}