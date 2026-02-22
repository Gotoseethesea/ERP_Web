using ERP.Shared.Repository;
using ERP_Web.Models;
using ERP_Web.Models.PrivilegeHub;
using global::ERP_Web.Models;
using SqlSugar;

namespace ERP_Web.Core.Service
{
    public class ApprovalService
    {
        private readonly SqlClient _db;

        public ApprovalService()
        {
            _db = new SqlClient();
        }

        /// <summary>
        /// 获取指定用户的待审批单据列表
        /// </summary>
        /// <param name="userCode">当前登录用户编码</param>
        /// <returns>待审批单据列表</returns>
        public async Task<List<PendingApprovalDto>> GetMyPendingApprovals(string userCode)
        {
            // 查询逻辑：当前用户是审批人、步骤状态是待审批、且是当前流程正在处理的步骤
            var pendingSteps = await _db.Db.Queryable<ApprovalStep>()
                .Includes(s => s.Approver)
                .Includes(s => s.ApprovalProcess,p => p.ReferenceDocumentCode)
                   // .ThenInclude(p => p.ReferenceDocumentCode)
                .Where(s => s.ApproverCode == userCode
                          && s.Status == ApprovalStatus.Pending
                          && s.StepNumber == s.ApprovalProcess.StepNumber)
                .OrderBy(s => s.Id)
                .ToListAsync();

            var result = new List<PendingApprovalDto>();
            foreach (var step in pendingSteps)
            {
                // 根据单据类型关联查询对应业务单据的基础信息
                var document = await GetDocumentBaseInfo(step.ApprovalProcess.Type, step.ApprovalProcess.ReferenceDocumentCode);
                if (document == null) continue;

                result.Add(new PendingApprovalDto
                {
                    ApprovalStepId = step.Id,
                    ProcessCode = step.ApprovalProcess.Code,
                    DocumentType = step.ApprovalProcess.Type,
                    DocumentTypeText = GetDocumentTypeText(step.ApprovalProcess.Type),
                    DocumentCode = step.ApprovalProcess.ReferenceDocumentCode,
                    DocumentTrxNo = document.TrxNo,
                    DocumentDate = document.Date,
                    Amount = document.AmountIncTax,
                    SubmitUser = document.InsertUser,
                    SubmitTime = document.InsertTime,
                    CurrentStep = step.StepNumber,
                    CurrentRole = step.Role
                });
            }

            return result;
        }

        /// <summary>
        /// 根据单据类型和编码获取单据基础信息
        /// </summary>
        private async Task<BaseDocument> GetDocumentBaseInfo(DocumentType type, string documentCode)
        {
            return type switch
            {
                DocumentType.PurchaseRequisition => await _db.Db.Queryable<PurchaseRequisition>().FirstAsync(d => d.Code == documentCode),
                // DocumentType.ExpenseReimbursement => await _db.Db.Queryable<ExpenseReimbursement>().FirstAsync(d => d.Code == documentCode),
                // DocumentType.LeaveRequest => await _db.Db.Queryable<LeaveRequest>().FirstAsync(d => d.Code == documentCode),
                _ => null
            };
        }

        /// <summary>
        /// 单据类型转中文显示
        /// </summary>
        private string GetDocumentTypeText(DocumentType type)
        {
            return type switch
            {
                DocumentType.PurchaseRequisition => "采购申请单",
                DocumentType.ExpenseReimbursement => "费用报销单",
                DocumentType.LeaveRequest => "请假申请单",
                _ => "未知单据"
            };
        }
    }

    /// <summary>
    /// 待审批单据展示DTO
    /// </summary>
    public class PendingApprovalDto
    {
        public long ApprovalStepId { get; set; }
        public string ProcessCode { get; set; }
        public DocumentType DocumentType { get; set; }
        public string DocumentTypeText { get; set; }
        public string DocumentCode { get; set; }
        public string DocumentTrxNo { get; set; }
        public DateOnly DocumentDate { get; set; }
        public decimal Amount { get; set; }
        public string SubmitUser { get; set; }
        public DateTime SubmitTime { get; set; }
        public int CurrentStep { get; set; }
        public string CurrentRole { get; set; }
    }
}
