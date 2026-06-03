using AntDesign.ProLayout;
using ERP_Web.Repository;
using ERP_Web.Models.HRMS;
using NPinyin;
using NPOI.POIFS.Properties;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography.X509Certificates;

namespace ERP_Web.Models
{
    public abstract class SettingBaseEntity<T> where T : class, new()
    {
        public string? Note { set; get; }
        public int? Sequence { set; get; }

        public bool Active { get; set; } = true;
        public static List<T> Select()
        {
            var db = new SqlClient();
            return db.Db.Queryable<T>()
                     .Where("Active = 1")  // ✅ 假设所有实体都有Active字段
                     .ToList();
        }

        public static List<T> SelectAll()
        {
            var db = new SqlClient();
            return db.Db.Queryable<T>().ToList();
        }

        public void Insert()
        {
            var db = new SqlClient();
            db.Db.Insertable<T>(this).ExecuteCommand();
        }
        public void Update()
        {
            var db = new SqlClient();
            db.Db.Updateable<T>(this).ExecuteCommand();
        }
        public void Delete()
        {

            var db = new SqlClient();
            db.Db.Updateable<T>(this).ExecuteCommand();
        }
    }

    public abstract class EntityBase<T> where T : class, new()
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public virtual int Code { get; set; } //= Guid.NewGuid().ToString("N");

        [SugarColumn(ColumnDescription = "备注信息")]
        public string? Note { get; set; }

        [SugarColumn(ColumnDescription = "排序序号")]
        public int? Sequence { get; set; }

        [SugarColumn(ColumnDescription = "启用状态")]
        public bool Active { get; set; } = true;

        // 同步操作
        public virtual List<T> Select(bool includeInactive = false)
        {
            var db = new SqlClient();
            return db.Db.Queryable<T>()
                .WhereIF(!includeInactive, "Active = 1")
                .ToList();
        }

        public virtual void Insert()
        {
            var db = new SqlClient();
            db.Db.Insertable(this as T).ExecuteCommand();
        }

        public virtual void Update()
        {
            var db = new SqlClient();
            db.Db.Updateable(this as T).ExecuteCommand();
        }

        public virtual void Delete(bool physicalDelete = false)
        {
            var db = new SqlClient();
            if (physicalDelete)
            {
                db.Db.Deleteable(this as T).ExecuteCommand();
            }
            else
            {
                Active = false;
                Update();
            }
        }

        // 异步操作
        public virtual async Task<List<T>> SelectAsync(bool includeInactive = false)
        {
            var db = new SqlClient();
            return await db.Db.Queryable<T>()
                .WhereIF(!includeInactive, "Active = 1")
                .ToListAsync();
        }

        public virtual async Task InsertAsync()
        {
            var db = new SqlClient();
            await db.Db.Insertable(this as T).ExecuteCommandAsync();
        }

        public virtual async Task UpdateAsync()
        {
            var db = new SqlClient();
            await db.Db.Updateable(this as T).ExecuteCommandAsync();
        }

        public virtual async Task DeleteAsync(bool physicalDelete = false)
        {
            var db = new SqlClient();
            if (physicalDelete)
            {
                await db.Db.Deleteable(this as T).ExecuteCommandAsync();
            }
            else
            {
                Active = false;
                await UpdateAsync();
            }
        }
    }
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
        InThenOut = 0 //先入后出 直入直出
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
        [Navigate(NavigateType.OneToMany, nameof(ParentId))]
        public List<Category> Children { get; set; } = new List<Category>();

        /// <summary>
        /// 排序字段,从小到大
        /// </summary>
        public static List<Category> Select()
        {
            SqlClient SSC = new SqlClient();
            List<Category> result = SSC.Db.Queryable<Category>()
                .IncludesAllFirstLayer()
                .Where(x => x.Active == true).ToList();
            return result;
    }
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

        // 查询方法
        public static List<Company> Select()
        {
            var sqlClient = new SqlClient();
            return sqlClient.Db.Queryable<Company>()
                .IncludesAllFirstLayer()
                .Where(x => x.Active == true)
                .ToList();
    }

        // 插入方法
        public async Task Insert()
        {
            var sqlClient = new SqlClient();
            await sqlClient.Db.Insertable(this).ExecuteCommandAsync();
        }

        // 更新方法
        public async Task Update()
        {
            var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable(this).ExecuteCommandAsync();
        }

        // "删除"方法（标记为无效）
        public async Task Delete()
        {
            this.Active = false;
            await Update();
        }
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
        public string? Phone { set; get; }
        public string? Email { set; get; }
        public string? Remark { set; get; }
        public string? ManagerName { set; get; }  //部门负责人
        [Display(Name = "上级部门代码")]
        public int? Superior { set; get; }
        [Display(Name = "上级部门名称")]
        public string? SuperiorDec { set; get; }
        [Display(Name = "部门路径")]
        public string? DepartmentPath { get; set; }  // 新增：存储部门层级路径
        public string? Note { set; get; }
        [Display(Name = "排序")]
        public int? Sequence { set; get; } = 1;
        public bool Active { set; get; } = true;
        // 导航属性：下级部门（一对多关系）
        [Navigate(NavigateType.OneToMany, nameof(Superior))]
        public List<Department> Children { get; set; } = new List<Department>();

        [Navigate(typeof(PositionDepartmentMapping), nameof(PositionDepartmentMapping.DepartmentCode), nameof(PositionDepartmentMapping.PositionCode))]//注意顺序
        public List<Position> Positions { get; set; }


        public static List<Department> Select()
        {
            var sqlClient = new SqlClient();
            return sqlClient.Db.Queryable<Department>().Where(z1 => z1.Active == true).ToList();
        }
        public void Insert()
        {
            var sqlClient = new SqlClient();
            sqlClient.Db.Insertable(this).ExecuteCommand();
        }
        public void Update()
        {
            var sqlClient = new SqlClient();
            sqlClient.Db.Updateable(this).ExecuteCommand();
        }
        public void Delete()
        {
            this.Active = false;
            this.Update();
        }
        // 扁平化树结构的方法
        public IEnumerable<Department> Flatten()
        {
            yield return this;
            if (Children != null)
            {
                foreach (var child in Children.SelectMany(c => c.Flatten()))
                {
                    yield return child;
                }
            }
    }
        // 构建树形结构的方法
        public static List<Department> BuildTree(List<Department> allDepartments, int? Superior = null)
        {
            return allDepartments
                .Where(d => d.Superior == Superior)
                .Select(d => new Department
                {
                    Id = d.Id,
                    Code = d.Code,
                    Name = d.Name,
                    Superior = d.Superior,
                    DepartmentPath = d.DepartmentPath,
                    Note = d.Note,
                    Sequence = d.Sequence,
                    Active = d.Active,
                    Children = BuildTree(allDepartments, d.Id)  // 递归构建子树
                })
                .OrderBy(d => d.Sequence)
                .ToList();
        }
        // 获取所有平面数据并构建树
        public static async Task<List<Department>> GetTree()
        {
            var sqlClient = new SqlClient();
            var allDepartments = await sqlClient.Db.Queryable<Department>()
                .Where(z1 => z1.Active)
                .ToListAsync();

            return BuildTree(allDepartments, null);
        }
        // 更新部门路径的方法
        public static void UpdateDepartmentPath(Department department, List<Department> allDepartments)
        {
            var path = new List<string>();
            int? currentId = department.Id;

            while (currentId.HasValue)
            {
                var current = allDepartments.FirstOrDefault(d => d.Id == currentId);
                if (current == null) break;

                path.Insert(0, current.Id.ToString());
                currentId = current.Superior;
            }

            department.DepartmentPath = path.Count > 0 ? $",{string.Join(",", path)}," : null;
        }

        // 获取下一个排序序号
        public static int GetNextSequence(List<Department> departments, int? Superior)
        {
            var siblings = departments
                .Where(d => d.Superior == Superior)
                .ToList();

            if (!siblings.Any()) return 1;

            return (int)(siblings.Max(d => d.Sequence) + 1);
        }
    }
    public class Employee
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; } = 0;
        [Display(Name = "职员代码")]
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        [Display(Name = "职员名称")]
        public string Name { set; get; }
        /// <summary>
        /// 性别 1=男 2=女
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// 身份证号
        /// </summary>
        public string? IdCard { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string? Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 所属部门编码
        /// </summary>
        public string DepartmentCode { get; set; }

        /// <summary>
        /// 岗位编码
        /// </summary>
        public string PositionCode { get; set; }

        /// <summary>
        /// 职级编码
        /// </summary>
        public string SalaryLevelCode { get; set; }

        /// <summary>
        /// 基本工资
        /// </summary>
        public decimal BaseSalary { get; set; }

        /// <summary>
        /// 岗位工资
        /// </summary>
        public decimal PostSalary { get; set; }

        /// <summary>
        /// 入职日期
        /// </summary>
        public DateTime HireDate { get; set; }

        /// <summary>
        /// 转正日期
        /// </summary>
        public DateTime? RegularDate { get; set; }

        /// <summary>
        /// 离职日期
        /// </summary>
        public DateTime? ResignDate { get; set; }

        /// <summary>
        /// 员工状态 1=在职 2=试用期 3=离职 4=退休 5=停职
        /// </summary>
        public int Status { get; set; } = 2;

        /// <summary>
        /// 紧急联系人
        /// </summary>
        public string? EmergencyContact { get; set; }

        /// <summary>
        /// 紧急联系电话
        /// </summary>
        public string? EmergencyPhone { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode), nameof(Department.Code))]
        public Department Department { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(PositionCode), nameof(Position.Code))]
        public Position Position { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(SalaryLevelCode), nameof(SalaryLevel.Code))]
        public SalaryLevel SalaryLevel { get; set; }

        /// <summary>
        /// 关联合同列表
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(EmployeeContract.EmployeeCode))]
        public List<EmployeeContract> Contracts { get; set; }
        #endregion
        public string? Note { set; get; }
        public bool Active { set; get; } = true;
        public DateTime? InsertTime { get; set; } = DateTime.Now;
        public string? InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; } = DateTime.Now;
        public string? UpdateUser { get; set; }

        public Employee() { }

        public void EmployeeInit(string str) {
            Code ??= Pinyin.GetInitials(Name);
            //如果有重复的加数字编码，后续优化
        }

        // 查询方法
        public static async Task<List<Employee>> Select()
        {
            var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<Employee>()
                .Where(z1 => z1.Active == true)
                .ToListAsync();
        }

        // 插入方法
        public async Task Insert()
        {
            var sqlClient = new SqlClient();
            await sqlClient.Db.Insertable(this).ExecuteCommandAsync();
        }

        // 更新方法
        public async Task Update()
        {
            var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable(this).ExecuteCommandAsync();
    }

        // "删除"方法（标记为无效）
        public async Task Delete()
        {
            this.Active = false;
            await Update();
        }
    }
    public class Warehouse : SettingBaseEntity<Warehouse>
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

