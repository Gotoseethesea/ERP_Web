using ERP_Web.Models.PrivilegeHub;
using ERP_Web.Repository;
using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace ERP_Web.Core.Service
{
    public class UserContext : IUserContext
    {
        // AsyncLocal实现异步上下文用户隔离，Blazor Server下每个请求/连接独立，不会串用户
        private static readonly AsyncLocal<User?> _currentUser = new();
        private readonly AuthenticationStateProvider _authStateProvider;

        public UserContext(AuthenticationStateProvider authStateProvider)
        {
            _authStateProvider = authStateProvider;
        }

        public User? CurrentUser => _currentUser.Value;

        // 兼容你原有实体类的静态调用方式（如UserContext.CurrentUser）
        public static User? Current => _currentUser.Value;

        public async Task<User?> GetCurrentUserAsync()
        {
            if (_currentUser.Value != null) return _currentUser.Value;

            var authState = await _authStateProvider.GetAuthenticationStateAsync();
            var userClaims = authState.User;
            if (!userClaims.Identity?.IsAuthenticated ?? true) return null;

            var account = userClaims.FindFirst("Account")?.Value;
            if (string.IsNullOrEmpty(account)) return null;

            var user = User.SelectByAccount(account);
            _currentUser.Value = user;
            return user;
        }

        public void SetCurrentUser(User user)
        {
            _currentUser.Value = user;
        }

        public void ClearCurrentUser()
        {
            _currentUser.Value = null;
        }
    }
}
