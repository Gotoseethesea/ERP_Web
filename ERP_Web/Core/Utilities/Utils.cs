using SqlSugar;

namespace ERP_Web.Core.Utilities
{
    public static class FilterUtils
    {
        public static ConditionalCollections CreateConditionalCollectionForColumn<T>(
            IEnumerable<T> values,
            string columnName,
            string columnType = "string",
            WhereType outerConditionType = WhereType.And,
            WhereType innerConditionType = WhereType.Or,
            ConditionalType conditionType = ConditionalType.Equal) // 新增条件类型参数，默认为Equal
        {
            // 空值处理
            if (values?.Any() != true)
                return new ConditionalCollections
                {
                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                };

            // 使用LINQ和索引创建条件列表
            var conditionalList = values
                .Where(item => item != null) // 过滤掉null值（特别是针对Nullable类型）
                .Select((item, index) => CreateConditionalItem(
                    item: item,
                    columnName: columnName,
                    columnType: columnType,
                    index: index,
                    outerConditionType: outerConditionType,
                    innerConditionType: innerConditionType,
                    conditionType: conditionType))
                .ToList();
            return new ConditionalCollections { ConditionalList = conditionalList };
        }

        // 辅助方法：创建单个条件项
        private static KeyValuePair<WhereType, ConditionalModel> CreateConditionalItem<T>(
            T item,
            string columnName,
            string columnType,
            int index,
            WhereType outerConditionType,
            WhereType innerConditionType,
            ConditionalType conditionType)
        {
            // 确定条件连接类型（第一个用outer，后续用inner）
            var whereType = index == 0 ? outerConditionType : innerConditionType;

            // 将值转换为字符串（根据类型处理）
            string fieldValue;
            if (item is DateOnly date)
            {
                // 处理DateOnly类型：转换为数据库友好的格式
                fieldValue = date.ToString("yyyy-MM-dd");
            }
            else
            {
                // 其他类型使用ToString()
                fieldValue = item.ToString();
            }

            return new KeyValuePair<WhereType, ConditionalModel>(
                whereType,
                new ConditionalModel
                {
                    ConditionalType = conditionType, // 使用传入的条件类型
                    FieldName = columnName,
                    FieldValue = fieldValue,
                    CSharpTypeName = columnType
                });
        }

        public static ConditionalCollections CreateConditionalCollectionForColumn(
        DateOnly?[] values
        , string columnName = "Date"
        , string columnType = "string"
        , WhereType outerConditionType = WhereType.And
        , WhereType innerConditionType = WhereType.And)
        {
            if (values?.Any() != true || values[0] == null)
                return new ConditionalCollections
                {
                    ConditionalList = new List<KeyValuePair<WhereType, ConditionalModel>>()
                };
            var conKeyValueList = new List<KeyValuePair<WhereType, SqlSugar.ConditionalModel>>();
            int i = 1;
            foreach (var item in values)
            {
                var conditionalType = i == 1 ? outerConditionType : innerConditionType;
                KeyValuePair<WhereType, SqlSugar.ConditionalModel> conKeyValue;
                conKeyValue = new KeyValuePair<WhereType, SqlSugar.ConditionalModel>(
                    i == 1 ? outerConditionType : innerConditionType,
                    new ConditionalModel()
                    {
                        ConditionalType = i == 1 ? ConditionalType.GreaterThanOrEqual : ConditionalType.LessThanOrEqual,
                        FieldName = columnName,
                        FieldValue = item.ToString(),
                        CSharpTypeName = columnType //设置类型 和C#名称一样常用的支持
                    }
                );
                i++;
                conKeyValueList.Add(conKeyValue);
            }
            var conditionalCollections = new ConditionalCollections() { ConditionalList = conKeyValueList };
            // conModels.Add(ConMonDept);
            return conditionalCollections;
        }

    }
}
