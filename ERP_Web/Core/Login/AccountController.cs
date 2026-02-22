using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using M = ERP_Web.Models;

namespace ERP_Web.Core.Login
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private readonly IWebHostEnvironment _env;

        public AccountController(IWebHostEnvironment env)
        {
            _env = env;
        }
        [HttpPost("login")]
        //[Route("/api/[controller]/[action]")]
        [Route("/loginApi")]
        public async Task<IActionResult> Login([FromForm] string name, [FromForm] string password)
        {
            // 验证用户
            //string name = "admin", password = "1";
            var user = M.PrivilegeHub.User.Login(name, password);
            if (user == null)
            {
                return Unauthorized();
            }
            // 创建Claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Name),
                new Claim(ClaimTypes.NameIdentifier, user.Account), // 必须
                new Claim("Account", user.Account),
                new Claim(ClaimTypes.NameIdentifier, user.Account)
            };

            // 添加角色
            var roles = user.Roles.Select(r => r.Name).ToArray();
            if (roles != null)
            {
                claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
            }

            // 创建ClaimsIdentity
            var identity = new ClaimsIdentity(
                claims,
                CookieAuthenticationDefaults.AuthenticationScheme,
                ClaimTypes.Name,
                ClaimTypes.Role
            );
            var principal = new ClaimsPrincipal(identity);
            // 登录并写入Cookie - 显式设置 CookieOptions
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTime.UtcNow.AddDays(30),
                    AllowRefresh = true
                });            
            return Ok();
        }
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // 退出登录并清除Cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            Console.WriteLine("已退出登录");
            return Ok(new { message = "已成功退出" });
        }
    }

    [ApiController]
    [Route("/api/[controller]/[action]")]
    public class AuthTestController : ControllerBase
    {
        [HttpGet]
        [Authorize] // 需要认证
        public IActionResult CheckAuth()
        {
            return Ok(new
            {
                User.Identity?.Name,
                User.Identity?.IsAuthenticated,
                Claims = User.Claims.Select(c => new { c.Type, c.Value })
            });
        }
    }

    [ApiController]
    ///api/Common/GetCategory
    [Route("/api/[controller]/[action]")]
    public class CommonController : Controller
    {
        [HttpGet]
        public IActionResult GetCategory()
        {
            return Json(new { code = 0, msg = "success", data = "hello 牛腩" });
        }
        public IActionResult GetCategory2()
        {
            return Json(new { code = 0, msg = "success", data = "hello 牛腩" });
        }
    }



    // 记录登录活动
    public class LoginActivityLogger
    {
        private readonly ILogger<LoginActivityLogger> _logger;

        public LoginActivityLogger(ILogger<LoginActivityLogger> logger)
        {
            _logger = logger;
        }

        public void LogLoginSuccess(string userId, string ipAddress)
        {
            _logger.LogInformation("用户 {UserId} 从 {IP} 登录成功", userId, ipAddress);
        }

        public void LogLoginFailure(string username, string ipAddress)
        {
            _logger.LogWarning("用户 {Username} 从 {IP} 登录失败", username, ipAddress);
        }

        // 在登录方法中使用
        //if (loginSuccess)
        //{
        //    _loginLogger.LogLoginSuccess(user.Id, HttpContext.Connection.RemoteIpAddress?.ToString());
        //}
        //else
        //{
        //    _loginLogger.LogLoginFailure(model.Username, HttpContext.Connection.RemoteIpAddress?.ToString());
        //}
    }

    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        [HttpGet("current")]
        public IActionResult GetCurrentUser()
        {
            // 直接返回当前用户的身份信息
            if (User.Identity.IsAuthenticated)
            {
                return Ok(new
                {
                    User.Identity.Name,
                    Claims = User.Claims.Select(c => new { c.Type, c.Value })
                });
            }
            return Unauthorized();
        }
    }
}
