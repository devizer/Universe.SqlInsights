using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        public string ProjectName { get; set; }

        [ForeignKey(nameof(Customer))]
        public int IdCustomer { get; set; }

        public virtual Customer Customer { get; set; }

        public virtual ICollection<Occupation> Employees { get; set; }

    }
}