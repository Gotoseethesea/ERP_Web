using ERP_Web.Repository;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace ERP_Web.Models
{
    public static class InventoryBalanceCalculator
    {
        // 定义用于存储交易明细的强类型类
        public class TransactionDetail
        {
            public DateOnly Date { get; set; }
            public decimal Quantity { get; set; }
            public decimal Amount { get; set; }
            public string CostingMethod { get; set; }
        }

        /// <summary>
        /// 根据库存记录重新计算所有存货的库存结余
        /// </summary>
        public static void RecalculateAllBalances()
        {
            using (var scope = new TransactionScope())
            using (var db = new SqlClient().Db)
            {
                // 清空现有库存结余
                db.Deleteable<InventoryBalance>().ExecuteCommand();

                // 获取所有有效的库存记录（按日期排序）
                var transactions = db.Queryable<InventoryTransaction>()
                    .Includes(x => x.ITInvInOuts)
                    .Includes(x => x.ITInvInOuts,xx=>xx.InventoryItem)
                    .Where(x => x.Active == true)
                    // 👇 可选新增：直接过滤直入直出单据，不需要则注释掉
                    //.Where(x => x.TrxType != 0)
                    .OrderBy(x => x.Date)
                    .ToList();
                // 创建按仓库+存货+规格分组的字典
                var balanceGroups = new Dictionary<string, List<TransactionDetail>>();

                // 填充分组字典
                foreach (var transaction in transactions)
                {
                    // 👇 新增：直入直出TrxType=0直接跳过，不参与库存计算（提升效率，完全符合你业务要求）
                    //if (transaction.TrxType == 0) continue;

                    foreach (var item in transaction.ITInvInOuts)
                    {
                        // 创建分组键
                        var groupKey = $"{transaction.WarehouseCode}|{item.InvCode}|{item.SKU}";

                        // 获取或创建分组
                        if (!balanceGroups.ContainsKey(groupKey))
                        {
                            balanceGroups[groupKey] = new List<TransactionDetail>();
                        }

                        // 添加交易明细
                        balanceGroups[groupKey].Add(new TransactionDetail
                        {
                            Date = transaction.Date,
                            // 👇 替换原硬编码判断：自动适配所有正负TrxType，兼容调拨类型
                            Quantity = transaction.TrxType == 0 ? 0 : Math.Sign(transaction.TrxType) * item.Quantity,
                            Amount = transaction.TrxType == 0 ? 0 : Math.Sign(transaction.TrxType) * item.Amount,
                            CostingMethod = item.InventoryItem?.CostingMethodCode ?? "MWA"
                        });
                    }
                }

                // 处理每个分组
                foreach (var group in balanceGroups)
                {
                    // 解析分组键
                    var keys = group.Key.Split('|');
                    var warehouseCode = keys[0];
                    var invCode = keys[1];
                    var sku = keys[2];

                    // 获取成本计价方法（使用组内第一个明细的成本方法）
                    var costingMethod = group.Value.First().CostingMethod;

                    if (costingMethod == "FIFO")
                    {
                        ProcessFIFOGroup(db, warehouseCode, invCode, sku, group.Value);
                    }
                    else
                    {
                        ProcessMWAGroup(db, warehouseCode, invCode, sku, group.Value);
                    }
                }
                scope.Complete();
            }
        }

        /// <summary>
        /// 处理移动加权平均法(MWA)的库存结余计算
        /// </summary>
        private static void ProcessMWAGroup(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<TransactionDetail> transactions)
        {
            decimal totalQuantity = 0;
            decimal totalAmount = 0;

            foreach (var t in transactions)
            {
                totalQuantity += t.Quantity;
                totalAmount += t.Amount;

                // 更新或创建库存结余记录
                var balance = db.Queryable<InventoryBalance>()
                    .Where(b => b.WarehouseCode == warehouseCode &&
                                b.InvCode == invCode &&
                                b.SKU == sku)
                    .First();

                if (balance == null)
                {
                    balance = new InventoryBalance
                    {
                        WarehouseCode = warehouseCode,
                        InvCode = invCode,
                        SKU = sku,
                        Quantity = totalQuantity,
                        Amount = totalAmount,
                        Price = totalQuantity != 0 ? totalAmount / totalQuantity : 0,
                        InsertTime = DateTime.Now,
                        UpdateTime = DateTime.Now
                    };
                    db.Insertable(balance).ExecuteCommand();
                }
                else
                {
                    balance.Quantity = totalQuantity;
                    balance.Amount = totalAmount;
                    balance.Price = totalQuantity != 0 ? totalAmount / totalQuantity : 0;
                    balance.UpdateTime = DateTime.Now;
                    db.Updateable(balance).ExecuteCommand();
                }
            }
        }

        /// <summary>
        /// 处理先进先出法(FIFO)的库存结余计算
        /// </summary>
        private static void ProcessFIFOGroup(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<TransactionDetail> transactions)
        {
            // 存储批次库存（入库批次）
            var inventoryLots = new List<FIFOInventoryLot>();

            // 按交易日期排序
            var sortedTransactions = transactions.OrderBy(t => t.Date).ToList();

            foreach (var t in sortedTransactions)
            {
                if (t.Quantity > 0) // 入库
                {
                    inventoryLots.Add(new FIFOInventoryLot
                    {
                        WarehouseCode = warehouseCode,
                        InvCode = invCode,
                        SKU = sku,
                        TransactionDate = t.Date,
                        Quantity = t.Quantity,
                        Price = t.Amount / t.Quantity,
                        Amount = t.Amount,
                        InsertTime = DateTime.Now
                    });
                }
                else // 出库
                {
                    decimal remainingQty = -t.Quantity;   //需要出库的数量

                    // 按时间顺序处理批次
                    foreach (var lot in inventoryLots.Where(l => l.Quantity > 0).OrderBy(l => l.TransactionDate).ToList())
                    {
                        if (remainingQty <= 0) break;

                        // 检查当前批次的数量是否大于或等于剩余需要处理的数量
                        if (lot.Quantity >= remainingQty)
                        {
                            // 如果当前批次的数量足够，则减少该批次的数量以匹配剩余需要处理的数量
                            lot.Quantity -= remainingQty;
                            // 更新该批次的总金额，基于更新后的数量和单价
                            lot.Amount = lot.Quantity * lot.Price;
                            // 将剩余需要处理的数量设为0，表示需求已完全满足
                            remainingQty = 0;
                        }
                        else
                        {
                            // 如果当前批次的数量不足以满足需求，则减少剩余需要处理的数量以匹配当前批次的数量
                            remainingQty -= lot.Quantity;
                            // 将当前批次的数量设为0，表示该批次已完全处理
                            lot.Quantity = 0;
                        }


                        // 更新批次
                        if (lot.Id > 0)
                        {
                            db.Updateable(lot).ExecuteCommand();
                        }
                    }

                    if (remainingQty > 0)
                    {
                        // 库存不足，创建负库存批次
                        inventoryLots.Add(new FIFOInventoryLot
                        {
                            WarehouseCode = warehouseCode,
                            InvCode = invCode,
                            SKU = sku,
                            TransactionDate = t.Date,
                            Quantity = -remainingQty,
                            Price = 0,
                            Amount = 0,
                            IsNegative = true,
                            InsertTime = DateTime.Now
                        });
                    }
                }
            }

            // 计算最终库存结余
            decimal finalQuantity = 0;
            decimal finalAmount = 0;

            foreach (var lot in inventoryLots.Where(l => l.Quantity > 0))
            {
                finalQuantity += lot.Quantity;
                finalAmount += lot.Amount;
            }

            // 更新或创建库存结余记录
            var balance = db.Queryable<InventoryBalance>()
                .Where(b => b.WarehouseCode == warehouseCode &&
                            b.InvCode == invCode &&
                            b.SKU == sku)
                .First();

            if (balance == null)
            {
                balance = new InventoryBalance
                {
                    WarehouseCode = warehouseCode,
                    InvCode = invCode,
                    SKU = sku,
                    Quantity = finalQuantity,
                    Amount = finalAmount,
                    Price = finalQuantity != 0 ? finalAmount / finalQuantity : 0,
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };
                db.Insertable(balance).ExecuteCommand();
            }
            else
            {
                balance.Quantity = finalQuantity;
                balance.Amount = finalAmount;
                balance.Price = finalQuantity != 0 ? finalAmount / finalQuantity : 0;
                balance.UpdateTime = DateTime.Now;
                db.Updateable(balance).ExecuteCommand();
            }

            // 保存批次明细
            SaveInventoryLots(db, warehouseCode, invCode, sku, inventoryLots);
        }

        /// <summary>
        /// 保存FIFO批次明细
        /// </summary>
        private static void SaveInventoryLots(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<FIFOInventoryLot> lots)
        {
            // 删除旧的批次记录
            db.Deleteable<FIFOInventoryLot>()
                .Where(l => l.WarehouseCode == warehouseCode &&
                            l.InvCode == invCode &&
                            l.SKU == sku)
                .ExecuteCommand();

            // 插入新的批次记录
            if (lots.Any())
            {
                db.Insertable(lots).ExecuteCommand();
            }
        }
    }

    /// <summary>
    /// FIFO批次明细表
    /// </summary>
    public class FIFOInventoryLot
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string WarehouseCode { get; set; }
        public string InvCode { get; set; }
        public string SKU { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public DateOnly TransactionDate { get; set; }
        public bool IsNegative { get; set; }
        public DateTime InsertTime { get; set; }
    }
}
