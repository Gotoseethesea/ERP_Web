using ERP_Web.Models;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;

namespace ERP_Web.Core.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举的Description特性标注的中文描述
        /// </summary>
        /// <param name="value">枚举值</param>
        /// <returns>中文描述，无Description则返回枚举名称</returns>
        public static string GetDescription(this Enum value)
        {
            if (value == null) return string.Empty;

            // 获取枚举字段
            FieldInfo field = value.GetType().GetField(value.ToString());
            if (field == null) return value.ToString();

            // 查找Description特性
            DescriptionAttribute[] attributes =
                (DescriptionAttribute[])field.GetCustomAttributes(typeof(DescriptionAttribute), false);

            // 有描述返回描述，否则返回枚举名
            return attributes.Length > 0 ? attributes[0].Description : value.ToString();
        }
    }


    public class EmployeeComparer : IEqualityComparer<Employee>
    {
        public bool Equals(Employee? x, Employee? y)
        {
            if (x == null || y == null) return false;
            return x.Code == y.Code; // 按主键比较
        }

        public int GetHashCode(Employee obj)
        {
            return obj.Code?.GetHashCode() ?? 0;
        }
    }

    public static class TreeExtensions
    {
        /// <summary>
        /// 把平面对象列表转换为树形结构
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="list">平面数据列表</param>
        /// <param name="childrenSelector">子节点集合选择器</param>
        /// <param name="parentIdSelector">父ID字段选择器</param>
        /// <param name="rootParentId">根节点的父ID值</param>
        /// <param name="idSelector">ID字段选择器</param>
        /// <returns>树形结构列表</returns>
        public static List<T> ToTree<T>(
            this List<T> list,
            Expression<Func<T, List<T>>> childrenSelector,
            Expression<Func<T, object>> parentIdSelector,
            object rootParentId,
            Expression<Func<T, object>> idSelector)
        {
            if (list == null || !list.Any()) return new List<T>();

            // 编译表达式获取属性值
            var getParentId = parentIdSelector.Compile();
            var getId = idSelector.Compile();
            var getChildren = childrenSelector.Compile();

            // 先转成字典加快查询
            var dict = list.ToDictionary(getId);
            var tree = new List<T>();

            foreach (var item in list)
            {
                var parentId = getParentId(item);
                // 根节点判断：父ID等于rootParentId，或者不存在父节点
                if (Equals(parentId, rootParentId) || !dict.ContainsKey(parentId))
                {
                    tree.Add(item);
                }
                else
                {
                    // 把当前节点加入父节点的子集合
                    if (dict.TryGetValue(parentId, out var parent))
                    {
                        var children = getChildren(parent);
                        children ??= new List<T>();
                        children.Add(item);
                        // 把赋值回父节点（如果属性是可写的）
                        var member = (childrenSelector.Body as MemberExpression)?.Member;
                        if (member != null)
                        {
                            member.ReflectedType.GetProperty(member.Name)?.SetValue(parent, children);
                        }
                    }
                }
            }

            return tree;
        }
    }
}
