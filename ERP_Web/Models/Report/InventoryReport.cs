using System.ComponentModel.DataAnnotations;

namespace ERP_Web.Models.Report
{
    #region 报表DTO实体
    /// <summary>
    /// 收发存汇总表DTO
    /// </summary>
    public class InventoryStockSummaryDto
    {
        [Display(Name = "仓库")]
        public string WarehouseName { get; set; } = "";
        [Display(Name = "商品编码")]
        public string InvCode { get; set; } = "";
        [Display(Name = "商品名称")]
        public string InvName { get; set; } = "";
        [Display(Name = "规格")]
        public string SpecDescription { get; set; } = "";
        [Display(Name = "单位")]
        public string Unit { get; set; } = "";

        #region 期初
        [Display(Name = "期初数量")]
        public decimal BeginQty { get; set; } = 0;
        [Display(Name = "期初单价")]
        public decimal BeginPrice { get; set; } = 0;
        [Display(Name = "期初金额")]
        public decimal BeginAmount { get; set; } = 0;
        #endregion

        #region 本期入库
        [Display(Name = "入库数量")]
        public decimal InQty { get; set; } = 0;
        [Display(Name = "入库单价")]
        public decimal InPrice { get; set; } = 0;
        [Display(Name = "入库金额")]
        public decimal InAmount { get; set; } = 0;
        #endregion

        #region 本期出库
        [Display(Name = "出库数量")]
        public decimal OutQty { get; set; } = 0;
        [Display(Name = "出库单价")]
        public decimal OutPrice { get; set; } = 0;
        [Display(Name = "出库金额")]
        public decimal OutAmount { get; set; } = 0;
        #endregion

        #region 期末结余
        [Display(Name = "结余数量")]
        public decimal EndQty { get; set; } = 0;
        [Display(Name = "结余单价")]
        public decimal EndPrice { get; set; } = 0;
        [Display(Name = "结余金额")]
        public decimal EndAmount { get; set; } = 0;
        #endregion
    }

    /// <summary>
    /// 出入库流水账DTO
    /// </summary>
    public class InventoryFlowDto
    {
        [Display(Name = "单据日期")]
        public DateOnly TransactionDate { get; set; }
        [Display(Name = "单据号")]
        public string TransactionCode { get; set; } = "";
        [Display(Name = "收发类别")]
        public string TrxGroupName { get; set; } = "";
        [Display(Name = "收发类别")]
        public int TrxType { get; set; }
        [Display(Name = "出入方向")]
        public string Direction { get; set; } = ""; // 入库/出库
        [Display(Name = "仓库")]
        public string WarehouseName { get; set; } = "";
        [Display(Name = "商品编码")]
        public string InvCode { get; set; } = "";
        [Display(Name = "商品名称")]
        public string InvName { get; set; } = "";
        [Display(Name = "规格")]
        public string SpecDescription { get; set; } = "";
        [Display(Name = "单位")]
        public string Unit { get; set; } = "";
        [Display(Name = "数量")]
        public decimal Quantity { get; set; } = 0;
        [Display(Name = "单价")]
        public decimal Price { get; set; } = 0;
        [Display(Name = "金额")]
        public decimal Amount { get; set; } = 0;
        [Display(Name = "摘要")]
        public string Explanation { get; set; } = "";
        [Display(Name = "制单人")]
        public string Operator { get; set; } = "";
        [Display(Name = "审批人")]
        public string Approver { get; set; } = "";
    }

    /// <summary>
    /// 库存预警DTO
    /// </summary>
    public class InventoryAlarmDto
    {
        public string WarehouseName { get; set; } = "";
        public string InvCode { get; set; } = "";
        public string InvName { get; set; } = "";
        public string SpecDescription { get; set; } = "";
        public decimal CurrentQty { get; set; } = 0;
        public decimal SafeStock { get; set; } = 0; // 可以后续在InventoryItem加安全库存字段
        public decimal ShortageQty => SafeStock - CurrentQty > 0 ? SafeStock - CurrentQty : 0;
        public string AlarmLevel => ShortageQty switch
        {
            > 50 => "严重短缺",
            > 10 => "短缺",
            > 0 => "预警",
            _ => "正常"
        };
    }
    #endregion

}
