using AntDesign;
using ERP_Web.Repository;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Web.Models
{
    public enum PoTrxType
    {
        PurchaseOrder = 1,
        PurchaseReturn = -1
    }

    public class PoTrxGroup
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; } = "";
        public string Name { get; set; } = ""; //采购类型名称:生鲜、百货、服装、机动采购
        //public int TrxType { get; set; } //1、Inbound（入库），-1、Outbound（出库），0、InThenOut  先入后出 直入直出
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }
    /// <summary>
    /// 采购申请单存货明细
    /// </summary>
    public class PRInvInOut : InvInOut
    {
        public string? PRCode { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();
    }

    /// <summary>
    /// 采购申请单 内部审批
    /// </summary>
    public class PurchaseRequisitions : BaseDocument
    {
        [Display(Name = "采购类型")]
        public string? PRTrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(PRTrxGroupCode))]//一对一 
        public PoTrxGroup PoTrxGroup { set; get; } = new OneToOneInitializer<PoTrxGroup>();
        //public string? WarehouseCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        //public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        //public string? CompanyCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        //public Company Company { set; get; } = new OneToOneInitializer<Company>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Explanation { set; get; }
        public string? Operator { set; get; }
        public string? Checker { set; get; }
        public string? Poster { set; get; }

        [Navigate(NavigateType.OneToMany, nameof(PRInvInOut.PRCode))]//一对多
        public List<PRInvInOut>? PRInvInOuts { set; get; }

        public PurchaseRequisitions()
        {
            GetTrxNo("PR");
            if (PRInvInOuts == null) PRInvInOuts = new List<PRInvInOut>();

        }

        public void UpdateInvPrice()
        {
            if (PRInvInOuts != null)
            {
                foreach (PRInvInOut prInvInOut in PRInvInOuts)
                {
                    prInvInOut.UpdatePriceByIn(); 
                }
            }
        }
        public void UpdateAmount()
        {
            Quantity = 0;
            Amount = 0;
            AmountIncTax = 0;
            if (PRInvInOuts != null && PRInvInOuts.Count > 0)
            {
                foreach (PRInvInOut prInvInOut in PRInvInOuts)
                {
                    Quantity += prInvInOut.Quantity;
                    Amount += prInvInOut.Amount;
                    AmountIncTax += prInvInOut.AmountIncTax;
                }
            }
        }
        public void CreatReceivingNote()
        {

        }
        public POInvInOut PRInvInOutToPO(PRInvInOut prInvInOut)
        {
            POInvInOut poInvInOut = new POInvInOut()
            {
                InvCode = prInvInOut.InvCode,
                InventoryItem = prInvInOut.InventoryItem,
                Quantity = prInvInOut.Quantity,
                Price = prInvInOut.Price,
                Amount = prInvInOut.Amount,
                TaxRate = prInvInOut.TaxRate,
                PriceIncTax = prInvInOut.PriceIncTax,
                AmountIncTax = prInvInOut.AmountIncTax,
                Note = prInvInOut.Note,
                Sequence = prInvInOut.Sequence,
                Active = prInvInOut.Active,
                WarehouseCode = prInvInOut.WarehouseCode,
                //CompanyCode = prInvInOut.CompanyCode
            };
            return poInvInOut;
        }

        public List<PurchaseOrder> CreatPurchaseOrder()
        {
            string[] companyCodes = new string[] { };
            List<PurchaseOrder> purchaseOrders = new List<PurchaseOrder>(); //按公司代码分组生成多个采购订单

            foreach (PRInvInOut prInvInOut in this.PRInvInOuts)
            {
                string companyCode = prInvInOut.CompanyCode ?? "";

                if (purchaseOrders.Count == 0)
                {
                    PurchaseOrder po = new PurchaseOrder()
                    {
                        PRTrxGroupCode = this.PRTrxGroupCode,
                        DepartmentCode = this.DepartmentCode,
                        EmployeeCode = this.EmployeeCode,
                        CompanyCode = companyCode,
                        Explanation = this.Explanation,
                        Operator = this.Operator,
                        Checker = this.Checker,
                        Poster = this.Poster,
                        POInvInOuts = new List<POInvInOut>()
                    };
                    companyCodes = companyCodes.Append(companyCode).Distinct().ToArray();
                    purchaseOrders.Add(po);
                }

                for (int i = 0; i < purchaseOrders.Count; i++)
                {
                    //PurchaseOrder purchaseOrder1 = purchaseOrders[i];
                    if (purchaseOrders[i].CompanyCode == companyCode)
                    {
                        //POInvInOut poInvInOut = PRInvInOutToPO(prInvInOut);
                        purchaseOrders[i].POInvInOuts.Add(PRInvInOutToPO(prInvInOut));
                        break;
                    }
                    else
                    {
                        bool exists = Array.Exists(companyCodes, element => element == companyCode);
                        if (exists) continue;
                        PurchaseOrder po = new PurchaseOrder()
                        {
                            PRTrxGroupCode = this.PRTrxGroupCode,
                            DepartmentCode = this.DepartmentCode,
                            EmployeeCode = this.EmployeeCode,
                            CompanyCode = companyCode,
                            Explanation = this.Explanation,
                            Operator = this.Operator,
                            Checker = this.Checker,
                            Poster = this.Poster,
                            POInvInOuts = new List<POInvInOut>()
                        };
                        companyCodes = companyCodes.Append(companyCode).Distinct().ToArray();
                        //POInvInOut poInvInOut = PRInvInOutToPO(prInvInOut);
                        po.POInvInOuts.Add(PRInvInOutToPO(prInvInOut));
                        purchaseOrders.Add(po);
                    }

                }
            }
            return purchaseOrders;
        }
    }

    /// <summary>
    /// 采购退货单存货明细
    /// </summary>
    public class POInvInOut : InvInOut
    {
        public string? POCode { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        //public string? CompanyCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        //public Company Company { set; get; } = new OneToOneInitializer<Company>();
    }

    //采购订单 正式采购 采购员同步发送到供应商
    public class PurchaseOrder : BaseDocument
    {
        [Display(Name = "采购类型")]
        public string? PRTrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(PRTrxGroupCode))]//一对一 
        public PoTrxGroup PoTrxGroup { set; get; } = new OneToOneInitializer<PoTrxGroup>();
        //public string? WarehouseCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        //public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();

        public string? Operator { set; get; }
        public string? Checker { set; get; }
        public string? Poster { set; get; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;

        [Navigate(NavigateType.OneToMany, nameof(POInvInOut.POCode))]//一对多
        public List<POInvInOut>? POInvInOuts { set; get; }

        public PurchaseOrder()
        {
            if (POInvInOuts == null) POInvInOuts = new List<POInvInOut>();
        }

        public void UpdateAmount()
        {
            Quantity = 0;
            Amount = 0;
            AmountIncTax = 0;
            if (POInvInOuts != null && POInvInOuts.Count > 0)
            {
                foreach (POInvInOut poInvInOut in POInvInOuts)
                {
                    Quantity += poInvInOut.Quantity;
                    Amount += poInvInOut.Amount;
                    AmountIncTax += poInvInOut.AmountIncTax;
                }
            }
        }
        public void CreatReceivingNote()
        {

        }

    }

    //采购收货单 供应商确认后自动生成，或者由采购申请单直接生成 记录实际收货情况
    public class ReceivingNote
    {
        [SugarColumn(IsIdentity = true)]
        public long Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        public long Code { set; get; }
        [Display(Name = "单号")]
        public string? TrxNo { set; get; }
        [Display(Name = "采购类型")]
        public string PRTrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(PRTrxGroupCode))]//一对一 
        public PoTrxGroup PoTrxGroup { set; get; } = new OneToOneInitializer<PoTrxGroup>();
        public int TrxType { set; get; }
        //public string? WarehouseCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        //public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        //public string? CompanyCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        //public Company Company { set; get; } = new OneToOneInitializer<Company>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Explanation { set; get; }
        public DateOnly Date { set; get; } = DateOnly.FromDateTime(DateTime.Now);
        public decimal Quantity { set; get; } = 0;
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { set; get; } = 0;
        [Column(TypeName = "decimal(4,2)")]
        public decimal TaxRate { set; get; } = 0;
        [Display(Name = "含税金额")]
        public decimal AmountIncTax { set; get; } = 0;
        public string? Note { set; get; }
        public string? Operator { set; get; }
        public string? Checker { set; get; }
        public string? Poster { set; get; }

        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public int? FiscalYear { set; get; }
        public int? Period { set; get; }

        public DateTime InsertTime { set; get; } = DateTime.Now;
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;

        //[Navigate(NavigateType.OneToMany, nameof(POInvTrx.POCode))]//一对多
        //public List<POInvTrx>? POInvTrxs { set; get; }

        //public ReceivingNote()
        //{
        //    FiscalYear = Date.Year;
        //    Period = Date.Month;
        //}

        //public void UpdateInvPrice()
        //{
        //    if (POInvTrxs != null)
        //    {
        //        if (this.TrxType == -1)
        //        {
        //            foreach (POInvTrx poInvTrx in POInvTrxs)
        //            {
        //                SqlClient SSC = new SqlClient();
        //                InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
        //                    .IncludesAllFirstLayer()
        //                    .Where(xx => xx.WarehouseCode == poInvTrx.WarehouseCode && xx.InvCode == poInvTrx.InvCode)
        //                    .First();
        //                if (invBalOld == null) return;
        //                poInvTrx.Price = invBalOld.Price;
        //                poInvTrx.TaxRate = 0;
        //                poInvTrx.PriceChange();
        //            }
        //        }
        //        UpdateAmount();
        //    }
        //}
        //public void UpdateAmount()
        //{
        //    Quantity = 0;
        //    Amount = 0;
        //    AmountIncTax = 0;
        //    if (POInvTrxs != null && POInvTrxs.Count > 0)
        //    {
        //        foreach (POInvTrx poInvTrx in POInvTrxs)
        //        {
        //            Quantity += poInvTrx.Quantity;
        //            Amount += poInvTrx.Amount;
        //            AmountIncTax += poInvTrx.AmountIncTax;
        //        }
        //    }
        //}
        //生成采购入库单
        public void CreatWarehouseWarrant()
        {

        }
    }


    /* public class POInvTrx
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { set; get; }
        public long? POCode { set; get; }
        public string InvCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public IcInv Inv { set; get; } = new OneToOneInitializer<IcInv>();
        public decimal Quantity { set; get; } = 0;
        //public decimal CheckQuantity { set; get; } = 0; 
        public decimal Price { set; get; } = 0;
        public decimal Amount { set; get; }
        public decimal TaxRate { set; get; } = 0;
        public decimal PriceIncTax { set; get; } = 0;
        public decimal AmountIncTax { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>(); 
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨

        public POInvTrx()
        {

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

        public void InvTrxUpdateCode(long Code)
        {
            this.POCode = Code;
            if (this.Inv != null)
            {
                this.InvCode = this.Inv.Code;
            }
        }

        public void UpdatePrice(decimal Price)
        {
            this.Price = Price;
        }

        public void UpdateAmount()
        {
            this.Amount = this.Quantity * this.Price;
            if (this.TaxRate == null) this.TaxRate = 0;
            this.PriceIncTax = this.Price * (1 + this.TaxRate);
            this.AmountIncTax = this.Quantity * this.PriceIncTax;
        }
    }    */
}
