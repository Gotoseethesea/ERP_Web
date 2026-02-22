using ERP_Web.Models.PrivilegeHub;
using ERP_Web.Repository;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq.Expressions;
using System.Text.Json.Serialization;
using static Dm.net.buffer.ByteArrayBuffer;

namespace ERP_Web.Models
{
    enum InventoryCostingMethodEnum
    {
        FIFO, //先进先出法
        LIFO, //后进先出法
        AverageCost //加权平均法
    }

    enum InventoryValuationMethodEnum
    {
        StandardCost, //标准成本法
        ActualCost //实际成本法
    }

    public enum StepType
    {
        Submit = 0,      // 提交操作记录（新增）
        Approval = 1,   // 审批步骤
        CC = 2,       // 抄送步骤
        Circulate = 3 // 新增：传阅节点
    }

    /// <summary>
    /// 审批拒绝模式
    /// </summary>
    public enum RejectMode
    {
        /// <summary>
        /// 直接退回草稿，流程终止
        /// </summary>
        ToDraft = 0,
        /// <summary>
        /// 退回上一级审批人，重新走审批
        /// </summary>
        ToPrevious = 1
    }

    public enum ApprovalStatus
    {
        /// <summary>
        /// 待审批
        /// </summary>
        Pending = 0,    // 待审批
        /// <summary>
        /// 已批准
        /// </summary>
        Approved = 1,   // 已批准
        /// <summary>
        /// 已拒绝
        /// </summary>
        Rejected = 2,   // 已拒绝
        /// <summary>
        /// 已退回（需要重新提交）
        /// </summary>
        Returned = 3    // 已退回（需要重新提交）
    }

    /// <summary>
    /// 单据状态
    /// </summary>
    public enum DocumentStatus
    {
        /// <summary>
        /// 单据状态-未提交
        /// </summary>
        Draft,      //未提交
        /// <summary>
        /// 单据状态-已提交
        /// </summary>
        Submitted,  //已提交

        /// <summary>
        /// 单据状态-待审批
        /// </summary>
        Pending, //待审批
        /// <summary>
        /// 单据状态-审核中
        /// </summary>
        Auditing,    //审核中
        /// <summary>
        /// 单据状态-已审核
        /// </summary>
        Approved,   //已审核
        /// <summary>
        /// 单据状态-已完成
        /// </summary>
        Completed,  //已完成
        /// <summary>
        /// 单据状态-已过账
        /// </summary>
        Posted,      //已过账
        /// <summary>
        /// 单据状态-已拒绝
        /// </summary>
        Rejected,   //已拒绝
        /// <summary>
        /// 单据状态-已取消
        /// </summary>
        Cancelled,   //已取消
        /// <summary>
        /// 单据状态-已作废
        /// </summary>
        Voided      //已作废
    }

    /// <summary>
    /// 单据类型枚举
    /// </summary>
    public enum DocumentType
    {
        PurchaseRequisition = 1,   // 采购申请单
        ExpenseReimbursement = 2,  // 费用报销单
        LeaveRequest = 3,          // 请假申请单
                                   // 可以根据实际业务扩展
    }

    /// <summary>
    /// 存货收发明细基类
    /// </summary>
    public class InvInOut
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { set; get; }
        public string InvCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(InvCode))]//一对一
        public InventoryItem InventoryItem { set; get; } = new OneToOneInitializer<InventoryItem>();
        public string? SKU { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(SKU))]//一对一
        public Specification Specification { set; get; } = new OneToOneInitializer<Specification>();
        [Column(TypeName = "decimal(18,2)")]
        public decimal Quantity { set; get; } = 0;
        [Column(TypeName = "decimal(20,10)")]
        public decimal Price { set; get; } = 0;
        [Column(TypeName = "decimal(22,10)")]
        public decimal Amount { set; get; }
        [Column(TypeName = "decimal(4,2)")]
        public decimal TaxRate { set; get; } = 0;
        [Column(TypeName = "decimal(20,10)")]
        public decimal PriceIncTax { set; get; } = 0;
        [Column(TypeName = "decimal(22,10)")]
        public decimal AmountIncTax { set; get; }
        public string? Note { set; get; }
        public int? Sequence { set; get; }
        public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime UpdateTime { set; get; } = DateTime.Now;

        public InvInOut()
        {
        }
        public InvInOut(string st)
        {
            Id = SnowFlakeSingle.Instance.NextId();
            Quantity = 0;
            Price = 0;
            Amount = 0;
            TaxRate = 0;
            PriceIncTax = 0;
            AmountIncTax = 0;
            InvCode = string.Empty;
        }
        public void QuantityChange()
        {
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }
        public void PriceChange()
        {
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }
        public void PriceIncTaxChange()
        {
            this.Price = this.PriceIncTax / (1 + this.TaxRate);
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }

        public void TaxRateChange()
        {
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.Amount = this.Price * this.Quantity;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }

        public void AmountChange()
        {
            this.Price = this.Amount / this.Quantity;
            this.PriceIncTax = (1 + this.TaxRate) * this.Price;
            this.AmountIncTax = this.PriceIncTax * this.Quantity;
        }

        public void AmountIncTaxChange()
        {
            this.PriceIncTax = this.AmountIncTax / this.Quantity;
            this.Price = this.PriceIncTax / (1 + this.TaxRate);
            this.Amount = this.Price * this.Quantity;
        }

        //public void InvTrxUpdateCode(int Code)
        //{
        //    this.IcCode = Code;
        //    if (this.Inv != null)
        //    {
        //        this.InvCode = this.Inv.Code;
        //    }
        //}

        public void UpdatePriceByIn()
        {
            SqlClient SSC = new SqlClient();
            InventoryBalance invBalOld = SSC.Db.Queryable<InventoryBalance>()
                .IncludesAllFirstLayer()
                .Where(xx => xx.InvCode == this.InvCode)
                .First();
            if (invBalOld == null) return;
            this.Price = invBalOld.Price;
            PriceChange();
        }
        public void UpdateAmount()
        {
            this.Amount = this.Quantity * this.Price;
            if (this.TaxRate == null) this.TaxRate = 0;
            this.PriceIncTax = this.Price * (1 + this.TaxRate);
            this.AmountIncTax = this.Quantity * this.PriceIncTax;
        }
    }

    /// <summary>
    /// 单据基类
    /// </summary>
    /// 
    public class BaseDocument
    {
        /// <summary>
        /// 单据Id（唯一标识）
        /// </summary>
        public long Id { get; set; }
        /// <summary>
        /// 单据编号（如PO2023001、MAT2023001）
        /// </summary>
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; } = string.Empty;
        /// <summary>
        /// 单号给用户查看，规则待定 字母+年月+当月序号  PR单：PR260100001
        /// </summary>
        public string? TrxNo { set; get; }
        /// <summary>
        /// 单据日期
        /// </summary>
        public DateOnly Date { get; set; } = new DateOnly();
        /// <summary>
        /// 单据状态（如草稿、已审核、已执行）
        /// </summary>
        public DocumentStatus? Status { get; set; } = DocumentStatus.Draft;
        public decimal Quantity { set; get; } = 0;
        [Column(TypeName = "decimal(12,2)")]
        public decimal Amount { set; get; } = 0;
        //[Column(TypeName = "decimal(4,2)")]
        //public decimal TaxRate { set; get; } = 0;
        [Display(Name = "含税金额"), Column(TypeName = "decimal(12,2)")]
        public decimal AmountIncTax { set; get; } = 0;
        public string? Explanation { get; set; }
        [Navigate(NavigateType.OneToMany, nameof(SysAttachment.RefCode))]
        public List<SysAttachment>? Attachments { get; set; }
        public bool? Active { set; get; } = true;  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
        public int? FiscalYear { set; get; }
        public int? Period { set; get; }
        public string? InsertUser { get; set; }
        public DateTime InsertTime { get; set; } = DateTime.Now;
        public string? UpdateUser { get; set; }
        public DateTime? UpdateTime { get; set; } = DateTime.Now;

        public BaseDocument()
        {
            Id = SnowFlakeSingle.Instance.NextId();
            Date = DateOnly.FromDateTime(DateTime.Now);
            FiscalYear = DateTime.Now.Year;
            Period = DateTime.Now.Month;
        }
        public void DateUpdate()
        {
            FiscalYear = Date.Year;
            Period = Date.Month;
        }

        public void GetCode()
        {
            this.Code ="DB"+Id.ToString();
        }

        public void GetCode(string prefix)
        {
            this.Code = prefix == null?"DB": prefix + Id.ToString();
        }

        public void Delete()
        {
            this.Active = false;
            //SqlClient SSC = new SqlClient();
            //var result = SSC.Db.Updateable(this).ExecuteCommand();
        }

        //public void GetTrxNo<T>(string prefix = "DB") where T : class, IHaveTrxNo, new()
        //{
        //    // 1. 定义前缀（如PR）
        //    prefix ??= "DB"; // 空合并赋值
        //    // 2. 获取当前年月（格式：YYMM）
        //    var yearMonth = DateTime.Now.ToString("yyMM");

        //    // 3. 拼接当前年月前缀（如PR2601）
        //    var currentPrefix = $"{prefix}{yearMonth}";
        //    var seqLength = 5; // 序号位数（固定5位，不足补零）
        //    // 使用反射获取TrxNo属性
        //    var property = typeof(T).GetProperty("TrxNo");
        //    if (property == null)
        //        throw new Exception("类型T不包含TrxNo属性");
        //    // 构建表达式树：t => t.TrxNo
        //    var parameter = Expression.Parameter(typeof(T), "t");
        //    var propertyAccess = Expression.Property(parameter, property);
        //    var lambda = Expression.Lambda<Func<T, string>>(propertyAccess, parameter);

        //    SqlClient SSC = new SqlClient();
        //    // 4. 查询数据库中当前前缀的最大序号（需结合EF Core或SQL实现）
        //    var maxSeqStr = SSC.Db.Queryable<T>()
        //        .Where(t => t.TrxNo != null && t.TrxNo.StartsWith(currentPrefix))
        //        //.Select(t => t.TrxNo.Substring(currentPrefix.Length)) // 提取序号部分
        //        //.DefaultIfEmpty("0")
        //        //.Max();
        //        .Max<string>("TrxNo");
        //    maxSeqStr = maxSeqStr==null?"0": maxSeqStr.Replace(currentPrefix, ""); // 提取序号部分

        //    // 5. 生成新序号（+1后补零为5位）
        //    //var newSeq = (int.Parse(maxSeqStr) + 1).ToString("D5"); // D5表示5位，不足补零
        //    if (!int.TryParse(maxSeqStr, out int maxSeq)) maxSeq = 0; // 容错处理
        //    var newSeq = (maxSeq + 1).ToString($"D{seqLength}"); // 补零为5位：00001

        //    // 6. 最终单号
        //    this.TrxNo = $"{currentPrefix}{newSeq}";
        //    Console.WriteLine("TrxNo" + this.TrxNo.ToString());
        //}

        public void GetTrxNo<T>(string prefix = "DB") where T : class, new()
        {
            prefix ??= "DB";
            var yearMonth = DateTime.Now.ToString("yyMM");
            var currentPrefix = $"{prefix}{yearMonth}";
            const int seqLength = 5;

            SqlClient SSC = new SqlClient();

            // 方法1：使用动态表达式（推荐）
            var parameter = Expression.Parameter(typeof(T));
            var property = Expression.Property(parameter, "TrxNo");
            var lambda = Expression.Lambda<Func<T, string>>(property, parameter);

            string maxTrxNo = SSC.Db.Queryable<T>()
                .Where($"TrxNo IS NOT NULL AND TrxNo LIKE '{currentPrefix}%'")
                .Max(lambda);

            // 方法2：使用原始SQL（备选）
            // string maxTrxNo = SSC.Db.Ado.GetString(
            //    $"SELECT MAX(TrxNo) FROM {SSC.Db.EntityMaintenance.GetTableName(typeof(T))} WHERE TrxNo LIKE '{currentPrefix}%'");

            int maxSeq = 0;
            if (!string.IsNullOrEmpty(maxTrxNo))
            {
                // 精确提取序号部分（避免错误截取）
                string seqPart = maxTrxNo.Substring(currentPrefix.Length);

                // 安全转换（处理非数字字符）
                if (int.TryParse(seqPart, out int parsedSeq))
                {
                    maxSeq = parsedSeq;
                }
            }

            string newSeq = (maxSeq + 1).ToString($"D{seqLength}");
            this.TrxNo = $"{currentPrefix}{newSeq}";
            Console.WriteLine($"Generated TrxNo: {this.TrxNo}");
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
                DocumentStatus.Posted => "已过账",
                DocumentStatus.Voided => "已作废",
                _ => this.Status.ToString()
            };
            return statusDisplayName;
        }
    }
    /// <summary>
    /// 文档附件
    /// </summary>
    public class SysAttachment
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
        public string RefCode { get; set; } = string.Empty;
        public string RefType { get; set; }   // 关联类型 PurchaseOrder/Inventory等
        [SugarColumn(Length = 200)]
        public string? Name { get; set; }  // 原始文件名
        [SugarColumn(Length = 500)]
        public string Path { get; set; }      // 存储路径包含文件名
        [SugarColumn(Length = 20)]
        public string? Type { get; set; }  // pdf/png/docx等
        public long? Size { get; set; }    // 字节数
        public DateTime? UploadTime { get; set; } = DateTime.Now;    // 上传时间
    }

    /// <summary>
    /// 保存审批流程设置
    /// </summary>
    public class ApprovalType
    {
        public long Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; }
        public string Decscription{ get; set; }

        /// <summary>
        /// 单据类型（枚举，例如采购申请单、报销单等）
        /// </summary>
        [SugarColumn(ColumnDescription = "单据类型", IsNullable = false)]
        public DocumentType Type { get; set; }
        [Navigate(NavigateType.OneToMany, nameof(ApprovalNode.ApprovalTypeCode))]//一对多
        public List<ApprovalNode>? Nodes { get; set; }

        public ApprovalType()
        { 
        }

        public ApprovalType(string str)
        {
            Id = SnowFlakeSingle.Instance.NextId();
        }
    }
    
    public class ApprovalNode
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
        public string ApprovalTypeCode { get; set; }
        /// <summary>
        /// 审批步骤
        /// </summary>
        public int StepNumber { get; set; }

        /// <summary>
        /// 审批角色（如部门经理、财务主管等）
        /// </summary>
        public string Role { get; set; }

        public string? ApproverCode { get; set; }

        [Navigate(NavigateType.OneToOne, nameof(ApproverCode))]//一对一
        public User? Approver { get; set; }

        public string? Note { set; get; }
        public string? InsertUser { set; get; }
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime LastUpdateTime { set; get; } = DateTime.Now;
        public bool Active { set; get; } = true;

        public ApprovalNode()
        {

        }
        public ApprovalNode(string str)
        {
            //Id = SnowFlakeSingle.Instance.NextId();
        }
    }

    /// <summary>
    /// 单据审批实体类，用于记录单据的审批流程和状态
    /// </summary>
    public class ApprovalProcess
    {
        public long Id { get; set; }
        [SugarColumn(IsPrimaryKey = true)]
        public string Code { get; set; }
        /// <summary>
        /// 关联的业务单据ID（例如采购申请单ID）
        /// </summary>
        [SugarColumn(ColumnDescription = "关联业务单据Code", IsNullable = false)]
        public string ReferenceDocumentCode { get; set; }

        /// <summary>
        /// 单据类型（枚举，例如采购申请单、报销单等）
        /// </summary>
        [SugarColumn(ColumnDescription = "单据类型", IsNullable = false)]
        public DocumentType Type { get; set; }

        /// <summary>
        /// 当前审批步骤（从1开始）
        /// </summary>
        [SugarColumn(ColumnDescription = "审批步骤", IsNullable = false)]
        public int StepNumber { get; set; }

        [Navigate(NavigateType.OneToMany, nameof(ApprovalStep.ApprovalProcessCode))]//一对一
        public List<ApprovalStep>? Steps { get; set; }

        public ApprovalProcess() { }
        public ApprovalProcess(string str) {
            Id = SnowFlakeSingle.Instance.NextId();
            Code = "Pr"+Id.ToString();
            Steps = new();
        }
        public ApprovalProcess(User user, ApprovalType approvalType, string referenceDocumentCode)
        {
            // 基础信息初始化
            Id = SnowFlakeSingle.Instance.NextId();
            Code = referenceDocumentCode;  //$"Pr{Id}"; // 生成唯一流程编码
            ReferenceDocumentCode = referenceDocumentCode;
            Type = approvalType.Type; // 继承单据类型
            StepNumber = 1; // 初始步骤设为1（待审批状态）

            // 初始化审批步骤列表
            Steps = new List<ApprovalStep>();

            // 按步骤顺序处理审批节点
            if (approvalType.Nodes != null && approvalType.Nodes.Any())
            {
                // 按StepNumber排序确保顺序正确
                var sortedNodes = approvalType.Nodes
                    .OrderBy(n => n.StepNumber)
                    .ToList();
                Steps.Add(new ApprovalStep
                {
                    Id = SnowFlakeSingle.Instance.NextId(),
                    StepNumber = 0,
                    Role = "提交申请",
                    Approver = user,
                    ApproverCode = user.Code, // 关联审批人编码 方便查看，但是不作为审批判断
                    Type = StepType.Approval, // 默认为审批步骤
                    Status = ApprovalStatus.Approved, // 初始状态为待处理
                    Comments = string.Empty,
                    ApprovalTime = DateTime.MinValue // 未审批时使用最小时间
                });
                foreach (var node in sortedNodes)
                {
                    var approverUser = ApprovalStep.GetApproverUser(node);
                    Steps.Add(new ApprovalStep
                    {
                        Id = SnowFlakeSingle.Instance.NextId(),
                        StepNumber = node.StepNumber,
                        Role = node.Role,
                        Approver = approverUser,
                        ApproverCode = approverUser != null? approverUser.Code:null, // 关联审批人编码 方便查看，但是不作为审批判断
                        Type = StepType.Approval, // 默认为审批步骤
                        Status = ApprovalStatus.Pending, // 初始状态为待处理
                        Comments = string.Empty,
                        ApprovalTime = DateTime.MinValue // 未审批时使用最小时间
                    });
                }
            }
        }
    }

    public class ApprovalStep
    {
        [SugarColumn(IsPrimaryKey = true)]
        public long Id { get; set; }
    
        /// <summary>
        /// 关联的审批流程ID
        /// </summary>
        public string ApprovalProcessCode { get; set; }
        [Navigate(NavigateType.OneToOne, nameof(ApprovalProcessCode))] //获取审批
        public ApprovalProcess ApprovalProcess { get; set; }

        /// <summary>
        /// 审批步骤序号（从1开始）
        /// </summary>
        public int StepNumber { get; set; } = 0;

        /// <summary>
        /// 审批角色（如部门经理、财务主管等）
        /// </summary>
        public string? Role { get; set; }
        /// <summary>
        /// 审批人Code（关联用户表）
        /// </summary>
        public string? ApproverCode { get; set; }
        // 导航属性：审批人信息（可选，根据需求加载）
        [Navigate(NavigateType.OneToOne, nameof(ApproverCode))]
        public User? Approver { get; set; }
        /// <summary>节点类型：审批/传阅/抄送</summary>
        public StepType Type { get; set; }
        /// <summary>传阅节点：允许查看处理的用户列表</summary>
        public List<User> AllowUsers { get; set; } = new List<User>();

        /// <summary>
        /// 审批状态
        /// </summary>
        [SugarColumn(ColumnDescription = "审批状态", IsNullable = false)]
        public ApprovalStatus Status { get; set; } = ApprovalStatus.Pending;

        /// <summary>
        /// 审批意见
        /// </summary>
        [SugarColumn(ColumnDescription = "审批意见", Length = 500, IsNullable = true)]
        public string? Comments { get; set; }
        /// <summary>
        /// 审批时间
        /// </summary>
        [SugarColumn(ColumnDescription = "审批时间", IsNullable = false)]
        public DateTime? ApprovalTime { get; set; }

        public static User GetApproverUser(ApprovalNode node)
        {
            // 1. 优先使用直接指定的审批人编码
            if (!string.IsNullOrEmpty(node.ApproverCode))
            {
                var directUser = node.Approver;

                if (directUser != null)
                    return directUser;
            }

            // 2. 按审批角色匹配
            if (node.Role != null && node.Role.Length > 0)
            {

                List<User> users = User.Select();
                // 获取匹配所有指定角色的用户
                var roleUsers = users
                    .Where(u => u.Approval != null &&
                               u.Approval.Any() &&
                               u.Approval.Contains(node.Role))
                    .ToList();

                if (roleUsers.Count > 0)
                {
                    // 按优先级返回
                    return roleUsers
                        .OrderBy(u => u.Sequence ?? int.MaxValue) // 未设置优先级的放最后
                        .First();
                }
                else
                {
                    return null;
                }
            }

            // 3. 双重保障：返回默认审批人（如管理员）
            return null;
        }

    }
}
