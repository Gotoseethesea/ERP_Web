using SqlSugar;

namespace ERP_Web.Models
{
    //盘点表
    public class Stocktake
    {
        //var id=SnowFlakeSingle.Instance.NextId();//在程序中直接获取雪花ID
        //[SugarColumn(IsIdentity = true)]
        public long? ID { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; }
        public DateTime Date { set; get; } = DateTime.Now;
        public string WarehouseCode { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        public string? Explanation { set; get; }
        public decimal Quantity { set; get; } = 0;
        public decimal Amount { set; get; } = 0;
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public int? FiscalYear { set; get; }
        public int? Period { set; get; }
        public string? Operator { set; get; }
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;
        [Navigate(NavigateType.OneToMany, nameof(StocktakeDetail.StocktakeCode))]//一对多
        public List<StocktakeDetail>? StocktakeDetails { set; get; }

        //public Stocktake()
        //{
        //    Id = SnowFlakeSingle.Instance.NextId();
        //}
    }

    public class StocktakeDetail
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
        public string StocktakeCode { get; set; } //关联Stocktake.Code
        public string InvCode { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public IcInv Inv { set; get; } = new OneToOneInitializer<IcInv>();
        public decimal Quantity { set; get; } = 0;
        public decimal StockCount { get; set; } = 0;
        public decimal Price { set; get; } = 0;
        public decimal Amount { set; get; } = 0;
        //public decimal TaxRate { set; get; }
        //public decimal PriceIncTax { set; get; }
        //public decimal AmountIncTax { set; get; } = 0;
        public string? Note { set; get; }
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public int? FiscalYear { set; get; }
        public int? Period { set; get; }
        public string? Operator { set; get; }
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;
    }
}
