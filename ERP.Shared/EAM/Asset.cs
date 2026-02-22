using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace ERP.Shared.EAM
{
    [SugarTable("EAM_Asset")]
    public class Asset
    {
        [SugarColumn(ColumnName = "Id", IsIdentity = true)]
        public long Id { get; set; }

        [SugarColumn(ColumnName = "Code", IsPrimaryKey = true, Length = 50)]
        public string Code { get; set; } // 资产编码（全局唯一）

        [SugarColumn(ColumnName = "Name", Length = 200)]
        public string Name { get; set; } // 资产名称

        [SugarColumn(ColumnName = "CategoryId")]
        public int CategoryId { get; set; } // 分类ID

        [SugarColumn(ColumnName = "Spec", Length = 200, IsNullable = true)]
        public string Spec { get; set; } // 规格型号

        [SugarColumn(ColumnName = "Brand", Length = 100, IsNullable = true)]
        public string Brand { get; set; } // 品牌

        [SugarColumn(ColumnName = "PurchaseDate")]
        public DateTime PurchaseDate { get; set; } // 购入日期

        [SugarColumn(ColumnName = "OriginalValue", DecimalDigits = 2)]
        public decimal OriginalValue { get; set; } // 原值

        [SugarColumn(ColumnName = "NetValue", DecimalDigits = 2)]
        public decimal NetValue { get; set; } // 净值

        [SugarColumn(ColumnName = "AccumulatedDepreciation", DecimalDigits = 2)]
        public decimal AccumulatedDepreciation { get; set; } = 0; // 累计折旧

        [SugarColumn(ColumnName = "DepartmentCode", Length = 50, IsNullable = true)]
        public string DepartmentCode { get; set; } // 使用部门编码

        [SugarColumn(ColumnName = "EmployeeId", IsNullable = true)]
        public int? EmployeeId { get; set; } // 使用人ID

        [SugarColumn(ColumnName = "Location", Length = 200, IsNullable = true)]
        public string Location { get; set; } // 存放地点

        [SugarColumn(ColumnName = "Status")]
        public AssetStatus Status { get; set; } = AssetStatus.Idle;

        [SugarColumn(ColumnName = "Active")]
        public bool Active { get; set; } = true;

        [SugarColumn(ColumnName = "InsertTime")]
        public DateTime InsertTime { get; set; } = DateTime.Now;

        // 导航属性
        [Navigate(NavigateType.OneToOne, nameof(CategoryId))]
        public AssetCategory Category { get; set; }

    }

    [SugarTable("EAM_AssetCategory", TableDescription = "资产分类表")]
    public class AssetCategory : BaseEntity
    {
        // 👇 通用字段Id/Code/Name/Active/InsertTime/UpdateTime/Remark全部从BaseEntity继承，无需重复编写

        /// <summary>
        /// 上级分类ID，完全适配你现有`ToTree`扩展方法，和部门实体的`Superior`关联逻辑100%一致
        /// </summary>
        [SugarColumn(ColumnName = "SuperiorId", IsNullable = true, ColumnDescription = "上级分类ID")]
        public int? SuperiorId { get; set; }

        /// <summary>
        /// 分类默认折旧年限（年），如电子设备3年、办公家具5年
        /// </summary>
        [SugarColumn(ColumnName = "DepreciationYear", ColumnDescription = "折旧年限（年）")]
        public int DepreciationYear { get; set; } = 3;

        /// <summary>
        /// 分类默认残值率，默认5%
        /// </summary>
        [SugarColumn(ColumnName = "ResidualRate", DecimalDigits = 4, ColumnDescription = "残值率")]
        public decimal ResidualRate { get; set; } = 0.05m;

        #region 导航属性（适配SqlSugar和树形扩展）
        /// <summary>
        /// 上级分类
        /// </summary>
        [Navigate(NavigateType.OneToOne, nameof(SuperiorId))]
        public AssetCategory Parent { get; set; }

        /// <summary>
        /// 子分类列表，`ToTree`扩展方法会自动填充
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(SuperiorId))]
        public List<AssetCategory> Children { get; set; } = new List<AssetCategory>();
        #endregion
    }

    [SugarTable("EAM_AssetApproval")]
    public class AssetApproval : BaseEntity
    {
        [SugarColumn(ColumnName = "ApprovalType")]
        public int ApprovalType { get; set; } // 0=新增 1=变动 2=清理

        [SugarColumn(ColumnName = "AssetId", IsNullable = true)]
        public int? AssetId { get; set; } // 关联资产ID

        [SugarColumn(ColumnName = "Status")]
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Draft;

        [SugarColumn(ColumnName = "ApplicantId")]
        public int ApplicantId { get; set; } // 申请人ID

        [SugarColumn(ColumnName = "Active")]
        public bool Active { get; set; } = true;

        [SugarColumn(ColumnName = "InsertTime")]
        public DateTime InsertTime { get; set; } = DateTime.Now;

        // 导航属性
        [Navigate(NavigateType.OneToOne, nameof(AssetId))]
        public Asset Asset { get; set; }
    }


}
