using ERP_Web.Repository;
using ERP_Web.Core.Extensions;
using Org.BouncyCastle.Crypto;
using SqlSugar;
using System.ComponentModel;
using System.Transactions;
using static ERP_Web.Components.Pages.Temp___复制;

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
        public StocktakeStatus? Status { set; get; }
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
        public Stocktake()
        {
        }
        #region 构造函数（自动初始化默认值）
        public Stocktake(string str)
        {
            ID = SnowFlakeSingle.Instance.NextId();
            Active = true;
            Status = StocktakeStatus.Draft;
            LastUpdateTime = DateTime.Now;
        }
        #endregion

        #region 静态查询方法
        /// <summary>
        /// 查询所有有效盘点单
        /// </summary>
        public static List<Stocktake> Select()
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Stocktake>()
                .IncludesAllFirstLayer()
                .Includes(x => x.StocktakeDetails)
                .Where(x => x.Active == true)
                .OrderByDescending(x => x.Date)
                .ToList();
        }
        /// <summary>
        /// 按盘点单号查询单个盘点单（包含所有明细）
        /// </summary>
        public static Stocktake Select(string code)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Stocktake>()
                .IncludesAllFirstLayer()
                .Includes(x => x.StocktakeDetails, d => d.Inv)
                .Includes(x => x.StocktakeDetails, d => d.Specification)
                .Where(x => x.Code == code && x.Active == true)
                .First();
        }

        /// <summary>
        /// 按仓库+年度+期间查询盘点单（用于校验重复创建）
        /// </summary>
        public static bool ExistsByWarehousePeriod(string warehouseCode, int fiscalYear, int period)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Stocktake>()
                .Any(x => x.WarehouseCode == warehouseCode
                       && x.FiscalYear == fiscalYear
                       && x.Period == period
                       && x.Active == true);
        }

        /// <summary>
        /// 按状态查询盘点单
        /// </summary>
        public static List<Stocktake> SelectByStatus(StocktakeStatus status)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Stocktake>()
                .IncludesAllFirstLayer()
                .Where(x => x.Status == status && x.Active == true)
                .OrderByDescending(x => x.Date)
                .ToList();
        }

        /// <summary>
        /// 分页查询盘点单
        /// </summary>
        public static List<Stocktake> SelectPage(int pageIndex, int pageSize, out int total)
        {
            SqlClient SSC = new SqlClient();
            total = 0;
            return SSC.Db.Queryable<Stocktake>()
                .IncludesAllFirstLayer()
                .Where(x => x.Active == true)
                .OrderByDescending(x => x.Date)
                .ToPageList(pageIndex, pageSize, ref total);
        }
        #endregion

        #region 实例增删改方法
        /// <summary>
        /// 插入盘点单（包含明细，自动校验同仓库同期间重复）
        /// </summary>
        public void Insert()
        {
            // 前置校验：同仓库同期间不能有重复有效盘点单
            if (ExistsByWarehousePeriod(WarehouseCode, FiscalYear.Value, Period.Value))
            {
                throw new Exception($"仓库[{WarehouseCode}] {FiscalYear}年{Period}月已存在有效盘点单，请勿重复创建");
            }

            SqlClient SSC = new SqlClient();
            this.LastUpdateTime = DateTime.Now;
            // 导航插入，自动保存明细
            SSC.Db.InsertNav(this)
                .Include(z => z.StocktakeDetails)
                .ExecuteCommand();
        }

        /// <summary>
        /// 更新盘点单（包含明细）
        /// </summary>
        public void Update()
        {
            SqlClient SSC = new SqlClient();
            this.LastUpdateTime = DateTime.Now;
            // 导航更新，自动更新明细
            SSC.Db.UpdateNav(this)
                .Include(z => z.StocktakeDetails)
                .ExecuteCommand();
        }

        /// <summary>
        /// 软删除盘点单（同时删除关联明细）
        /// </summary>
        public void Delete()
        {
            using var scope = new TransactionScope();
            SqlClient SSC = new SqlClient();

            // 软删除主单
            this.Active = false;
            this.LastUpdateTime = DateTime.Now;
            SSC.Db.Updateable(this)
                .UpdateColumns(x => new { x.Active, x.LastUpdateTime })
                .ExecuteCommand();

            // 软删除关联明细
            if (StocktakeDetails != null)
            {
                SSC.Db.Updateable<StocktakeDetail>()
                    .UpdateColumns(x => x.Active, false)
                    .UpdateColumns(yy => yy.LastUpdateTime)
                    .Where(x => x.StocktakeCode == this.Code)
                    .ExecuteCommand();
            }

            scope.Complete();
        }
        #endregion

        #region 业务统计属性/方法（页面直接绑定用）


        /// <summary>
        /// 状态中文描述（页面直接绑定用）
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public string StatusText => Status.HasValue ? Status.Value.GetDescription() : "未知状态";
        /// <summary>
        /// 账面总数量
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal BookQuantity => GetBookQuantity();
        /// <summary>
        /// 实盘总数量
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal ActualQuantity => GetActualQuantity();
        /// <summary>
        /// 盘盈总数量
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal ProfitQuantity => GetProfitQuantity();
        /// <summary>
        /// 盘亏总数量
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal LossQuantity => GetLossQuantity();
        /// <summary>
        /// 盘盈总金额
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal ProfitAmount => GetProfitAmount();
        /// <summary>
        /// 盘亏总金额
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal LossAmount => GetLossAmount();
        /// <summary>
        /// 账实是否完全一致
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public bool IsAllMatch => ProfitQuantity == 0 && LossQuantity == 0;

        private decimal GetBookQuantity()
        {
            return StocktakeDetails?.Where(d => d.Active == true).Sum(d => d.Quantity) ?? 0;
        }

        private decimal GetActualQuantity()
        {
            return StocktakeDetails?.Where(d => d.Active == true).Sum(d => d.StockCount) ?? 0;
        }

        private decimal GetProfitQuantity()
        {
            return StocktakeDetails?.Where(d => d.Active == true).Sum(d => Math.Max(d.StockCount - d.Quantity, 0)) ?? 0;
        }

        private decimal GetLossQuantity()
        {
            return StocktakeDetails?.Where(d => d.Active == true).Sum(d => Math.Max(d.Quantity - d.StockCount, 0)) ?? 0;
        }

        private decimal GetProfitAmount()
        {
            return StocktakeDetails?.Where(d => d.Active == true).Sum(d => Math.Max((d.StockCount - d.Quantity) * d.Price, 0)) ?? 0;
        }

        private decimal GetLossAmount()
        {
            return StocktakeDetails?.Where(d => d.Active == true).Sum(d => Math.Max((d.Quantity - d.StockCount) * d.Price, 0)) ?? 0;
        }
        #endregion
    }

    // 枚举
    public enum StocktakeStatus
    {
        [Description("草稿")]
        Draft,
        [Description("已盘点")]
        Counted,
        [Description("已生成调整单")]
        Adjusted
    }

    public class StocktakeDetail
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
        public string StocktakeCode { get; set; } //关联Stocktake.Code
        public string InvCode { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public InventoryItem Inv { set; get; } = new OneToOneInitializer<InventoryItem>();
        public string SKU { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(SKU))]//一对一
        public Specification Specification { set; get; } = new OneToOneInitializer<Specification>();
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


        #region 构造函数
        public StocktakeDetail()
        { }


        public StocktakeDetail(string str)
        {
            Id = SnowFlakeSingle.Instance.NextId();
            Active = true;
            LastUpdateTime = DateTime.Now;
        }
        #endregion

        #region 静态查询方法
        /// <summary>
        /// 按盘点单号查询所有明细（包含商品、规格导航）
        /// </summary>
        public static List<StocktakeDetail> SelectByStocktakeCode(string stocktakeCode)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<StocktakeDetail>()
                .IncludesAllFirstLayer()
                .Where(x => x.StocktakeCode == stocktakeCode && x.Active == true)
                .OrderBy(x => x.InvCode)
                .ToList();
        }

        /// <summary>
        /// 按盘点单+商品编码查询单个明细
        /// </summary>
        public static StocktakeDetail SelectByInvCode(string stocktakeCode, string invCode, string sku = "")
        {
            SqlClient SSC = new SqlClient();
            var query = SSC.Db.Queryable<StocktakeDetail>()
                .IncludesAllFirstLayer()
                .Where(x => x.StocktakeCode == stocktakeCode && x.InvCode == invCode && x.Active == true);

            if (!string.IsNullOrEmpty(sku))
                query = query.Where(x => x.SKU == sku);

            return query.First();
        }
        #endregion

        #region 实例增删改方法
        /// <summary>
        /// 插入单个明细
        /// </summary>
        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            this.LastUpdateTime = DateTime.Now;
            SSC.Db.Insertable(this).ExecuteCommand();
        }

        /// <summary>
        /// 更新单个明细
        /// </summary>
        public void Update()
        {
            SqlClient SSC = new SqlClient();
            this.LastUpdateTime = DateTime.Now;
            SSC.Db.Updateable(this).ExecuteCommand();
        }

        /// <summary>
        /// 软删除单个明细
        /// </summary>
        public void Delete()
        {
            SqlClient SSC = new SqlClient();
            this.Active = false;
            this.LastUpdateTime = DateTime.Now;
            SSC.Db.Updateable(this)
                .UpdateColumns(x => new { x.Active, x.LastUpdateTime })
                .ExecuteCommand();
        }

        /// <summary>
        /// 批量更新实盘数据（实盘录入页专用，性能最优）
        /// </summary>
        public static void BatchUpdateStockCount(List<StocktakeDetail> details)
        {
            SqlClient SSC = new SqlClient();
            // 只更新实盘数量、备注、更新时间，不修改其他字段
            SSC.Db.Updateable(details)
                .UpdateColumns(x => new { x.StockCount, x.Note, x.LastUpdateTime })
                .ExecuteCommand();
        }
        #endregion

        #region 明细业务属性
        /// <summary>
        /// 差异数量（正=盘盈，负=盘亏）
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal DiffQuantity => StockCount - Quantity;
        /// <summary>
        /// 差异金额（正=盘盈，负=盘亏）
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public decimal DiffAmount => DiffQuantity * Price;
        /// <summary>
        /// 是否盘盈
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public bool IsProfit => DiffQuantity > 0;
        /// <summary>
        /// 是否盘亏
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public bool IsLoss => DiffQuantity < 0;
        /// <summary>
        /// 账实一致
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public bool IsMatch => DiffQuantity == 0;
        #endregion
    }
}
