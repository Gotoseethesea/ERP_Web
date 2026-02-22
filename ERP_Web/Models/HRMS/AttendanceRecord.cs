using ERP_Web.Models.PrivilegeHub;
using SqlSugar;
using System;

namespace ERP_Web.Models
{
    [SugarTable("Hr_AttendanceRecord")]
    public class AttendanceRecord
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 员工ID
        /// </summary>
        public long EmployeeId { get; set; }

        /// <summary>
        /// 考勤日期
        /// </summary>
        public DateTime AttendanceDate { get; set; }

        /// <summary>
        /// 上班打卡时间
        /// </summary>
        public DateTime? CheckInTime { get; set; }

        /// <summary>
        /// 下班打卡时间
        /// </summary>
        public DateTime? CheckOutTime { get; set; }

        /// <summary>
        /// 考勤状态 0=正常 1=迟到 2=早退 3=旷工 4=请假
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 工作时长（小时）
        /// </summary>
        public decimal WorkHours { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string Remark { get; set; }

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(EmployeeId))]
        public Employee Employee { get; set; }
        #endregion

        public string GetStatusDisplayName()
        {
            return Status switch
            {
                0 => "正常",
                1 => "迟到",
                2 => "早退",
                3 => "旷工",
                4 => "请假",
                _ => "未知"
            };
        }
    }

    [SugarTable("Hr_LeaveApplication")]
    public class LeaveApplication
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 申请单号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 申请人ID
        /// </summary>
        public long EmployeeId { get; set; }

        /// <summary>
        /// 请假类型 0=事假 1=病假 2=年假 3=婚假 4=产假 5=丧假
        /// </summary>
        public int LeaveType { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 请假时长（天）
        /// </summary>
        public decimal LeaveDays { get; set; }

        /// <summary>
        /// 请假原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 审批状态 0=待审批 1=已通过 2=已驳回
        /// </summary>
        public int ApprovalStatus { get; set; }

        /// <summary>
        /// 审批人
        /// </summary>
        public string Approver { get; set; }

        /// <summary>
        /// 审批意见
        /// </summary>
        public string ApprovalRemark { get; set; }

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(EmployeeId))]
        public Employee Employee { get; set; }
        #endregion

        public string GetLeaveTypeDisplayName()
        {
            return LeaveType switch
            {
                0 => "事假",
                1 => "病假",
                2 => "年假",
                3 => "婚假",
                4 => "产假",
                5 => "丧假",
                _ => "其他"
            };
        }

        public string GetApprovalStatusDisplayName()
        {
            return ApprovalStatus switch
            {
                0 => "待审批",
                1 => "已通过",
                2 => "已驳回",
                _ => "未知"
            };
        }
    }

    /// <summary>
    /// 请假申请单实体
    /// </summary>
    public class LeaveRequest : BaseDocument
    {
        /// <summary>
        /// 请假人编码
        /// </summary>
        public string LeaveUserCode { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(LeaveUserCode))]
        public User LeaveUser { get; set; }

        /// <summary>
        /// 请假类型：年假/病假/事假等
        /// </summary>
        public string LeaveType { get; set; }

        /// <summary>
        /// 开始时间
        /// </summary>
        public DateTime StartTime { get; set; }

        /// <summary>
        /// 结束时间
        /// </summary>
        public DateTime EndTime { get; set; }

        /// <summary>
        /// 请假天数
        /// </summary>
        public decimal LeaveDays { get; set; }

        /// <summary>
        /// 请假事由
        /// </summary>
        public string Reason { get; set; }

        public LeaveRequest()
        {
            GetCode("LV"); // 单据前缀LV
        }
    }


}
