using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Organization
    {
        public int Id { get; set; }

        [ForeignKey(nameof(TheDirector))]
        public int? DirectorId { get; set; }

        public Employee TheDirector { get; set; }

        [InverseProperty(nameof(Department.TheOrganization))]
        public virtual ICollection<Department> TheDepartments { get; set; }


        [InverseProperty(nameof(Employee.TheOrganization))]
        public virtual ICollection<Employee> TheEmployees { get; set; }

        [InverseProperty(nameof(RegionalDivision.ParentOrganization))]
        public virtual ICollection<RegionalDivision> TheSubDivisions { get; set; }


        [Required]
        public string Title { get; set; }

        [ForeignKey(nameof(TheCountry))]
        public short? CountryId { get; set; }
        public Country TheCountry { get; set; }


    }
}