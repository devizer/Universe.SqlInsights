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

    /*
    public class OrderDetail
    {
        public Guid ObjectID { get; set; }
        public DateTime Created { get; set; }
        public string Comments { get; set; }

        [ForeignKey("Order")]
        public Guid OrderID { get; set; }
        public Order Order { get; set; }

    }

    public class Order
    {
        public Guid ObjectID { get; set; }

        public Order PrevOrder { get; set; }
        [ForeignKey("PrevOrder")]
        public Guid? PrevOrderId { get; set; }

        [InverseProperty("Order")]
        public List<OrderDetail> OrderDetails { get; set; }
    }
    */

}