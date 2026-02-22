using SqlSugar;

namespace ERP_Web.Models.HRMS
{
    /// <summary>
    /// 简历主表
    /// </summary>
    [SugarTable("HR_Resume")]
    public class Resume
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 简历编号
        /// </summary>
        public string Code { get; set; } = "RES_" + DateTime.Now.ToString("yyyyMMddHHmmss");

        /// <summary>
        /// 姓名
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 性别 1=男 2=女
        /// </summary>
        public int Gender { get; set; }

        /// <summary>
        /// 身份证号
        /// </summary>
        public string? IdCard { get; set; }

        /// <summary>
        /// 手机号
        /// </summary>
        public string Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 出生日期
        /// </summary>
        public DateTime? Birthday { get; set; }

        /// <summary>
        /// 最高学历
        /// </summary>
        public string? Education { get; set; }

        /// <summary>
        /// 毕业院校
        /// </summary>
        public string? School { get; set; }

        /// <summary>
        /// 工作年限
        /// </summary>
        public int WorkYears { get; set; }

        /// <summary>
        /// 期望薪资
        /// </summary>
        public decimal ExpectSalary { get; set; }

        /// <summary>
        /// 期望部门
        /// </summary>
        public string ExpectDepartmentCode { get; set; }

        /// <summary>
        /// 期望岗位
        /// </summary>
        public string ExpectPositionCode { get; set; }

        /// <summary>
        /// 期望入职日期
        /// </summary>
        public DateTime ExpectHireDate { get; set; } = DateTime.Now;

        /// <summary>
        /// 紧急联系人
        /// </summary>
        public string? EmergencyContact { get; set; }

        /// <summary>
        /// 紧急联系电话
        /// </summary>
        public string? EmergencyPhone { get; set; }

        /// <summary>
        /// 简历状态 0=草稿 1=面试中 2=已录用 3=已拒绝
        /// </summary>
        public int Status { get; set; } = 0;

        /// <summary>
        /// 面试评价
        /// </summary>
        public string? InterviewRemark { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToMany, nameof(ResumeEducation.ResumeCode))]
        public List<ResumeEducation> Educations { get; set; } = [];

        [Navigate(NavigateType.OneToMany, nameof(ResumeWorkExperience.ResumeCode))]
        public List<ResumeWorkExperience> WorkExperiences { get; set; } = [];
        #endregion
    }

    /// <summary>
    /// 简历教育经历
    /// </summary>
    [SugarTable("HR_Resume_Education")]
    public class ResumeEducation
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string ResumeCode { get; set; }
        public string School { get; set; }
        public string Major { get; set; }
        public string Education { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? Remark { get; set; }
    }

    /// <summary>
    /// 简历工作经历
    /// </summary>
    [SugarTable("HR_Resume_WorkExperience")]
    public class ResumeWorkExperience
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }
        public string ResumeCode { get; set; }
        public string CompanyName { get; set; }
        public string Position { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string WorkContent { get; set; }
        public decimal? Salary { get; set; }
        public string? ReasonForLeaving { get; set; }
    }
}
