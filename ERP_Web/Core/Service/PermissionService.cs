using ERP_Web.Repository;
using ERP_Web.Models.PrivilegeHub;
using SqlSugar;
using System.Runtime.Intrinsics.X86;

namespace ERP_Web.Core.Service
{
    //public class PermissionService
    //{
    //    // ✅ 从数据库获取用户权限
    //    public static HashSet<string> GetUserPermissions(User user)
    //    {
    //        SqlClient SSC = new SqlClient();

    //        //// 1. 获取用户的所有角色ID
    //        //var roleIds = SSC.Db.Queryable<UserRole>()
    //        //    .Where(ur => ur.UserId == user.Id)
    //        //    .Select(ur => ur.RoleId)
    //        //    .ToList();

    //        //// 2. 通过角色ID获取权限Key
    //        //var permissionKeys = SSC.Db.Queryable<RolePermission>()
    //        //    .Where(rp => roleIds.Contains(rp.RoleId)) // 使用内存Contains
    //        //    .Select(rp => rp.Permission.PermissionKey) // 通过导航属性获取
    //        //    .Distinct()
    //        //    .ToList();

    //        var permissionKeys = SSC.Db.Queryable<UserRole, RolePermission, Permission>(
    //                (ur, rp, p) => ur.RoleId == rp.RoleId && rp.PermissionId == p.Id
    //            )
    //            .Where((ur, rp, p) => ur.UserId == user.Id)
    //            .Select((ur, rp, p) => p.PermissionKey)
    //            .Distinct()
    //            .ToList();
    //        return new HashSet<string>(permissionKeys);
    //    }
    //}
    public class PermissionService
    {

        // 获取用户所有权限（包含按钮权限）
        public  HashSet<string> GetUserPermissions(User user)
        {
            SqlClient SC = new();
            var permissionKeys = SC.Db.Queryable<UserRole, RolePermission, Permission>(
                (ur, rp, p) => ur.RoleId == rp.RoleId && rp.PermissionId == p.Id
                )
                .Where((ur, rp, p) => ur.UserId == user.Id)
                .Select((ur, rp, p) => p.PermissionKey)
                .Distinct()
                .ToList();

            return new HashSet<string>(permissionKeys);
        }

        // 获取用户在当前页面的有权限按钮
        public List<SysButton> GetAuthorizedButtons(User user, string pageRoute)
        {
            // 获取页面ID
            SqlClient SC = new();
            var pageId = SC.Db.Queryable<SysPage>()
                .Where(p => p.RouteName == pageRoute)
                .Select(p => p.Id)
                .First();

            if (pageId == 0) return new List<SysButton>();

            // 获取用户所有权限键
            var userPermissions = GetUserPermissions(user);

            // 查询当前页面的所有按钮及其所需权限
            return SC.Db.Queryable<SysButton>()
                .Where(b => b.PageId == pageId && b.IsDisabled == false)
                .ToList()
                .Where(b => userPermissions.Contains(b.PermissionKey))
                .OrderBy(b => b.OrderIndex)
                .ToList();
        }
    }

}
