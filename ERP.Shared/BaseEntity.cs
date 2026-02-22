using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ERP.Shared
{
    using SqlSugar;
    using System.ComponentModel;
    using static System.Runtime.InteropServices.JavaScript.JSType;

    /// <summary>
    /// 所有业务实体基类，内置通用字段
    /// </summary>
    public class BaseEntity
    {
        /// <summary>
        /// 主键ID
        /// </summary>
        [SugarColumn(ColumnName = "Id", IsIdentity = true, ColumnDescription = "主键ID")]
        public long Id { get; set; }

        /// <summary>
        /// 编码
        /// </summary>
        [SugarColumn(ColumnName = "Code", IsPrimaryKey = true, Length = 50, IsNullable = true, ColumnDescription = "编码")]
        public virtual string Code { get; set; }
        /// <summary>
        /// 单据日期
        /// </summary>
        public DateOnly Date { get; set; } = new DateOnly();
        /// <summary>
        /// 名称
        /// </summary>
        [SugarColumn(ColumnName = "Name", Length = 200, IsNullable = true, ColumnDescription = "名称")]
        public virtual string Name { get; set; }

        /// <summary>
        /// 是否启用（软删除标记）
        /// </summary>
        [SugarColumn(ColumnName = "Active", ColumnDescription = "是否启用")]
        [DefaultValue(true)]
        public bool Active { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [SugarColumn(ColumnName = "InsertTime", ColumnDescription = "创建时间")]
        public DateTime InsertTime { get; set; } = DateTime.Now;

        /// <summary>
        /// 更新时间
        /// </summary>
        [SugarColumn(ColumnName = "UpdateTime", IsNullable = true, ColumnDescription = "更新时间")]
        public DateTime? UpdateTime { get; set; }

        public int? FiscalYear { set; get; }
        public int? Period { set; get; }
        /// <summary>
        /// 备注
        /// </summary>
        [SugarColumn(ColumnName = "Remark", Length = 1000, IsNullable = true, ColumnDescription = "备注")]
        public string Remark { get; set; }

        public BaseEntity()
        {
            Id = SnowFlakeSingle.Instance.NextId();
            Date = DateOnly.FromDateTime(DateTime.Now);
            FiscalYear = DateTime.Now.Year;
            Period = DateTime.Now.Month;
        }
    }
}
