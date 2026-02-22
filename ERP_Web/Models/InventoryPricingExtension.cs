using ERP_Web.Models;
using ERP_Web.Repository;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ERP_Web.Models
{
    /// <summary>
    /// 存货计价方式扩展类
    /// 提供按不同计价方式计算出库单价的功能
    /// 支持：移动加权平均法(MWA)、先进先出法(FIFO)、个别计价法(SPEC)、全月平均法(WA)
    /// </summary>
    public static class InventoryPricingExtension
    {
        /// <summary>
        /// 计算指定商品在指定仓库的出库单价
        /// </summary>
        /// <param name="warehouseCode">仓库编码</param>
        /// <param name="invCode">存货编码</param>
        /// <param name="sku">商品SKU</param>
        /// <param name="quantity">出库数量</param>
        /// <param name="costingMethodCode">计价方式编码（可选，自动从商品档案获取）</param>
        /// <returns>出库单价</returns>
        /// <exception cref="ArgumentException">参数错误</exception>
        /// <exception cref="InvalidOperationException">计价方式不支持</exception>
        /// <exception cref="InventoryShortageException">库存不足</exception>
        public static decimal CalculateOutboundPrice(string warehouseCode, string invCode, string sku, decimal quantity, string costingMethodCode = null)
        {
            if (string.IsNullOrWhiteSpace(warehouseCode))
                throw new ArgumentException("仓库编码不能为空", nameof(warehouseCode));
            if (string.IsNullOrWhiteSpace(invCode))
                throw new ArgumentException("存货编码不能为空", nameof(invCode));
            if (string.IsNullOrWhiteSpace(sku))
                throw new ArgumentException("SKU不能为空", nameof(sku));
            if (quantity < 0)
                throw new ArgumentException("出库数量必须大于0", nameof(quantity));

            // 如果未指定计价方式，从商品档案获取
            if (string.IsNullOrWhiteSpace(costingMethodCode))
            {
                var inventoryItem = InventoryItem.Select(invCode);
                if (inventoryItem == null)
                    throw new ArgumentException($"未找到存货编码为{invCode}的商品档案", nameof(invCode));
                
                costingMethodCode = inventoryItem.CostingMethodCode;
                if (string.IsNullOrWhiteSpace(costingMethodCode))
                    throw new InvalidOperationException($"商品{invCode}未设置计价方式");
            }

            using var db = new SqlClient().Db;
            return costingMethodCode switch
            {
                "MWA" => CalculateMovingWeightedAveragePrice(db, warehouseCode, invCode, sku),
                "FIFO" => CalculateFifoPrice(db, warehouseCode, invCode, sku, quantity),
                "SPEC" => CalculateSpecificPrice(db, warehouseCode, invCode, sku),
                "WA" => CalculateWeightedAveragePrice(db, warehouseCode, invCode, sku),
                _ => throw new InvalidOperationException($"不支持的计价方式: {costingMethodCode}")
            };
        }

        /// <summary>
        /// 计算移动加权平均法下的出库单价
        /// </summary>
        private static decimal CalculateMovingWeightedAveragePrice(SqlSugarClient db, string warehouseCode, string invCode, string sku)
        {
            var balance = db.Queryable<InventoryBalance>()
                .Where(x => x.WarehouseCode == warehouseCode &&
                            x.InvCode == invCode &&
                            x.SKU == sku &&
                            x.Quantity > 0)
                .First();
            if (balance == null)
            {
                return 0;
                //throw new InventoryShortageException($"库存不足: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}");
            }
            return balance.Price;
        }

        /// <summary>
        /// 计算先进先出法下的出库单价（加权平均）
        /// </summary>
        private static decimal CalculateFifoPrice(SqlSugarClient db, string warehouseCode, string invCode, string sku, decimal quantity)
        {
            decimal remainingQty = quantity;
            decimal totalAmount = 0;
            decimal totalQty = 0;

            var fifoBalances = db.Queryable<FIFOInventoryLot>()
                .Where(x => x.WarehouseCode == warehouseCode &&
                            x.InvCode == invCode &&
                            x.SKU == sku &&
                            x.Quantity > 0)
                .OrderBy(x => x.InsertTime) // 先进先出，按入库时间排序
                .ToList();

            if (!fifoBalances.Any())
            {
                return 0;
                //throw new InventoryShortageException($"库存不足: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}");
            }

            foreach (var lof in fifoBalances)
            {
                if (remainingQty <= 0) break;

                decimal takeQty = Math.Min(remainingQty, lof.Quantity);  
                totalAmount += takeQty * lof.Price;
                totalQty += takeQty;
                remainingQty -= takeQty;
            }

            if (remainingQty > 0)
            {
                throw new InvalidOperationException(
                    $"库存不足: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}.\n" +
                    $"需求: {quantity}, 可用: {totalQty}");

                //throw new InventoryShortageException(
                //    $"库存不足: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}.\n" +
                //    $"需求: {quantity}, 可用: {totalQty}");
            }
            if (totalQty ==0)
            {
                return 0;
            }
            else
            {
                return totalAmount / totalQty;
            }
        }

        /// <summary>
        /// 计算个别计价法下的出库单价
        /// 注：个别计价法需要业务端传入批次号或唯一标识，此方法为基础实现，可根据实际需求扩展
        /// </summary>
        private static decimal CalculateSpecificPrice(SqlSugarClient db, string warehouseCode, string invCode, string sku)
        {
            // 个别计价法通常需要指定批次或唯一库存记录ID
            // 此处为默认实现，返回当前唯一库存的价格，实际使用时建议增加批次参数
            var balances = db.Queryable<InventoryBalance>()
                .Where(x => x.WarehouseCode == warehouseCode &&
                            x.InvCode == invCode &&
                            x.SKU == sku &&
                            x.Quantity > 0)
                .ToList();

            if (balances.Count == 0)
                throw new InvalidOperationException($"库存不足: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}");
            
            if (balances.Count > 1)
                throw new InvalidOperationException(
                    $"个别计价法下存在多个库存记录，请指定具体批次: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}");

            return balances[0].Price;
        }

        /// <summary>
        /// 计算全月平均法下的出库单价
        /// 公式：(月初库存金额 + 本月入库金额) / (月初库存数量 + 本月入库数量)
        /// </summary>
        private static decimal CalculateWeightedAveragePrice(SqlSugarClient db, string warehouseCode, string invCode, string sku)
        {
            var now = DateTime.Now;
            var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayOfNextMonth = firstDayOfMonth.AddMonths(1);

            // 月初库存余额
            var openingBalance = db.Queryable<InventoryBalance>()
                .Where(x => x.WarehouseCode == warehouseCode &&
                            x.InvCode == invCode &&
                            x.SKU == sku &&
                            x.InsertTime < firstDayOfMonth)
                .OrderByDescending(x => x.InsertTime)
                .First();

            decimal openingQty = openingBalance?.Quantity ?? 0;
            decimal openingAmount = openingBalance?.Amount ?? 0;

            // 本月入库金额和数量
            var inboundTransactions = db.Queryable<InventoryTransaction>()
                .Includes(t => t.ITInvInOuts)
                .Where(t => t.WarehouseCode == warehouseCode &&
                            t.TrxType == 1 && // 入库
                            t.InsertTime >= firstDayOfMonth &&
                            t.InsertTime < firstDayOfNextMonth &&
                            t.Active == true).ToList()
                .SelectMany(t => t.ITInvInOuts)
                .Where(io => io.InvCode == invCode && io.SKU == sku)
                .GroupBy(io => new { io.InvCode, io.SKU })
                .Select(g => new
                {
                    TotalQty = g.Sum(io => io.Quantity),
                    TotalAmount = g.Sum(io => io.Amount)
                })
                .First();

            decimal inboundQty = inboundTransactions?.TotalQty ?? 0;
            decimal inboundAmount = inboundTransactions?.TotalAmount ?? 0;

            decimal totalQty = openingQty + inboundQty;
            decimal totalAmount = openingAmount + inboundAmount;

            if (totalQty <= 0)
                throw new InvalidOperationException(($"库存不足: 物料 {invCode}, SKU {sku}, 仓库 {warehouseCode}"));

            return totalAmount / totalQty;
        }

        /// <summary>
        /// 批量计算出库单价
        /// </summary>
        /// <param name="warehouseCode">仓库编码</param>
        /// <param name="outboundItems">出库明细列表（包含InvCode、SKU、Quantity）</param>
        /// <returns>出库明细列表（补充Price字段）</returns>
        public static List<OutboundPricingResult> CalculateBatchOutboundPrices(string warehouseCode, List<OutboundItem> outboundItems)
        {
            var results = new List<OutboundPricingResult>();
            
            foreach (var item in outboundItems)
            {
                try
                {
                    var price = CalculateOutboundPrice(
                        warehouseCode, 
                        item.InvCode, 
                        item.SKU, 
                        item.Quantity, 
                        item.CostingMethodCode);
                    
                    results.Add(new OutboundPricingResult
                    {
                        InvCode = item.InvCode,
                        SKU = item.SKU,
                        Quantity = item.Quantity,
                        Price = price,
                        Amount = price * item.Quantity,
                        Success = true,
                        Message = "计算成功"
                    });
                }
                catch (Exception ex)
                {
                    results.Add(new OutboundPricingResult
                    {
                        InvCode = item.InvCode,
                        SKU = item.SKU,
                        Quantity = item.Quantity,
                        Price = 0,
                        Amount = 0,
                        Success = false,
                        Message = ex.Message
                    });
                }
            }

            return results;
        }
    }

    /// <summary>
    /// 出库计算请求项
    /// </summary>
    public class OutboundItem
    {
        public string InvCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public string? CostingMethodCode { get; set; }
    }

    /// <summary>
    /// 出库计算结果
    /// </summary>
    public class OutboundPricingResult
    {
        public string InvCode { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
