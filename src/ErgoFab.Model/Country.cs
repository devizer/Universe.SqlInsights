using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ErgoFab.Model
{
    public class Country
    {
        public short Id { get; set; }

        public string LocalName { get; set; }

        public string EnglishName { get; set; }

        public byte[]? Flag { get; set; }

    }


}