using System.Linq.Expressions;

namespace ERP_Web.Core.Utilities
{
    public static class Sorter<T>
    {
        private static readonly Dictionary<string, Func<T, object>> Cache = new();

        public static void Sort(T[] array, string propertyName)
        {
            if (!Cache.TryGetValue(propertyName, out var keySelector))
            {
                var param = Expression.Parameter(typeof(T));
                var property = Expression.Property(param, propertyName);
                var conversion = Expression.Convert(property, typeof(object));

                keySelector = Expression.Lambda<Func<T, object>>(conversion, param).Compile();
                Cache[propertyName] = keySelector;
            }

            Array.Sort(array, (x, y) =>
                Comparer<object>.Default.Compare(keySelector(x), keySelector(y)));
        }
    }
    // 使用
    //Sorter<User>.Sort(users, "JoinDate");
}
