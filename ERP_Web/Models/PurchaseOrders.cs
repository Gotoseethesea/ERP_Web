using AntDesign;
using AntDesign.Charts;
using ERP_Web.Repository;
using ERP_Web.Core.Constants;
using ERP_Web.Models.PrivilegeHub;
using MathNet.Numerics.Providers.SparseSolver;
using OneOf.Types;
using SqlSugar;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Transactions;
using NPOI.SS.Formula.Functions;

namespace ERP_Web.Models
{
    public enum PoTrxType
    {
        PurchaseOrder = 1,
        PurchaseReturn = -1
    }

    public class PoTrxGroup
    {
        [SugarColumn(IsIdentity = true)]
        public int Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; } = "";
        public string Name { get; set; } = ""; //采购类型名称:生鲜、百货、服装、机动采购
        //public int TrxType { get; set; } //1、Inbound（入库），-1、Outbound（出库），0、InThenOut  先入后出 直入直出
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool Active { set; get; } = true;
    }
    /// <summary>
    /// 采购申请单存货明细
    /// </summary>
    public class PRInvInOut : InvInOut
    {
        public string? PRCode { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();
    }

    /// <summary>
    /// 采购申请单 内部审批
    /// </summary>
    public class PurchaseRequisition : BaseDocument
    {
        [Display(Name = "采购类型")]
        public string? PRTrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(PRTrxGroupCode))]//一对一 
        public PoTrxGroup PoTrxGroup { set; get; } = new OneToOneInitializer<PoTrxGroup>();
        //public string? WarehouseCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        //public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        //public string? CompanyCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        //public Company Company { set; get; } = new OneToOneInitializer<Company>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? ApprovalTypeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(ApprovalTypeCode))]//一对一
        public ApprovalType? ApprovalType { set; get; }   //保存审批节点，审批节点包含审批类型（部门负责人审批、财务审批、总经理审批等）和审批人（可以是具体的审批人，也可以是一个角色，由系统自动匹配到具体的审批人）
        public string? ApprovalProcessCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(ApprovalProcessCode))]//一对一
        public ApprovalProcess? ApprovalProcess { set; get; }   //保存审批流程，进行审批时根据流程自动匹配审批节点和审批人，进行审批动作执行

        public string? Operator { set; get; }
        public string? Checker { set; get; }
        public string? Poster { set; get; }

        [Navigate(NavigateType.OneToMany, nameof(PRInvInOut.PRCode))]//一对多
        public List<PRInvInOut>? PRInvInOuts { set; get; }

        [SugarColumn(IsIgnore = true)]
        public string DepartmentName => Department?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string EmployeeName => Employee?.Name ?? string.Empty;

        public PurchaseRequisition() { }

        public PurchaseRequisition(string str)
        {
            GetTrxNo<PurchaseRequisition>("PR");
            if (PRInvInOuts == null) PRInvInOuts = new List<PRInvInOut>();

        }

        public void UpdateInvPrice()
        {
            if (PRInvInOuts != null)
            {
                foreach (PRInvInOut prInvInOut in PRInvInOuts)
                {
                    prInvInOut.UpdatePriceByIn(); 
                }
            }
        }
        public void UpdateAmount()
        {
            this.Quantity = 0;
            this.Amount = 0;
            this.AmountIncTax = 0;
            if (PRInvInOuts != null && PRInvInOuts.Count > 0)
            {
                foreach (PRInvInOut prInvInOut in PRInvInOuts)
                {
                    this.Quantity += prInvInOut.Quantity;
                    this.Amount += prInvInOut.Amount;
                    this.AmountIncTax += prInvInOut.AmountIncTax;
                }
            }
        }
        public void CreatReceivingNote()
        {

        }
        public POInvInOut PRInvInOutToPO(PRInvInOut prInvInOut)
        {
            POInvInOut poInvInOut = new POInvInOut()
            {
                InvCode = prInvInOut.InvCode,
                InventoryItem = prInvInOut.InventoryItem,
                Quantity = prInvInOut.Quantity,
                Price = prInvInOut.Price,
                Amount = prInvInOut.Amount,
                TaxRate = prInvInOut.TaxRate,
                PriceIncTax = prInvInOut.PriceIncTax,
                AmountIncTax = prInvInOut.AmountIncTax,
                Note = prInvInOut.Note,
                Sequence = prInvInOut.Sequence,
                Active = prInvInOut.Active,
                WarehouseCode = prInvInOut.WarehouseCode,
                //CompanyCode = prInvInOut.CompanyCode
            };
            return poInvInOut;
        }

        //public enum PurchaseRequisitionStatus
        //{
        //    Draft,        // 草稿
        //    Submitted,    // 已提交
        //    Approved,     // 已批准
        //    Rejected,     // 已拒绝
        //    Completed     // 已完成
        //}


        /// <summary>
        /// 将采购申请单转换为采购订单列表（按公司分组）
        /// </summary>
        /// <returns>生成的采购订单列表</returns>
        public List<PurchaseOrder> ConvertToPurchaseOrders()
        {
            if (this.PRInvInOuts == null || this.PRInvInOuts.Count == 0)
                return new List<PurchaseOrder>();

            // 按公司代码分组
            var groupedByCompany = this.PRInvInOuts
                .GroupBy(detail => detail.CompanyCode ?? string.Empty)
                .ToList();

            var purchaseOrders = new List<PurchaseOrder>();

            foreach (var group in groupedByCompany)
            {
                string companyCode = group.Key;

                // 创建采购订单
                var po = new PurchaseOrder("")
                {
                    // 基础信息
                    InsertTime = DateTime.Now,
                    InsertUser = this.InsertUser,
                    // 关联信息
                    PRTrxGroupCode = this.PRTrxGroupCode,
                    DepartmentCode = this.DepartmentCode,
                    EmployeeCode = this.EmployeeCode,
                    CompanyCode = companyCode,
                    Explanation = this.Explanation,
                    Operator = this.Operator,
                    Checker = this.Checker,
                    Poster = this.Poster,
                    // 初始化明细列表
                    POInvInOuts = new List<POInvInOut>()
                };

                // 转换明细项
                foreach (var prDetail in group)
                {
                    po.POInvInOuts.Add(new POInvInOut
                    {
                        // 复制字段
                        InvCode = prDetail.InvCode,
                        InventoryItem = prDetail.InventoryItem,
                        SKU = prDetail.SKU,
                        Quantity = prDetail.Quantity,
                        Price = prDetail.Price,
                        Amount = prDetail.Amount,
                        TaxRate = prDetail.TaxRate,
                        PriceIncTax = prDetail.PriceIncTax,
                        AmountIncTax = prDetail.AmountIncTax,
                        Note = prDetail.Note,
                        Sequence = prDetail.Sequence,
                        Active = prDetail.Active,
                        WarehouseCode = prDetail.WarehouseCode,
                        // 后期优化需要增加 PR单与PO单的关系串联，优化穿透功能
                        // 注意：这里不需要设置CompanyCode，因为POInvInOut没有CompanyCode（根据你提供的实体）
                        // 但是，如果POInvInOut有CompanyCode，可以设置：CompanyCode = prDetail.CompanyCode
                    });
                }

                // 计算采购订单的汇总信息
                po.UpdateAmount();

                purchaseOrders.Add(po);
            }
            this.Status = DocumentStatus.Completed;
            return purchaseOrders;
        }

        public virtual void Delete()
        {
            this.Active = false;
            SqlClient SSC = new SqlClient();
            var result = SSC.Db.Updateable(this).ExecuteCommand();
        }
        public void Update()
        {
            SqlClient SSD = new SqlClient();
            SSD.Db.UpdateNav(this)
                .IncludesAllFirstLayer() //自动2级
                .IncludeByNameString(nameof(PurchaseRequisition.ApprovalProcess)).ThenIncludeByNameString(nameof(ApprovalProcess.Steps))
                .ExecuteCommand();
        }
        public void Sumit(User user)
        {
            if (!(Status == DocumentStatus.Draft || Status == DocumentStatus.Rejected))
            {
                throw new Exception("只能草稿状态和被拒绝的采购单才能提交");
            }

            // 1. 初始化审批流程
            if (this.ApprovalProcess == null)
            {
                // 首次提交：新建审批流程
                //this.ApprovalProcess = new ApprovalProcess("");
                this.ApprovalProcess = new ApprovalProcess(user, this.ApprovalType, this.Code);

                this.Status = DocumentStatus.Submitted;
                this.UpdateTime = DateTime.Now;
                // 当前步骤指向本次提交节点，待审批步骤会自动识别
                this.ApprovalProcess.StepNumber = 1;

                // 5. 设置当前第一个待审批人
                this.Checker = this.ApprovalProcess.Steps
                    .OrderBy(s => s.StepNumber)
                    .FirstOrDefault(s => s.Status == ApprovalStatus.Pending)
                    ?.Approver?.Name;
                this.Update();
                return;
            }
            else
            {
                // 重新提交：保留审批流程，删除未审批节点，保留已审批节点历史痕迹
                var deleteApprovalSteps = this.ApprovalProcess.Steps
                    .Where(s => s.Type == StepType.Approval && s.Status == ApprovalStatus.Pending)
                    .ToList();
                this.ApprovalProcess.Steps.RemoveAll(s => deleteApprovalSteps.Contains(s));

                // 重新提交：将原有所有未作废的审批节点标记为已退回（保留历史痕迹）
                var oldApprovalSteps = this.ApprovalProcess.Steps
                    .Where(s => s.Type == StepType.Approval && s.Status != ApprovalStatus.Returned && s.Status != ApprovalStatus.Rejected)
                    .ToList();

                //不修改之前的状态
                //oldApprovalSteps.ForEach(s =>
                //{
                //    s.Status = ApprovalStatus.Returned;
                //    s.Comments += " | 单据撤回重提，本节点作废";
                //});
            }

            // 2. 获取最新审批流程配置，生成全新的审批节点（全流程重新走）
            var allNodes = this.ApprovalType.Nodes.OrderBy(n => n.StepNumber).ToList();
            // 计算新节点的起始序号：取现有步骤最大序号+1，避免和历史节点冲突，永久留痕
            int maxStepNumber = this.ApprovalProcess.Steps.Any() ? this.ApprovalProcess.Steps.Max(s => s.StepNumber) : 0;

            foreach (var node in allNodes)
            {
                var approverUser = ApprovalStep.GetApproverUser(node);
                this.ApprovalProcess.Steps.Add(new ApprovalStep
                {
                    Id = SnowFlakeSingle.Instance.NextId(),
                    ApprovalProcessCode = this.ApprovalProcess.Code,
                    StepNumber = maxStepNumber + node.StepNumber, // 序号顺延，不覆盖历史
                    Role = node.Role,
                    Approver = approverUser,
                    ApproverCode = approverUser != null ? approverUser.Code : null,
                    Type = StepType.Approval,
                    Status = ApprovalStatus.Pending,
                    Comments = string.Empty,
                    ApprovalTime = DateTime.MinValue
                });
            }

            // 3. 新增本次提交操作记录节点（放在本次审批节点最前面）
            var submitStep = new ApprovalStep
            {
                Id = SnowFlakeSingle.Instance.NextId(),
                ApprovalProcessCode = this.ApprovalProcess.Code,
                StepNumber = maxStepNumber, // 序号在本次审批节点前面
                Role = "提交申请",
                ApproverCode = this.InsertUser,
                Type = StepType.Submit,
                Status = ApprovalStatus.Approved,
                Comments = maxStepNumber == 0 ? "首次提交采购申请单" : "重新提交采购申请单",
                ApprovalTime = DateTime.Now
            };
            // 插入到本次审批节点的前面
            var insertIndex = this.ApprovalProcess.Steps.FindIndex(s => s.StepNumber == maxStepNumber + 1);
            if (insertIndex < 0) insertIndex = this.ApprovalProcess.Steps.Count;
            this.ApprovalProcess.Steps.Insert(insertIndex, submitStep);

            // 4. 重置状态
            this.Status = DocumentStatus.Submitted;
            this.UpdateTime = DateTime.Now;
            // 当前步骤指向本次提交节点，待审批步骤会自动识别
            this.ApprovalProcess.StepNumber = maxStepNumber;

            // 5. 设置当前第一个待审批人
            this.Checker = this.ApprovalProcess.Steps
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault(s => s.Status == ApprovalStatus.Pending)
                ?.Approver?.Name;

            this.Update();
        }

        /// <summary>
        /// 撤回已提交的采购申请单
        /// </summary>
        /// <param name="remark">撤回备注（可选）</param>
        /// <param name="operatorUser">操作人，不传默认取当前登录用户</param>
        /// <returns>操作结果，和Check方法返回格式一致</returns>
        public Dictionary<bool, string> Withdraw(string remark = null, User operatorUser = null)
        {
            // ------------ 前置校验 ------------
            // 校验1：只能撤回已提交/审批中且未完成审批的单据
            if (Status != DocumentStatus.Submitted && Status != DocumentStatus.Auditing)
            {
                return new Dictionary<bool, string> { { false, "只能撤回【已提交】或【审批中】且未完成审批的采购申请单" } };
            }

            // 校验2：只有单据提交人本人可以撤回
            operatorUser ??= UserContext.CurrentUser;
            if (operatorUser == null || operatorUser.Account != this.InsertUser)
            {
                // 如果你的提交人字段是EmployeeCode，就改为：operatorUser.Code != this.EmployeeCode
                return new Dictionary<bool, string> { { false, "只有单据提交人本人可以撤回" } };
            }

            // 校验3：已被审批人处理过的单据不可撤回
            if (this.ApprovalProcess == null || this.ApprovalProcess.Steps == null)
            {
                return new Dictionary<bool, string> { { false, "审批流程不存在，无法撤回" } };
            }
            if (this.ApprovalProcess.Steps.Any(s => s.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected))
            {
                return new Dictionary<bool, string> { { false, "单据已被审批人处理，无法撤回" } };
            }

            try
            {
                // ------------ 执行撤回操作 ------------
                // 1. 单据状态回滚为草稿
                this.Status = DocumentStatus.Draft;
                // 2. 作废原有审批流程（两种方案二选一）
                // 方案1：直接清空关联，下次提交重新生成新流程（简单）
                // this.ApprovalProcess = null;
                // this.ApprovalProcessCode = null;
                // 方案2：保留历史流程，标记为作废（推荐，方便留痕，需要先给ApprovalProcess加IsCancelled字段）
                // this.ApprovalProcess.IsCancelled = true;
                // 3. 清空当前审批人
                this.Checker = null;
                // 4. 更新操作时间
                this.UpdateTime = DateTime.Now;

                // ========== 可选：增加撤回留痕字段，需要先在PurchaseRequisition实体中新增对应字段 ==========
                // this.WithdrawUser = operatorUser.Name;
                // this.WithdrawTime = DateTime.Now;
                // this.WithdrawRemark = remark;

                // 保存到数据库
                this.Update();

                return new Dictionary<bool, string> { { true, $"撤回成功，单据已回到草稿状态。{(string.IsNullOrEmpty(remark) ? "" : $"撤回备注：{remark}")}" } };
            }
            catch (Exception ex)
            {
                return new Dictionary<bool, string> { { false, $"撤回失败：{ex.Message}" } };
            }
        }

        /// <summary>
        /// 审批方法，审批人审批时调用，审批人可以是具体的审批人，也可以是一个角色，由系统自动匹配到具体的审批人进行审批
        /// </summary>
        /// <param name="user"></param>
        /// <param name="Comments"></param>
        /// <returns></returns>
        public Dictionary<bool, string> Approve2(User user = null, string Comments = "默认同意")
        {
            if (Status != DocumentStatus.Submitted && Status != DocumentStatus.Pending && Status != DocumentStatus.Auditing)
            {
                return new Dictionary<bool, string> { { false, "只能审批【已提交】或【待审批】或【审批中】的采购申请单" } };
            }

            user ??= UserContext.CurrentUser;
            if (user == null)
            {
                return new Dictionary<bool, string> { { false, "获取当前登录用户信息失败" } };
            }

            if (this.ApprovalProcess == null || !this.ApprovalProcess.Steps.Any())
            {
                return new Dictionary<bool, string> { { false, "审批流程未定义，无法审批" } };
            }

            // 找到当前待审批的节点
            var currentStep = this.ApprovalProcess.Steps
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault(s => s.Status == ApprovalStatus.Pending);

            if (currentStep == null)
            {
                // 无后续节点：审批完成
                this.Status = DocumentStatus.Approved;
                this.Checker = null;

                return new Dictionary<bool, string> { { false, "当前没有待审批的节点" } };
            }

            // ========== 权限校验：适配多对多审批角色结构 ==========
            if (!user.ApprovalRoles.Any(r => r.Code == currentStep.Role ||r.Name == currentStep.Role))
            {
                return new Dictionary<bool, string> { { false, $"当前审批角色为【{currentStep.Role}】，您没有审批权限" } };
            }
            try
            {
                // 1. 更新当前审批节点状态
                currentStep.Status = ApprovalStatus.Approved;
                currentStep.Comments = string.IsNullOrEmpty(currentStep.Comments)
                            ? Comments
                            : $"{currentStep.Comments} - {currentStep.ApprovalTime?.ToString("yyyy-MM-dd HH:mm")} | {Comments}";
                currentStep.ApprovalTime = DateTime.Now;
                currentStep.ApproverCode = user.Code;
                currentStep.Approver = user;

                // 2. 推进流程步骤
                this.ApprovalProcess.StepNumber = currentStep.StepNumber;

                // 3. 查找下一个待审批节点
                var nextStep = this.ApprovalProcess.Steps
                    .OrderBy(s => s.StepNumber)
                    .FirstOrDefault(s => s.StepNumber > currentStep.StepNumber && (s.Status == ApprovalStatus.Pending || s.Status == ApprovalStatus.Rejected));

                if (nextStep != null)
                {
                    // 还有后续节点：更新为审批中状态，设置下一个审批人
                    this.Status = DocumentStatus.Auditing;
                    this.Checker = nextStep.Approver?.Name;
                    nextStep.Status = ApprovalStatus.Pending;
                    var resultMsg = $"审批成功：{currentStep.Role}（{user.Name}），下一步待{nextStep.Role}（{nextStep.Approver?.Name}）审批";
                    return new Dictionary<bool, string> { { true, resultMsg } };
                }
                else
                {
                    // 无后续节点：审批完成
                    this.Status = DocumentStatus.Approved;
                    this.Checker = null;
                    return new Dictionary<bool, string> { { true, $"审批完成：{currentStep.Role}（{user.Name}），采购单已全部通过审批" } };
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<bool, string> { { false, $"审批失败：{ex.Message}" } };
            }
            finally
            {
                this.Update();
            }
        }


        public Dictionary<bool, string> Approve(User user = null, string Comments = "默认同意")
        {
            if (Status != DocumentStatus.Submitted && Status != DocumentStatus.Pending && Status != DocumentStatus.Auditing)
            {
                return new Dictionary<bool, string> { { false, "只能审批【已提交】或【待审批】或【审批中】的采购申请单" } };
            }

            user ??= UserContext.CurrentUser;
            if (user == null)
            {
                return new Dictionary<bool, string> { { false, "获取当前登录用户信息失败" } };
            }

            if (this.ApprovalProcess == null || !this.ApprovalProcess.Steps.Any())
            {
                return new Dictionary<bool, string> { { false, "审批流程未定义，无法审批" } };
            }

            // 找到当前待处理的节点（支持审批节点+传阅节点）
            var currentStep = this.ApprovalProcess.Steps
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault(s => s.Status == ApprovalStatus.Pending);

            if (currentStep == null)
            {
                this.Status = DocumentStatus.Approved;
                this.Checker = null;
                return new Dictionary<bool, string> { { false, "当前没有待处理的节点" } };
            }

            // ========== 权限校验：区分传阅节点和审批节点 ==========
            if (currentStep.Type == StepType.Approval
                && !user.ApprovalRoles.Any(r => r.Code == currentStep.Role || r.Name == currentStep.Role))
            {
                return new Dictionary<bool, string> { { false, $"当前审批角色为【{currentStep.Role}】，您没有审批权限" } };
            }
            if (currentStep.Type == StepType.Circulate
                && !currentStep.AllowUsers.Any(u => u.Id == user.Id))
            {
                // 传阅节点仅允许指定传阅人处理
                return new Dictionary<bool, string> { { false, $"该传阅节点不属于您，您无法处理" } };
            }

            try
            {
                // 1. 更新当前节点状态为已处理
                currentStep.Status = ApprovalStatus.Approved;
                currentStep.Comments = string.IsNullOrEmpty(currentStep.Comments)
                            ? Comments
                            : $"{currentStep.Comments} - {currentStep.ApprovalTime?.ToString("yyyy-MM-dd HH:mm")} | {Comments}";
                currentStep.ApprovalTime = DateTime.Now;
                currentStep.ApproverCode = user.Code;
                currentStep.Approver = user;

                // 2. 更新流程当前步骤
                this.ApprovalProcess.StepNumber = currentStep.StepNumber;

                // 3. 查找下一个待处理节点（统一按序号向后找，自动衔接传阅/审批）
                var nextStep = this.ApprovalProcess.Steps
                    .OrderBy(s => s.StepNumber)
                    .FirstOrDefault(s => s.StepNumber > currentStep.StepNumber && s.Status == ApprovalStatus.Pending);

                if (nextStep != null)
                {
                    this.Status = DocumentStatus.Auditing;
                    // 区分节点类型显示提示
                    string nextRoleTip = nextStep.Type == StepType.Approval
                        ? $"下一步待{nextStep.Role}（{nextStep.Approver?.Name}）审批"
                        : $"下一步待{string.Join("、", nextStep.AllowUsers.Select(u => u.Name))}传阅";
                    this.Checker = nextStep.Type == StepType.Approval
                        ? nextStep.Approver?.Name
                        : string.Join("、", nextStep.AllowUsers.Select(u => u.Name));

                    var resultMsg = $"{(currentStep.Type == StepType.Approval ? "审批" : "传阅确认")}成功：{user.Name}，{nextRoleTip}";
                    return new Dictionary<bool, string> { { true, resultMsg } };
                }
                else
                {
                    // 无后续节点：所有流程（审批+传阅）全部完成
                    this.Status = DocumentStatus.Approved;
                    this.Checker = null;
                    string finishTip = currentStep.Type == StepType.Approval
                        ? "采购单已全部通过审批"
                        : "所有审批和传阅已全部完成";
                    return new Dictionary<bool, string> { { true, $"处理完成：{user.Name}，{finishTip}" } };
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<bool, string> { { false, $"处理失败：{ex.Message}" } };
            }
            finally
            {
                this.Update();
            }
        }

        /// <summary>
        /// 审批拒绝
        /// </summary>
        /// <param name="rejectMode">拒绝模式：退回草稿/退回上一级</param>
        /// <param name="comment">拒绝原因</param>
        /// <param name="user">操作人，默认取当前登录用户</param>
        /// <returns></returns>
        public Dictionary<bool, string> Reject(User user, string comment, RejectMode rejectMode = RejectMode.ToDraft)
        {
            if (Status != DocumentStatus.Submitted && Status != DocumentStatus.Pending && Status != DocumentStatus.Auditing)
            {
                return new Dictionary<bool, string> { { false, "只能拒绝【已提交】或【待审批】或【审批中】的采购申请单" } };
            }

            //user ??= UserContext.CurrentUser; 不获取当前用户，必须传入操作用户，避免审批人误操作导致单据被退回草稿无法找回
            if (user == null)
            {
                return new Dictionary<bool, string> { { false, "获取当前登录用户信息失败，操作无法继续，请登录" } };
            }

            if (this.ApprovalProcess == null || !this.ApprovalProcess.Steps.Any())
            {
                return new Dictionary<bool, string> { { false, "审批流程未定义，无法拒绝" } };
            }

            // 找到当前待审批的节点
            var currentStep = this.ApprovalProcess.Steps
                .OrderBy(s => s.StepNumber)
                .FirstOrDefault(s => s.Status == ApprovalStatus.Pending);

            if (currentStep == null)
            {
                return new Dictionary<bool, string> { { false, "当前没有待审批的节点" } };
            }

            // 权限校验
            if (!user.ApprovalRoles.Any(r => r.Code == currentStep.Role || r.Name == currentStep.Role))
            {
                return new Dictionary<bool, string> { { false, $"当前审批角色为【{currentStep.Role}】，您没有拒绝权限" } };
            }

            try
            {
                // 1. 先标记当前拒绝节点为已拒绝（作为历史保留）
                currentStep.Status = ApprovalStatus.Rejected;
                currentStep.Comments = string.IsNullOrEmpty(currentStep.Comments)
                            ? comment
                            : $"{currentStep.Comments} - {currentStep.ApprovalTime?.ToString("yyyy-MM-dd HH:mm")} | {comment}";
                currentStep.ApprovalTime = DateTime.Now;
                currentStep.ApproverCode = user.Code;
                currentStep.Approver = user;

                if (rejectMode == RejectMode.ToDraft)
                {
                    // ========== 可选：给所有历史已处理节点加退回标记 ==========

                    var historySteps = this.ApprovalProcess.Steps
                        .Where(s => s.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected)
                        .ToList();  //对原来对象的引用进行操作，直接修改原对象，无需重新赋值

                    historySteps.ForEach(s =>
                    {
                        s.Comments = string.IsNullOrEmpty(s.Comments)
                            ? "流程已退回，本节点为历史记录"
                            : $"{s.Comments} - {s.ApprovalTime?.ToString("yyyy-MM-dd HH:mm")} | 流程已退回，本节点为历史记录";

                    });
                    // ========== 优化：退回草稿逻辑（保留历史、删除未审批节点） ==========
                    // 筛选出所有未审批的审批节点（仅删除Type=Approval且Status=Pending的节点）
                    var pendingApprovalSteps = this.ApprovalProcess.Steps
                        .Where(s => s.Type == StepType.Approval && s.Status == ApprovalStatus.Pending)
                        .ToList();

                    // 从流程中移除未审批节点（SqlSugar UpdateNav会自动删除数据库中对应的关联记录）
                    foreach (var step in pendingApprovalSteps)
                    {
                        this.ApprovalProcess.Steps.Remove(step);
                    }

                    // 回滚状态
                    this.Status = DocumentStatus.Rejected;
                    this.Checker = null;
                    // StepNumber设置为最后一个已完成步骤的序号，保留历史进度痕迹
                    this.ApprovalProcess.StepNumber = this.ApprovalProcess.Steps
                        .Where(s => s.Status is ApprovalStatus.Approved or ApprovalStatus.Rejected)
                        .Max(s => (int?)s.StepNumber) ?? 0;

                    return new Dictionary<bool, string> { { true, $"已退回草稿：{currentStep.Role}（{user.Name}）拒绝了申请，原因：{comment}。已保留{this.ApprovalProcess.Steps.Count(s => s.Status != ApprovalStatus.Pending)}条历史审批记录" } };
                }
                else
                {
                    // ========== 模式2：退回上一级审批人（原有逻辑保持不变） ==========
                    // 找到上一级已通过的审批节点
                    var previousStep = this.ApprovalProcess.Steps
                        .OrderByDescending(s => s.StepNumber)
                        .FirstOrDefault(s => s.StepNumber < currentStep.StepNumber && s.Status == ApprovalStatus.Approved && s.Type == StepType.Approval);

                    if (previousStep == null)
                    {
                        // 没有上一级节点（当前是第一个审批节点），自动退回草稿
                        this.Status = DocumentStatus.Rejected;
                        this.Checker = null;
                        this.ApprovalProcess.StepNumber = 0;
                        return new Dictionary<bool, string> { { true, $"当前是第一个审批节点，已自动退回拒绝状态，原因：{comment}" } };
                    }
                    //// 上一级节点重置为待审批状态
                    //previousStep.Status = ApprovalStatus.Pending;
                    //previousStep.Comments += $" - {previousStep.ApprovalTime?.ToString("yyyy-MM-dd HH:mm")} | 被下级【{currentStep.Role}】退回，原因：{comment}";
                    //previousStep.ApprovalTime = null;

                    //// 回退步骤号到上一级
                    //this.ApprovalProcess.StepNumber = previousStep.StepNumber;
                    //this.Status = DocumentStatus.Auditing;
                    //this.Checker = previousStep.Approver?.Name;
                    //原来是不修改审批节点，直接修改节点状态

                    // ✅ 核心修改：不修改原上一级节点的状态，新增待审批节点
                    // 取出所有节点按原顺序排序 处理后续节点
                    var allSteps = this.ApprovalProcess.Steps.OrderBy(s => s.StepNumber).ToList();
                    // 找到当前拒绝节点在列表中的位置
                    int currentIndex = allSteps.FindIndex(s => s.StepNumber == currentStep.StepNumber);
                    // 分离出当前节点之后的原有后续节点（需要更新StepNumber）
                    var afterSteps = allSteps.Skip(currentIndex + 1).ToList();
                    // ✅ 核心更新：遍历后续原有节点，统一递增StepNumber（因为插入了2个新节点，所以每个后续节点+2）
                    foreach (var step in afterSteps)
                    {
                        step.StepNumber += 2;
                    }


                    var stepList = this.ApprovalProcess.Steps.OrderBy(s => s.StepNumber).ToList();
                    //int insertBaseIndex = stepList.FindIndex(s => s.StepNumber == currentStep.StepNumber);

                    // ✅ 新增1：给原上级新增待审批节点，原有原上级节点保留已通过状态作为历史

                    int newPrevStepNumber = currentStep.StepNumber+1;
                    var newPrevStep = new ApprovalStep
                    {
                        StepNumber = newPrevStepNumber,
                        Role = previousStep.Role,
                        Type = StepType.Approval,
                        Status = ApprovalStatus.Pending,
                        ApproverCode = previousStep.ApproverCode,
                        Approver = previousStep.Approver,
                        Comments = $"原[{previousStep.StepNumber}:{previousStep.Role}]已审批，被[{currentStep.StepNumber}:{currentStep.Role}]（{user.Name}）退回，重新审批，退回原因：{comment}"
                    };
                    stepList.Insert(currentIndex + 1, newPrevStep);

                    // ✅ 新增2：给当前拒绝角色新增待复核节点，原有当前节点保留已拒绝状态作为历史
                    int newCurrStepNumber = currentStep.StepNumber + 2;
                    var newCurrStep = new ApprovalStep
                    {
                        StepNumber = newCurrStepNumber,
                        Role = currentStep.Role,
                        Type = StepType.Approval,
                        Status = ApprovalStatus.Pending,
                        ApproverCode = currentStep.ApproverCode,
                        Approver = currentStep.Approver,
                        Comments = $"原[{currentStep.StepNumber}:{currentStep.Role}]已拒绝，等待[{newPrevStep.StepNumber}:{newPrevStep.Role}]重新审批后，再次复核"
                    };
                    stepList.Insert(currentIndex + 2, newCurrStep);

                    // 更新流程和单据状态
                    this.ApprovalProcess.Steps = stepList;
                    this.ApprovalProcess.StepNumber = newPrevStepNumber;
                    this.Status = DocumentStatus.Auditing;
                    this.Checker = newPrevStep.Approver?.Name;

                    return new Dictionary<bool, string> { { true, $"已退回上一级：{currentStep.Role}（{user.Name}）拒绝了申请，已退回给{previousStep.Role}（{previousStep.Approver?.Name}）重新审批，原因：{comment}" } };
                }
            }
            catch (Exception ex)
            {
                return new Dictionary<bool, string> { { false, $"拒绝失败：{ex.Message}" } };
            }
            finally
            {
                this.Update();
            }
        }

        public class ApprovalResult
        {
            public bool Success { get; }
            public string Message { get; }

            public ApprovalResult(bool success, string message)
            {
                Success = success;
                Message = message;
            }
        }

        public string GetStatusDisplayName()
        {
            string statusDisplayName = (this.Status ?? DocumentStatus.Draft) switch
            {
                DocumentStatus.Draft => "未提交",
                DocumentStatus.Submitted => "已提交",
                DocumentStatus.Pending => "待审批",
                DocumentStatus.Auditing => "审批中",
                DocumentStatus.Approved => "已批准",
                DocumentStatus.Rejected => "已拒绝",
                DocumentStatus.Completed => "已完成",
                DocumentStatus.Cancelled => "已取消",
                _ => this.Status.ToString() // 如果Status为null，则上面的空合并运算符已经处理，所以这里实际上不会执行到null的情况
            };
            return statusDisplayName;
        }

        public List<PurchaseOrder> CreatPurchaseOrder()
        {
            string[] companyCodes = new string[] { };
            List<PurchaseOrder> purchaseOrders = new List<PurchaseOrder>(); //按公司代码分组生成多个采购订单

            foreach (PRInvInOut prInvInOut in this.PRInvInOuts)
            {
                string companyCode = prInvInOut.CompanyCode ?? "";

                if (purchaseOrders.Count == 0)
                {
                    PurchaseOrder po = new PurchaseOrder()
                    {
                        PRTrxGroupCode = this.PRTrxGroupCode,
                        DepartmentCode = this.DepartmentCode,
                        EmployeeCode = this.EmployeeCode,
                        CompanyCode = companyCode,
                        Explanation = this.Explanation,
                        Operator = this.Operator,
                        Checker = this.Checker,
                        Poster = this.Poster,
                        POInvInOuts = new List<POInvInOut>()
                    };
                    companyCodes = companyCodes.Append(companyCode).Distinct().ToArray();
                    purchaseOrders.Add(po);
                }

                for (int i = 0; i < purchaseOrders.Count; i++)
                {
                    //PurchaseOrder purchaseOrder1 = purchaseOrders[i];
                    if (purchaseOrders[i].CompanyCode == companyCode)
                    {
                        //POInvInOut poInvInOut = PRInvInOutToPO(prInvInOut);
                        purchaseOrders[i].POInvInOuts.Add(PRInvInOutToPO(prInvInOut));
                        break;
                    }
                    else
                    {
                        bool exists = Array.Exists(companyCodes, element => element == companyCode);
                        if (exists) continue;
                        PurchaseOrder po = new PurchaseOrder()
                        {
                            PRTrxGroupCode = this.PRTrxGroupCode,
                            DepartmentCode = this.DepartmentCode,
                            EmployeeCode = this.EmployeeCode,
                            CompanyCode = companyCode,
                            Explanation = this.Explanation,
                            Operator = this.Operator,
                            Checker = this.Checker,
                            Poster = this.Poster,
                            POInvInOuts = new List<POInvInOut>()
                        };
                        companyCodes = companyCodes.Append(companyCode).Distinct().ToArray();
                        //POInvInOut poInvInOut = PRInvInOutToPO(prInvInOut);
                        po.POInvInOuts.Add(PRInvInOutToPO(prInvInOut));
                        purchaseOrders.Add(po);
                    }

                }
            }
            return purchaseOrders;
        }
    }

    /// <summary>
    /// 采购退货单存货明细
    /// </summary>
    public class POInvInOut : InvInOut
    {
        public string? POCode { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        //public string? CompanyCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        //public Company Company { set; get; } = new OneToOneInitializer<Company>();
    }

    //采购订单 正式采购 采购员同步发送到供应商
    public class PurchaseOrder : BaseDocument
    {
        [Display(Name = "采购类型")]
        public string? PRTrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(PRTrxGroupCode))]//一对一 
        public PoTrxGroup PoTrxGroup { set; get; } = new OneToOneInitializer<PoTrxGroup>();
        //public string? WarehouseCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        //public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();

        public string? Operator { set; get; }
        public string? Checker { set; get; }
        public string? Poster { set; get; }

        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;

        [Navigate(NavigateType.OneToMany, nameof(POInvInOut.POCode))]//一对多
        public List<POInvInOut>? POInvInOuts { set; get; }

        [SugarColumn(IsIgnore = true)]
        public string DepartmentName => Department?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string CompanyName => Company?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string EmployeeName => Employee?.Name ?? string.Empty;

        public PurchaseOrder(){ }
        public PurchaseOrder(string str)
        {
            GetTrxNo<PurchaseOrder>("PO");
            GetCode("PO");
            if (POInvInOuts == null) POInvInOuts = new List<POInvInOut>();
        }

        // 插入方法
        public void Insert()
        {
            this.GetTrxNo<PurchaseOrder>("PO");
            SqlClient SSD = new();
            SSD.Db.InsertNav(this).Include(z => z.POInvInOuts).ExecuteCommand();
        }

        // 更新方法（使用导航更新）
        public void Update()
        {
            SqlClient SSD = new();
            SSD.Db.UpdateNav(this)
                    .Include(z => z.POInvInOuts) // 更新明细
                    .ExecuteCommand();
        }

        // 删除方法（逻辑删除，将Active设置为false）
        public void Delete()
        {
            this.Active = false;
            SqlClient SSD = new();
            SSD.Db.UpdateNav(this)
                    .Include(z => z.POInvInOuts) // 更新明细
                    .ExecuteCommand();
        }

        // 查询单个采购订单（根据主键Code）
        public static PurchaseOrder Get(string code)
        {
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<PurchaseOrder>()
                    .Includes(x => x.POInvInOuts) // 加载明细
                    .Includes(x => x.PoTrxGroup)  // 加载采购类型
                    .Includes(x => x.Department)  // 加载部门
                    .Includes(x => x.Employee)    // 加载员工
                    .Includes(x => x.Company)     // 加载公司
                    .First(x => x.Code == code);
            }
        }

        // 查询采购订单列表（根据条件）
        public static List<PurchaseOrder> GetList(Expression<Func<PurchaseOrder, bool>> whereExpression)
        {
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<PurchaseOrder>()
                    .Includes(x => x.POInvInOuts)
                    .Where(whereExpression)
                    .ToList();
            }
        }

        // 更新金额（重新计算总数量、总金额等）
        public void UpdateAmount()
        {
            Quantity = 0;
            Amount = 0;
            AmountIncTax = 0;
            if (POInvInOuts != null && POInvInOuts.Count > 0)
            {
                foreach (POInvInOut poInvInOut in POInvInOuts)
                {
                    Quantity += poInvInOut.Quantity;
                    Amount += poInvInOut.Amount;
                    AmountIncTax += poInvInOut.AmountIncTax;
                }
            }
        }

        // 创建收货单（待实现）
        public ReceivingNote CreateReceivingNote()
        {
            // 1. 创建收货单主单
            var receivingNote = new ReceivingNote("")
            {
                // 基础信息
                //Code = GenerateReceivingNoteCode(), // 生成收货单号
                Date = DateOnly.FromDateTime(DateTime.Now),
                Explanation = this.Explanation,
                //Note = this.Note,
                Active = true,
                FiscalYear = DateTime.Now.Year,
                Period = DateTime.Now.Month,
                InsertTime = DateTime.Now,
                LastUpdateTime = DateTime.Now,

                // 关联采购订单信息
                POTrxGroupCode = this.PRTrxGroupCode,
                CompanyCode = this.CompanyCode,
                DepartmentCode = this.DepartmentCode,
                EmployeeCode = this.EmployeeCode,
                Operator = this.Operator,
                Checker = null, // 初始未审核
                Poster = null,  // 初始未过账

                // 金额相关（初始为0，后续收货时更新）
                Quantity = 0,
                Amount = 0,
                //TaxRate = this.TaxRate, // 假设税率相同
                AmountIncTax = 0
            };

            // 2. 创建收货单明细
            receivingNote.RNInvInOuts = new List<RNInvInOut>();
            if (this.POInvInOuts != null)
            {
                foreach (var poItem in this.POInvInOuts)
                {
                    var rnItem = new RNInvInOut
                    {
                        // 基础信息
                        Id = SnowFlakeSingle.Instance.NextId(), // 生成明细编号
                        Active = true,
                        // 物料信息
                        InvCode = poItem.InvCode,
                        InventoryItem = poItem.InventoryItem,
                        SKU = poItem.SKU,
                        Specification = poItem.Specification,
                        Quantity = poItem.Quantity, // 订单数量
                        ReceivedQuantity = 0,       // 初始收货数量为0
                        Price = poItem.Price,
                        Amount = poItem.Amount,
                        TaxRate = poItem.TaxRate,
                        PriceIncTax = poItem.PriceIncTax,
                        AmountIncTax = poItem.AmountIncTax,
                        Sequence = poItem.Sequence,
                        Note = poItem.Note,
                        // 关联信息
                        RNCode = receivingNote.Code, // 关联主单
                        WarehouseCode = poItem.WarehouseCode, // 仓库相同
                                                              // 其他字段根据业务需要复制...
                    };
                    receivingNote.RNInvInOuts.Add(rnItem);
        }
            }
            // 3. 更新收货单主单的汇总信息
            receivingNote.UpdateAmount();

            return receivingNote;
    }
    }


    public class RNInvInOut : InvInOut
    {
        public string? RNCode { set; get; }
        public string? WarehouseCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        [Column(TypeName = "decimal(12,2)")]
        public decimal ReceivedQuantity { set; get; }  //实际收货数量
                                                       //public string? CompanyCode { set; get; }
                                                       //[Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
                                                       //public Company Company { set; get; } = new OneToOneInitializer<Company>();

        public void ReceivedQuantityChange()
        {
            //this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.ReceivedQuantity;
            this.AmountIncTax = this.PriceIncTax * this.ReceivedQuantity;
        }
    }
    //采购收货单 供应商确认后自动生成，或者由采购申请单直接生成 记录实际收货情况
    public class ReceivingNote : BaseDocument
    {
        public string POTrxGroupCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(POTrxGroupCode))]//一对一 
        public PoTrxGroup PoTrxGroup { set; get; } = new OneToOneInitializer<PoTrxGroup>();
        //public int TrxType { set; get; }
        //public string? WarehouseCode { set; get; }
        //[Navigate(NavigateType.OneToOne, nameof(WarehouseCode))]//一对一
        //public Warehouse Warehouse { set; get; } = new OneToOneInitializer<Warehouse>();
        public string? DepartmentCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(DepartmentCode))]//一对一
        public Department Department { set; get; } = new OneToOneInitializer<Department>();
        public string? CompanyCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(CompanyCode))]//一对一
        public Company Company { set; get; } = new OneToOneInitializer<Company>();
        public string? EmployeeCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
        public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
        public string? Explanation { set; get; }
        public DateOnly Date { set; get; } = DateOnly.FromDateTime(DateTime.Now);
        public decimal Quantity { set; get; } = 0;
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { set; get; } = 0;
        [Column(TypeName = "decimal(4,2)")]
        public decimal TaxRate { set; get; } = 0;
        [Display(Name = "含税金额")]
        public decimal AmountIncTax { set; get; } = 0;
        public string? Note { set; get; }
        public string? Operator { set; get; }
        public string? Checker { set; get; }
        public string? Poster { set; get; }

        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public int? FiscalYear { set; get; }
        public int? Period { set; get; }

        public DateTime InsertTime { set; get; } = DateTime.Now;
        /// <summary>
        /// 修改时间
        /// </summary>
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;

        [Navigate(NavigateType.OneToMany, nameof(RNInvInOut.RNCode))]//一对多
        public List<RNInvInOut>? RNInvInOuts { set; get; }

        [SugarColumn(IsIgnore = true)]
        public string DepartmentName => Department?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string CompanyName => Company?.Name ?? string.Empty;

        [SugarColumn(IsIgnore = true)]
        public string EmployeeName => Employee?.Name ?? string.Empty;

        public ReceivingNote() { }
        public ReceivingNote(string str)
        {
            FiscalYear = Date.Year;
            Period = Date.Month;
            GetTrxNo<ReceivingNote>("RN");
            GetCode("RN");
            if (RNInvInOuts == null) RNInvInOuts = new List<RNInvInOut>();
        }
        public void Insert()
        {
            this.GetTrxNo<ReceivingNote>("RN");
            SqlClient SSD = new();
            SSD.Db.InsertNav(this).Include(z => z.RNInvInOuts).ExecuteCommand();
    }

        // 更新方法（使用导航更新）
        public void Update()
    {
            SqlClient SSD = new();
            SSD.Db.UpdateNav(this).Include(z => z.RNInvInOuts) // 更新明细
                    .ExecuteCommand();
        }

        // 删除方法（逻辑删除，将Active设置为false）
        public void Delete()
        {
            this.Active = false;
            SqlClient SSD = new();
            SSD.Db.UpdateNav(this)
                    .Include(z => z.RNInvInOuts) // 更新明细
                    .ExecuteCommand();
        }

        // 查询单个采购订单（根据主键Code）
        public static ReceivingNote Get(string code)
        {
            using (var db = new SqlClient().Db)
            {
                return db.Queryable<ReceivingNote>()
                    .Includes(x => x.RNInvInOuts) // 加载明细
                    .Includes(x => x.PoTrxGroup)  // 加载采购类型
                    .Includes(x => x.Department)  // 加载部门
                    .Includes(x => x.Employee)    // 加载员工
                    .Includes(x => x.Company)     // 加载公司
                    .First(x => x.Code == code);
            }
        }

        // 查询采购订单列表（根据条件）
        public static List<PurchaseOrder> GetList(Expression<Func<PurchaseOrder, bool>> whereExpression)
        {
            using (var db = new SqlClient().Db)
        {
                return db.Queryable<PurchaseOrder>()
                    .Includes(x => x.POInvInOuts)
                    .Where(whereExpression)
                    .ToList();
            }
        }
        public void UpdateAmount()
        {
            Quantity = 0;
            Amount = 0;
            AmountIncTax = 0;
            if (RNInvInOuts != null)
            {
                foreach (var item in RNInvInOuts)
        {
                    Quantity += item.Quantity;
                    Amount += item.Amount;
                    AmountIncTax += item.AmountIncTax;
                }
            }
        }
        //生成入库单
        public List<InventoryTransaction> ConvertToInventoryTransactions()
        {
            // 1. 按仓库分组明细
            var warehouseGroups = this.RNInvInOuts?
                .Where(rn => rn.ReceivedQuantity > 0) // 只处理实际收货的明细
                .GroupBy(rn => rn.WarehouseCode)
                .ToList() ?? new List<IGrouping<string?, RNInvInOut>>();

            var inventoryTransactions = new List<InventoryTransaction>();

            // 2. 为每个仓库创建独立的入库单
            foreach (var warehouseGroup in warehouseGroups)
            {
                string warehouseCode = warehouseGroup.Key ?? "DEFAULT";

                // 创建入库单主单
                var inventoryTransaction = new InventoryTransaction
                {
                    Code = GenerateInventoryTransactionCode(warehouseCode),
                    Status = DocumentStatus.Draft,
                    TrxType = 1, // 入库类型
                    TrxGroupCode = "I02",   //默认采购入库，可以根据业务需要调整
                    WarehouseCode = warehouseCode,
                    DepartmentCode = this.DepartmentCode,
                    CompanyCode = this.CompanyCode,
                    EmployeeCode = this.EmployeeCode,
                    Explanation = $"由收货单 {this.Code} 生成（仓库: {warehouseCode})",
                    Note = this.Note,
                    Operator = this.Operator,
                    InsertUser = UserContext.CurrentUser?.Name ?? "System",
                    InsertTime = DateTime.Now,
                    UpdateTime = DateTime.Now
                };

                // 3. 添加当前仓库的明细
                inventoryTransaction.ITInvInOuts = new List<ITInvInOut>();
                foreach (var rnItem in warehouseGroup)
                {
                    var invItem = new ITInvInOut
        {
                        InvCode = rnItem.InvCode,
                        InventoryItem = rnItem.InventoryItem,
                        SKU = rnItem.SKU,
                        Specification = rnItem.Specification,
                        Quantity = rnItem.ReceivedQuantity,
                        Price = rnItem.Price,
                        Amount = rnItem.Price * rnItem.ReceivedQuantity,
                        TaxRate = rnItem.TaxRate,
                        PriceIncTax = rnItem.PriceIncTax,
                        AmountIncTax = rnItem.PriceIncTax * rnItem.ReceivedQuantity,
                        IcCode = inventoryTransaction.Code
                    };
                    inventoryTransaction.ITInvInOuts.Add(invItem);
        }

                // 4. 更新当前入库单的汇总信息
                inventoryTransaction.UpdateAmount();
                inventoryTransactions.Add(inventoryTransaction);
            }
            return inventoryTransactions;
        }

        // 生成带仓库标识的入库单号
        private string GenerateInventoryTransactionCode(string warehouseCode)
        {
            // 格式: IT-WH001-20240320153045
            string warehousePart = string.IsNullOrEmpty(warehouseCode)
                ? "WH"
                : warehouseCode.Replace(" ", "").Substring(0, Math.Min(5, warehouseCode.Length));

            return $"IT-{warehousePart}-{DateTime.Now:yyyyMMddHHmmss}";
        }

        // 入库流程增强
        public void CompleteReceiving()
        {
            if (this.Status != DocumentStatus.Approved)
                throw new InvalidOperationException("仅已审核的收货单可完成入库");

            using (var scope = new TransactionScope())
        {
                // 生成按仓库分组的入库单
                var inventoryTransactions = this.ConvertToInventoryTransactions();

                foreach (var transaction in inventoryTransactions)
            {
                    // 设置状态并保存
                    transaction.Status = DocumentStatus.Completed;
                    transaction.Insert();

                    // 更新库存
                    InventoryBalance.UpdateBalance(new List<InventoryTransaction> { transaction });
            }

                // 更新收货单状态
                this.Status = DocumentStatus.Completed;
                this.Update();

                scope.Complete();
        }

            // 记录日志
            //AuditLog.Log($"收货单 {this.Code} 已完成入库，生成 {inventoryTransactions.Count} 个入库单");
        }

        public void UpdateAmount()
        {
            this.Amount = this.Quantity * this.Price;
            if (this.TaxRate == null) this.TaxRate = 0;
            this.PriceIncTax = this.Price * (1 + this.TaxRate);
            this.AmountIncTax = this.Quantity * this.PriceIncTax;
        }
    }    */
}
