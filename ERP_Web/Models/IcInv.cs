using ERP_Web.Repository;
using NPOI.Util;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace ERP_Web.Models
{
    public class Unit
    {
        [SugarColumn(IsIdentity = true)]
        int Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        public string Name { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }
    public class InventoryUnit
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { set; get; }
        [Display(Name = "存货编码")]
        public string InvCode { set; get; }
        [Display(Name = "单位")]
        public string Unit { set; get; }
        public bool? IsMinimumUnit { set; get; } = false;
        public decimal ConversionRatio { set; get; } //换算
        //[Navigate(typeof(InvUnitMapping), nameof(InvUnitMapping.UnitCode), nameof(InvUnitMapping.InvCode))]//注意顺序
        //public List<InventoryUnit> InvList { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
        public InventoryUnit()
        {
            if (ConversionRatio == 0) ConversionRatio = 1;
            if (IsMinimumUnit == true) ConversionRatio = 1;
        }
    }
    public class InvUnitMapping
    {
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public int InvCode { get; set; }
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public int UnitCode { get; set; }
    }
    public class IcInv
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; }

        [SugarColumn(IsPrimaryKey = true)]
        //[Display(Description = "商品编码")]
        public string Code { set; get; }
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
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;
        public bool Active { set; get; } = true;

        //[Display(Description = "商品图片")]
        //public string? InvImage { set; get; } = "";
    }

    public class InventoryCostingMethod
    {
        [SugarColumn(IsIdentity = true)]
        [Required]  //必填标记
        //[Display(Description = "商品编码")]
        public int Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        [Display(Name = "编码")]
        public string Code { set; get; } 
        [Display(Name = "名称")]
        public string Name { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;

        public static List<InventoryCostingMethod> Select()
        {
            SqlClient SSC = new SqlClient();
            List<InventoryCostingMethod> result = SSC.Db.Queryable<InventoryCostingMethod>()
                .IncludesAllFirstLayer().Where(x => x.Active == true).ToList();
            return result;
        }
    }
  
}
