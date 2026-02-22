using SqlSugar;
using System;

namespace ERP_Web.Models
{
    [SugarTable("Hr_RecruitmentPlan")]
    public class RecruitmentPlan
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 计划编号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 招聘职位
        /// </summary>
        public string Position { get; set; }

        /// <summary>
        /// 所属部门ID
        /// </summary>
        public long DepartmentId { get; set; }

        /// <summary>
        /// 招聘人数
        /// </summary>
        public int HeadCount { get; set; }

        /// <summary>
        /// 要求学历
        /// </summary>
        public string Education { get; set; }

        /// <summary>
        /// 要求工作经验
        /// </summary>
        public string Experience { get; set; }

        /// <summary>
        /// 计划开始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 计划完成日期
        /// </summary>
        public DateTime EndDate { get; set; }

        /// <summary>
        /// 状态 0=草稿 1=招聘中 2=已完成 3=已取消
        /// </summary>
        public int Status { get; set; }

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(DepartmentId))]
        public Department Department { get; set; }
        #endregion

        public string GetStatusDisplayName()
        {
            return Status switch
            {
                0 => "草稿",
                1 => "招聘中",
                2 => "已完成",
                3 => "已取消",
                _ => "未知"
            };
        }
    }


    [SugarTable("Hr_Candidate")]
    public class Candidate
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 候选人编号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 应聘职位
        /// </summary>
        public string ApplyPosition { get; set; }

        /// <summary>
        /// 联系电话
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// 工作经验
        /// </summary>
        public int WorkYears { get; set; }

        /// <summary>
        /// 最高学历
        /// </summary>
        public string Education { get; set; }

        /// <summary>
        /// 期望薪资
        /// </summary>
        public decimal ExpectedSalary { get; set; }

        /// <summary>
        /// 面试状态 0=初筛 1=一面 2=二面 3=已Offer 4=已入职 5=已淘汰
        /// </summary>
        public int InterviewStatus { get; set; }

        /// <summary>
        /// 关联招聘计划ID
        /// </summary>
        public long? RecruitmentPlanId { get; set; }

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(RecruitmentPlanId))]
        public RecruitmentPlan RecruitmentPlan { get; set; }
        #endregion

        public string GetInterviewStatusDisplayName()
        {
            return InterviewStatus switch
            {
                0 => "初筛",
                1 => "一面",
                2 => "二面",
                3 => "已发Offer",
                4 => "已入职",
                5 => "已淘汰",
                _ => "未知"
            };
        }
    }
}
