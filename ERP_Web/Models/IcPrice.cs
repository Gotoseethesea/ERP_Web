using SqlSugar;

namespace ERP_Web.Models
{
    public class IcPrice
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]

        public int ID { get; set; }
        public string InvCode { get; set; }
        public decimal Quantity { set; get; } = 0;
        public decimal Price { set; get; } = 0;
        public decimal Amount { set; get; } = 0;
        public DateTime DateTime { get; set; } = DateTime.Now;

        public IcPrice()
        {

        }

    }
}
