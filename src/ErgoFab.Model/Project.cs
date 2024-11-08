using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        public string ProjectName { get; set; }
        public virtual Duration TheProjectDuration { get; set; }

        [ForeignKey(nameof(TheCustomer))]
        public int IdCustomer { get; set; }

        public virtual Customer TheCustomer { get; set; }

        public virtual ICollection<Occupation> Employees { get; set; }

    }


}