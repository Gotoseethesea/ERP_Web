using ERP_Web.Models;
using global::ERP_Web.Models;


namespace ERP_Web.Core.Helpers
{
    public static class Pinyin
    {
        public static string GetInitials(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            // 这里应该是实际的拼音转换逻辑
            // 实际项目中可能会使用 NuGet 包如 NPinyin 或 Microsoft Visual Studio International Pack
        
            // 简化的实现：取每个字的首字母
            var result = new StringBuilder();
            foreach (char c in name)
            {
                // 只处理汉字字符
                if (c >= 0x4E00 && c <= 0x9FA5)
                {
                    // 这里应该有实际的拼音转换逻辑
                    // 简化版：取汉字对应的拼音首字母
                    result.Append(GetPinyinInitial(c));
                }
                else
                {
                    result.Append(c);
                }
            }
        
            return result.ToString().ToUpper();
        }

        // 简化的拼音首字母获取（实际项目中应使用专业库）
        private static char GetPinyinInitial(char c)
        {
            // 这里应该有完整的汉字到拼音首字母的映射
            // 简化实现：只处理几个示例字符
            switch (c)
            {
                case '张': return 'Z';
                case '三': return 'S';
                case '李': return 'L';
                case '四': return 'S';
                case '王': return 'W';
                case '五': return 'W';
                default: return c;
            }
        }
    }
}