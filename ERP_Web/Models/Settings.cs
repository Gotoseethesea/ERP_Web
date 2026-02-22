using AntDesign;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using System.ComponentModel.DataAnnotations;

namespace ERP_Web.Models
{
    public class TrxGroup
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; } = "";
        public string Name { get; set; }
        public int TrxType { get; set; } //1、Inbound（入库），-1、Outbound（出库），0、InThenOut  先入后出 直入直出
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }

    public enum TrxType
    {
        //1、Inbound（入库），-1、Outbound（出库），0、InThenOut  先入后出 直入直出
        Inbound = 1, //收货入库
        Outbound = -1,//出库
        InThenOut = 0//先入后出 直入直出
    }
    public class Category
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; } = 0;
        [Display(Name = "分类代码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; }
        [Display(Name = "存货分类")]
        public string Name { get; set; } = "";
        public string? Note { get; set; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
        public int ParentId { set; get; } = 0;
        /// <summary>
        /// 分类路径，存储所有父级ID，如：,1,2,3,
        /// </summary>
        public string? CategoryPath { set; get; } = "";
        /// <summary>
        /// 排序字段,从小到大
        /// </summary>
    }

    public class Company
    {
        [Display(Name = "公司代码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        [Display(Name = "公司名称")]
        public string Name { set; get; }
        [Display(Name = "公司分类")]
        public string? CompanyGroup { set; get; }
        [Display(Name = "公司性质")]
        public string? CompanyType { set; get; }
        public string? TaxId { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; } = 0;

    }

    public class Department
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; } = 0;
        [Display(Name = "部门代码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        [Display(Name = "部门名称")]
        public string Name { set; get; }
        [Display(Name = "上级部门代码")]
        public string? Superior { set; get; }
        [Display(Name = "上级部门名称")]
        public string? SuperiorDec { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;

    }

    public class Employee
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; } = 0;
        [Display(Name = "部门代码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        [Display(Name = "部门名称")]
        public string Name { set; get; }
        [Display(Name = "上级部门代码")]
        public int? Superior { set; get; }
        [Display(Name = "上级部门名称")]
        public string? SuperiorDec { set; get; }
        public string? Note { set; get; }
        public bool Active { set; get; } = true;
    }

    public class Warehouse
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; } = 0;
        [Display(Name = "仓库代码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        [Display(Name = "仓库名称")]
        public string Name { set; get; }
        [Display(Name = "仓库收发类型")]
        public string? TrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(TrxGroupCode))]//一对一 
        public TrxGroup TrxGroup { set; get; } = new OneToOneInitializer<TrxGroup>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }

    public class MenuList
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { set; get; }
        public string? Code { set; get; }
        public string Name { set; get; } = "Home";              //菜单名称
        public string Path { set; get; } = "/";                 //菜单路径
        public string Key { set; get; } = "any";                //绑定标签{ set; get; }
        //public string[] ParentKeys { set; get; } = new string[] { "any" };
        public string? Icon { set; get; }                       //图标
        public bool HideChildrenInMenu { set; get; } = false;   //会把这个路由的子节点在 menu
        public bool HideInMenu { set; get; } = false;           //是否在菜单中隐藏
        public string? Locale { set; get; }                     //可以设置菜单名称的国际化表示49应该
        public int? ParentId { set; get; }
        [Navigate(NavigateType.OneToMany, nameof(MenuList.ParentId))]//一对多
        public List<MenuList>? Children { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }

    public class User
    {
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        public string? Account { set; get; }
        public string? Password { set; get; }
        public string? Name { set; get; }
        public string? Role { set; get; }
        public string? Department { set; get; }
        public string? JobTitle { set; get; }
        public string? Language { set; get; }
        public string? Phone { set; get; }
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();

        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }
}

