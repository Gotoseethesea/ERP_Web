using AntDesign.ProLayout;
using ERP_Web.Repository;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Components.Authorization;
using NPinyin;
using SqlSugar;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using static Microsoft.AspNetCore.Http.HttpContext;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ERP_Web.Models.PrivilegeHub
{
    // ==================== 用户实体 ====================
    public class User
    {
        public long Id { set; get; }       //后期改为 int 自增列
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { set; get; }
        public string? Account { set; get; }
        public string? Password { set; get; }
        public string? Name { set; get; }
        public string? Approval { set; get; }  //角色 审批角色
        public string? FeatureRole { set; get; }  //功能权限角色
        /// <summary>
        /// 导航属性：用户拥有的权限角色
        /// </summary>
        [Navigate(typeof(UserRole), nameof(UserRole.UserId), nameof(UserRole.RoleId), nameof(User.Id), nameof(Role.Id))]     //下次优化：改为直接导航到Role实体，简化查询
        public List<Role> Roles { get; set; } // 用户-角色多对多关系 替换掉原来的单一角色字段 FeatureRole

        /// <summary>
        /// 导航属性：用户拥有的审批角色（多对多）
        /// 原ApprovalRole字符串字段可以保留做兼容，后续可以废弃
        /// </summary>
        [Navigate(typeof(UserApprovalRole), nameof(UserApprovalRole.UserId), nameof(UserApprovalRole.ApprovalRoleId), nameof(User.Id), nameof(ApprovalRole.Id))]
        public List<ApprovalRole> ApprovalRoles { get; set; }  // 用户-角色多对多关系 替换掉原来的单一角色字段 ApprovalRole
        public string? Language { set; get; }
        public string? Phone { set; get; }
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;


        public User() { }
        public void UserInit()
        {
            // 当Employee可能为null时
            Id = Id = SnowFlakeSingle.Instance.NextId();
            Name ??= Employee?.Name ?? "Default";
            Account ??= Pinyin.GetInitials(Name);
            Code = Account+Id.ToString();
            Password ??= "1";
        }

        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Insertable(this).ExecuteCommand();
        }
        public static List<User> Select()
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<User>().Where(z1 => z1.Active == true).ToList();
        }

        public static List<User> SelectIncUnActive()
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<User>().ToList();
        }

        public static User SelectByAccount(string Account)
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<User>()
                .IncludesAllFirstLayer()
                .Where(z1 => z1.Account == Account && z1.Active == true).First(); ;
        }

        public void Update()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Updateable(this).ExecuteCommand();
        }

        public List<Role> GetRoles()
        {
            SqlClient SSC = new SqlClient();
            var Roles = SSC.Db.Queryable<User, UserRole, Role>(
                (u, ur, r) => u.Id == ur.UserId && ur.RoleId == r.Id
                )
                .Where((u, ur, r) => u.Id == this.Id)
                .Select((u, ur, r) => r)
                .Distinct()
                .ToList();
            return Roles;
        }

        // 新增：获取用户所有审批角色
        public List<ApprovalRole> GetApprovalRoles()
        {
            var db = new SqlClient();
            return db.Db.Queryable<User, UserApprovalRole, ApprovalRole>(
                    (u, ur, r) => u.Id == ur.UserId && ur.ApprovalRoleId == r.Id
                )
                .Where((u, ur, r) => u.Id == this.Id)
                .Select((u, ur, r) => r)
                .Distinct()
                .ToList();
        }

        // 新增：更新用户审批角色（事务操作）
        public async Task UpdateApprovalRoles(List<int> roleIds)
        {
            var db = new SqlClient();
            try
            {
                db.Db.Ado.BeginTran();

                // 1. 删除现有关联
                await db.Db.Deleteable<UserApprovalRole>()
                    .Where(ur => ur.UserId == this.Id)
                    .ExecuteCommandAsync();

                // 2. 插入新关联
                if (roleIds != null && roleIds.Count > 0)
                {
                    var userRoles = roleIds.Select(roleId => new UserApprovalRole
                    {
                        UserId = this.Id,
                        ApprovalRoleId = roleId
                    }).ToList();
                    await db.Db.Insertable(userRoles).ExecuteCommandAsync();
                }

                // 3. 更新导航属性
                this.ApprovalRoles = await db.Db.Queryable<ApprovalRole>()
                    .Where(r => roleIds.Contains(r.Id))
                    .ToListAsync();

                db.Db.Ado.CommitTran();
            }
            catch (Exception ex)
            {
                db.Db.Ado.RollbackTran();
                throw new Exception($"更新审批角色失败: {ex.Message}");
            }
        }

        public async Task UpdateRoles(List<int> roleIds)
        {
            SqlClient SSC = new SqlClient();

            try
            {
                // 开启事务
                SSC.Db.Ado.BeginTran();

                // 1. 删除用户现有的所有角色关联
                await SSC.Db.Deleteable<UserRole>()
                    .Where(ur => ur.UserId == this.Id)
                    .ExecuteCommandAsync();

                // 2. 插入新的角色关联
                if (roleIds != null && roleIds.Count > 0)
                {
                    var userRoles = roleIds.Select(roleId => new UserRole
                    {
                        UserId = this.Id,
                        RoleId = roleId
                    }).ToList();

                    await SSC.Db.Insertable(userRoles).ExecuteCommandAsync();
                }

                // 3. 更新用户对象的角色导航属性
                this.Roles = await SSC.Db.Queryable<Role>()
                    .Where(r => roleIds.Contains(r.Id))
                    .ToListAsync();

                // 提交事务
                SSC.Db.Ado.CommitTran();
            }
            catch (Exception ex)
            {
                // 回滚事务
                SSC.Db.Ado.RollbackTran();
                throw new Exception($"更新用户角色失败: {ex.Message}");
            }
        }

        public static User Login(string Account, string Password)
        {
            SqlClient SSC = new SqlClient();
            var user = SSC.Db.Queryable<User>()
            .Where(u => u.Account == Account &&
                        u.Password == Password &&
                        u.Active).First();
            if (user != null)
            {
                user.Roles = user.GetRoles();// 预加载角色信息
            }
            return user;
        }

        // ==================== 登录登出方法 ====================
        // 登录方法：创建Claims并写入Cookie
        public static async Task LoginAsync(User user)
        {
            // 1. 创建Claims集合
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("UserId", user.Id.ToString()),
                new Claim("Account", user.Account),
                new Claim(ClaimTypes.NameIdentifier, user.Account) // 唯一标识建议用Account
            };

            // 2. 添加角色Claims
            var roles = user.Roles.Select(r => r.Name).ToArray();
            claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            // 3. 创建身份
            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // 4. 创建认证票据
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // 持久化Cookie
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30), // 30天有效期
                AllowRefresh = true // 允许刷新
            };
            // 5. 登录并写入Cookie
            var context = new HttpContextAccessor().HttpContext;

            await context.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                authProperties);
        }

        public static async Task LogoutAsync()
        {
            // 登出并删除Cookie
            var context = new HttpContextAccessor().HttpContext;
            await context.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }
    }

    // ==================== 角色实体 ====================
    public class Role
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        public string? Code { set; get; }
        public string Name { set; get; } = "Home";       //权限名称
        //public string? Description { set; get; } //权限名称
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
        [Navigate(typeof(UserRole), nameof(UserRole.RoleId), nameof(UserRole.UserId), nameof(Role.Id), nameof(User.Id))]//注意顺序
        public List<User>? users { get; set; }//只能是null不能赋默认值

        [Navigate(typeof(RolePermission), nameof(RolePermission.RoleId), nameof(RolePermission.PermissionId), nameof(Role.Id), nameof(Permission.Id))]//注意顺序
        public List<Permission>? Permissions { get; set; }//只能是null不能赋默认值


        public void Insert()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Insertable(this).ExecuteCommand();
        }
        public static List<Role> Select()
        {
            SqlClient SSC = new SqlClient();
            return SSC.Db.Queryable<Role>().IncludesAllFirstLayer().IncludesAllSecondLayer(xx => xx.Permissions).ToList();
        }

        public void Update()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Updateable(this).ExecuteCommand();
        }

        public void Delete()
        {
            SqlClient SSC = new SqlClient();
            this.Active = false;
            SSC.Db.Updateable(this).ExecuteCommand();
        }

        public void GetPri()
        {
            SqlClient SSC = new SqlClient();
            SSC.Db.Updateable(this).ExecuteCommand();
        }

    }
    // ==================== 用户-角色关联实体 ====================
    public class UserRole
    {
        [SugarColumn(IsPrimaryKey = true)] // 复合主键
        public long UserId { get; set; }

        [SugarColumn(IsPrimaryKey = true)]
        public int RoleId { get; set; }

        // 导航到用户
        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        public User User { get; set; }

        // 导航到角色
        [Navigate(NavigateType.OneToOne, nameof(RoleId))]
        public Role Role { get; set; }
    }

    // ==================== 审批角色实体 ====================

    /// <summary>
    /// 审批角色实体
    /// </summary>
    [SugarTable("ApprovalRole")]
    public class ApprovalRole
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        public string? Code { set; get; }
        public string Name { set; get; } = string.Empty;
        /// <summary>
        /// 审批角色描述
        /// </summary>
        [SugarColumn(Length = 200)]
        public string? Description { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;

        /// <summary>
        /// 多对多导航：拥有该审批角色的用户
        /// </summary>
        [Navigate(typeof(UserApprovalRole), nameof(UserApprovalRole.ApprovalRoleId), nameof(UserApprovalRole.UserId), nameof(ApprovalRole.Id), nameof(User.Id))]
        public List<User>? Users { get; set; }

        #region 基础CRUD方法
        public void Insert()
        {
            var db = new SqlClient();
            db.Db.Insertable(this).ExecuteCommand();
        }

        public static List<ApprovalRole> Select()
        {
            var db = new SqlClient();
            return db.Db.Queryable<ApprovalRole>().Where(r => r.Active).OrderBy(r => r.Sequence).ToList();
        }

        public void Update()
        {
            var db = new SqlClient();
            db.Db.Updateable(this).ExecuteCommand();
        }

        public void Delete()
        {
            var db = new SqlClient();
            this.Active = false;
            db.Db.Updateable(this).ExecuteCommand();
        }
        #endregion
    }

    /// <summary>
    /// 用户-审批角色多对多关联表
    /// </summary>
    [SugarTable("UserApprovalRole")]
    public class UserApprovalRole
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long UserId { get; set; }

        [SugarColumn(IsPrimaryKey = true)]
        public int ApprovalRoleId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(UserId))]
        public User User { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ApprovalRoleId))]
        public ApprovalRole ApprovalRole { get; set; }
    }
    // ==================== 权限实体 ====================
    public class Permission
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)] // 自增主键
        public int Id { get; set; }

        [SugarColumn(Length = 100, IsNullable = false)]
        public string PermissionKey { get; set; } = string.Empty; // 如 "purchase:create"

        [SugarColumn(Length = 100, IsNullable = false)]
        public string? Description { get; set; } = string.Empty; // 如 "purchase:create"

        [SugarColumn(IsIgnore = true)] // 仅在代码中使用
        public string Category => PermissionKey.Split(':')[0];

        /// <summary>
        /// 通过用户获取对应的权限 PermissionKeys
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public static HashSet<string> GetUserPermissions(User user)
        {
            SqlClient SSC = new SqlClient();

            //// 1. 获取用户的所有角色ID
            //var roleIds = SSC.Db.Queryable<UserRole>()
            //    .Where(ur => ur.UserId == user.Id)
            //    .Select(ur => ur.RoleId)
            //    .ToList();

            //// 2. 通过角色ID获取权限Key
            //var permissionKeys = SSC.Db.Queryable<RolePermission>()
            //    .Where(rp => roleIds.Contains(rp.RoleId)) // 使用内存Contains
            //    .Select(rp => rp.Permission.PermissionKey) // 通过导航属性获取
            //    .Distinct()
            //    .ToList();

            var permissionKeys = SSC.Db.Queryable<UserRole, RolePermission, Permission>(
                    (ur, rp, p) => ur.RoleId == rp.RoleId && rp.PermissionId == p.Id
                )
                .Where((ur, rp, p) => ur.UserId == user.Id)
                .Select((ur, rp, p) => p.PermissionKey)
                .Distinct()
                .ToList();
            return new HashSet<string>(permissionKeys);
        }
    }

    // ==================== 角色-权限关联实体 ====================
    public class RolePermission
    {
        [SugarColumn(IsPrimaryKey = true)]
        public int RoleId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(RoleId))]
        public Role Role { get; set; }

        [SugarColumn(IsPrimaryKey = true)]
        public int PermissionId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(PermissionId))]
        public Permission Permission { get; set; }

        // 新增按钮权限关联
        [SugarColumn(IsNullable = true)]
        public int? ButtonId { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ButtonId))]
        public SysButton Button { get; set; }
    }
    public class AppMenu
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { set; get; }
        public string? Code { set; get; }
        public string Name { set; get; } = "Home";
        public string Path { set; get; } = "/";
        public string Key { set; get; } = "any";
        public string? Icon { set; get; }
        public bool HideChildrenInMenu { set; get; } = false;
        public bool HideInMenu { set; get; } = false;
        public string? Locale { set; get; }

        [SugarColumn(IsNullable = true)]
        public int? ParentId { set; get; }

        [Navigate(NavigateType.OneToMany, nameof(ParentId))]
        public List<AppMenu>? Children { set; get; }

        // 🔽 核心优化点：用权限标识替代角色字段
        public string PermissionKey { get; set; } = ""; // 如 "menu:view:purchase"
        public string? Module { get; set; } // 如 "Hr" "Inv"
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;

        public static List<AppMenu> Select(){
            var sqlClient = new SqlClient();
            return (sqlClient.Db.Queryable<AppMenu>()
                        .Where(m => m.Active == true)
                        .ToList()); // 注意：这里我们获取所有菜单，然后自己构建树
        }


        // 🔽 优化构建方法 - 添加权限过滤
        public static MenuDataItem[] CreateMenu(List<AppMenu> menuData, HashSet<string> userPermissions)
        {
            return menuData
                .Where(menu => HasPermission(menu, userPermissions))
                .Select(item => MapToDataItem(item, userPermissions))
                .ToArray();
        }

        // 🔽 权限检查方法
        private static bool HasPermission(AppMenu menu, HashSet<string> userPermissions)
        {
            // 权限检查逻辑
            return string.IsNullOrEmpty(menu.PermissionKey) || // 无权限要求
                   menu.PermissionKey == "*" ||              // 全开放
                   userPermissions.Contains(menu.PermissionKey); // 有权限
        }

        // 🔽 递归映射方法
        private static MenuDataItem MapToDataItem(AppMenu item, HashSet<string> userPermissions)
        {
            var menuItem = new MenuDataItem
            {
                Path = item.Path,
                Name = item.Name,
                Key = item.Key,
                Icon = item.Icon,
                HideChildrenInMenu = item.HideChildrenInMenu,
                HideInMenu = item.HideInMenu
            };

            if (item.Children?.Count > 0)
            {
                menuItem.Children = CreateMenu(item.Children, userPermissions);
            }

            return menuItem;
        }

        // 🔽 新增方法：通过菜单ID获取所有上级节点ID（包括自身）
        public static List<int> GetParentIds(List<AppMenu> allMenus, int menuId)
        {
            var result = new List<int>();
            // 使用字典加速ID查找 (O(1)时间复杂度)
            var menuDict = allMenus.ToDictionary(m => m.Id);

            // 循环向上查找父节点
            int? currentId = menuId;
            while (currentId.HasValue && menuDict.TryGetValue(currentId.Value, out var currentMenu))
            {
                result.Add(currentId.Value);  // 添加当前节点ID
                currentId = currentMenu.ParentId;  // 移动到父节点

                // 防止循环引用导致死循环
                if (result.Count > allMenus.Count) break;
            }
            return result;
        }

        public static List<string> GetParentIds(List<AppMenu> allMenus, string menuId)
        {
            var result = new List<string>();
            // 使用字典加速ID查找 (O(1)时间复杂度)
            var menuDict = allMenus.ToDictionary(m => m.Id);

            // 循环向上查找父节点
            int? currentId = int.Parse(menuId);
            while (currentId.HasValue && menuDict.TryGetValue(currentId.Value, out var currentMenu))
            {
                result.Add(currentId.Value.ToString());  // 添加当前节点ID
                currentId = currentMenu.ParentId;  // 移动到父节点

                // 防止循环引用导致死循环
                if (result.Count > allMenus.Count) break;
            }
            return result;
        }

        public static List<string> GetParentKeys(List<AppMenu> allMenus, string menuId)
        {
            var result = new List<string>();
            // 使用字典加速ID查找 (O(1)时间复杂度)
            var menuDict = allMenus.ToDictionary(m => m.Id);

            // 循环向上查找父节点
            int? currentId = int.Parse(menuId);
            while (currentId.HasValue && menuDict.TryGetValue(currentId.Value, out var currentMenu))
            {
                result.Add(currentId.Value.ToString());  // 添加当前节点ID
                currentId = currentMenu.ParentId;  // 移动到父节点

                // 防止循环引用导致死循环
                if (result.Count > allMenus.Count) break;
            }


            return result;
        }

        public static List<string> GetParentPermissionKeys(List<AppMenu> allMenus, string menuId)
        {
            // 1️⃣ 创建字典加速查找 (O(1) 时间复杂度)
            var menuDict = allMenus.ToDictionary(m => m.Id);
            var permissionKeys = new List<string>();

            // 2️⃣ 安全转换ID
            if (!int.TryParse(menuId, out int targetId))
            {
                // 记录日志或抛出异常
                Console.Error.WriteLine($"⚠️ 无效的菜单ID格式: {menuId}");
                return permissionKeys;
            }

            // 3️⃣ 循环向上查找父节点
            int? currentId = targetId;
            int loopCount = 0;
            int maxLoops = allMenus.Count + 1; // 防止循环引用

            while (currentId.HasValue && loopCount < maxLoops)
            {
                if (menuDict.TryGetValue(currentId.Value, out var currentMenu))
                {
                    // 4️⃣ 添加当前节点的PermissionKey
                    permissionKeys.Add(currentMenu.PermissionKey);

                    // 5️⃣ 移动到父节点
                    currentId = currentMenu.ParentId;
                }
                else
                {
                    // 找不到节点时中断循环
                    break;
                }

                loopCount++;
            }

            // 6️⃣ 返回从当前节点到根节点的权限键列表
            return permissionKeys;
        }




        // 🔽 优化版：获取从根节点到当前节点的路径ID（按层级顺序）
        public static List<int> GetParentIdsOrdered(List<AppMenu> allMenus, int menuId)
        {
            var path = new Stack<int>();  // 使用栈保证层级顺序
            var menuDict = allMenus.ToDictionary(m => m.Id);

            int? currentId = menuId;
            while (currentId.HasValue && menuDict.TryGetValue(currentId.Value, out var currentMenu))
            {
                path.Push(currentId.Value);
                currentId = currentMenu.ParentId;
                if (path.Count > allMenus.Count) break;
            }
            return path.ToList();  // 返回从根到当前节点的ID路径
        }

    }

    /// <summary>
    /// 业务系统缩写/模块信息实体类
    /// 用于存储各类业务系统的名称、简写、英文全称、说明等字典数据
    /// 适配页面下拉选择、数据查询关联、模块标识展示等场景
    /// </summary>
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
        public string? Role { set; get; }                     //设置该菜单所属权限  * 表示全部
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;

        public static MenuDataItem[] CreateMenu(List<MenuList> menuData)
        {
            MenuDataItem[] menuDataRes = new MenuDataItem[menuData.Count];
            int i = 0;
            foreach (var item in menuData)
            {
                MenuDataItem menuItem = new MenuDataItem
                {
                    Path = item.Path,
                    Name = item.Name,
                    Key = item.Key,
                    Icon = item.Icon,
                    // ParentKeys = item.ParentKeys
                };
                if (item.Children != null && item.Children.Count > 0)
                {
                    menuItem.Children = new MenuDataItem[] { };
                    // CreateMenu(item.Children);
                    menuItem.Children = CreateMenu(item.Children);
                }
                if (item.HideChildrenInMenu)
                {
                    menuItem.HideChildrenInMenu = item.HideChildrenInMenu;
                }
                if (item.HideInMenu)
                {
                    menuItem.HideInMenu = item.HideInMenu;
                }

                menuDataRes[i] = menuItem;
                i++;
            }
            return menuDataRes;
        }


        // 🔽 新增方法：通过菜单ID获取所有上级节点ID（包括自身）
        public static List<int> GetParentIds(List<AppMenu> allMenus, int menuId)
        {
            var result = new List<int>();
            // 使用字典加速ID查找 (O(1)时间复杂度)
            var menuDict = allMenus.ToDictionary(m => m.Id);

            // 循环向上查找父节点
            int? currentId = menuId;
            while (currentId.HasValue && menuDict.TryGetValue(currentId.Value, out var currentMenu))
            {
                result.Add(currentId.Value);  // 添加当前节点ID
                currentId = currentMenu.ParentId;  // 移动到父节点

                // 防止循环引用导致死循环
                if (result.Count > allMenus.Count) break;
            }
            return result;
        }

        // 🔽 优化版：获取从根节点到当前节点的路径ID（按层级顺序）
        public static List<int> GetParentIdsOrdered(List<AppMenu> allMenus, int menuId)
        {
            var path = new Stack<int>();  // 使用栈保证层级顺序
            var menuDict = allMenus.ToDictionary(m => m.Id);

            int? currentId = menuId;
            while (currentId.HasValue && menuDict.TryGetValue(currentId.Value, out var currentMenu))
            {
                path.Push(currentId.Value);
                currentId = currentMenu.ParentId;
                if (path.Count > allMenus.Count) break;
            }
            return path.ToList();  // 返回从根到当前节点的ID路径
        }

    }

    //public interface IAuthenticationService
    //{
    //    Task LoginAsync(User user);
    //    Task LogoutAsync();
    //}

    //public class CookieAuthenticationService : IAuthenticationService
    //{
    //    private readonly IHttpContextAccessor _httpContextAccessor;

    //    public CookieAuthenticationService(IHttpContextAccessor httpContextAccessor)
    //    {
    //        _httpContextAccessor = httpContextAccessor;
    //    }

    //    public async Task LoginAsync(User user)
    //    {
    //        // 实现上面的LoginAsync方法
    //    }

    //    public async Task LogoutAsync()
    //    {
    //        // 实现上面的LogoutAsync方法
    //    }
    //}
    // ==================== 模拟数据生成器 ====================
    public class MockDataGenerator
    {
        public static void Initialize(SqlSugarClient db)
        {
            // 清空所有表（仅测试环境）
            db.DbMaintenance.TruncateTable<User>();
            db.DbMaintenance.TruncateTable<Role>();
            db.DbMaintenance.TruncateTable<UserRole>();
            db.DbMaintenance.TruncateTable<Permission>();
            db.DbMaintenance.TruncateTable<RolePermission>();

            // 1. 创建权限
            var permissions = new List<Permission>
            {
                // 基础访问权限
                new Permission { PermissionKey = "menu:view:home" },
                new Permission { PermissionKey = "menu:view:purchase" },
                new Permission { PermissionKey = "menu:view:inventory" },
                new Permission { PermissionKey = "menu:view:setting" },
            
                // 采购模块权限
                new Permission { PermissionKey = "purchase:create" },
                new Permission { PermissionKey = "purchase:view" },
                new Permission { PermissionKey = "purchase:approve" },
                new Permission { PermissionKey = "purchase:order:view" },
                new Permission { PermissionKey = "inventory:receive:view" },
            
                // 库存模块权限
                new Permission { PermissionKey = "inventory:manage" },
                new Permission { PermissionKey = "inventory:view" },
                new Permission { PermissionKey = "inventory:balance:view" },
                new Permission { PermissionKey = "inventory:stocktake" },
            
                // 系统权限
                new Permission { PermissionKey = "system:initialize" },
                new Permission { PermissionKey = "system:dept:manage" },
                new Permission { PermissionKey = "system:config" },
                new Permission { PermissionKey = "inventory:category:manage" },
                new Permission { PermissionKey = "inventory:item:manage" },
                new Permission { PermissionKey = "inventory:warehouse:manage" }
            };
            db.Insertable(permissions).ExecuteReturnIdentity();

            // 2. 创建角色
            var roles = new List<Role>
            {
                new Role { Name = "系统管理员" },
                new Role { Name = "采购主管" },
                new Role { Name = "库存经理" },
                new Role { Name = "普通员工" },
                new Role { Name = "财务专员" }
            };
            db.Insertable(roles).ExecuteReturnIdentity();

            // 3. 创建用户
            var users = new List<User>
            {
                new User { Name = "admin" },
                new User { Name = "purchase_manager" },
                new User { Name = "inventory_supervisor" },
                new User { Name = "staff_zhang" },
                new User { Name = "finance_li" }
            };
            db.Insertable(users).ExecuteReturnIdentity();

            // 4. 角色-权限关联
            var rolePermissions = new List<RolePermission>();

            // 系统管理员 - 所有权限
            foreach (var perm in permissions)
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = roles[0].Id,
                    PermissionId = perm.Id
                });
            }

            // 采购主管
            var purchasePerms = permissions.Where(p =>
                p.PermissionKey.StartsWith("menu:view") ||
                p.PermissionKey.StartsWith("purchase:") ||
                p.PermissionKey == "inventory:receive:view"
            );
            foreach (var perm in purchasePerms)
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = roles[1].Id,
                    PermissionId = perm.Id
                });
            }

            // 库存经理
            var inventoryPerms = permissions.Where(p =>
                p.PermissionKey.StartsWith("menu:view") ||
                p.PermissionKey.StartsWith("inventory:") ||
                p.PermissionKey == "system:config"
            );
            foreach (var perm in inventoryPerms)
            {
                rolePermissions.Add(new RolePermission
                {
                    RoleId = roles[2].Id,
                    PermissionId = perm.Id
                });
            }

            // 普通员工 - 基础权限
            rolePermissions.AddRange(new[]
            {
            new RolePermission { RoleId = roles[3].Id, PermissionId = permissions[0].Id }, // home
            new RolePermission { RoleId = roles[3].Id, PermissionId = permissions[1].Id }, // purchase view
            new RolePermission { RoleId = roles[3].Id, PermissionId = permissions[5].Id }  // purchase view
        });

            // 财务专员
            rolePermissions.AddRange(new[]
            {
            new RolePermission { RoleId = roles[4].Id, PermissionId = permissions[0].Id },  // home
            new RolePermission { RoleId = roles[4].Id, PermissionId = permissions[1].Id },  // purchase view
            new RolePermission { RoleId = roles[4].Id, PermissionId = permissions[5].Id },  // purchase view
            new RolePermission { RoleId = roles[4].Id, PermissionId = permissions[7].Id },  // order view
            new RolePermission { RoleId = roles[4].Id, PermissionId = permissions[10].Id }  // inventory view
        });

            db.Insertable(rolePermissions).ExecuteCommand();

            // 5. 用户-角色关联
            var userRoles = new List<UserRole>
        {
            // 管理员拥有所有角色
            new UserRole { UserId = users[0].Id, RoleId = roles[0].Id },
            new UserRole { UserId = users[0].Id, RoleId = roles[1].Id },
            new UserRole { UserId = users[0].Id, RoleId = roles[2].Id },
            
            // 采购主管
            new UserRole { UserId = users[1].Id, RoleId = roles[1].Id },
            
            // 库存经理
            new UserRole { UserId = users[2].Id, RoleId = roles[2].Id },
            
            // 普通员工
            new UserRole { UserId = users[3].Id, RoleId = roles[3].Id },
            
            // 财务专员
            new UserRole { UserId = users[4].Id, RoleId = roles[4].Id }
        };
            db.Insertable(userRoles).ExecuteCommand();
        }
    }

    public class UserLog
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public DateTime ActionTime { get; set; }
        public string ActionType { get; set; }
        public string ActionDescription { get; set; }
        public string Operator { get; set; }
        public string IpAddress { get; set; }
        public string Details { get; set; } // 可存储JSON格式的详细信息
    }
}
