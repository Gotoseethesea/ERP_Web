using SqlSugar;

namespace ERP_Web.Models.HRMS
{

    public class PositionDepartmentMapping
    {
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public string PositionCode { get; set; }
        [SugarColumn(IsPrimaryKey = true)]//中间表可以不是主键
        public string DepartmentCode { get; set; }
    }

    [SugarTable("HR_Position")]
    public class Position
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(IsPrimaryKey = true)]
        /// <summary>
        /// 岗位编码
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 岗位名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 所属部门编码
        /// </summary>
        public string? DepartmentCodes { get; set; }
        /// <summary>
        /// 导航属性：所属部门
        /// </summary>
        [Navigate(typeof(PositionDepartmentMapping), nameof(PositionDepartmentMapping.PositionCode), nameof(PositionDepartmentMapping.DepartmentCode))]//注意顺序
        public List<Department> Departments { get; set; }
        /// <summary>
        /// 岗位描述
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// 岗位级别
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool Active { get; set; } = true;


        /// <summary>
        /// ✅ 新增：直接获取部门编码列表（非数据库映射，自动拆分DepartmentCodes字段）
        /// </summary>
        [SugarColumn(IsIgnore = true)]
        public List<string> DepartmentCodeList
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DepartmentCodes))
                    return new List<string>();
                return DepartmentCodes.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
            }
        }
    }

    /// <summary>
    /// 职级
    /// </summary>
    [SugarTable("HR_SalaryLevel")]
    public class SalaryLevel
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 等级编码
        /// </summary>    
        [SugarColumn(IsPrimaryKey = true)]

        public string Code { get; set; }

        /// <summary>
        /// 等级名称
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// 基本工资
        /// </summary>
        public decimal BaseSalary { get; set; }

        /// <summary>
        /// 岗位工资
        /// </summary>
        public decimal PostSalary { get; set; }

        /// <summary>
        /// 绩效工资基数
        /// </summary>
        public decimal PerformanceBase { get; set; }
    }
    [SugarTable("HR_Employee")]
    public class HREmployee
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 员工工号
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; }

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
        public string? Phone { get; set; }

        /// <summary>
        /// 邮箱
        /// </summary>
        public string? Email { get; set; }

        /// <summary>
        /// 所属部门编码
        /// </summary>
        public string DepartmentCode { get; set; }

        /// <summary>
        /// 岗位编码
        /// </summary>
        public string PositionCode { get; set; }

        /// <summary>
        /// 职级编码
        /// </summary>
        public string SalaryLevelCode { get; set; }

        /// <summary>
        /// 基本工资
        /// </summary>
        public decimal BaseSalary { get; set; }

        /// <summary>
        /// 岗位工资
        /// </summary>
        public decimal PostSalary { get; set; }

        /// <summary>
        /// 入职日期
        /// </summary>
        public DateTime HireDate { get; set; }

        /// <summary>
        /// 转正日期
        /// </summary>
        public DateTime? RegularDate { get; set; }

        /// <summary>
        /// 离职日期
        /// </summary>
        public DateTime? ResignDate { get; set; }

        /// <summary>
        /// 员工状态 1=在职 2=试用期 3=离职 4=退休 5=停职
        /// </summary>
        public int Status { get; set; } = 2;

        /// <summary>
        /// 紧急联系人
        /// </summary>
        public string? EmergencyContact { get; set; }

        /// <summary>
        /// 紧急联系电话
        /// </summary>
        public string? EmergencyPhone { get; set; }

        /// <summary>
        /// 备注
        /// </summary>
        public string? Remark { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode), nameof(Department.Code))]
        public Department Department { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(PositionCode), nameof(Position.Code))]
        public Position Position { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(SalaryLevelCode), nameof(SalaryLevel.Code))]
        public SalaryLevel SalaryLevel { get; set; }

        /// <summary>
        /// 关联合同列表
        /// </summary>
        [Navigate(NavigateType.OneToMany, nameof(EmployeeContract.EmployeeCode))]
        public List<EmployeeContract> Contracts { get; set; }
        #endregion
    }

    /// <summary>
    /// 员工合同实体
    /// </summary>
    [SugarTable("HR_EmployeeContract")]
    public class EmployeeContract
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 合同编号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 员工编码
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// 合同类型 1=劳动合同 2=实习协议 3=劳务合同
        /// </summary>
        public int ContractType { get; set; }

        /// <summary>
        /// 开始日期
        /// </summary>
        public DateTime StartDate { get; set; }

        /// <summary>
        /// 结束日期
        /// </summary>
        public DateTime? EndDate { get; set; }

        /// <summary>
        /// 是否无固定期限
        /// </summary>
        public bool IsPermanent { get; set; } = false;

        /// <summary>
        /// 合同状态 1=有效 2=已到期 3=已解除
        /// </summary>
        public int Status { get; set; } = 1;
    }

    [SugarTable("Hr_EmployeeTransfer")]
    public class EmployeeTransfer
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public long Id { get; set; }

        /// <summary>
        /// 异动单号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 员工ID
        /// </summary>
        public long EmployeeId { get; set; }

        /// <summary>
        /// 异动类型 0=转正 1=调岗 2=升职 3=降职 4=离职
        /// </summary>
        public int TransferType { get; set; }

        /// <summary>
        /// 原部门ID
        /// </summary>
        public long OldDepartmentId { get; set; }

        /// <summary>
        /// 新部门ID
        /// </summary>
        public long NewDepartmentId { get; set; }

        /// <summary>
        /// 原职位
        /// </summary>
        public string OldPosition { get; set; }

        /// <summary>
        /// 新职位
        /// </summary>
        public string NewPosition { get; set; }

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// 异动原因
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

        public bool Active { get; set; } = true;
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string InsertUser { get; set; }
        public DateTime? UpdateTime { get; set; }
        public string UpdateUser { get; set; }

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(EmployeeId))]
        public Employee Employee { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(OldDepartmentId))]
        public Department OldDepartment { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(NewDepartmentId))]
        public Department NewDepartment { get; set; }
        #endregion

        public string GetTransferTypeDisplayName()
        {
            return TransferType switch
            {
                0 => "转正",
                1 => "调岗",
                2 => "升职",
                3 => "降职",
                4 => "离职",
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
    /// 人事异动单
    /// </summary>
    [SugarTable("HR_Transaction")]
    public class HrTransaction
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 单据号
        /// </summary>
        public string Code { get; set; }

        /// <summary>
        /// 异动类型 1=入职 2=离职 3=调岗 4=调薪 5=转正 6=部门调动
        /// </summary>
        public int TrxType { get; set; }

        /// <summary>
        /// 员工编码
        /// </summary>
        public string EmployeeCode { get; set; }

        /// <summary>
        /// 生效日期
        /// </summary>
        public DateTime EffectiveDate { get; set; }

        /// <summary>
        /// 原部门编码（调岗/调动用）
        /// </summary>
        public string? OldDepartmentCode { get; set; }

        /// <summary>
        /// 新部门编码（调岗/调动/入职用）
        /// </summary>
        public string? NewDepartmentCode { get; set; }

        /// <summary>
        /// 原岗位编码（调岗用）
        /// </summary>
        public string? OldPositionCode { get; set; }

        /// <summary>
        /// 新岗位编码（调岗/入职用）
        /// </summary>
        public string? NewPositionCode { get; set; }

        /// <summary>
        /// 原薪资（调薪用）
        /// </summary>
        public decimal? OldTotalSalary { get; set; }

        /// <summary>
        /// 新薪资（调薪/入职用）
        /// </summary>
        public decimal? NewTotalSalary { get; set; }

        /// <summary>
        /// 异动原因
        /// </summary>
        public string Reason { get; set; }

        /// <summary>
        /// 制单人
        /// </summary>
        public string Operator { get; set; }

        /// <summary>
        /// 审批人
        /// </summary>
        public string? Approver { get; set; }

        /// <summary>
        /// 审批状态 0=草稿 1=审批中 2=已通过 3=已驳回
        /// </summary>
        public int ApprovalStatus { get; set; } = 0;

        /// <summary>
        /// 是否已生效（审批通过后自动更新员工档案，标记为已生效）
        /// </summary>
        public bool IsEffective { get; set; } = false;

        #region 导航属性
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode), nameof(Employee.Code))]
        public Employee Employee { get; set; }
        #endregion
    }

    /// <summary>
    /// 人事异动类型枚举
    /// </summary>
    public enum HrTrxTypeEnum
    {
        入职 = 1,
        离职 = 2,
        调岗 = 3,
        调薪 = 4,
        转正 = 5,
        部门调动 = 6
    }

}
