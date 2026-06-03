using ERP_Web.Repository;
using NPOI.XWPF.UserModel;
using SqlSugar;
//using Microsoft.EntityFrameworkCore.Metadata.Internal;
//using SqlSugar;
using System;
//using SysteCollections.Generic;
//using SysteComponentModel.DataAnnotations;
//using SysteComponentModel.DataAnnotations.Schema;
//using SysteDrawing.Printing;
//using SysteReflection.Emit;
//using SysteRuntime.CompilerServices;
//using SysteRuntime.Intrinsics.X86;
//using static Dnet.buffer.ByteArrayBuffer;
//using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Database;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ERP_Web.Models
{

    /// <summary>
    /// 库存出入库主表
    /// 入库单，出库单，调拨单隐藏，直拨单显示，trxType 1入库，-1出库，调拨  0直拨
    /// </summary>
    public class Ic
    {
        //下次改造：修改名称为InventoryTransaction
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Code { set; get; }
        public string? TrxNo { set; get; }
        [Display(Name = "出入库类型")]
        public string TrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(TrxGroupCode))]//一对一 

        public TrxGroup TrxGroup { set; get; } = new OneToOneInitializer<TrxGroup>();

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
        public int? InvTrxCode {  set; get; }
        [Navigate(NavigateType.OneToMany, nameof(InvTrx.IcCode))]//一对多
        public List<InvTrx>? InvTrxs { set; get; }

        public Ic()
        {
            FiscalYear = Date.Year;
            Period = Date.Month;        
        }

        public void UpdateInvPrice()
        {
            if (InvTrxs != null)
            {
                if (this.TrxType == -1) {
                    foreach (InvTrx invTrx in InvTrxs)
                    {
                        SqlClient SSC = new SqlClient();
                        InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                            .IncludesAllFirstLayer()
                            .Where(xx => xx.WarehouseCode == this.WarehouseCode && xx.InvCode == invTrx.InvCode)
                            .First();
                        if (invBalOld == null) return;
                        invTrx.Price = invBalOld.Price;
                        invTrx.TaxRate = 0;
                        invTrx.PriceChange();
                    }
                }
                UpdateAmount();
            }
        }
        public void UpdateAmount()
        {
            Quantity = 0;
            Amount = 0;
            AmountIncTax = 0;
            if (InvTrxs != null)
            {
                foreach (InvTrx invTrx in InvTrxs)
                {
                    Quantity += invTrx.Quantity;
                    Amount += invTrx.Amount;
                    AmountIncTax += invTrx.AmountIncTax;
                }
            }
        }
    }
    public class InvTrx
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { set; get; }
        public int? IcCode { set; get; }
        public string InvCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public IcInv Inv { set; get; } = new OneToOneInitializer<IcInv>();
        public decimal Quantity { set; get; } = 0;
        public decimal Price { set; get; } = 0;
        public decimal Amount { set; get; }
        public decimal TaxRate { set; get; } = 0;
        public decimal PriceIncTax { set; get; } = 0;
        public decimal AmountIncTax { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨

        public InvTrx()
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
            this.Price =this.PriceIncTax / (1 + this.TaxRate);
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

        public void InvTrxUpdateCode(int Code)
        {
            this.IcCode = Code;
            if (this.Inv != null) {
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
    }

    public class InventoryBalance2
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string InvCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public IcInv Inv { set; get; } = new OneToOneInitializer<IcInv>();
        [Display(Name = "数量")]
        public decimal Quantity { set; get; } = 0;
        [Display(Name = "单价")]
        public decimal Price { set; get; } = 0;
        [Display(Name = "金额")]
        public decimal Amount { set; get; } = 0;
        [Display(Name = "税率")]
        public decimal TaxRate { set; get; }
        [Display(Name = "含税单价")]
        public decimal PriceIncTax { set; get; }
        [Display(Name = "含税金额")]
        public decimal AmountIncTax { set; get; } = 0;
        [Display(Name = "备注")]
        public string? Note { set; get; }

        public DateTime InsertTime { set; get; } = DateTime.Now;
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;


        public static void UpdateBalance(List<Ic> ics)
        {
            SqlClient SSC = new SqlClient();
            foreach (var ic in ics)
            {
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
                            invBalOld.Quantity -= invTrx.Quantity;
                            invBalOld.Amount -= invTrx.Amount;
                            if (invBalOld.Quantity != 0) invBalOld.Price = invBalOld.Amount / invBalOld.Quantity;
                            invBalOld.Note = ic.Code.ToString();
                            invBalOld.UpdateTime = DateTime.Now;
                            SSC.Db.Updateable(invBalOld).ExecuteCommand();
                        }
                    }
                }
            }
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
