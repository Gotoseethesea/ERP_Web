// UserService.cs
using Org.BouncyCastle.Crypto.Generators;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ERP_Web.Models.PrivilegeHub;

namespace ERP_Web.Core.Service
{
    public interface IUserService
    {
        Task<List<User>> GetAllUsersAsync();
        Task<User> GetUserByIdAsync(long id);
        Task CreateUserAsync(User user);
        Task UpdateUserAsync(User user);
        Task ToggleUserStatusAsync(long userId, bool isActive);
        Task<List<Role>> GetUserRolesAsync(long userId);
        Task UpdateUserRolesAsync(long userId, List<int> roleIds);
    }

    public class UserService : IUserService
    {
        private readonly ISqlSugarClient _db;

        public UserService(ISqlSugarClient sqlSugarClient)
        {
            _db = sqlSugarClient;
        }

        /// <summary>
        /// 获取所有用户（包含角色信息）
        /// </summary>
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _db.Queryable<User>()
                .IncludesAllFirstLayer()
                .IncludesAllSecondLayer(x => x.Roles)               
                //.Includes(u => u.Roles).inroles => roles.Includes(r => r.Permissions)) // 嵌套加载角色权限
                .ToListAsync();
        }

        /// <summary>
        /// 根据ID获取用户详情
        /// </summary>
        public async Task<User> GetUserByIdAsync(long id)
        {
            return await _db.Queryable<User>()
                .Where(u => u.Id == id)
                .Includes(u => u.Roles)
                .FirstAsync();
        }

        /// <summary>
        /// 创建新用户（带初始密码）
        /// </summary>
        public async Task CreateUserAsync(User user)
        {
            // 基础校验
            if (string.IsNullOrWhiteSpace(user.Account))
                throw new ArgumentException("账号不能为空");

            if (await _db.Queryable<User>().AnyAsync(u => u.Account == user.Account))
                throw new InvalidOperationException("账号已存在");

            // 设置初始密码
            user.Password = "123456"; //BCrypt.Net.BCrypt.HashPassword("123456"); // 默认密码
            //user.InsertTime = DateTime.Now;

            await _db.Insertable(user).ExecuteCommandAsync();
        }

        /// <summary>
        /// 更新用户信息（不处理密码）
        /// </summary>
        public async Task UpdateUserAsync(User user)
        {
            var existing = await GetUserByIdAsync(user.Id);
            if (existing == null)
                throw new KeyNotFoundException("用户不存在");

            // 仅允许更新非敏感字段
            existing.Name = user.Name;
            //existing.Email = user.Email;
            existing.Phone = user.Phone;
            //existing.DepartmentId = user.DepartmentId;
            existing.Active = user.Active;

            await _db.Updateable(existing).ExecuteCommandAsync();
        }

        /// <summary>
        /// 切换用户启用状态
        /// </summary>
        public async Task ToggleUserStatusAsync(long userId, bool isActive)
        {
            await _db.Updateable<User>()
                .SetColumns(u => u.Active == isActive)
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 获取用户关联角色
        /// </summary>
        public async Task<List<Role>> GetUserRolesAsync(long userId)
        {
            return await _db.Queryable<UserRole>()
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.Role)
                .ToListAsync();
        }

        /// <summary>
        /// 更新用户角色（原子操作）
        /// </summary>
        public async Task UpdateUserRolesAsync(long userId, List<int> roleIds)
        {
            await _db.Ado.UseTranAsync(async () =>
            {
                // 1. 清空现有角色
                await _db.Deleteable<UserRole>()
                    .Where(ur => ur.UserId == userId)
                    .ExecuteCommandAsync();

                // 2. 添加新角色
                if (roleIds?.Count > 0)
                {
                    var newRoles = roleIds.Select(roleId => new UserRole
                    {
                        UserId = userId,
                        RoleId = roleId,
                        //InsertTime = DateTime.Now
                    }).ToList();

                    await _db.Insertable(newRoles).ExecuteCommandAsync();
                }
            });
        }

        // 可选：密码管理方法
        public async Task ResetPasswordAsync(long userId, string newPassword)
        {
            //var hash = BCrypt.PasswordToByteArray(newPassword);
            await _db.Updateable<User>()
                .SetColumns(u => u.Password == newPassword)
                .Where(u => u.Id == userId)
                .ExecuteCommandAsync();
        }
    }
}
