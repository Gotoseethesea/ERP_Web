using ERP_Web.Repository;
using ERP_Web.Models;
using ERP_Web.Models.PrivilegeHub;
using SqlSugar;

namespace ERP_Web.Core.Helpers
{
    public class ApprovalService
    {
        // 获取当前节点的审批人
        public static User GetApproverUser(ApprovalNode node)
        {
            // 1. 优先使用直接指定的审批人编码
            if (!string.IsNullOrEmpty(node.ApproverCode))
            {
                var directUser = GetActiveUsers()
                    .FirstOrDefault(u => u.Code == node.ApproverCode && u.Active);

                if (directUser != null)
                    return directUser;
            }

            // 2. 按审批角色匹配
            if (node.ApprovalRoles != null && node.ApprovalRoles.Length > 0)
            {
                // 获取匹配所有指定角色的用户
                var roleUsers = GetActiveUsers()
                    .Where(u => u.Approval != null &&
                               u.Approval.Any() &&
                               node.ApprovalRoles.Any(role => u.Approval.Contains(role)))
                    .ToList();

                if (roleUsers.Count > 0)
                {
                    // 按优先级返回
                    return roleUsers
                        .OrderBy(u => u.Sequence ?? int.MaxValue) // 未设置优先级的放最后
                        .First();
                }
            }

            // 3. 双重保障：返回默认审批人（如管理员）
            return GetFallbackApprover();
        }

        // 获取所有活跃用户（带缓存）
        private static List<User> _activeUsersCache;
        private static DateTime _cacheTime = DateTime.MinValue;

        private static List<User> GetActiveUsers()
        {
            if (_activeUsersCache == null || (DateTime.Now - _cacheTime).TotalMinutes > 30)
            {
                SqlClient db = new SqlClient();
                _activeUsersCache = db.Db.Queryable<User>()
                    .Where(u => u.Active)
                    .Includes(u => u.Employee) // 关联员工信息
                    .ToList();
                _cacheTime = DateTime.Now;
            }
            return _activeUsersCache;
        }

        // 备用审批人策略
        private static User GetFallbackApprover()
        {
            return GetActiveUsers()
                .FirstOrDefault(u => u.FeatureRole?.Contains("Administrator") == true)
                ?? new User
                {
                    Code = "SYS_ADMIN",
                    Name = "系统管理员",
                    Employee = new Employee { Name = "默认审批人" }
                };
        }
    }

    // 审批节点类增强
    public class ApprovalNode
    {
        public string ApproverCode { get; set; }   // 直接指定的审批人编码

        [SugarColumn(IsIgnore = true)]  // 不保存到数据库
        public string[] ApprovalRoles { get; set; }  // 需要匹配的审批角色

        // 使用枚举设置审批角色
        public void SetApprovalRoles(params ApprovalRoleType[] roles)
        {
            ApprovalRoles = roles?.Select(r => r.ToString()).ToArray();
        }
    }

    // 审批角色类型枚举
    public enum ApprovalRoleType
    {
        DepartmentManager,  // 部门经理
        FinanceManager,     // 财务经理
        PurchaseManager,    // 采购主管
        InventoryManager,   // 库存主管
        SystemAdministrator // 系统管理员
    }

}
