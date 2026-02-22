using ERP_Web.Repository;
using System.Transactions;

namespace ERP_Web.Models
{
    // 新增单据类型编码（和你现有PROFIT/LOSS对齐）
    public static class TrxTypeCode
    {
        // 原有类型
        public const string Profit = "PROFIT"; // 盘盈
        public const string Loss = "LOSS"; // 盘亏
                                           // 新增类型
        public const string DirectInOut = "DIRECT_INOUT"; // 直入直出
        public const string TransferOut = "TRANSFER_OUT"; // 调拨出库
        public const string TransferIn = "TRANSFER_IN"; // 调拨入库
    }

    // 新增TrxType数值标识（和原有1=入库/-1=出库对齐）
    public static class TrxTypeFlag
    {
        public const int In = 1; // 普通入库
        public const int Out = -1; // 普通出库
        public const int Direct = 0; // 直入直出
        public const int TransferOut = -2; // 调拨出库
        public const int TransferIn = 2; // 调拨入库
    }

    public class InventoryTransfer
    {


    /// <summary>
    /// 仓库调拨业务方法
    /// </summary>
    /// <param name="fromWarehouseCode">调出仓库</param>
    /// <param name="toWarehouseCode">调入仓库</param>
    /// <param name="invCode">商品编码</param>
    /// <param name="sku">规格</param>
    /// <param name="quantity">调拨数量</param>
    /// <param name="remark">备注</param>
        public static void CreateTransfer(string fromWarehouseCode, string toWarehouseCode,
            string invCode, string sku, decimal quantity, string remark = "")
        {
            using var scope = new TransactionScope();
            using var db = new SqlClient().Db;

            // 1. 生成调拨出库单（复用现有出库逻辑，自动计算调出成本）
            var outTrx = new InventoryTransaction
            {
                WarehouseCode = fromWarehouseCode,
                TrxType = TrxTypeFlag.TransferOut,
                //TrxNo = GenerateBillNo("TRANSFER_OUT"),
                Date = DateOnly.FromDateTime(DateTime.Now),
                Approver = "",//CurrentUser.Name,
                Active = true,
                Explanation = $"调拨至{toWarehouseCode}：{remark}"
            };
            outTrx.ITInvInOuts.Add(new ITInvInOut
            {
                InvCode = invCode,
                SKU = sku,
                Quantity = quantity,
                // 自动取调出仓库当前成本价，复用现有计价逻辑
                //Price = GetCurrentCost(fromWarehouseCode, invCode, sku)
            });
            outTrx.Insert(); // 复用你现有Insert逻辑，自动更新库存

            // 2. 生成调拨入库单（成本和出库完全一致，不需要重新计算）
            var inTrx = new InventoryTransaction
            {
                WarehouseCode = toWarehouseCode,
                TrxType = TrxTypeFlag.TransferIn,
                //BillNo = GenerateBillNo("TRANSFER_IN"),
                Date = DateOnly.FromDateTime(DateTime.Now),
                //Approver = CurrentUser.Name,
                Active = true,
                //RelatedBillNo = outTrx.BillNo, // 关联调出单号
                //Remark = $"从{fromWarehouseCode}调拨：{remark}"
            };
            inTrx.ITInvInOuts.Add(new ITInvInOut
            {
                InvCode = invCode,
                SKU = sku,
                Quantity = quantity,
                Price = outTrx.ITInvInOuts.First().Price, // 和出库成本完全一致
                Amount = outTrx.ITInvInOuts.First().Amount
            });
            inTrx.Insert(); // 复用现有Insert逻辑，自动更新库存

            scope.Complete();
        }

        /// <summary>
        /// 直入直出业务方法
        /// </summary>
        /// <param name="warehouseCode">过账仓库</param>
        /// <param name="invCode">商品编码</param>
        /// <param name="sku">规格</param>
        /// <param name="quantity">数量</param>
        /// <param name="costPrice">成本价</param>
        /// <param name="relatedInBillNo">上游入库单号</param>
        /// <param name="relatedOutBillNo">下游出库单号</param>
        /// <param name="remark">备注</param>
        public static void CreateDirectInOut(string warehouseCode, string invCode, string sku,
            decimal quantity, decimal costPrice, string relatedInBillNo, string relatedOutBillNo, string remark = "")
        {
            var trx = new InventoryTransaction
            {
                WarehouseCode = warehouseCode,
                TrxType = TrxTypeFlag.Direct,
                //BillNo = GenerateBillNo("DIRECT"),
                Date = DateOnly.FromDateTime(DateTime.Now),
                //Approver = CurrentUser.Name,
                Active = true,
                //RelatedBillNo = $"{relatedInBillNo},{relatedOutBillNo}",
                //Remark = remark
            };
            // 同时插入入库和出库明细，正负抵消，库存不变，仅记录流水
            trx.ITInvInOuts.AddRange(new List<ITInvInOut>
            {
                new() // 入库明细
                {
                    InvCode = invCode, SKU = sku, Quantity = quantity, Price = costPrice, Amount = quantity * costPrice
                },
                new() // 出库明细
                {
                    InvCode = invCode, SKU = sku, Quantity = quantity, Price = costPrice, Amount = -quantity * costPrice
                }
            });
            trx.Insert();
        }


    }
}
