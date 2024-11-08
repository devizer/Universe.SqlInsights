using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ErgoFab.Model
{
    public class Expert
    {
        public int Id { get; set; }

        [MaxLength(5)] public string Title { get; set; }
        [MaxLength(300)] public string Name { get; set; }
        [MaxLength(400)] public string Surname { get; set; }

        // [InverseProperty(nameof(Industry.TheExperts))]
        public virtual ICollection<Industry> TheIndustries { get; set; }
    }

    public class Industry
    {
        public int Id { get; set; }

        [MaxLength(450)] public string Name { get; set; }
        [MaxLength(1024)] public string Description { get; set; }

        // [InverseProperty(nameof(Expert.TheIndustries))]
        public virtual ICollection<Expert> TheExperts { get; set; }
    }
}