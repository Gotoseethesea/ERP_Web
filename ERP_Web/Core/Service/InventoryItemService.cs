using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SqlSugar;
using ERP_Web.Models;

namespace ERP_Web.Core.Service
{
    public static class SpecificationService
    {
        /// <summary>
        /// 根据商品编码获取所有规格
        /// </summary>
        public static async Task<List<Specification>> GetByInvCode(string invCode)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<Specification>()
                .Where(s => s.InvCode == invCode && s.Active == true)
                .Includes(s => s.Options) // 包含关联的选项
                .ToListAsync();
        }

        /// <summary>
        /// 添加新规格
        /// </summary>
        public static async Task Insert(Specification spec, List<int>? optionIds = null)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.InsertNav(spec)
                .Include(s => s.Options, optionIds?.Select(id => new InvAttributeOption { Id = id }))
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 更新规格
        /// </summary>
        public static async Task Update(Specification spec, List<int>? optionIds = null)
        {
            using var sqlClient = new SqlClient();

            // 先删除旧的关联
            await sqlClient.Db.Deleteable<SpecificationAttributeOptionMapping>()
                .Where(m => m.SKU == spec.SKU)
                .ExecuteCommandAsync();

            // 更新规格并添加新关联
            await sqlClient.Db.UpdateNav(spec)
                .Include(s => s.Options, optionIds?.Select(id => new InvAttributeOption { Id = id }))
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 设置默认规格
        /// </summary>
        public static async Task SetAsDefault(long specId, string invCode)
        {
            using var sqlClient = new SqlClient();

            // 重置所有默认规格
            await sqlClient.Db.Updateable<Specification>()
                .SetColumns(s => s.IsDefault == false)
                .Where(s => s.InvCode == invCode)
                .ExecuteCommandAsync();

            // 设置当前规格为默认
            await sqlClient.Db.Updateable<Specification>()
                .SetColumns(s => s.IsDefault == true)
                .Where(s => s.Id == specId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除规格（软删除）
        /// </summary>
        public static async Task Delete(long specId)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable<Specification>()
                .SetColumns(s => s.Active == false)
                .Where(s => s.Id == specId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据ID获取规格
        /// </summary>
        public static async Task<Specification?> GetById(long specId)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<Specification>()
                .Where(s => s.Id == specId)
                .Includes(s => s.Options)
                .FirstAsync();
        }
    }

    public static class AttributeOptionService
    {
        /// <summary>
        /// 根据属性ID获取所有选项
        /// </summary>
        public static async Task<List<InvAttributeOption>> GetByAttributeId(int attributeId)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<InvAttributeOption>()
                .Where(o => o.AttributeId == attributeId && o.Active == true)
                .ToListAsync();
        }

        /// <summary>
        /// 添加新选项
        /// </summary>
        public static async Task Insert(InvAttributeOption option)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.Insertable(option).ExecuteCommandAsync();
        }

        /// <summary>
        /// 更新选项
        /// </summary>
        public static async Task Update(InvAttributeOption option)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable(option).ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除选项（软删除）
        /// </summary>
        public static async Task Delete(int optionId)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable<InvAttributeOption>()
                .SetColumns(o => o.Active == false)
                .Where(o => o.Id == optionId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据ID获取选项
        /// </summary>
        public static async Task<InvAttributeOption?> GetById(int optionId)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<InvAttributeOption>()
                .Where(o => o.Id == optionId)
                .FirstAsync();
        }
    }

    public static class InventoryItemService
    {
        /// <summary>
        /// 获取所有存货档案（树形结构）
        /// </summary>
        public static async Task<List<InventoryItemView>> GetAll()
        {
            using var sqlClient = new SqlClient();

            // 获取所有存货
            var items = await sqlClient.Db.Queryable<InventoryItem>()
                .Where(i => i.Active == true)
                .Includes(
                    i => i.Specifications, // 包含规格
                    i => i.Category,       // 包含分类
                    i => i.Attributes,      // 包含属性
                    i => i.Attributes.First().Options // 包含属性选项
                )
                .ToListAsync();

            // 转换为树形视图
            return items.Select(item => item.InventoryItemListByInventoryItem()).ToList();
        }

        /// <summary>
        /// 添加新存货
        /// </summary>
        public static async Task Insert(InventoryItem item)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.InsertNav(item).IncludesAllFirstLayer().ExecuteCommandAsync();
        }

        /// <summary>
        /// 更新存货
        /// </summary>
        public static async Task Update(InventoryItem item)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.UpdateNav(item).IncludesAllFirstLayer().ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除存货（软删除）
        /// </summary>
        public static async Task Delete(string code)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable<InventoryItem>()
                .SetColumns(i => i.Active == false)
                .Where(i => i.Code == code)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据编码获取存货
        /// </summary>
        public static async Task<InventoryItem?> GetByCode(string code)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<InventoryItem>()
                .Where(i => i.Code == code)
                .Includes(
                    i => i.Specifications,
                    i => i.Category,
                    i => i.Attributes,
                    i => i.Attributes.First().Options
                )
                .FirstAsync();
        }
    }

    public static class AttributeService
    {
        /// <summary>
        /// 根据商品编码获取所有属性
        /// </summary>
        public static async Task<List<InvAttribute>> GetByInvCode(string invCode)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<InvAttribute>()
                .Where(a => a.InvCode == invCode && a.Active == true)
                .Includes(a => a.Options) // 包含选项
                .ToListAsync();
        }

        /// <summary>
        /// 添加新属性
        /// </summary>
        public static async Task Insert(InvAttribute attribute)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.InsertNav(attribute)
                .Include(a => a.Options)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 更新属性
        /// </summary>
        public static async Task Update(InvAttribute attribute)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.UpdateNav(attribute)
                .Include(a => a.Options)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 删除属性（软删除）
        /// </summary>
        public static async Task Delete(int attributeId)
        {
            using var sqlClient = new SqlClient();
            await sqlClient.Db.Updateable<InvAttribute>()
                .SetColumns(a => a.Active == false)
                .Where(a => a.Id == attributeId)
                .ExecuteCommandAsync();
        }

        /// <summary>
        /// 根据ID获取属性
        /// </summary>
        public static async Task<InvAttribute?> GetById(int attributeId)
        {
            using var sqlClient = new SqlClient();
            return await sqlClient.Db.Queryable<InvAttribute>()
                .Where(a => a.Id == attributeId)
                .Includes(a => a.Options)
                .FirstAsync();
        }
    }
}