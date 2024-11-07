using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Organization
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Director))]
        public int? DirectorId { get; set; }

        public Employee Director { get; set; }

        [InverseProperty("Organization")]
        public virtual ICollection<Department> Departments { get; set; }


        [InverseProperty(nameof(Employee.Organization))]
        public virtual ICollection<Employee> Employees { get; set; }

        [InverseProperty("ParentOrganization")]
        public virtual ICollection<RegionalDivision> SubDivisions { get; set; }


        [Required]
        public string Title { get; set; }

        [ForeignKey(nameof(Country))]
        public short? CountryId { get; set; }
        public Country Country { get; set; }


    }
}