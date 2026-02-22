using ERP.Shared.Repository;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP.Shared.EAM
{

    public class SystemBusinessModule
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; }
        /// <summary>
        /// 模块/系统中文名称
        /// 示例：进销存系统、采购系统
        /// </summary>
        [SugarColumn(Length = 50, ColumnDescription = "系统中文名称", IsNullable = false)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 通用简写
        /// 示例：PSI、PMS
        /// </summary>
        [SugarColumn(Length = 20, ColumnDescription = "通用简写", IsNullable = false)]
        public string Abbreviation { get; set; } = string.Empty;

        /// <summary>
        /// 完整英文全称
        /// </summary>
        [SugarColumn(Length = 200, ColumnDescription = "完整英文全称", IsNullable = false)]
        public string FullEnglishName { get; set; } = string.Empty;

        /// <summary>
        /// 说明与补充描述
        /// 存储适用场景、注意事项、相近缩写区分等信息
        /// </summary>
        [SugarColumn(Length = 500, ColumnDescription = "功能描述", IsNullable = true)]
        public string? Description { get; set; }

        [SugarColumn(Length = 500, ColumnDescription = "说明补充", IsNullable = true)]
        public string? Remark { get; set; }

        public string? Icon { get; set; }
        public string? Style { get; set; }
        /// <summary>
        /// 排序号
        /// 用于页面展示时的自定义排序
        /// </summary>
        [SugarColumn(ColumnDescription = "排序号", DefaultValue = "0")]
        public int SortOrder { get; set; } = 0;

        /// <summary>
        /// 是否启用
        /// false时不在前端展示、不参与业务关联
        /// </summary>
        [SugarColumn(ColumnDescription = "是否启用", DefaultValue = "1")]
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(ColumnDescription = "创建时间", IsOnlyIgnoreUpdate = true)]
        public DateTime CreateTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [SugarColumn(ColumnDescription = "更新时间", IsOnlyIgnoreInsert = true)]
        public DateTime? UpdateTime { get; set; }

        public static List<SystemBusinessModule> Select()
        {
            var db = new SqlClient().Db;
            return db.Queryable<SystemBusinessModule>()
                .Where(m => m.IsEnabled) // 仅查询启用的模块
                .OrderBy(m => m.SortOrder) // 按排序号升序
                .ToList();
        }

    }

    // 资产状态
    public enum AssetStatus
    {
        [Description("闲置")]
        Idle = 0,
        [Description("在用")]
        InUse = 1,
        [Description("调拨中")]
        Transferring = 2,
        [Description("维修中")]
        Repairing = 3,
        [Description("已报废")]
        Scraped = 4,
        [Description("已出售")]
        Sold = 5
    }

    // 变动类型
    public enum AlterationType
    {
        [Description("部门调拨")]
        DepartmentTransfer = 0,
        [Description("使用人变更")]
        UserChange = 1,
        [Description("信息修改")]
        InfoUpdate = 2,
        [Description("原值调整")]
        ValueAdjust = 3
    }

    // 清理类型
    public enum DisposalType
    {
        [Description("报废")]
        Scrap = 0,
        [Description("出售")]
        Sell = 1,
        [Description("捐赠")]
        Donate = 2,
        [Description("盘亏")]
        InventoryLoss = 3
    }

    // 审批状态
    public enum ApprovalStatus
    {
        [Description("草稿")]
        Draft = 0,
        [Description("审批中")]
        Approving = 1,
        [Description("已通过")]
        Approved = 2,
        [Description("已驳回")]
        Rejected = 3
    }



}
