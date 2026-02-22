using Dm.util;
using ERP_Web.Models;
using ERP_Web.Models.Report;
using NPOI.SS.Formula.Functions;
using SqlSugar;

namespace ERP_Web.Repository
{
    public static class InventoryReport
    {
        #region 收发存汇总表查询
        /// <summary>
        /// 查询收发存汇总表
        /// </summary>
        /// <param name="year">统计年份</param>
        /// <param name="month">统计月份</param>
        /// <param name="warehouseCode">仓库编码（空则查所有）</param>
        /// <param name="invCode">商品编码（空则查所有）</param>
        /// <param name="categoryCode">商品分类（空则查所有）</param>
        /// <returns></returns>
        public static List<InventoryStockSummaryDto> GetStockSummary(int year, int month, string? warehouseCode = null, string? invCode = null, string? categoryCode = null)
        {
            var db = new SqlClient().Db;
            var startDate = new DateOnly(year, month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);
            var lastMonthEnd = startDate.AddDays(-1);

            // 1. 先取期初数据：优先从月结表取，没有则通过明细计算
            var closingRecord = db.Queryable<InventoryClosingRecord>()
                .First(x => x.ClosingDate == lastMonthEnd && x.Status == ClosingStatus.Completed);

            var InventoryBalanceHistorys = closingRecord != null
                ? InventoryBalanceHistory.SelectByClosingRecordId(closingRecord.Id)
                    .ToList()
                : new List<InventoryBalanceHistory>();

            var result = new List<InventoryStockSummaryDto>();

            // 2. 统计本期出入库明细
            var details = db.Queryable<InventoryTransaction>()
                .Includes(x => x.Warehouse)
                .Includes(x => x.ITInvInOuts, x => x.Specification)
                .Includes(x => x.ITInvInOuts, x => x.InventoryItem)
                .Where(x => x.Date >= startDate && x.Date <= endDate && x.Active == true /*&& !string.IsNullOrEmpty(x.Approver)*/)
                .WhereIF(!string.IsNullOrEmpty(warehouseCode), x => x.WarehouseCode == warehouseCode).ToList()
                .SelectMany(x => x.ITInvInOuts, (trx, detail) => new
                {
                    trx.WarehouseCode,
                    WarehouseName = trx.Warehouse.Name,
                    detail.InvCode,
                    InvName = detail.InventoryItem.Name,
                    detail.SKU,
                    detail.Specification.Description,
                    detail.Specification.Unit,
                    trx.TrxType,
                    detail.Quantity,
                    detail.Price,
                    detail.Amount
                })
                .WhereIF(!string.IsNullOrEmpty(invCode), x => x.InvCode == invCode)
                .ToList();

            foreach(var item in InventoryBalanceHistorys)
            {
                details.Add(new
                {
                    WarehouseCode = item.WarehouseCode,
                    WarehouseName = string.Empty,
                    InvCode = item.InvCode,
                    InvName = string.Empty,
                    SKU = item.SKU,
                    Description = string.Empty,
                    Unit = string.Empty,
                    TrxType = 0,
                    Quantity = item.Quantity*0,
                    Price = item.Price * 0,
                    Amount = item.Amount * 0
                });
            }

            // 3. 按仓库+商品+SKU分组统计
            var groups = details.GroupBy(x => new { x.WarehouseCode, x.InvCode, x.SKU }).ToList();

            foreach (var group in groups)
            {
                var item = new InventoryStockSummaryDto
                {
                    WarehouseName = group.First().WarehouseName,
                    InvCode = group.Key.InvCode,
                    InvName = group.First().InvName,
                    SpecDescription = group.First().Description ?? "无",
                    Unit = group.First().Unit ?? "个",
                    InQty = group.Where(x => x.TrxType == 1).Sum(x => x.Quantity),
                    InAmount = group.Where(x => x.TrxType == 1).Sum(x => x.Amount),
                    OutQty = group.Where(x => x.TrxType == -1).Sum(x => x.Quantity),
                    OutAmount = group.Where(x => x.TrxType == -1).Sum(x => x.Amount)
                };

                // 计算平均单价
                item.InPrice = item.InQty > 0 ? Math.Round(item.InAmount / item.InQty, 4) : 0;
                item.OutPrice = item.OutQty > 0 ? Math.Round(item.OutAmount / item.OutQty, 4) : 0;

                // 取期初数据
                if (closingRecord != null)
                {
                    var begin = InventoryBalanceHistory.SelectByInventory(closingRecord.Id,group.Key.WarehouseCode , group.Key.InvCode, group.Key.SKU);
                    if (begin != null)
                    {
                        item.BeginQty = begin.Quantity;
                        item.BeginAmount = begin.Amount;
                        item.BeginPrice = begin.Quantity > 0 ? Math.Round(begin.Amount / begin.Quantity, 4) : 0;
                    }
                }
                else
                {
                    // 没有月结数据则通过明细计算期初
                    var beginDetails = db.Queryable<InventoryTransaction>()
                        .Includes(x => x.ITInvInOuts)
                        .Where(x => x.Date < startDate && x.Active == true && !string.IsNullOrEmpty(x.Approver) && x.WarehouseCode == group.Key.WarehouseCode).ToList()
                        .SelectMany(x => x.ITInvInOuts, (trx, d) => new { trx.TrxType, trx.WarehouseCode, d.InvCode,d.SKU, d.Quantity, d.Amount })
                        .Where(x => x.InvCode == group.Key.InvCode && x.SKU == group.Key.SKU)
                        .ToList();
                    item.BeginQty = beginDetails.Sum(x => x.TrxType == 1 ? x.Quantity : -x.Quantity);
                    item.BeginAmount = beginDetails.Sum(x => x.TrxType == 1 ? x.Amount : -x.Amount);
                    item.BeginPrice = item.BeginQty > 0 ? Math.Round(item.BeginAmount / item.BeginQty, 4) : 0;
                }

                // 计算期末
                item.EndQty = item.BeginQty + item.InQty - item.OutQty;
                item.EndAmount = item.BeginAmount + item.InAmount - item.OutAmount;
                item.EndPrice = item.EndQty > 0 ? Math.Round(item.EndAmount / item.EndQty, 4) : 0;

                result.Add(item);
            }

            // 过滤分类
            if (!string.IsNullOrEmpty(categoryCode))
            {
                result = result.Where(x => db.Queryable<InventoryItem>().First(i => i.Code == x.InvCode).CategoryCode == categoryCode).ToList();
            }

            return result;
        }
        #endregion

        #region 出入库流水账查询
        /// <summary>
        /// 查询出入库流水
        /// </summary>
        public static List<InventoryFlowDto> GetFlowList(DateOnly startDate, DateOnly endDate, string? warehouseCode = null, string? invCode = null, string? trxGroupCode = null, int? trxType = null)
        {
            var db = new SqlClient().Db;

            return db.Queryable<InventoryTransaction>()
                .Includes(x => x.Warehouse)
                .Includes(x => x.TrxGroup)
                .Includes(x => x.ITInvInOuts)
                .Includes(x => x.Warehouse)
                .Includes(x => x.ITInvInOuts, x => x.Specification)
                .Includes(x => x.ITInvInOuts, x => x.InventoryItem)
                .Where(x => x.Date >= startDate && x.Date <= endDate && x.Active == true)
                .WhereIF(!string.IsNullOrEmpty(warehouseCode), x => x.WarehouseCode == warehouseCode)
                .WhereIF(!string.IsNullOrEmpty(trxGroupCode), x => x.TrxGroupCode == trxGroupCode)
                .WhereIF(trxType.HasValue, x => x.TrxType == trxType.Value).ToList()
                .SelectMany(x => x.ITInvInOuts, (trx, detail) => new InventoryFlowDto
                {
                    TransactionDate = trx.Date,
                    TransactionCode = trx.Code,
                    TrxGroupName = trx.TrxGroup.Name ?? "未知",
                    Direction = trx.TrxType == 1 ? "入库" : "出库",
                    WarehouseName = trx.Warehouse.Name ?? "未知",
                    InvCode = detail.InvCode,
                    InvName = detail.InventoryItem.Name ?? "未知",
                    SpecDescription = detail.Specification.Description ?? "无",
                    Unit = detail.Specification.Unit ?? "个",
                    Quantity = detail.Quantity,
                    Price = detail.Price,
                    Amount = detail.Amount,
                    Explanation = trx.Explanation ?? "",
                    Operator = trx.Operator ?? "",
                    Approver = trx.Approver ?? ""
                })
                .WhereIF(!string.IsNullOrEmpty(invCode), x => x.InvCode == invCode || x.InvName.Contains(invCode))
                .OrderByDescending(x => x.TransactionDate)
                .ToList();
        }
        /// <summary>
        /// 查询出入库流水账
        /// </summary>
        /// <param name="startDate">开始日期</param>
        /// <param name="endDate">结束日期</param>
        /// <param name="warehouseCode">仓库编码（可选）</param>
        /// <param name="categoryCode">商品分类编码（可选）</param>
        /// <param name="invName">商品名称/编码模糊查询（可选）</param>
        /// <param name="trxType">出入库类型：1=入库，-1=出库（可选）</param>
        /// <returns>流水明细列表</returns>
        public static List<InventoryFlowDto> GetFlow(DateOnly startDate, DateOnly endDate,
            string? warehouseCode = null, string? categoryCode = null,
            string? invName = null, int? trxType = null)
        {
            using var db = new SqlClient().Db;

            var query = db.Queryable<InventoryTransaction>()
                // 关联出入库明细
                .LeftJoin<ITInvInOut>((trx, detail) => trx.Code == detail.IcCode)
                // 关联仓库
                .LeftJoin<Warehouse>((trx, detail, wh) => trx.WarehouseCode == wh.Code)
                // 关联商品
                .LeftJoin<InventoryItem>((trx, detail, wh, item) => detail.InvCode == item.Code)
                // 关联规格
                .LeftJoin<Specification>((trx, detail, wh, item, spec) => detail.SKU == spec.SKU && detail.InvCode == spec.InvCode)
                // 基础过滤：已审核、未删除
                .Where((trx) => trx.Date >= startDate && trx.Date <= endDate
                            && trx.Active == true /*&& !string.IsNullOrEmpty(trx.Approver)*/)
                // 可选筛选条件
                .WhereIF(!string.IsNullOrEmpty(warehouseCode), (trx) => trx.WarehouseCode == warehouseCode)
                .WhereIF(!string.IsNullOrEmpty(categoryCode), (trx, detail, wh, item) => item.CategoryCode == categoryCode)
                .WhereIF(!string.IsNullOrEmpty(invName), (trx, detail, wh, item) => item.Name.Contains(invName) || item.Code.Contains(invName))
                .WhereIF(trxType.HasValue, (trx) => trx.TrxType == trxType.Value)
                // 字段映射（已解决属性重名问题，仓库名重命名为WarehouseName，商品名重命名为InvName）
                .OrderBy(trx => trx.Date)
                .Select((trx, detail, wh, item, spec) => new InventoryFlowDto
                {
                    TransactionDate = trx.Date,
                    WarehouseName = wh.Name,
                    InvCode = item.Code,
                    InvName = item.Name,
                    SpecDescription = spec.Description ?? "",
                    Unit = spec.Unit ?? "",
                    TrxType = trx.TrxType,
                    Quantity = trx.TrxType == 1 ? detail.Quantity : -detail.Quantity,
                    Price = detail.Price,
                    Amount = trx.TrxType == 1 ? detail.Amount : -detail.Amount,
                    TransactionCode = trx.Code,
                    Explanation = trx.Explanation ?? ""
                })
                .ToList();
            var res = query;
            return res;


        }
        #endregion

        #region 库存预警查询
        public static List<InventoryAlarmDto> GetAlarmList(string? warehouseCode = null)
        {
            var db = new SqlClient().Db;
            return db.Queryable<InventoryBalance>()
                .Includes(x => x.Warehouse)
                .Includes(x => x.Inv)
                .Includes(x => x.Specification)
                .Where(x => x.Quantity > 0)
                .WhereIF(!string.IsNullOrEmpty(warehouseCode), x => x.WarehouseCode == warehouseCode)
                .Select(x => new InventoryAlarmDto
                {
                    WarehouseName = x.Warehouse.Name,
                    InvCode = x.InvCode,
                    InvName = x.Inv.Name,
                    SpecDescription = x.Specification.Description ?? "无",
                    CurrentQty = x.Quantity,
                    // 如果InventoryItem加了SafeStock字段，这里直接取就行
                    // SafeStock = x.Inv.SafeStock
                })
                .ToList();
        }
        #endregion
    }
}
