using SqlSugar;
using System;

namespace ERP_Web.Models
{
    [SugarTable("Hr_SalaryCalculate")]
    public class SalaryCalculate
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 核算年度
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 核算月份
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// 核算状态 0=待核算 1=核算中 2=已完成 3=已发放
        /// </summary>
        public int Status { get; set; }

        /// <summary>
        /// 总人数
        /// </summary>
        public int TotalEmployee { get; set; }

        /// <summary>
        /// 应发工资总额
        /// </summary>
        public decimal TotalSalary { get; set; }

        /// <summary>
        /// 个税总额
        /// </summary>
        public decimal TotalTax { get; set; }

        /// <summary>
        /// 社保公积金总额
        /// </summary>
        public decimal TotalSocialSecurity { get; set; }

        /// <summary>
        /// 实发工资总额
        /// </summary>
        public decimal TotalActualSalary { get; set; }

        /// <summary>
        /// 发放日期
        /// </summary>
        public DateTime? PayDate { get; set; }

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        public string GetStatusDisplayName()
        {
            return Status switch
            {
                0 => "待核算",
                1 => "核算中",
                2 => "已完成",
                3 => "已发放",
                _ => "未知"
            };
        }
    }

    [SugarTable("Hr_SalaryRecord")]
    public class SalaryRecord
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 关联核算ID
        /// </summary>
        public long SalaryCalculateId { get; set; }

        /// <summary>
        /// 员工ID
        /// </summary>
        public long EmployeeId { get; set; }

        /// <summary>
        /// 年度
        /// </summary>
        public int Year { get; set; }

        /// <summary>
        /// 月份
        /// </summary>
        public int Month { get; set; }

        /// <summary>
        /// 基本工资
        /// </summary>
        public decimal BaseSalary { get; set; }

        /// <summary>
        /// 绩效工资
        /// </summary>
        public decimal PerformanceSalary { get; set; }

        /// <summary>
        /// 加班工资
        /// </summary>
        public decimal OvertimeSalary { get; set; }

        /// <summary>
        /// 补贴
        /// </summary>
        public decimal Allowance { get; set; }

        /// <summary>
        /// 应发工资
        /// </summary>
        public decimal TotalSalary { get; set; }

        /// <summary>
        /// 个税
        /// </summary>
        public decimal Tax { get; set; }

        /// <summary>
        /// 社保个人部分
        /// </summary>
        public decimal SocialSecurityPersonal { get; set; }

        /// <summary>
        /// 公积金个人部分
        /// </summary>
        public decimal HousingFundPersonal { get; set; }

        /// <summary>
        /// 扣款
        /// </summary>
        public decimal Deduction { get; set; }

        /// <summary>
        /// 实发工资
        /// </summary>
        public decimal ActualSalary { get; set; }

        /// <summary>
        /// 发放状态 0=未发放 1=已发放
        /// </summary>
        public int PayStatus { get; set; }

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(EmployeeId))]
        public Employee Employee { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(SalaryCalculateId))]
        public SalaryCalculate SalaryCalculate { get; set; }
        #endregion

        public string GetPayStatusDisplayName()
        {
            return PayStatus switch
            {
                0 => "未发放",
                1 => "已发放",
                _ => "未知"
            };
        }
    }
}
