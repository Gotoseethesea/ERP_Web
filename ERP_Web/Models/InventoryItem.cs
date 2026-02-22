using Dm;
using ERP_Web.Repository;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Transactions;

namespace ERP_Web.Models
{
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
        [Navigate(NavigateType.OneToMany, nameof(InvAttribute.InvCode))]//一对多
        public List<InvAttribute> Attributes { set; get; }
        //public string? SKU { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(SKU))]//一对一 
        [Navigate(NavigateType.OneToMany, nameof(Specification.InvCode))]//一对多
        public List<Specification>? Specifications { set; get; }
        public string? CategoryCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CategoryCode))]//一对一 
        public Category Category { get; set; } = new OneToOneInitializer<Category>();
        //public string? Unit { set; get; }
        //[Navigate(NavigateType.OneToMany, nameof(InventoryUnit.InvCode))]//注意顺序
        //public List<InventoryUnit> UnitList { set; get; }
        public string? CostingMethodCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CostingMethodCode))]//一对一
        public InventoryCostingMethod CostingMethod { get; set; } = new OneToOneInitializer<InventoryCostingMethod>();
        public string? Note { set; get; }
        [Display(Description = "商品图片")]
        public string? Image { set; get; } = "";
        public string? InsertUser { set; get; }
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime? UpdateTime { set; get; } = DateTime.Now;
        public bool Active { set; get; } = true;
        //public bool IsDeleted { set; get; } //是否被删除，用于同步
        //public bool IsModified { set; get; } //是否被修改过，用于同步
        public InventoryItem() { }
        public InventoryItem(string str)
        {
            //SqlClient SSC = new SqlClient();
            Id = SnowFlakeSingle.Instance.NextId(); //SSC.GetNextID("InventoryItem");雪花算法生成ID
            if (this.Specifications == null)
            {
                Specification spec = new Specification
                {
                    Id = SnowFlakeSingle.Instance.NextId(),
                    InvCode = this.Code,
                    Description = string.Empty,
                    Unit = "个",
                };
                this.Specifications = new List<Specification>() { spec };
            }
        }
        public static List<InventoryItem> SimpleSelect()
        {
            SqlClient SSC = new SqlClient();
            List<InventoryItem> result = SSC.Db.Queryable<InventoryItem>()
                .Where(x => x.Active == true).ToList();
            return result;
        }
        public static List<InventoryItem> Select()
        {
            SqlClient SSC = new SqlClient();
            List<InventoryItem> result = SSC.Db.Queryable<InventoryItem>()
                .IncludesAllFirstLayer()
                .IncludesAllSecondLayer(xx => xx.Attributes)
                .IncludesAllSecondLayer(xx => xx.Specifications)
                .Where(x => x.Active == true).ToList();
            return result;
        }
        public static InventoryItem Select(string code)
        {
            SqlClient SSC = new SqlClient();
            var result = SSC.Db.Queryable<InventoryItem>()
                .IncludesAllFirstLayer()
                .IncludesAllSecondLayer(xx => xx.Attributes)
                .IncludesAllSecondLayer(xx => xx.Specifications)
                .Where(x => x.Active == true && x.Code  == code).First();
            return result;
        }
        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            bool result = SSC.Db.InsertNav(this).IncludesAllFirstLayer()
                .ThenIncludeByNameString(nameof(Attributes))
                .ThenIncludeByNameString(nameof(Specifications))
                .ExecuteCommand();
        }
        public void Update()
        {
            SqlClient SSC = new SqlClient();
            bool result = SSC.Db.UpdateNav(this)
                .IncludesAllFirstLayer()
                .ThenIncludeByNameString(nameof(Attributes))
                .ThenIncludeByNameString(nameof(Specifications))
                .ExecuteCommand();
        }
        public void Delete()
        {
            SqlClient SSC = new SqlClient();
            this.Active = false;
            bool result = SSC.Db.UpdateNav(this).IncludesAllFirstLayer().ExecuteCommand();
        }

        // 反射复制属性方法，适用于属性名称和类型相同的情况，可以根据需要进行优化和扩展
        public static T CopyProperties<T>(object source, T target)
        {
            var sourceProps = source.GetType().GetProperties();
            var targetProps = target.GetType().GetProperties();

            foreach (var sourceProp in sourceProps)
            {
                var targetProp = targetProps.FirstOrDefault(p =>
                    p.Name == sourceProp.Name && p.PropertyType == sourceProp.PropertyType);
                if (targetProp != null && targetProp.CanWrite)
                {
                    targetProp.SetValue(target, sourceProp.GetValue(source));
                }
            }
            return target;
        }

        // 使用示例：复制父级 Order 的属性到新 OrderItem
        //var newItem = CopyProperties(order, new OrderItem());
        //newItem.ProductName = "手机"; // 补充其他属性
        public InventoryItemView InventoryItemListByInventoryItem()
        {
            //List<InventoryItemView> result = new List<InventoryItemView>();
            //if (this.Specifications == null) return;
            InventoryItemView invView = CopyProperties<InventoryItemView>(this, new InventoryItemView());
            //invView.Specifications = this.Specifications.Where(x => x.IsDefault == true).ToList().Count()  ?? this.Specifications.ToList();
            var defaultSpec = this.Specifications.FirstOrDefault(x => x.IsDefault == true);
            invView.Specifications = defaultSpec != null
                ? new List<Specification> { defaultSpec }
                : this.Specifications.ToList();

            invView.Children = new List<InventoryItemView>();
            if (this.Specifications != null)
            {
                foreach (var spec in this.Specifications)
                {
                    if (spec.IsDefault == true) continue;
                    InventoryItemView invView2 = new InventoryItemView();
                    invView2 = CopyProperties<InventoryItemView>(this, new InventoryItemView());
                    invView2.Name = string.Empty; //非默认规格不显示名称
                    invView2.Code = spec.SKU;
                    invView2.CategoryCode = string.Empty;
                    //invView2.Category.Name = string.Empty;
                    invView2.Specifications = new List<Specification> { spec };
                    invView.Children.Add(invView2);
                }
            }
            return invView;
        }
    }
    public class InventoryItemView : InventoryItem
    {
        public List<InventoryItemView> Children { set; get; }
    }
    public class ITInvInOut : InvInOut
    {
        public string? IcCode { set; get; }
    }
    public class Specification
    {
        [Display(Name = "编码")]
        public long Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        public string? SKU { set; get; }
        public string InvCode { set; get; }
        //[Display(Name = "规格")]
        public string? Description { set; get; }
        public string? Unit { set; get; }  //如果是默认规格则表示基础单位，否则表示非基本单位，一般是最小出库或者销售单位
        public decimal ConversionRatio { set; get; } = decimal.One;  //与基本单位的换算率，默认1表示与基本单位相同
        public bool? IsDefault { set; get; } = false;  //是否默认规格，true表示默认规格，false表示非默认规格，null表示不区分默认规格
        public string? Note { set; get; }
        public bool? Active { set; get; } = true;
        public DateTime? InsertTime { set; get; } = DateTime.Now;
        public DateTime? UpdateTime { set; get; } = DateTime.Now;
        public string? InsertUser { set; get; } = string.Empty;
        /// <summary>
        /// 规格对应的属性选项列表，使用中间表SpecificationAttributeOptionMapping进行多对多关联，可以通过选项ID获取属性选项的详细信息，如价格，也可以通过属性选项获取对应的规格信息生成Description
        /// </summary>
        [Navigate(typeof(SpecificationAttributeOptionMapping), nameof(SpecificationAttributeOptionMapping.SKU), nameof(SpecificationAttributeOptionMapping.OptionId))]//注意顺序
        public List<InvAttributeOption> Options { set; get; } = new List<InvAttributeOption>();

        public Specification() { }
        public Specification(string str)
        {
            Id = SnowFlakeSingle.Instance.NextId(); //SSC.GetNextID("Specification");雪花算法生成ID
            SKU = "SKU" + Id.ToString(); //默认生成SKU，可以根据实际需求修改生成规则
            this.InvCode = str;
            SqlClient SSC = new SqlClient();
            bool isdafult = SSC.Db.Queryable<Specification>().Where(x => x.InvCode == this.InvCode && x.IsDefault == true).Any();
            if (!isdafult)
            {
                this.IsDefault = true; //如果没有默认规格，则当前规格设置为默认
            }
        }
        public void GetDesc()
        {
            if (Options != null && Options.Count > 0)
            {
                Description = string.Join(";", Options.Select(o => o.GetAttributeName()+":"+o.Value));
            }
        }
        public static Specification SelectBySKU(string SKU)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Specification>().IncludesAllFirstLayer().Where(x => x.SKU == SKU).First();
        }
        public static List<Specification> GetByInvCode(string InvCode)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Specification>().IncludesAllFirstLayer().Where(x => x.InvCode == InvCode).OrderBy(xx=>xx.IsDefault).ToList();
        }

        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            bool result = SSC.Db.InsertNav(this).IncludesAllFirstLayer()//自动2级
                .ExecuteCommand();
        }
        public void Update()
        {
            SqlClient SSC = new SqlClient();
            bool result = SSC.Db.UpdateNav(this).IncludesAllFirstLayer().ExecuteCommand();
        }
        public void Delete()
        {
            SqlClient SSC = new SqlClient();
            this.Active = false;
            bool result = SSC.Db.UpdateNav(this).IncludesAllFirstLayer().ExecuteCommand();
        }
        public static List<Specification> Select()
        {
            SqlClient SSC = new SqlClient();
            List<Specification> result = SSC.Db.Queryable<Specification>()
              .IncludesAllFirstLayer().Where(x => x.Active == true).ToList();
            return result;
        }
        public static List<Specification> Select(string Code)
        {
            SqlClient SSC = new SqlClient();
            List<Specification> result = SSC.Db.Queryable<Specification>()
              .IncludesAllFirstLayer().Where(x => x.Active == true && x.InvCode == Code).ToList();
            return result;
        }
    }
    public class InvAttribute
    {
        [SugarColumn(IsPrimaryKey = true,IsIdentity = true)]
        public int Id { get; set; }
        public string InvCode { set; get; }
        public string Attribute { get; set; }  // 属性名称，如“颜色”
        [Navigate(NavigateType.OneToMany, nameof(InvAttributeOption.AttributeId))]//一对一
        public List<InvAttributeOption> Options { get; set; } // 属性选项，如["黑","白","灰"]

        [SugarColumn(IsIgnore = true)]
        public int SelectedOptionId { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(SelectedOptionId))]//一对一
        public InvAttributeOption SelectedOption { get; set; }
        public bool Active { get; set; } = true;
        public string? InsertUser { set; get; } = string.Empty;
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime UpdateTime { set; get; } = DateTime.Now;

        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.InsertNav(this).IncludesAllFirstLayer()
                .ExecuteCommand();
        }

        public static List<InvAttribute> GetByInvCode(string Code)
        {
            SqlClient SSC = new SqlClient();
            List<InvAttribute> result = SSC.Db.Queryable<InvAttribute>()
              .IncludesAllFirstLayer().Where(x => x.Active == true && x.InvCode == Code).ToList();
            return result;
        }
        public string GetOptionStr()
        {
            string result = "";
            if (Options != null)
            {
                foreach (var item in Options)
                {
                    result += item.Value + ";";
                }
            }
            return result;
        }
    }
    public class InvAttributeOption
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public int AttributeId { get; set; }
        public string Value { get; set; }
        public decimal? Price { get; set; } = 0;
        public string? Note { get; set; }
        public bool Active { get; set; } = true;
        public string? InsertUser { set; get; } = string.Empty;
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime UpdateTime { set; get; } = DateTime.Now;

        public void Insert()
        {
            var SSC = new SqlClient();
            SSC.Db.Insertable(this).ExecuteCommand();
        }

        public void Update()
        {
            var SSC = new SqlClient();
            SSC.Db.Updateable(this).ExecuteCommand();
        }

        public string GetAttributeName()
        {
            var SSC = new SqlClient();
            return SSC.Db.Queryable<InvAttribute>().Where(x => x.Id == this.AttributeId).Select(x => x.Attribute).First();
        }
        public static List<InvAttributeOption> GetByAttributeId(int AttributeId)
        {
            var SSC = new SqlClient();
            return SSC.Db.Queryable<InvAttributeOption>().Where(x => x.AttributeId == AttributeId).ToList();
        }
    }
    public class SpecificationAttributeOptionMapping
    {
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public string SKU { get; set; }
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public int OptionId { get; set; }
    }

    /// <summary>
    /// 库存出入库主表
    /// 入库单，出库单，调拨单隐藏，直拨单显示，trxType 1入库，-1出库，调拨  0直拨
    /// </summary>
    public class InventoryTransaction: BaseDocument
    {
        //下次改造：修改名称为InventoryTransaction
        [Display(Name = "出入库类型")]
        public string? TrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(TrxGroupCode))]//一对一 
        public TrxGroup? TrxGroup { set; get; } = new OneToOneInitializer<TrxGroup>();
        public int TrxType { set; get; }
        public string? WarehouseCode { set; get; } 
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Note { set; get; }
        public string? Operator { set; get; }
        public string? Approver { set; get; }   //Approver
        public string? Poster { set; get; }

        [SugarColumn(Length = 20)]
        public string? SourceDocumentCode { get; set; } // 来源单据号
        [Navigate(NavigateType.OneToOne, nameof(SourceDocumentCode))]
        public ReceivingNote? SourceDocument { get; set; }

        [Navigate(NavigateType.OneToMany, nameof(ITInvInOut.IcCode))]//一对多
        public List<ITInvInOut>? ITInvInOuts { set; get; }


        // 👇 新增：加[SugarColumn(IsIgnore = true)]标记，SqlSugar不会把它当成数据库字段
        [SugarColumn(IsIgnore = true)]
        public string TrxGroupName => TrxGroup?.Name ?? "未知类型";

        [SugarColumn(IsIgnore = true)]
        public string WarehouseName => Warehouse?.Name ?? string.Empty;

        // 👇 改名为SourceDocumentNo，避免和原有SourceDocumentCode字段重名
        [SugarColumn(IsIgnore = true)]
        public string SourceDocumentNo => SourceDocument?.Code ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string DepartmentName => Department?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string CompanyName => Company?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string EmployeeName => Employee?.Name ?? string.Empty;

        public InventoryTransaction()
        {
        }
        public InventoryTransaction(string str)
        {
            FiscalYear = Date.Year;
            Period = Date.Month;
        } 

        public static List<InventoryTransaction> Select()
        {
            //this.GetTrxNo<ReceivingNote>("IC");
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<InventoryTransaction>()
                    .Includes(x => x.ITInvInOuts)
                    .Where(x => x.Active == true)
                    .ToList();
            }
        }
        public static List<InventoryTransaction> Select(int year,int month)
        {
            //this.GetTrxNo<ReceivingNote>("IC");
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<InventoryTransaction>()
                    .Includes(x => x.ITInvInOuts)
                    .Where(x => x.Active == true && x.FiscalYear == year && x.Period == month)
                    .ToList();
            }
        }

        public static InventoryTransaction Select(string Code)
        {
            //this.GetTrxNo<ReceivingNote>("IC");
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<InventoryTransaction>()
                    .IncludesAllFirstLayer()
                    .IncludesAllSecondLayer(xx=>xx.ITInvInOuts)
                    .Includes(xx => xx.ITInvInOuts,xy=>xy.Specification)
                    .Includes(xx => xx.ITInvInOuts, xy => xy.InventoryItem,xz=>xz.Specifications)
                    .Where(x => x.Active == true && x.Code == Code)
                    .First();
            }
        }

        public static int SelectForClosing(int year, int month)
        {
            //this.GetTrxNo<ReceivingNote>("IC");
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<InventoryTransaction>()
                    .Where(x => x.FiscalYear == year && x.Period == month && x.Active == true && x.Approver == null)
                    .ToList().Count();
            }
        }
        public void UpdateAmount()
        {
            Quantity = 0;
            Amount = 0;
            AmountIncTax = 0;
            if (ITInvInOuts != null)
            {
                foreach (ITInvInOut IcInvInOut in ITInvInOuts)
                {
                    Quantity += IcInvInOut.Quantity;
                    Amount += IcInvInOut.Amount;
                    AmountIncTax += IcInvInOut.AmountIncTax;
                }
            }
        }

        public static bool Approve()
        {
            return true;
        }

        public void Insert()
        {
            this.Code = this.TrxNo ?? "IT" + this.Id.ToString();
            this.UpdateInvPrice();

            using var scope = new TransactionScope();
            var db = new SqlClient().Db;
            try
            {
                // 1. 前置校验：非空校验、结账期间校验
                ValidateTransaction();

                // 2. 如果是出库单，自动按计价方式计算出库成本
                if (TrxType == -1)
                {
                    UpdateInvPrice();
                    UpdateAmount();
                }

                // 3. 生成单据号+插入单据
                this.GetTrxNo<InventoryTransaction>("IT");
                db.InsertNav(this)
                  .Include(xx=>xx.ITInvInOuts)
                  //.ThenIncludeByNameString(nameof(ITInvInOuts))
                  .ExecuteCommand();

                // 4. 按计价方式更新库存结余（MWA/FIFO自动适配）
                InventoryBalance.UpdateBalance(this);

                scope.Complete();
            }
            catch (Exception ex)
            {
                throw new Exception($"库存单据插入失败：{ex.Message}", ex);
            }
        }

        public void Update()
        {
            using var scope = new TransactionScope();
            var db = new SqlClient().Db;
            try
            {
                // 1. 前置校验：单据存在性、是否已结账、是否已审核
                var oldTrx = db.Queryable<InventoryTransaction>()
                               .Includes(x => x.ITInvInOuts)
                               .First(x => x.Id == this.Id && x.Active == true);
                if (oldTrx == null) throw new Exception("单据不存在");
                ValidateTransactionEditable(oldTrx);

                // 2. 冲销旧单据的库存（反向执行旧单据的库存操作）
                var reverseTrx = oldTrx.CloneReverse();
                InventoryBalance.UpdateBalance(reverseTrx);

                // 3. 如果是出库单，重新计算新的出库成本
                if (TrxType == -1)
                {
                    UpdateInvPrice();
                    UpdateAmount();
                }

                // 4. 更新单据
                db.UpdateNav(this)
                  .Include(xx=>xx.ITInvInOuts)
                  .ExecuteCommand();

                // 5. 按新单据更新库存
                InventoryBalance.UpdateBalance(this);

                scope.Complete();
            }
            catch (Exception ex)
            {
                throw new Exception($"库存单据更新失败：{ex.Message}", ex);
            }
        }

        public void Delete()
        {
            using var scope = new TransactionScope();
            var db = new SqlClient().Db;
            try
            {
                // 1. 前置校验：单据存在性、是否已结账、是否已审核
                var oldTrx = db.Queryable<InventoryTransaction>()
                               .Includes(x => x.ITInvInOuts)
                               .First(x => x.Id == this.Id && x.Active == true);
                if (oldTrx == null) throw new Exception("单据不存在");
                ValidateTransactionEditable(oldTrx);

                // 2. 冲销旧单据的库存
                var reverseTrx = oldTrx.CloneReverse();
                InventoryBalance.UpdateBalance(reverseTrx);

                // 3. 标记删除
                this.Active = false;
                db.UpdateNav(this)
                  .Include(z => z.ITInvInOuts)
                  .ExecuteCommand();

                scope.Complete();
            }
            catch (Exception ex)
            {
                throw new Exception($"库存单据删除失败：{ex.Message}", ex);
            }
        }

        /// <summary>
        /// 按计价方式自动计算出库成本（替换原逻辑）
        /// </summary>
        public void UpdateInvPrice()
        {
            if (ITInvInOuts == null || TrxType >= 0) return;

            foreach (var invInOut in ITInvInOuts)
            {
                // 调用计价扩展类，自动适配商品的计价方式
                invInOut.Price = InventoryPricingExtension.CalculateOutboundPrice(
                    warehouseCode: this.WarehouseCode,
                    invCode: invInOut.InvCode,
                    sku: invInOut.SKU,
                    quantity: invInOut.Quantity
                );
                invInOut.TaxRate = 0;
                invInOut.PriceChange();
            }
            UpdateAmount();
        }

        #region 辅助方法
        /// <summary>
        /// 校验单据基本信息
        /// </summary>
        private void ValidateTransaction()
        {
            if (string.IsNullOrEmpty(WarehouseCode)) throw new Exception("仓库不能为空");
            if (ITInvInOuts == null || ITInvInOuts.Count == 0) throw new Exception("单据明细不能为空");

            // 校验是否在已结账期间
            var maxClosingDate = new SqlClient().Db.Queryable<InventoryClosingRecord>()
                                                .Where(x => x.Status == ClosingStatus.Completed)
                                                .Max(x => x.ClosingDate);
            if (Date <= maxClosingDate) throw new Exception($"当前日期{Date:yyyy-MM-dd}所在期间已结账，无法操作");
        }

        /// <summary>
        /// 校验单据是否可编辑
        /// </summary>
        private void ValidateTransactionEditable(InventoryTransaction trx)
        {
            // 可扩展校验：已审核单据不可编辑、已过账单据不可编辑等
            if (!string.IsNullOrEmpty(trx.Approver)) throw new Exception("已审核单据无法修改/删除");
        }

        /// <summary>
        /// 生成反向冲销单据
        /// </summary>
        private InventoryTransaction CloneReverseold()
        {
            var reverse = (InventoryTransaction)this.MemberwiseClone();
            reverse.TrxType = TrxType * -1; // 反向交易类型
            reverse.ITInvInOuts = ITInvInOuts.Select(x => new ITInvInOut
            {
                InvCode = x.InvCode,
                SKU = x.SKU,
                Quantity = x.Quantity,
                Price = x.Price,
                Amount = x.Amount
            }).ToList();
            return reverse;
        }
        /// <summary>
        /// 生成反向冲销单据
        /// </summary>
        private InventoryTransaction CloneReverse()
        {
            var reverse = (InventoryTransaction)this.MemberwiseClone();
            // 👇 修改：直入直出反向后还是0，其他交易类型正负反转
            reverse.TrxType = TrxType == 0 ? 0 : TrxType * -1;
            reverse.ITInvInOuts = ITInvInOuts.Select(x => new ITInvInOut
            {
                InvCode = x.InvCode,
                SKU = x.SKU,
                // 👇 新增：直入直出直接反转明细数量正负
                Quantity = TrxType == 0 ? -x.Quantity : x.Quantity,
                Price = x.Price,
                Amount = TrxType == 0 ? -x.Amount : x.Amount
            }).ToList();
            return reverse;
        }


        public void Update2()
        {
            //this.GetTrxNo<ReceivingNote>("IC");
            SqlClient SSD = new();
            SSD.Db.UpdateNav(this).Include(z => z.ITInvInOuts).ExecuteCommand();
        }
        public void Delete2()
        {
            //this.GetTrxNo<ReceivingNote>("IC");
            SqlClient SSD = new();
            this.Active = false;
            SSD.Db.UpdateNav(this).Include(z => z.ITInvInOuts).ExecuteCommand();
        }
        public void Insert2()
        {
            this.GetTrxNo<InventoryTransaction>("IT");
            SqlClient SSD = new();
            try
            {
                SSD.Db.InsertNav(this).IncludesAllFirstLayer()
                //.ThenIncludeByNameString(nameof(ITInvInOut.Specification))
                .ExecuteCommand();
            }
            catch
            {

            }
            InventoryBalance.UpdateBalance(this);
        }
        public void UpdateInvPrice2()
        {
            if (ITInvInOuts != null)
            {
                if (this.TrxType == -1)
                {
                    foreach (ITInvInOut IcInvInOut in ITInvInOuts)
                    {
                        SqlClient SSC = new SqlClient();
                        InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                            .IncludesAllFirstLayer()
                            .Where(xx => xx.WarehouseCode == this.WarehouseCode && xx.InvCode == IcInvInOut.InvCode && xx.SKU == IcInvInOut.SKU)
                            .First();
                        if (invBalOld == null) return;
                        IcInvInOut.Price = invBalOld.Price;
                        IcInvInOut.TaxRate = 0;
                        IcInvInOut.PriceChange();
                    }
                }
                UpdateAmount();
            }
        }

        #endregion

    }

    public class InventoryBalance
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse? Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? InvCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public InventoryItem? Inv { set; get; } = new OneToOneInitializer<InventoryItem>();
        public string? SKU { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(SKU))]//一对一
        public Specification? Specification { set; get; } = new OneToOneInitializer<Specification>();
        [Display(Name = "数量")]
        public decimal Quantity { set; get; } = 0;
        [Display(Name = "单价")]
        public decimal Price { set; get; } = 0;
        [Display(Name = "金额")]
        public decimal Amount { set; get; } = 0;
        [Display(Name = "税率")]
        public decimal TaxRate { set; get; } = 0;
        [Display(Name = "含税单价")]
        public decimal PriceIncTax { set; get; } = 0;
        [Display(Name = "含税金额")]
        public decimal AmountIncTax { set; get; } = 0;
        [Display(Name = "备注")]
        public string? Note { set; get; }

        public DateTime InsertTime { set; get; } = DateTime.Now;
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime? UpdateTime { set; get; } = DateTime.Now;

        public static void UpdateBalance(List<InventoryTransaction> transactions)
        {
            using (var scope = new TransactionScope()) // 添加事务处理
            using (var db = new SqlClient().Db)
            {
                foreach (var transaction in transactions)
                {
                    // 👇 新增：直入直出TrxType=0，直接跳过库存更新，不影响库存结余
                    if (transaction.TrxType == 0) continue;
                    foreach (var invInOut in transaction.ITInvInOuts)
                    {
                        // 👇 新增：直入直出直接取明细自身的正负，其他交易类型取TrxType的符号
                        decimal multiplier = transaction.TrxType == 0 ? 1 : Math.Sign(transaction.TrxType);
                        // 👇 修改判断条件：大于0是入库，小于0是出库，兼容调拨类型
                        switch (invInOut.InventoryItem?.CostingMethodCode)
                        {
                            case "MWA":
                                UpdateMovingWeightedAverage(db, transaction, invInOut, multiplier); // 移动加权平均法更新逻辑
                                break;
                            case "FIFO":
                                if (multiplier > 0) // 所有入库类交易
                                {
                                    CreateFifoInventoryRecord(db, transaction, invInOut);
                                }
                                else if (multiplier < 0) // 所有出库类交易
                                {
                                    ProcessFifoOutbound(db, transaction, invInOut);
                                }
                                // multiplier=0（直入直出已通过明细正负处理，不需要额外操作）
                                break;
                            default:
                                // 默认处理逻辑或错误记录
                                break;
                        }
                    }

                    //foreach (var invInOut in transaction.ITInvInOuts)
                    //{
                    //    switch (invInOut.InventoryItem?.CostingMethodCode)
                    //    {
                    //        case "MWA":
                    //            UpdateMovingWeightedAverage(db, transaction, invInOut); // 移动加权平均法更新逻辑
                    //            break;
                    //        case "FIFO":
                    //            if (transaction.TrxType == 1) // 入库
                    //            {
                    //                CreateFifoInventoryRecord(db, transaction, invInOut);
                    //            }
                    //            else if (transaction.TrxType == -1) // 出库
                    //            {
                    //                ProcessFifoOutbound(db, transaction, invInOut);
                    //            }
                    //            break;
                    //        default:
                    //            // 默认处理逻辑或错误记录
                    //            break;
                    //    }
                    //}
                }
                scope.Complete(); // 提交事务
            }
        }
        public static void UpdateBalance(InventoryTransaction transaction)
        {
            // 👇 新增：直入直出直接跳过
            if (transaction.TrxType == 0) return;

            using (var scope = new TransactionScope()) // 添加事务处理
            using (var db = new SqlClient().Db)
            {
                foreach (var invInOut in transaction.ITInvInOuts)
                {
                    // 👇 新增：直入直出直接取明细自身的正负，其他交易类型取TrxType的符号
                    //decimal multiplier = transaction.TrxType == 0 ? 1 : Math.Sign(transaction.TrxType);
                    decimal multiplier = Math.Sign(transaction.TrxType);
                    // 👇 修改判断条件：大于0是入库，小于0是出库，兼容调拨类型
                    switch (invInOut.InventoryItem?.CostingMethodCode)
                    {
                        case "MWA":
                            UpdateMovingWeightedAverage(db, transaction, invInOut, multiplier); // 移动加权平均法更新逻辑
                            break;
                        case "FIFO":
                            if (multiplier > 0) // 所有入库类交易
                            {
                                CreateFifoInventoryRecord(db, transaction, invInOut);
                            }
                            else if (multiplier < 0) // 所有出库类交易
                            {
                                ProcessFifoOutbound(db, transaction, invInOut);
                            }
                            // multiplier=0（直入直出已通过明细正负处理，不需要额外操作）
                            break;
                        default:
                            // 默认处理逻辑或错误记录
                            break;
                    }
                }
                scope.Complete(); // 提交事务
            }
        }

        // 移动加权平均法更新逻辑
        private static void UpdateMovingWeightedAverageold(SqlSugarClient db, InventoryTransaction transaction, ITInvInOut invInOut)
        {
            var balance = db.Queryable<InventoryBalance>()
                .Where(x => x.WarehouseCode == transaction.WarehouseCode &&
                            x.InvCode == invInOut.InvCode &&
                            x.SKU == invInOut.SKU)
                .First();

            if (balance == null)
            {

                balance = new InventoryBalance
                {
                    WarehouseCode = transaction.WarehouseCode,
                    InvCode = invInOut.InvCode,
                    SKU = invInOut.SKU,
                    Quantity = invInOut.Quantity,
                    Price = invInOut.Price,
                    Amount = invInOut.Amount,
                    Note = transaction.Code,
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                db.Insertable(balance).ExecuteCommand();
            }
            else
            {
                // 根据交易类型更新
                decimal multiplier = transaction.TrxType == 1 ? 1 : -1;
                balance.Quantity += multiplier * invInOut.Quantity;
                balance.Amount += multiplier * invInOut.Amount;

                if (balance.Quantity != 0)
                {
                    balance.Price = balance.Amount / balance.Quantity;
                }

                balance.Note = transaction.Code;
                balance.UpdateTime = DateTime.Now;
                db.Updateable(balance).ExecuteCommand();
            }
        }
        // 移动加权平均法更新逻辑（新增multiplier参数）
        private static void UpdateMovingWeightedAverage(SqlSugarClient db, InventoryTransaction transaction, ITInvInOut invInOut, decimal multiplier = 1)
        {
            var balance = db.Queryable<InventoryBalance>()
                .Where(x => x.WarehouseCode == transaction.WarehouseCode &&
                            x.InvCode == invInOut.InvCode &&
                            x.SKU == invInOut.SKU)
                .First();

            if (balance == null)
            {
                balance = new InventoryBalance
                {
                    WarehouseCode = transaction.WarehouseCode,
                    InvCode = invInOut.InvCode,
                    SKU = invInOut.SKU,
                    // 👇 用传入的multiplier计算最终数量金额
                    Quantity = multiplier * invInOut.Quantity,
                    Price = invInOut.Price,
                    Amount = multiplier * invInOut.Amount,
                    Note = transaction.Code,
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                db.Insertable(balance).ExecuteCommand();
            }
            else
            {
                // 👇 用传入的multiplier计算最终数量金额
                balance.Quantity += multiplier * invInOut.Quantity;
                balance.Amount += multiplier * invInOut.Amount;

                if (balance.Quantity != 0)
                {
                    balance.Price = balance.Amount / balance.Quantity;
                }

                balance.Note = transaction.Code;
                balance.UpdateTime = DateTime.Now;
                db.Updateable(balance).ExecuteCommand();
            }
        }

        // FIFO入库创建新记录
        private static void CreateFifoInventoryRecord(SqlSugarClient db, InventoryTransaction transaction, ITInvInOut invInOut)
        {
            var newBalance = new InventoryBalance
            {
                WarehouseCode = transaction.WarehouseCode,
                InvCode = invInOut.InvCode,
                SKU = invInOut.SKU,
                Quantity = invInOut.Quantity,
                Price = invInOut.Price,
                Amount = invInOut.Amount,
                Note = transaction.Code,
                InsertTime = DateTime.Now,
                UpdateTime = DateTime.Now,
                //BatchNumber = transaction.BatchNumber, // 添加批次号字段
                //ReceivedDate = DateTime.Now
            };
            db.Insertable(newBalance).ExecuteCommand();
        }

        // FIFO出库处理逻辑
        private static void ProcessFifoOutbound(SqlSugarClient db, InventoryTransaction transaction, ITInvInOut invInOut)
        {
            decimal remainingQty = invInOut.Quantity;
            // 🔴 修复：从FIFO批次表查询有效批次，而不是查询InventoryBalance
            var fifoLots = db.Queryable<FIFOInventoryLot>()
                .Where(x => x.WarehouseCode == transaction.WarehouseCode &&
                            x.InvCode == invInOut.InvCode &&
                            x.SKU == invInOut.SKU &&
                            x.Quantity > 0)
                .OrderBy(x => x.TransactionDate) // 先进先出，按入库日期排序
                .ToList();

            // 处理出库扣减批次（和RecalculateAllBalances逻辑完全对齐）
            foreach (var lot in fifoLots)
            {
                if (remainingQty <= 0) break;

                if (lot.Quantity >= remainingQty)
                {
                    // 当前批次足够完成出库
                    lot.Quantity -= remainingQty;
                    lot.Amount = lot.Quantity * lot.Price;
                    remainingQty = 0;

                    if (lot.Quantity == 0)
                    {
                        // 批次用完删除（也可以保留标记删除，根据业务需要调整）  后续调整数据结构，保留历史记录
                        db.Deleteable<FIFOInventoryLot>().Where(x => x.Id == lot.Id).ExecuteCommand();
                    }
                    else
                    {
                        db.Updateable(lot).ExecuteCommand();
                    }
                }
                else
                {
                    // 当前批次不足，完全消耗该批次
                    remainingQty -= lot.Quantity;
                    db.Deleteable<FIFOInventoryLot>().Where(x => x.Id == lot.Id).ExecuteCommand();
                }
            }

            // 库存不足处理
            if (remainingQty > 0)
            {
                // 🔴 修复：和批量重算逻辑一致，库存不足时创建负库存批次
                db.Insertable(new FIFOInventoryLot
                {
                    WarehouseCode = transaction.WarehouseCode,
                    InvCode = invInOut.InvCode,
                    SKU = invInOut.SKU,
                    TransactionDate = transaction.Date,
                    Quantity = -remainingQty,
                    Price = 0,
                    Amount = 0,
                    IsNegative = true,
                    InsertTime = DateTime.Now
                }).ExecuteCommand();
            }

            // 🔴 修复：最后更新InventoryBalance汇总值（所有批次的数量、金额总和）
            var allLots = db.Queryable<FIFOInventoryLot>()
                .Where(x => x.WarehouseCode == transaction.WarehouseCode &&
                            x.InvCode == invInOut.InvCode &&
                            x.SKU == invInOut.SKU)
                .ToList();
            decimal totalQty = allLots.Sum(l => l.Quantity);
            decimal totalAmt = allLots.Sum(l => l.Amount);

            var balance = db.Queryable<InventoryBalance>()
                .First(x => x.WarehouseCode == transaction.WarehouseCode &&
                            x.InvCode == invInOut.InvCode &&
                            x.SKU == invInOut.SKU);
            if (balance == null)
            {
                balance = new InventoryBalance
                {
                    WarehouseCode = transaction.WarehouseCode,
                    InvCode = invInOut.InvCode,
                    SKU = invInOut.SKU,
                    Quantity = totalQty,
                    Amount = totalAmt,
                    Price = totalQty != 0 ? totalAmt / totalQty : 0,
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                db.Insertable(balance).ExecuteCommand();
            }
            else
            {
                balance.Quantity = totalQty;
                balance.Amount = totalAmt;
                balance.Price = totalQty != 0 ? totalAmt / totalQty : 0;
                balance.UpdateTime = DateTime.Now;
                db.Updateable(balance).ExecuteCommand();
            }
        }

        private static void ProcessFifoOutboundOld(SqlSugarClient db, InventoryTransaction transaction, ITInvInOut invInOut)
        {
            decimal remainingQty = invInOut.Quantity;
            var fifoBalances = db.Queryable<InventoryBalance>()
                .Where(x => x.WarehouseCode == transaction.WarehouseCode &&
                            x.InvCode == invInOut.InvCode &&
                            x.SKU == invInOut.SKU &&
                            x.Quantity > 0)
                .OrderBy(x => x.InsertTime) // 先进先出，按接收日期排序
                .ToList();

            foreach (var balance in fifoBalances)
            {
                if (remainingQty <= 0) break;

                if (balance.Quantity >= remainingQty)
                {
                    // 当前批次足够完成出库
                    balance.Quantity -= remainingQty;
                    balance.Amount -= remainingQty * balance.Price;
                    remainingQty = 0;

                    if (balance.Quantity == 0)
                    {
                        //后期考虑不进行物理删除，而是作废并且修改更新时间
                        db.Deleteable<InventoryBalance>().Where(x => x.Id == balance.Id).ExecuteCommand();
                    }
                    else
                    {
                        balance.UpdateTime = DateTime.Now;
                        db.Updateable(balance).ExecuteCommand();
                    }
                }
                else
                {
                    // 当前批次不足，完全消耗该批次
                    remainingQty -= balance.Quantity;
                    db.Deleteable<InventoryBalance>().Where(x => x.Id == balance.Id).ExecuteCommand();
                }
            }

            // 检查是否完全出库
            if (remainingQty > 0)
            {
                // 库存不足处理
                throw new InventoryShortageException(
                    $"库存不足: 物料 {invInOut.InvCode}, SKU {invInOut.SKU}, 仓库 {transaction.WarehouseCode}.\n" +
                    $"需求: {invInOut.Quantity}, 可用: {invInOut.Quantity - remainingQty}");
            }
        }

        // 库存不足异常类
        public class InventoryShortageException : Exception
        {
            public InventoryShortageException(string message) : base(message) { }
        }

        public static void UpdateBalance(Ic ic)
        {
            SqlClient SSC = new SqlClient();
            var WarehouseCode = ic.WarehouseCode;
            foreach (var invTrx in ic.InvTrxs)
            {
                InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                    .IncludesAllFirstLayer()
                    .Where(xx => /* xx.FiscalYear == int.Parse(FiscalYear) && xx.Period == int.Parse(Period) &&  */xx.WarehouseCode == WarehouseCode && xx.InvCode == invTrx.InvCode)
                    .First();
                if (invBalOld == null)
                {
                    InventoryBalance invBalNew = new()
                    {
                        WarehouseCode = WarehouseCode,
                        InvCode = invTrx.InvCode,
                        Quantity = invTrx.Quantity,
                        Price = invTrx.Price,
                        Amount = invTrx.Amount,
                        Note = ic.Code.ToString(),
                        InsertTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                    };
                    invBalNew.WarehouseCode = WarehouseCode;
                    SSC.Db.Insertable(invBalNew).ExecuteCommand();
                }
                else
                {
                    if (ic.TrxType == 1)
                    {
                        invBalOld.Quantity += invTrx.Quantity;
                        invBalOld.Amount += invTrx.Amount;
                        if (invBalOld.Quantity != 0) invBalOld.Price = invBalOld.Amount / invBalOld.Quantity;
                        invBalOld.Note = ic.Code.ToString();
                        invBalOld.UpdateTime = DateTime.Now;
                        SSC.Db.Updateable(invBalOld).ExecuteCommand();
                    }
                    else if (ic.TrxType == -1)
                    {
                        decimal Price;
                        invBalOld.Quantity -= invTrx.Quantity;
                        invBalOld.Amount -= invTrx.Quantity* invBalOld.Price;  //出库单价不变只减去数量和对应金额
                        invBalOld.Note = ic.Code.ToString();
                        invBalOld.UpdateTime = DateTime.Now;
                        SSC.Db.Updateable(invBalOld).ExecuteCommand();
                    }
                }
            }
        }

        public static void UpdateBalance(String WarehouseCode,int TrxType, List<InvTrx> invTrxs)
        {
            SqlClient SSC = new SqlClient();
            foreach (var invTrx in invTrxs)
            {
                InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                    .IncludesAllFirstLayer()
                    .Where(xx => xx.WarehouseCode == WarehouseCode && xx.InvCode == invTrx.InvCode)
                    .First();
                if (invBalOld == null)
                {
                    InventoryBalance invBalNew = new()
                    {
                        WarehouseCode = WarehouseCode,
                        InvCode = invTrx.InvCode,
                        Quantity = invTrx.Quantity,
                        Price = invTrx.Price,
                        Amount = invTrx.Amount,
                        InsertTime = DateTime.Now,
                        UpdateTime = DateTime.Now,
                    };
                    invBalNew.WarehouseCode = WarehouseCode;
                    SSC.Db.Insertable(invBalNew).ExecuteCommand();
                }
                else
                {
                    if (TrxType == 1)
                    {
                        invBalOld.Quantity += invTrx.Quantity;
                        invBalOld.Amount += invTrx.Amount;
                        if (invBalOld.Quantity != 0) invBalOld.Price = invBalOld.Amount / invBalOld.Quantity;
                        invBalOld.UpdateTime = DateTime.Now;
                        SSC.Db.Updateable(invBalOld).ExecuteCommand();
                    }
                    else if (TrxType == -1)
                    {
                        invBalOld.Quantity -= invTrx.Quantity;
                        invBalOld.Amount -= invTrx.Amount;
                        if (invBalOld.Quantity != 0) invBalOld.Price = invBalOld.Amount / invBalOld.Quantity;
                        invBalOld.UpdateTime = DateTime.Now;
                        SSC.Db.Updateable(invBalOld).ExecuteCommand();
                    }
                }
            }
        }
        public static void UpdateBalance(String WarehouseCode, int TrxType, InvTrx invTrx)
        {
            SqlClient SSC = new SqlClient();

            InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                .IncludesAllFirstLayer()
                .Where(xx => xx.WarehouseCode == WarehouseCode && xx.InvCode == invTrx.InvCode)
                .First();
            if (invBalOld == null)
            {
                InventoryBalance invBalNew = new()
                {
                    WarehouseCode = WarehouseCode,
                    InvCode = invTrx.InvCode,
                    Quantity = invTrx.Quantity,
                    Price = invTrx.Price,
                    Amount = invTrx.Amount,
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                };
                invBalNew.WarehouseCode = WarehouseCode;
                SSC.Db.Insertable(invBalNew).ExecuteCommand();
            }
            else
            {
                if (TrxType == 1)
                {
                    invBalOld.Quantity += invTrx.Quantity;
                    invBalOld.Amount += invTrx.Amount;
                    if (invBalOld.Quantity != 0) invBalOld.Price = invBalOld.Amount / invBalOld.Quantity;
                    invBalOld.UpdateTime = DateTime.Now;
                    SSC.Db.Updateable(invBalOld).ExecuteCommand();
                }
                else if (TrxType == -1)
                {
                    invBalOld.Quantity -= invTrx.Quantity;
                    invBalOld.Amount -= invTrx.Amount;
                    if (invBalOld.Quantity != 0) invBalOld.Price = invBalOld.Amount / invBalOld.Quantity;
                    invBalOld.UpdateTime = DateTime.Now;
                    SSC.Db.Updateable(invBalOld).ExecuteCommand();
                }
            }
            
        }
    }
}
