using AntDesign;
using ERP_Web.Repository;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Web.Models
{
    enum InventoryCostingMethodEnum
    {
        FIFO, //先进先出法
        LIFO, //后进先出法
        AverageCost //加权平均法
    }

    enum InventoryValuationMethodEnum
    {
        StandardCost, //标准成本法
        ActualCost //实际成本法
    }
    /// <summary>
    /// 单据状态
    /// </summary>
    public enum StatusEnum
    {
        /// <summary>
        /// 单据状态-草稿
        /// </summary>
        Draft,      //草稿
        /// <summary>
        /// 单据状态-已提交
        /// </summary>
        Submitted,  //已提交
        /// <summary>
        /// 单据状态-审核中
        /// </summary>
        Auditing,    //审核中
        /// <summary>
        /// 单据状态-已审核
        /// </summary>
        Approved,   //已审核
        /// <summary>
        /// 单据状态-已完成
        /// </summary>
        Completed,  //已完成
        /// <summary>
        /// 单据状态-已过账
        /// </summary>
        Posted,      //已过账
        /// <summary>
        /// 单据状态-已拒绝
        /// </summary>
        Rejected,   //已拒绝
        /// <summary>
        /// 单据状态-已取消
        /// </summary>
        Cancelled,   //已取消
        /// <summary>
        /// 单据状态-已作废
        /// </summary>
        Voided      //已作废
    }

    /// <summary>
    /// 存货档案
    /// </summary>
    public class InventoryItem 
    {
        public long Id { set; get; }
        [Display(Name = "编码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; } = string.Empty;
        [Display(Name = "商品名称")]
        public string Name { set; get; } = "";
        public string? Specification { set; get; }
        public string? CategoryCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CategoryCode))]//一对一 
        public Category? Category { get; set; } = new OneToOneInitializer<Category>();
        public string? Unit { set; get; }
        [Navigate(NavigateType.OneToMany, nameof(InventoryUnit.InvCode))]//注意顺序
        public List<InventoryUnit> UnitList { set; get; }
        public string? InventoryCostingMethodCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(InventoryCostingMethodCode))]//一对一
        public InventoryCostingMethod InventoryCostingMethod { get; set; } = new OneToOneInitializer<InventoryCostingMethod>();
        public string? Note { set; get; }
        public string? InsertUser { set; get; }
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;
        public bool Active { set; get; } = true;
        //[Display(Name = "商品图片")]
        //public string? InvImage { set; get; } = "";
        //public bool IsDeleted { set; get; } //是否被删除，用于同步
        //public bool IsModified { set; get; } //是否被修改过，用于同步

        public InventoryItem()
        {
            //SqlClient SSC = new SqlClient();
            Id = SnowFlakeSingle.Instance.NextId(); //SSC.GetNextID("InventoryItem");雪花算法生成ID
            if (UnitList == null)
            {
                UnitList = new List<InventoryUnit>();
                UnitList.Add(new InventoryUnit()
                {
                    Id = SnowFlakeSingle.Instance.NextId(),
                    InvCode = this.Code,
                    Unit = this.Unit ?? "个",
                    IsMinimumUnit = true,
                    ConversionRatio = 1,
                    Active = true
                });
            }
        }
    }
    /// <summary>
    /// 存货收发明细基类
    /// </summary>
    public class InvInOut
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { set; get; }
        //public int? DocumentCode { set; get; }
        public string InvCode { set; get;}
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public InventoryItem InventoryItem { set; get; } = new OneToOneInitializer<InventoryItem>();
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { set; get; } = 0;
        [Column(TypeName = "decimal(20,10)")]
        public decimal Price { set; get; } = 0;
        [Column(TypeName = "decimal(22,10)")]
        public decimal Amount { set; get; }
        [Column(TypeName = "decimal(4,2)")]
        public decimal TaxRate { set; get; } = 0;
        [Column(TypeName = "decimal(20,10)")]
        public decimal PriceIncTax { set; get; } = 0;
        [Column(TypeName = "decimal(22,10)")]
        public decimal AmountIncTax { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨

        public InvInOut()
        {
            Quantity = 0;
            Price = 0;
            Amount = 0;
            TaxRate = 0;
            PriceIncTax = 0;
            AmountIncTax = 0;
        }
        public void QuantityChange()
        {
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }
        public void PriceChange()
        {
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }
        public void PriceIncTaxChange()
        {
            this.Price = this.PriceIncTax / (1 + this.TaxRate);
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }

        public void TaxRateChange()
        {
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }

        public void AmountChange()
        {
            this.Price = this.Amount / this.Quantity;
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }

        public void AmountInTaxChange()
        {
            this.PriceIncTax = this.AmountIncTax / this.Quantity;
            this.Price = this.PriceIncTax / (1 + this.TaxRate);
            this.Amount = this.Price * this.Quantity;
        }

        //public void InvTrxUpdateCode(int Code)
        //{
        //    this.IcCode = Code;
        //    if (this.Inv != null)
        //    {
        //        this.InvCode = this.Inv.Code;
        //    }
        //}

        public void UpdatePriceByIn()
        {
            SqlClient SSC = new SqlClient();
            InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                .IncludesAllFirstLayer()
                .Where(xx => xx.InvCode == this.InvCode)
                .First();
            if (invBalOld == null) return;
            this.Price = invBalOld.Price;
            PriceChange();
        }
        public void UpdateAmount()
        {
            this.Amount = this.Quantity * this.Price;
            if (this.TaxRate == null) this.TaxRate = 0;
            this.PriceIncTax = this.Price * (1 + this.TaxRate);
            this.AmountIncTax = this.Quantity * this.PriceIncTax;
        }
    }
    /// <summary>
    /// 购物车存货明细
    /// </summary>
    public class ShoppingCart : InvInOut
    {
        public string? SCCode { set; get; }
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Explanation { set; get; }
        public DateOnly Date { set; get; } = DateOnly.FromDateTime(DateTime.Now);
        public string? Operator { set; get; }
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;
    }
    /// <summary>
    /// 单据基类
    /// </summary>
    public class BaseDocument
    {
        /// <summary>
        /// 单据Id（唯一标识）
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 单据编号（如PO2023001、MAT2023001）
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; } = string.Empty;
        /// <summary>
        /// 单号给用户查看，规则待定 字母+年月+当月序号  PR单：PR260100001
        /// </summary>
        public string? TrxNo { set; get; }
        /// <summary>
        /// 单据日期
        /// </summary>
        public DateOnly Date { get; set; } = new DateOnly();
        /// <summary>
        /// 单据状态（如草稿、已审核、已执行）
        /// </summary>
        public StatusEnum? Status { get; set; } = StatusEnum.Draft;
        public decimal Quantity { set; get; } = 0;
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { set; get; } = 0;
        //[Column(TypeName = "decimal(4,2)")]
        //public decimal TaxRate { set; get; } = 0;
        [Display(Name = "含税金额")]
        public decimal AmountIncTax { set; get; } = 0;
        public string? Explanation { get; set; }

        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public int? FiscalYear { set; get; }
        public int? Period { set; get; }
        public string? InsertUser { get; set; }
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string? UpdateUser { get; set; }
        public DateTime? UpdateTime { get; set; } = DateTime.Now;

        public BaseDocument()
        {
            Id = SnowFlakeSingle.Instance.NextId();
            FiscalYear = Date.Year;
            Period = Date.Month;
        }
        public void DateUpdate()
        {
            FiscalYear = Date.Year;
            Period = Date.Month;
        }

        public void GetTrxNo()
        {
            // 1. 定义前缀（如PR）
            var prefix = "DB";

            // 2. 获取当前年月（格式：YYMM）
            var yearMonth = DateTime.Now.ToString("yyMM");

            // 3. 拼接当前年月前缀（如PR2601）
            var currentPrefix = $"{prefix}{yearMonth}";
            var seqLength = 5; // 序号位数（固定5位，不足补零）

            SqlClient SSC = new SqlClient();
            // 4. 查询数据库中当前前缀的最大序号（需结合EF Core或SQL实现）
            var maxSeqStr = SSC.Db.Queryable<PurchaseRequisitions>()
                .Where(t => t.TrxNo != null && t.TrxNo.StartsWith(currentPrefix))
                //.Select(t => t.TrxNo.Substring(currentPrefix.Length)) // 提取序号部分
                //.DefaultIfEmpty("0")
                //.Max();
                .Select(t => t.TrxNo.Substring(currentPrefix.Length))
                .Max<string>(TrxNo);

            // 5. 生成新序号（+1后补零为5位）
            //var newSeq = (int.Parse(maxSeqStr) + 1).ToString("D5"); // D5表示5位，不足补零
            if (!int.TryParse(maxSeqStr, out int maxSeq)) maxSeq = 0; // 容错处理
            var newSeq = (maxSeq + 1).ToString($"D{seqLength}"); // 补零为5位：00001


            // 6. 最终单号
            this.TrxNo = $"{currentPrefix}{newSeq}";
        }

        public void GetTrxNo(string prefix)
        {
            // 1. 定义前缀（如PR）
            prefix ??= "DB"; // 空合并赋值
            // 2. 获取当前年月（格式：YYMM）
            var yearMonth = DateTime.Now.ToString("yyMM");

            // 3. 拼接当前年月前缀（如PR2601）
            var currentPrefix = $"{prefix}{yearMonth}";
            var seqLength = 5; // 序号位数（固定5位，不足补零）

            SqlClient SSC = new SqlClient();
            // 4. 查询数据库中当前前缀的最大序号（需结合EF Core或SQL实现）
            //var maxSeqStr = SSC.Db.Queryable<PurchaseRequisitions>()
            //    .Where(t => t.TrxNo != null && t.TrxNo.StartsWith(currentPrefix))
            //    //.Select(t => t.TrxNo.Substring(currentPrefix.Length)) // 提取序号部分
            //    //.DefaultIfEmpty("0")
            //    //.Max();
            //    .Select(t => t.TrxNo.Substring(currentPrefix.Length))
            //    .Max<string>("TrxNo");
            var maxSeqStr = "0";


            // 5. 生成新序号（+1后补零为5位）
            //var newSeq = (int.Parse(maxSeqStr) + 1).ToString("D5"); // D5表示5位，不足补零
            if (!int.TryParse(maxSeqStr, out int maxSeq)) maxSeq = 0; // 容错处理
            var newSeq = (maxSeq + 1).ToString($"D{seqLength}"); // 补零为5位：00001


            // 6. 最终单号
            this.TrxNo = $"{currentPrefix}{newSeq}";
        }

    }
}
