using SqlSugar;

namespace ERP_Web.Models
{
    /// <summary>
    /// 商品图片
    /// </summary>
    public class IcInvImage
    {
        /// <summary>
        /// 主键自增
        /// </summary>
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int ImageId { set; get; }
        /// <summary>
        /// 商品ID
        /// </summary>
        public int ProductId { set; get; }
        /// <summary>
        /// 图片名称 
        /// </summary>
        public string? Title { set; get; }
    }
}
