namespace ERP_Web.Components.Custom
{
    using ERP_Web.Models.PrivilegeHub;
    using Microsoft.AspNetCore.Components.Authorization;
    using System.Security.Claims;

    public class FakeAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal _anonymous = new(new ClaimsIdentity());
        private ClaimsPrincipal _current;

        public FakeAuthenticationStateProvider()
        {
            _current = _anonymous;
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            return Task.FromResult(new AuthenticationState(_current));
        }

        // 标记为已登录，roles 可以为 null 或数组
        public void MarkUserAsAuthenticated(User user)
        {
            //roles = user.FeatureRole;
            var name = user.Name;
            var roles = user.Roles.Select(r => r.Name).ToArray();
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim("Account", user.Account),
                new Claim(ClaimTypes.NameIdentifier, user.Name)

            };
            if (roles != null)
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));

            var identity = new ClaimsIdentity(claims, "fake");
            _current = new ClaimsPrincipal(identity);

            // 通知所有订阅者（UI）认证状态已改变
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
        }

        // 注销
        public void MarkUserAsLoggedOut()
        {
            _current = _anonymous;
            NotifyAuthenticationStateChanged(Task.FromResult(new AuthenticationState(_current)));
        }
    }
}
