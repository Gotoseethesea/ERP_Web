using SqlSugar;

namespace ERP_Web.Models.PrivilegeHub
{
    // ==================== 页面实体 ====================
    public class SysPage
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(Length = 100, IsNullable = false)]
        public string RouteName { get; set; } // 路由名称（唯一标识）如："PrView"

        [SugarColumn(Length = 100, IsNullable = false)]
        public string DisplayName { get; set; } // 显示名称

        [SugarColumn(IsNullable = true)]
        public string Description { get; set; }
    }

    // ==================== 功能按钮实体 ====================
    public class SysButton
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(IsNullable = false)]
        public int PageId { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(PageId))]
        public SysPage Page { get; set; }

        [SugarColumn(Length = 50, IsNullable = false)]
        public string ButtonCode { get; set; } // 按钮唯一标识 如："PrView_Refresh"

        [SugarColumn(Length = 50, IsNullable = false)]
        public string DisplayText { get; set; } // 显示文本

        [SugarColumn(Length = 20, IsNullable = false)]
        public string ButtonType { get; set; } // Primary/Danger/Dashed等

        [SugarColumn(Length = 50, IsNullable = true)]
        public string IconType { get; set; } // 图标类型

        public int OrderIndex { get; set; } // 显示顺序

        [SugarColumn(Length = 100, IsNullable = false)]
        public string ActionName { get; set; } // 执行的方法名

        [SugarColumn(Length = 100, IsNullable = false)]
        public string PermissionKey { get; set; } // 关联的权限键

        [SugarColumn(IsNullable = false, DefaultValue = "true")]
        public bool IsDisabled { get; set; } = false;
    }

    // ==================== 扩展角色权限关联 ====================
    // 修改原有 RolePermission 实体，增加按钮权限关联
    //[SugarTable("RolePermissions")]
    //public class RolePermission
    //{
    //    [SugarColumn(IsPrimaryKey = true)]
    //    public int RoleId { get; set; }

    //    [Navigate(NavigateType.OneToOne, nameof(RoleId))]
    //    public Role Role { get; set; }

    //    [SugarColumn(IsPrimaryKey = true)]
    //    public int PermissionId { get; set; }

    //    [Navigate(NavigateType.OneToOne, nameof(PermissionId))]
    //    public Permission Permission { get; set; }

    //    // 新增按钮权限关联
    //    [SugarColumn(IsNullable = true)]
    //    public int? ButtonId { get; set; }

    //    [Navigate(NavigateType.OneToOne, nameof(ButtonId))]
    //    public SysButton Button { get; set; }
    //}
}
