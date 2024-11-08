using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Occupation
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Employee))]
        public int EmployeeId { get; set; }

        public virtual Employee Employee { get; set; }

        [ForeignKey(nameof(TheProject))]
        public int ProjectId { get; set; }

        public virtual Project TheProject { get; set; }

        public string Role { get; set; }
    }
}