using ERP_Web.Models.PrivilegeHub;

namespace ERP_Web.Core.Service
{
    public interface IUserContext
    {
        /// <summary>
        /// 当前登录用户（异步上下文隔离，不会串用户）
        /// </summary>
        User? CurrentUser { get; }

        /// <summary>
        /// 异步初始化当前用户信息
        /// </summary>
        Task<User?> GetCurrentUserAsync();

        /// <summary>
        /// 设置当前上下文用户（中间件/布局页调用）
        /// </summary>
        void SetCurrentUser(User user);

        /// <summary>
        /// 清除当前上下文用户
        /// </summary>
        void ClearCurrentUser();
    }
}
