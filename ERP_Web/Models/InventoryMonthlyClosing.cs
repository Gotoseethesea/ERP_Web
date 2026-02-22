using ERP_Web.Repository;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;

namespace ERP_Web.Models
{
    public static class InventoryMonthlyClosingOld
    {
        /// <summary>
        /// 执行月度库存结账
        /// </summary>
        /// <param name="closingDate">结账日期（通常为月末最后一天）</param>
        public static void PerformMonthlyClosing(DateOnly closingDate)
        {
            using (var scope = new TransactionScope())
            using (var db = new SqlClient().Db)
            {
                // 获取当前库存结余
                var currentBalances = db.Queryable<InventoryBalance>().ToList();

                // 创建月度结账记录
                var closingRecord = new InventoryClosingRecord
                {
                    ClosingDate = closingDate,
                    CreatedAt = DateTime.Now,
                    Status = ClosingStatus.Completed
                };
                closingRecord.Id = db.Insertable(closingRecord).ExecuteReturnIdentity();

                // 保存历史库存结余
                foreach (var balance in currentBalances)
                {
                    var history = new InventoryBalanceHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = balance.WarehouseCode,
                        InvCode = balance.InvCode,
                        SKU = balance.SKU,
                        Quantity = balance.Quantity,
                        Price = balance.Price,
                        Amount = balance.Amount,
                        CostingMethod = GetCostingMethod(db, balance.InvCode)
                    };
                    db.Insertable(history).ExecuteCommand();
                }

                // 保存FIFO批次历史
                var fifoLots = db.Queryable<FIFOInventoryLot>().ToList();
                foreach (var lot in fifoLots)
                {
                    var lotHistory = new FIFOInventoryLotHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = lot.WarehouseCode,
                        InvCode = lot.InvCode,
                        SKU = lot.SKU,
                        Quantity = lot.Quantity,
                        Price = lot.Price,
                        Amount = lot.Amount,
                        TransactionDate = lot.TransactionDate,
                        IsNegative = lot.IsNegative
                    };
                    db.Insertable(lotHistory).ExecuteCommand();
                }
                scope.Complete();
            }
        }

        /// <summary>
        /// 获取存货的成本计价方法
        /// </summary>
        private static string GetCostingMethod(SqlSugarClient db, string invCode)
        {
            return db.Queryable<InventoryItem>()
                .Where(i => i.Code == invCode)
                .Select(i => i.CostingMethodCode)
                .First() ?? "MWA";
        }

        /// <summary>
        /// 计算指定月份的库存结余
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="month">月份</param>
        public static void CalculateMonthlyBalance(int year, int month)
        {
            // 获取上个月结账记录
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            using (var db = new SqlClient().Db)
            {
                // 获取上个月结账记录
                var prevClosing = db.Queryable<InventoryClosingRecord>()
                    .Where(c => c.ClosingDate.Year == prevYear && c.ClosingDate.Month == prevMonth)
                    .First();

                if (prevClosing == null)
                {
                    throw new Exception($"找不到 {prevYear}年{prevMonth}月的结账记录");
                }

                // 获取上个月的历史结余
                var prevBalances = db.Queryable<InventoryBalanceHistory>()
                    .Where(h => h.ClosingRecordId == prevClosing.Id)
                    .ToList();

                // 获取当月的库存交易记录
                var startDate = new DateOnly(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var transactions = db.Queryable<InventoryTransaction>()
                    .Includes(x => x.ITInvInOuts)
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .OrderBy(t => t.Date)
                    .ToList();

                // 按仓库+存货+规格分组处理
                var balanceGroups = new Dictionary<string, List<TransactionDetail>>();

                // 填充分组字典
                foreach (var transaction in transactions)
                {
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
                            Quantity = transaction.TrxType == 1 ? item.Quantity : -item.Quantity,
                            Amount = transaction.TrxType == 1 ? item.Amount : -item.Amount
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

                    // 获取上期结余
                    var prevBalance = prevBalances.FirstOrDefault(b =>
                        b.WarehouseCode == warehouseCode &&
                        b.InvCode == invCode &&
                        b.SKU == sku);

                    // 获取成本计价方法
                    var costingMethod = prevBalance?.CostingMethod ?? "MWA";

                    // 根据计价方法计算当月结余
                    if (costingMethod == "FIFO")
                    {
                        CalculateFIFOMonthlyBalance(db, warehouseCode, invCode, sku, group.Value, prevBalance);
                    }
                    else
                    {
                        CalculateMWAMonthlyBalance(db, warehouseCode, invCode, sku, group.Value, prevBalance);
                    }
                }
            }
        }

        /// <summary>
        /// 计算移动加权平均法的月度库存结余
        /// </summary>
        private static void CalculateMWAMonthlyBalance(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<TransactionDetail> transactions,
            InventoryBalanceHistory prevBalance)
        {
            decimal totalQuantity = prevBalance?.Quantity ?? 0;
            decimal totalAmount = prevBalance?.Amount ?? 0;

            foreach (var t in transactions)
            {
                totalQuantity += t.Quantity;
                totalAmount += t.Amount;
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

        /// <summary>
        /// 计算先进先出法的月度库存结余
        /// </summary>
        private static void CalculateFIFOMonthlyBalance(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<TransactionDetail> transactions,
            InventoryBalanceHistory prevBalance)
        {
            // 创建批次库存列表
            var inventoryLots = new List<FIFOInventoryLot>();

            // 添加上期结余批次
            if (prevBalance != null)
            {
                // 获取上期FIFO批次历史
                var prevLots = db.Queryable<FIFOInventoryLotHistory>()
                    .Where(l => l.ClosingRecordId == prevBalance.ClosingRecordId &&
                                l.WarehouseCode == warehouseCode &&
                                l.InvCode == invCode &&
                                l.SKU == sku)
                    .ToList();

                foreach (var lot in prevLots)
                {
                    inventoryLots.Add(new FIFOInventoryLot
                    {
                        WarehouseCode = warehouseCode,
                        InvCode = invCode,
                        SKU = sku,
                        TransactionDate = lot.TransactionDate,
                        Quantity = lot.Quantity,
                        Price = lot.Price,
                        Amount = lot.Amount,
                        IsNegative = lot.IsNegative,
                        InsertTime = DateTime.Now
                    });
                }
            }

            // 按交易日期排序
            var sortedTransactions = transactions.OrderBy(t => t.Date).ToList();

            // 处理当月交易
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
                    decimal remainingQty = -t.Quantity;

                    // 按时间顺序处理批次
                    foreach (var lot in inventoryLots.Where(l => l.Quantity > 0)
                        .OrderBy(l => l.TransactionDate)
                        .ToList())
                    {
                        if (remainingQty <= 0) break;

                        if (lot.Quantity >= remainingQty)
                        {
                            lot.Quantity -= remainingQty;
                            lot.Amount = lot.Quantity * lot.Price;
                            remainingQty = 0;
                        }
                        else
                        {
                            remainingQty -= lot.Quantity;
                            lot.Quantity = 0;
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

        // 交易明细类（内部使用）
        public class TransactionDetail
        {
            public DateOnly Date { get; set; }
            public decimal Quantity { get; set; }
            public decimal Amount { get; set; }
        }
    }

    public static class InventoryMonthlyClosing
    {
        /// <summary>
        /// 执行月度库存结账
        /// </summary>
        /// <param name="closingDate">结账日期（通常为月末最后一天）</param>
        public static void PerformMonthlyClosing(DateOnly closingDate)
        {
            using (var scope = new TransactionScope())
            using (var db = new SqlClient().Db)
            {
                // 获取当前库存结余
                var currentBalances = db.Queryable<InventoryBalance>().ToList();

                // 创建月度结账记录
                var closingRecord = new InventoryClosingRecord
                {
                    ClosingDate = closingDate,
                    CreatedAt = DateTime.Now,
                    Status = ClosingStatus.Completed
                };
                closingRecord.Id = db.Insertable(closingRecord).ExecuteReturnIdentity();

                // 保存历史库存结余
                foreach (var balance in currentBalances)
                {
                    var history = new InventoryBalanceHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = balance.WarehouseCode,
                        InvCode = balance.InvCode,
                        SKU = balance.SKU,
                        Quantity = balance.Quantity,
                        Price = balance.Price,
                        Amount = balance.Amount,
                        CostingMethod = GetCostingMethod(db, balance.InvCode)
                    };
                    db.Insertable(history).ExecuteCommand();
                }

                // 保存FIFO批次历史
                var fifoLots = db.Queryable<FIFOInventoryLot>().ToList();
                foreach (var lot in fifoLots)
                {
                    var lotHistory = new FIFOInventoryLotHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = lot.WarehouseCode,
                        InvCode = lot.InvCode,
                        SKU = lot.SKU,
                        Quantity = lot.Quantity,
                        Price = lot.Price,
                        Amount = lot.Amount,
                        TransactionDate = lot.TransactionDate,
                        IsNegative = lot.IsNegative
                    };
                    db.Insertable(lotHistory).ExecuteCommand();
                }
                scope.Complete();
            }
        }

        /// <summary>
        /// 执行月度库存结账（按期间独立计算，和实时库存隔离）
        /// </summary>
        /// <param name="year">结账年份</param>
        /// <param name="month">结账月份</param>
        /// <param name="syncCurrentBalance">是否同步计算结果到当前库存（建议仅首次月结或重算后使用）</param>
        public static void PerformMonthlyClosing(int year, int month, bool syncCurrentBalance = false)
        {
            var closingDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));
            using var scope = new TransactionScope();
            using var db = new SqlClient().Db;

            #region 1. 前置校验
            // ... 原有其他校验不变 ...
            // 👇 新增：统计本期直入直出单据数量
            var directInOut = db.Queryable<InventoryTransaction>()
                .Where(x => x.Date.Year == year && x.Date.Month == month && x.Active == true && x.TrxType == 0)
                .ToList();
            if (directInOut.Count() > 0)
            {
                // 可选：日志记录/提示用户本期有多少张直入直出单据，不影响库存计算
                Console.WriteLine($"本期共{directInOut.Count()}张直入直出单据，已自动过滤库存影响");
            }
            // 检查是否已经结账
            if (InventoryClosingRecord.IsPeriodClosed(year, month))
            {
                throw new Exception($"{year}年{month}月已经完成结账，请勿重复操作");
            }
            // 检查本期是否有未审核的库存单据
            var unApprovedCount = db.Queryable<InventoryTransaction>()
                .Where(x => x.Date.Year == year && x.Date.Month == month && x.Active == true && string.IsNullOrEmpty(x.Approver))
                .Count();
            if (unApprovedCount > 0)
            {
                throw new Exception($"本期存在{unApprovedCount}张未审核的库存单据，请先审核所有单据后再结账");
            }
            // 检查上期是否已结账（首次月结跳过）
            var prevYear = month == 1 ? year - 1 : year;
            var prevMonth = month == 1 ? 12 : month - 1;
            var hasPrevClosing = InventoryClosingRecord.IsPeriodClosed(prevYear, prevMonth);
            var firstClosing = db.Queryable<InventoryClosingRecord>().Count() == 0;
            if (!firstClosing && !hasPrevClosing)
            {
                throw new Exception($"{prevYear}年{prevMonth}月尚未结账，请先完成上月结账");
            }
            #endregion

            #region 2. 创建结账记录
            var closingRecord = new InventoryClosingRecord
            {
                ClosingDate = closingDate,
                CreatedAt = DateTime.Now,
                Status = ClosingStatus.Processing
            };
            closingRecord.Id = closingRecord.Insert();
            #endregion

            #region 3. 获取期初数据
            List<InventoryBalanceHistory> beginBalances = [];
            List<FIFOInventoryLotHistory> beginFifoLots = [];
            if (hasPrevClosing)
            {
                var prevClosingRecord = InventoryClosingRecord.SelectByYearMonth(prevYear, prevMonth);
                beginBalances = InventoryBalanceHistory.SelectByClosingRecordId(prevClosingRecord.Id);
                beginFifoLots = FIFOInventoryLotHistory.SelectByClosingRecordId(prevClosingRecord.Id);
            }
            #endregion

            #region 4. 获取本期出入库明细
            var startDate = new DateOnly(year, month, 1);
            var endDate = closingDate;
            var transactions = db.Queryable<InventoryTransaction>()
                .Includes(x => x.ITInvInOuts)
                .Where(x => x.Date >= startDate && x.Date <= endDate && x.Active == true && !string.IsNullOrEmpty(x.Approver))
                // 👇 可选新增：过滤直入直出单据，不需要则注释掉
                //.Where(x => x.TrxType != 0)
                .OrderBy(x => x.Date)
                .ToList();
            #endregion

            #region 5. 按计价方式计算本期期末结余
            // 按仓库+商品+SKU分组所有出入库明细
            var trxGroups = transactions
                .SelectMany(trx => trx.ITInvInOuts.Select(detail => new
                {
                    trx.WarehouseCode,
                    detail.InvCode,
                    detail.SKU,
                    trx.Date,
                    trx.TrxType,
                    detail.Quantity,
                    detail.Amount,
                    detail.InventoryItem.CostingMethodCode
                }))
                .GroupBy(x => new { x.WarehouseCode, x.InvCode, x.SKU })
                .ToList();
                var directInOutItem = directInOut.SelectMany(trx => trx.ITInvInOuts.Select(detail => new
                {
                    trx.WarehouseCode,
                    detail.InvCode,
                    detail.SKU,
                    trx.Date,
                    trx.TrxType,
                    detail.Quantity,
                    detail.Amount,
                    detail.InventoryItem.CostingMethodCode
                }))
                .GroupBy(x => new { x.WarehouseCode, x.InvCode, x.SKU })
                .ToList();

            // 合并期初有但本期无发生的商品及直入直出的商品
            var allInventoryKeys = beginBalances.Select(b => new { b.WarehouseCode, b.InvCode, b.SKU })
                .Union(trxGroups.Select(g => g.Key))
                .Union(directInOutItem.Select(g => g.Key))
                .Distinct()
                .ToList();

            List<InventoryBalanceHistory> endBalances = [];
            List<FIFOInventoryLotHistory> endFifoLots = [];

            foreach (var key in allInventoryKeys)
            {
                // 取对应商品的计价方式
                var costingMethod = beginBalances.FirstOrDefault(b => b.WarehouseCode == key.WarehouseCode && b.InvCode == key.InvCode && b.SKU == key.SKU)?.CostingMethod
                    ?? trxGroups.FirstOrDefault(g => g.Key == key)?.FirstOrDefault()?.CostingMethodCode
                    ?? "MWA";

                costingMethod = GetCostingMethod(new SqlClient().Db, key.InvCode);

                // 取该商品的期初结余
                var beginBalance = beginBalances.FirstOrDefault(b => b.WarehouseCode == key.WarehouseCode && b.InvCode == key.InvCode && b.SKU == key.SKU);
                // 取该商品的本期交易明细
                //var itemTrxs = trxGroups.FirstOrDefault(g => g.Key == key)?.Select(t => new TransactionDetail
                //{
                //    Date = t.Date,
                //    Quantity = t.TrxType == 1 ? t.Quantity : -t.Quantity,
                //    Amount = t.TrxType == 1 ? t.Amount : -t.Amount
                //}).ToList() ?? [];

                // 取该商品的本期交易明细
                var itemTrxs = trxGroups.FirstOrDefault(g => g.Key == key)?.Select(t => new TransactionDetail
                {
                    Date = t.Date,
                    // 👇 替换原硬编码判断：自动适配所有正负TrxType，TrxType=0时数量金额都为0
                    Quantity = t.TrxType == 0 ? 0 : Math.Sign(t.TrxType) * t.Quantity,
                    Amount = t.TrxType == 0 ? 0 : Math.Sign(t.TrxType) * t.Amount
                }).ToList() ?? [];


                if (costingMethod == "FIFO")
                {
                    // 如果成本计算方法是FIFO（先进先出），则进行以下操作
                    var lots = new List<FIFOInventoryLot>(); // 初始化一个FIFOInventoryLot列表，用于存储批次信息
                    // 加载期初批次信息，筛选条件为仓库编号、存货编号和SKU均匹配
                    var beginLots = beginFifoLots.Where(l => l.WarehouseCode == key.WarehouseCode && l.InvCode == key.InvCode && l.SKU == key.SKU)
                        .Select(l => new FIFOInventoryLot // 对筛选出的每个批次信息进行投影，转换为FIFOInventoryLot对象
                        {
                            WarehouseCode = l.WarehouseCode, // 设置仓库编号
                            InvCode = l.InvCode, // 设置存货编号
                            SKU = l.SKU, // 设置SKU
                            TransactionDate = l.TransactionDate, // 设置交易日期
                            Quantity = l.Quantity, // 设置数量
                            Price = l.Price, // 设置单价
                            Amount = l.Amount, // 设置金额
                            IsNegative = l.IsNegative // 设置是否为负数批次
                        }).ToList(); // 将投影后的结果转换为列表
                    lots.AddRange(beginLots); // 将期初批次信息添加到lots列表中

                    // 处理本期交易
                    foreach (var trx in itemTrxs.OrderBy(t => t.Date))
                    {
                        if (trx.Quantity > 0) // 入库
                        {
                            lots.Add(new FIFOInventoryLot
                            {
                                WarehouseCode = key.WarehouseCode,
                                InvCode = key.InvCode,
                                SKU = key.SKU,
                                TransactionDate = trx.Date,
                                Quantity = trx.Quantity,
                                Price = trx.Quantity > 0 ? trx.Amount / trx.Quantity : 0,
                                Amount = trx.Amount
                            });
                        }
                        else // 出库
                        {
                            var remainingQty = -trx.Quantity;
                            foreach (var lot in lots.Where(l => l.Quantity > 0).OrderBy(l => l.TransactionDate).ToList())
                            {
                                if (remainingQty <= 0) break;
                                if (lot.Quantity >= remainingQty)
                                {
                                    lot.Quantity -= remainingQty;
                                    lot.Amount = lot.Quantity * lot.Price;
                                    remainingQty = 0;
                                }
                                else
                                {
                                    remainingQty -= lot.Quantity;
                                    lot.Quantity = 0;
                                }
                            }
                            if (remainingQty > 0)
                            {
                                lots.Add(new FIFOInventoryLot
                                {
                                    WarehouseCode = key.WarehouseCode,
                                    InvCode = key.InvCode,
                                    SKU = key.SKU,
                                    TransactionDate = trx.Date,
                                    Quantity = -remainingQty,
                                    Price = 0,
                                    Amount = 0,
                                    IsNegative = true
                                });
                            }
                        }
                    }

                    // 计算期末结余
                    var validLots = lots.Where(l => l.Quantity != 0).ToList();
                    var endQty = validLots.Sum(l => l.Quantity);
                    var endAmt = validLots.Sum(l => l.Amount);

                    // 保存期末结余
                    endBalances.Add(new InventoryBalanceHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = key.WarehouseCode,
                        InvCode = key.InvCode,
                        SKU = key.SKU,
                        Quantity = endQty,
                        Price = endQty != 0 ? endAmt / endQty : 0,
                        Amount = endAmt,
                        CostingMethod = costingMethod
                    });

                    // 保存期末FIFO批次历史
                    endFifoLots.AddRange(validLots.Select(l => new FIFOInventoryLotHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = l.WarehouseCode,
                        InvCode = l.InvCode,
                        SKU = l.SKU,
                        TransactionDate = l.TransactionDate,
                        Quantity = l.Quantity,
                        Price = l.Price,
                        Amount = l.Amount,
                        IsNegative = l.IsNegative
                    }));
                }
                else
                {
                    // 移动加权平均计算
                    var endQty = (beginBalance?.Quantity ?? 0) + itemTrxs.Sum(t => t.Quantity);
                    var endAmt = (beginBalance?.Amount ?? 0) + itemTrxs.Sum(t => t.Amount);

                    endBalances.Add(new InventoryBalanceHistory
                    {
                        ClosingRecordId = closingRecord.Id,
                        WarehouseCode = key.WarehouseCode,
                        InvCode = key.InvCode,
                        SKU = key.SKU,
                        Quantity = endQty,
                        Price = endQty != 0 ? endAmt / endQty : 0,
                        Amount = endAmt,
                        CostingMethod = costingMethod
                    });
                }
            }
            #endregion

            #region 6. 批量保存月结结果
            InventoryBalanceHistory.InsertRange(endBalances);
            if (endFifoLots.Any())
            {
                FIFOInventoryLotHistory.InsertRange(endFifoLots);
            }
            #endregion

            #region 7. 可选：同步结果到当前实时库存
            if (syncCurrentBalance)
            {
                // 清空当前库存
                db.Deleteable<InventoryBalance>().ExecuteCommand();
                db.Deleteable<FIFOInventoryLot>().ExecuteCommand();

                // 插入最新计算的结余
                var currentBalances = endBalances.Select(b => new InventoryBalance
                {
                    WarehouseCode = b.WarehouseCode,
                    InvCode = b.InvCode,
                    SKU = b.SKU,
                    Quantity = b.Quantity,
                    Price = b.Price,
                    Amount = b.Amount,
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                }).ToList();
                db.Insertable(currentBalances).ExecuteCommand();

                // 插入最新FIFO批次
                var currentLots = endFifoLots.Select(l => new FIFOInventoryLot
                {
                    WarehouseCode = l.WarehouseCode,
                    InvCode = l.InvCode,
                    SKU = l.SKU,
                    TransactionDate = l.TransactionDate,
                    Quantity = l.Quantity,
                    Price = l.Price,
                    Amount = l.Amount,
                    IsNegative = l.IsNegative,
                    InsertTime = DateTime.Now
                }).ToList();
                if (currentLots.Any())
                {
                    db.Insertable(currentLots).ExecuteCommand();
                }
            }
            #endregion

            #region 8. 更新结账状态为完成
            closingRecord.Status = ClosingStatus.Completed;
            closingRecord.Update();
            #endregion

            scope.Complete();
        }

        /// <summary>
        /// 反结账（删除指定期间的月结记录）
        /// </summary>
        public static void ReverseClosing(int year, int month)
        {
            var closingRecord = InventoryClosingRecord.SelectByYearMonth(year, month);
            if (closingRecord == null) throw new Exception($"{year}年{month}月未结账，无法反结账");
            var db = new SqlClient().Db;
            // 检查是否有后续期间的结账记录
            var nextClosing = db.Queryable<InventoryClosingRecord>()
                .Where(x => x.ClosingDate > closingRecord.ClosingDate && x.Status == ClosingStatus.Completed)
                .Any();
            if (nextClosing) throw new Exception("存在后续期间的结账记录，请先反结账后续期间");

            closingRecord.Delete();
        }

        // 保留你原来的其他方法不变
        // ... 原CalculateMonthlyBalance、CalculateMWAMonthlyBalance、CalculateFIFOMonthlyBalance等方法不用改
        /// <summary>
        /// 获取存货的成本计价方法
        /// </summary>
        private static string GetCostingMethod(SqlSugarClient db, string invCode)
        {
            return db.Queryable<InventoryItem>()
                .Where(i => i.Code == invCode)
                .Select(i => i.CostingMethodCode)
                .First() ?? "MWA";
        }

        /// <summary>
        /// 计算指定月份的库存结余
        /// </summary>
        /// <param name="year">年份</param>
        /// <param name="month">月份</param>
        public static void CalculateMonthlyBalance(int year, int month)
        {
            // 获取上个月结账记录
            var prevMonth = month == 1 ? 12 : month - 1;
            var prevYear = month == 1 ? year - 1 : year;

            using (var db = new SqlClient().Db)
            {
                // 获取上个月结账记录
                var prevClosing = db.Queryable<InventoryClosingRecord>()
                    .Where(c => c.ClosingDate.Year == prevYear && c.ClosingDate.Month == prevMonth)
                    .First();

                if (prevClosing == null)
                {
                    throw new Exception($"找不到 {prevYear}年{prevMonth}月的结账记录");
                }

                // 获取上个月的历史结余
                var prevBalances = db.Queryable<InventoryBalanceHistory>()
                    .Where(h => h.ClosingRecordId == prevClosing.Id)
                    .ToList();

                // 获取当月的库存交易记录
                var startDate = new DateOnly(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                var transactions = db.Queryable<InventoryTransaction>()
                    .Includes(x => x.ITInvInOuts)
                    .Where(t => t.Date >= startDate && t.Date <= endDate)
                    .OrderBy(t => t.Date)
                    .ToList();

                // 按仓库+存货+规格分组处理
                var balanceGroups = new Dictionary<string, List<TransactionDetail>>();

                // 填充分组字典
                foreach (var transaction in transactions)
                {
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
                            Quantity = transaction.TrxType == 1 ? item.Quantity : -item.Quantity,
                            Amount = transaction.TrxType == 1 ? item.Amount : -item.Amount
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

                    // 获取上期结余
                    var prevBalance = prevBalances.FirstOrDefault(b =>
                        b.WarehouseCode == warehouseCode &&
                        b.InvCode == invCode &&
                        b.SKU == sku);

                    // 获取成本计价方法
                    var costingMethod = prevBalance?.CostingMethod ?? "MWA";

                    // 根据计价方法计算当月结余
                    if (costingMethod == "FIFO")
                    {
                        CalculateFIFOMonthlyBalance(db, warehouseCode, invCode, sku, group.Value, prevBalance);
                    }
                    else
                    {
                        CalculateMWAMonthlyBalance(db, warehouseCode, invCode, sku, group.Value, prevBalance);
                    }
                }
            }
        }

        /// <summary>
        /// 计算移动加权平均法的月度库存结余
        /// </summary>
        private static void CalculateMWAMonthlyBalance(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<TransactionDetail> transactions,
            InventoryBalanceHistory prevBalance)
        {
            decimal totalQuantity = prevBalance?.Quantity ?? 0;
            decimal totalAmount = prevBalance?.Amount ?? 0;

            foreach (var t in transactions)
            {
                totalQuantity += t.Quantity;
                totalAmount += t.Amount;
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

        /// <summary>
        /// 计算先进先出法的月度库存结余
        /// </summary>
        private static void CalculateFIFOMonthlyBalance(
            SqlSugarClient db,
            string warehouseCode,
            string invCode,
            string sku,
            List<TransactionDetail> transactions,
            InventoryBalanceHistory prevBalance)
        {
            // 创建批次库存列表
            var inventoryLots = new List<FIFOInventoryLot>();

            // 添加上期结余批次
            if (prevBalance != null)
            {
                // 获取上期FIFO批次历史
                var prevLots = db.Queryable<FIFOInventoryLotHistory>()
                    .Where(l => l.ClosingRecordId == prevBalance.ClosingRecordId &&
                                l.WarehouseCode == warehouseCode &&
                                l.InvCode == invCode &&
                                l.SKU == sku)
                    .ToList();

                foreach (var lot in prevLots)
                {
                    inventoryLots.Add(new FIFOInventoryLot
                    {
                        WarehouseCode = warehouseCode,
                        InvCode = invCode,
                        SKU = sku,
                        TransactionDate = lot.TransactionDate,
                        Quantity = lot.Quantity,
                        Price = lot.Price,
                        Amount = lot.Amount,
                        IsNegative = lot.IsNegative,
                        InsertTime = DateTime.Now
                    });
                }
            }

            // 按交易日期排序
            var sortedTransactions = transactions.OrderBy(t => t.Date).ToList();

            // 处理当月交易
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
                    decimal remainingQty = -t.Quantity;

                    // 按时间顺序处理批次
                    foreach (var lot in inventoryLots.Where(l => l.Quantity > 0)
                        .OrderBy(l => l.TransactionDate)
                        .ToList())
                    {
                        if (remainingQty <= 0) break;

                        if (lot.Quantity >= remainingQty)
                        {
                            lot.Quantity -= remainingQty;
                            lot.Amount = lot.Quantity * lot.Price;
                            remainingQty = 0;
                        }
                        else
                        {
                            remainingQty -= lot.Quantity;
                            lot.Quantity = 0;
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

        // 交易明细类（内部使用）
        public class TransactionDetail
        {
            public DateOnly Date { get; set; }
            public decimal Quantity { get; set; }
            public decimal Amount { get; set; }
        }
    }

    /// <summary>
    /// 库存月度结账记录
    /// </summary>
    public class InventoryClosingRecord
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 结账日期（月末最后一天）
        /// </summary>
        public DateOnly ClosingDate { get; set; }

        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// 结账状态
        /// </summary>
        public ClosingStatus Status { get; set; }

        /// <summary>
        /// 查询所有结账记录
        /// </summary>
        public static List<InventoryClosingRecord> Select()
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<InventoryClosingRecord>()
                .OrderByDescending(x => x.ClosingDate)
                .ToList();
        }

        /// <summary>
        /// 按ID查询结账记录
        /// </summary>
        public static InventoryClosingRecord Select(int id)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<InventoryClosingRecord>()
                .First(x => x.Id == id);
        }

        /// <summary>
        /// 按年月查询结账记录
        /// </summary>
        public static InventoryClosingRecord? SelectByYearMonth(int year, int month)
        {
            var firstDay = new DateOnly(year, month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<InventoryClosingRecord>()
                .Where(x => x.ClosingDate >= firstDay && x.ClosingDate <= lastDay)
                .First();
        }

        /// <summary>
        /// 获取最近一次已完成的结账记录
        /// </summary>
        public static InventoryClosingRecord? GetLastCompleted()
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<InventoryClosingRecord>()
                .Where(x => x.Status == ClosingStatus.Completed)
                .OrderByDescending(x => x.ClosingDate)
                .First();
        }

        /// <summary>
        /// 判断指定年月是否已结账
        /// </summary>
        public static bool IsPeriodClosed(int year, int month)
        {
            var record = SelectByYearMonth(year, month);
            return record != null && record.Status == ClosingStatus.Completed;
        }

        /// <summary>
        /// 插入结账记录（返回自增ID）
        /// </summary>
        public int Insert()
        {
            SqlClient SSC = new SqlClient();
            this.CreatedAt = DateTime.Now;
            return SSC.Db.Insertable(this).ExecuteReturnIdentity();
        }

        /// <summary>
        /// 更新结账记录
        /// </summary>
        public void Update()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Updateable(this).ExecuteCommand();
        }

        /// <summary>
        /// 删除结账记录（同时关联删除对应历史结余和批次历史）
        /// </summary>
        public void Delete()
        {
            using var scope = new TransactionScope();
            SqlClient SSC = new SqlClient();
            // 删除关联的历史结余
            SSC.Db.Deleteable<InventoryBalanceHistory>()
                .Where(x => x.ClosingRecordId == this.Id)
                .ExecuteCommand();
            // 删除关联的FIFO批次历史
            SSC.Db.Deleteable<FIFOInventoryLotHistory>()
                .Where(x => x.ClosingRecordId == this.Id)
                .ExecuteCommand();
            // 删除结账记录本身
            SSC.Db.Deleteable(this).ExecuteCommand();
            scope.Complete();
        }

    }

    /// <summary>
    /// 结账状态
    /// </summary>
    public enum ClosingStatus
    {
        Pending,    // 待处理
        Processing, // 处理中
        Completed,  // 已完成
        Failed      // 已失败
    }

    /// <summary>
    /// 历史库存结余
    /// </summary>
    public class InventoryBalanceHistory
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 关联的结账记录ID
        /// </summary>
        public int ClosingRecordId { get; set; }

        public string WarehouseCode { get; set; }
        public string InvCode { get; set; }
        public string SKU { get; set; }

        /// <summary>
        /// 结存数量
        /// </summary>
        public decimal Quantity { get; set; }

        /// <summary>
        /// 结存单价
        /// </summary>
        public decimal Price { get; set; }

        /// <summary>
        /// 结存金额
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// 成本计价方法
        /// </summary>
        public string CostingMethod { get; set; }  //可以是 "MWA" 或 "FIFO" 可以通过inv得到

        /// <summary>
        /// 按结账记录ID查询所有历史结余
        /// </summary>
        public static List<InventoryBalanceHistory> SelectByClosingRecordId(int closingRecordId)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<InventoryBalanceHistory>()
                .IncludesAllFirstLayer()
                .Where(x => x.ClosingRecordId == closingRecordId)
                .ToList();
        }

        /// <summary>
        /// 查询指定商品某期的历史结余
        /// </summary>
        public static InventoryBalanceHistory? SelectByInventory(int closingRecordId, string warehouseCode, string invCode, string sku)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<InventoryBalanceHistory>()
                .First(x => x.ClosingRecordId == closingRecordId
                         && x.WarehouseCode == warehouseCode
                         && x.InvCode == invCode
                         && x.SKU == sku);
        }

        /// <summary>
        /// 批量插入历史结余（结账时专用，性能最优）
        /// </summary>
        public static void InsertRange(List<InventoryBalanceHistory> list)
        {
            if (list == null || list.Count == 0) return;
            SqlClient SSC = new SqlClient();
            // 批量插入支持10万级数据高效写入
            SSC.Db.Insertable(list).ExecuteCommand();
        }

        /// <summary>
        /// 单条插入（不推荐，批量请用InsertRange）
        /// </summary>
        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Insertable(this).ExecuteCommand();
        }

    }

    /// <summary>
    /// FIFO批次历史记录
    /// </summary>
    public class FIFOInventoryLotHistory
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 关联的结账记录ID
        /// </summary>
        public int ClosingRecordId { get; set; }

        public string WarehouseCode { get; set; }
        public string InvCode { get; set; }
        public string SKU { get; set; }
        public decimal Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal Amount { get; set; }
        public DateOnly TransactionDate { get; set; }
        public bool IsNegative { get; set; }

        /// <summary>
        /// 按结账记录ID查询所有FIFO批次历史
        /// </summary>
        public static List<FIFOInventoryLotHistory> SelectByClosingRecordId(int closingRecordId)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<FIFOInventoryLotHistory>()
                .Where(x => x.ClosingRecordId == closingRecordId)
                .OrderBy(x => x.TransactionDate)
                .ToList();
        }

        /// <summary>
        /// 批量插入FIFO批次历史（结账时专用）
        /// </summary>
        public static void InsertRange(List<FIFOInventoryLotHistory> list)
        {
            if (list == null || list.Count == 0) return;
            SqlClient SSC = new SqlClient();
            SSC.Db.Insertable(list).ExecuteCommand();
        }

        /// <summary>
        /// 单条插入（不推荐）
        /// </summary>
        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Insertable(this).ExecuteCommand();
        }

    }
}
