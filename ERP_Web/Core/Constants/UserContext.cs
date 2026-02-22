using ERP_Web.Models.PrivilegeHub;
using global::ERP_Web.Models;


namespace ERP_Web.Core.Constants
{
    // 在根目录或Services文件夹下
    public static class UserContext
    {
        public static User CurrentUser { get; set; }

        public static bool IsLoggedIn => CurrentUser != null;

        public static void Logout()
        {
            CurrentUser = null;
        }
    }

    // 新增单据类型编码（和你现有PROFIT/LOSS对齐）
    public static class TrxTypeCode
    {
        // 原有类型
        public const string Profit = "PROFIT"; // 盘盈
        public const string Loss = "LOSS"; // 盘亏
                                           // 新增类型
        public const string DirectInOut = "DIRECT_INOUT"; // 直入直出
        public const string TransferOut = "TRANSFER_OUT"; // 调拨出库
        public const string TransferIn = "TRANSFER_IN"; // 调拨入库
    }

    // 新增TrxType数值标识（和原有1=入库/-1=出库对齐）
    public static class TrxTypeFlag
    {
        public const int In = 1; // 普通入库
        public const int Out = -1; // 普通出库
        public const int Direct = 0; // 直入直出
        public const int TransferOut = -2; // 调拨出库
        public const int TransferIn = 2; // 调拨入库
    }

}
