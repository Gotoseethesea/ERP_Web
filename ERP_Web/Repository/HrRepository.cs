using ERP_Web.Models;
using ERP_Web.Models.HRMS;
using SqlSugar;

namespace ERP_Web.Repository
{
    public class HrRepository
    {
        private readonly SqlSugarClient _db;

        public HrRepository()
        {
            _db = new SqlClient().Db;
        }
        #region 薪资等级管理CRUD
        public List<SalaryLevel> GetSalaryLevelList(string? keyword = null)
        {
            var query = _db.Queryable<SalaryLevel>();
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(s => s.Code.Contains(keyword) || s.Name.Contains(keyword));
            return query.ToList();
        }

        public SalaryLevel GetSalaryLevelByCode(string code) => _db.Queryable<SalaryLevel>().First(s => s.Code == code);

        public int SaveSalaryLevel(SalaryLevel level) => _db.Insertable(level).ExecuteCommand();

        public int DeleteSalaryLevel(string code) => _db.Deleteable<SalaryLevel>().Where(s => s.Code == code).ExecuteCommand();
        #endregion
        #region 岗位管理CRUD
        public List<Position> GetPositionList(string? keyword = null)
        {
            var query = _db.Queryable<Position>().Includes(p => p.Departments);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(p => p.Code.Contains(keyword) || p.Name.Contains(keyword));
            return query.ToList();
        }

        public Position GetPositionByCode(string code) => _db.Queryable<Position>().Includes(p => p.Departments).First(p => p.Code == code);

        public int SavePosition(Position position) => _db.Insertable(position).ExecuteCommand();

        //public int DeletePosition(string code) => _db.Deleteable<Position>().Where(p => p.Code == code).ExecuteCommand();

        /// <summary>
        /// 删除岗位（逻辑删除，仅标记为不启用）
        /// </summary>
        public int DeletePosition(string code)
        {
            return _db.Updateable<Position>()
                .SetColumns(p => p.Active, false)
                .Where(p => p.Code == code)
                .ExecuteCommand();
        }
        #endregion


        #region 岗位多部门关联操作
        /// <summary>
        /// 根据岗位编码获取关联的部门编码列表
        /// </summary>
        public List<string> GetPositionDepartmentCodeList(string positionCode)
        {
            var position = _db.Queryable<Position>().First(p => p.Code == positionCode);
            return position?.DepartmentCodeList ?? new List<string>();
        }

        /// <summary>
        /// 根据岗位编码获取关联的部门实体列表（带完整部门信息）
        /// </summary>
        public List<Department> GetPositionDepartments(string positionCode)
        {
            return _db.Queryable<Position>()
                .Includes(p => p.Departments)
                .First(p => p.Code == positionCode)?
                .Departments ?? new List<Department>();
        }

        /// <summary>
        /// 更新岗位关联的部门（自动维护DepartmentCodes字段和中间表）
        /// </summary>
        public void UpdatePositionDepartments(string positionCode, List<string> departmentCodes)
        {
            _db.Ado.BeginTran();
            try
            {
                var position = _db.Queryable<Position>().First(p => p.Code == positionCode);
                // 更新DepartmentCodes字段（逗号分隔存储）
                position.DepartmentCodes = string.Join(',', departmentCodes.Where(d => !string.IsNullOrEmpty(d)));
                _db.Updateable(position).UpdateColumns(p => p.DepartmentCodes).ExecuteCommand();

                // 更新多对多中间表
                _db.Deleteable<PositionDepartmentMapping>().Where(m => m.PositionCode == positionCode).ExecuteCommand();
                if (departmentCodes.Any(d => !string.IsNullOrEmpty(d)))
                {
                    var mappings = departmentCodes.Where(d => !string.IsNullOrEmpty(d)).Select(dc => new PositionDepartmentMapping
                    {
                        PositionCode = positionCode,
                        DepartmentCode = dc
                    }).ToList();
                    _db.Insertable(mappings).ExecuteCommand();
                }

                _db.Ado.CommitTran();
            }
            catch
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }
        #endregion

        #region 员工档案CRUD
        public List<Employee> GetEmployeeList(string? keyword = null, int? status = null)
        {
            var query = _db.Queryable<Employee>()
                .Includes(e => e.Department)
                .Includes(e => e.Position)
                .Includes(e => e.SalaryLevel)
                .Includes(e => e.Contracts);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(e => e.Code.Contains(keyword) || e.Name.Contains(keyword) || e.Phone.Contains(keyword));
            if (status.HasValue)
                query = query.Where(e => e.Status == status.Value);
            return query.ToList();
        }

        public Employee GetEmployeeByCode(string code)
        {
            return _db.Queryable<Employee>()
                .Includes(e => e.Department)
                .Includes(e => e.Position)
                .Includes(e => e.SalaryLevel)
                .Includes(e => e.Contracts)
                .First(e => e.Code == code);
        }

        public int SaveEmployee(Employee employee) => _db.Insertable(employee).ExecuteCommand();

        //public int DeleteEmployee(string code) => _db.Deleteable<Employee>().Where(e => e.Code == code).ExecuteCommand();

        /// <summary>
        /// 员工离职逻辑删除
        /// </summary>
        public int DeleteEmployee(string code, string? resignReason = null)
        {
            //return _db.Updateable<Employee>()
            //    .SetColumns(e => e.Status, 3) // 标记为离职
            //    .SetColumns(e => e.ResignDate, DateTime.Now)
            //    .SetColumns(e => e.Remark, string.IsNullOrEmpty(resignReason) ? e.Remark : resignReason)
            //    .Where(e => e.Code == code)
            //    .ExecuteCommand();
            return 0;
        }
        #endregion

        #region 人事异动单CRUD
        public List<HrTransaction> GetHrTrxList(string? keyword = null, int? trxType = null, int? approvalStatus = null)
        {
            var query = _db.Queryable<HrTransaction>()
                .Includes(t => t.Employee)
                .Includes(t => t.Employee.Department)
                .Includes(t => t.Employee.Position);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(t => t.Code.Contains(keyword) || t.Employee.Name.Contains(keyword));
            if (trxType.HasValue)
                query = query.Where(t => t.TrxType == trxType.Value);
            if (approvalStatus.HasValue)
                query = query.Where(t => t.ApprovalStatus == approvalStatus.Value);
            return query.OrderByDescending(t => t.Id).ToList();
        }
        public HrTransaction GetHrTrxByCode(string code)
        {
            return _db.Queryable<HrTransaction>()
                .Includes(t => t.Employee)
                .Includes(t => t.Employee.Department)
                .Includes(t => t.Employee.Position)
                .First(t => t.Code == code);
        }

        public int SaveHrTrx(HrTransaction trx) => _db.Insertable(trx).ExecuteCommand();

        /// <summary>
        /// 审批通过后执行：自动更新员工档案，标记异动生效
        /// </summary>
        public void ApproveHrTrx(string trxCode)
        {
            _db.Ado.BeginTran();
            try
            {
                var trx = GetHrTrxByCode(trxCode);
                var emp = GetEmployeeByCode(trx.EmployeeCode);

                switch ((HrTrxTypeEnum)trx.TrxType)
                {
                    case HrTrxTypeEnum.入职:
                        emp.Status = 2; // 试用期
                        emp.HireDate = trx.EffectiveDate;
                        emp.DepartmentCode = trx.NewDepartmentCode;
                        emp.PositionCode = trx.NewPositionCode;
                        if (trx.NewTotalSalary.HasValue)
                        {
                            emp.BaseSalary = trx.NewTotalSalary.Value * 0.7M; // 可按你司薪资规则调整拆分比例
                            emp.PostSalary = trx.NewTotalSalary.Value * 0.3M;
                        }
                        break;
                    case HrTrxTypeEnum.离职:
                        emp.Status = 3; // 离职
                        emp.ResignDate = trx.EffectiveDate;
                        break;
                    case HrTrxTypeEnum.调岗:
                    case HrTrxTypeEnum.部门调动:
                        emp.DepartmentCode = trx.NewDepartmentCode;
                        emp.PositionCode = trx.NewPositionCode;
                        break;
                    case HrTrxTypeEnum.调薪:
                        if (trx.NewTotalSalary.HasValue)
                        {
                            emp.BaseSalary = trx.NewTotalSalary.Value * 0.7M;
                            emp.PostSalary = trx.NewTotalSalary.Value * 0.3M;
                        }
                        break;
                    case HrTrxTypeEnum.转正:
                        emp.Status = 1; // 正式在职
                        emp.RegularDate = trx.EffectiveDate;
                        break;
                }

                // 更新员工档案+标记异动生效
                _db.Updateable(emp).ExecuteCommand();
                trx.ApprovalStatus = 2;
                trx.IsEffective = true;
                _db.Updateable(trx).ExecuteCommand();

                _db.Ado.CommitTran();
            }
            catch
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }
        #endregion

        #region 简历管理CRUD+业务逻辑
        public List<Resume> GetResumeList(string? keyword = null, int? status = null)
        {
            var query = _db.Queryable<Resume>()
                .Includes(r => r.Educations)
                .Includes(r => r.WorkExperiences);
            if (!string.IsNullOrEmpty(keyword))
                query = query.Where(r => r.Name.Contains(keyword) || r.Phone.Contains(keyword));
            if (status.HasValue)
                query = query.Where(r => r.Status == status.Value);
            return query.OrderByDescending(r => r.Id).ToList();
        }

        public Resume GetResumeByCode(string code)
        {
            return _db.Queryable<Resume>()
                .Includes(r => r.Educations)
                .Includes(r => r.WorkExperiences)
                .First(r => r.Code == code);
        }

        public int SaveResume(Resume resume) => _db.Insertable(resume).ExecuteCommand();

        /// <summary>
        /// 简历审批通过后自动生成员工档案+入职异动单
        /// </summary>
        public void ResumeToEmployee(string resumeCode)
        {
            _db.Ado.BeginTran();
            try
            {
                var resume = GetResumeByCode(resumeCode);
                if (resume.Status == 2) throw new Exception("该简历已生成员工档案，请勿重复操作");

                // 生成员工工号（可按你司规则调整）
                var empCode = "EMP_" + DateTime.Now.ToString("yyyyMMddHHmmss");
                // 生成员工档案
                var emp = new Employee
                {
                    Code = empCode,
                    Name = resume.Name,
                    Gender = resume.Gender,
                    IdCard = resume.IdCard,
                    Phone = resume.Phone,
                    Email = resume.Email,
                    DepartmentCode = resume.ExpectDepartmentCode,
                    PositionCode = resume.ExpectPositionCode,
                    BaseSalary = resume.ExpectSalary * 0.7M,
                    PostSalary = resume.ExpectSalary * 0.3M,
                    HireDate = resume.ExpectHireDate,
                    Status = 2, // 试用期
                    EmergencyContact = resume.EmergencyContact,
                    EmergencyPhone = resume.EmergencyPhone
                };
                SaveEmployee(emp);

                // 自动生成入职异动单
                var trx = new HrTransaction
                {
                    Code = "HR_TRX_" + DateTime.Now.ToString("yyyyMMddHHmmss"),
                    TrxType = (int)HrTrxTypeEnum.入职,
                    EmployeeCode = empCode,
                    EffectiveDate = resume.ExpectHireDate,
                    NewDepartmentCode = resume.ExpectDepartmentCode,
                    NewPositionCode = resume.ExpectPositionCode,
                    NewTotalSalary = resume.ExpectSalary,
                    Reason = "简历录用审批通过，自动生成入职异动",
                    Operator = "系统自动生成",
                    ApprovalStatus = 2,
                    IsEffective = true
                };
                SaveHrTrx(trx);

                // 标记简历已录用
                resume.Status = 2;
                _db.Updateable(resume).ExecuteCommand();

                _db.Ado.CommitTran();
            }
            catch
            {
                _db.Ado.RollbackTran();
                throw;
            }
        }
        #endregion
    }
}
