using ERP_Web.Repository;
using ERP_Web.Models.PrivilegeHub;
using NPOI.SS.Formula.Functions;
using SqlSugar;
using System.Runtime.Intrinsics.X86;

namespace ERP_Web.Models
{
    // 购物车项模型 (单据行)
    public class CartItem : InvInOut// 考虑重命名为 ShoppingCartItem
    {
        public string ShoppingCartCode { get; set; } // 关联到 ShoppingCart 单据
    }
    public class ShoppingCart
    {
        public long Id { set; get; }
        [SugarColumn(IsPrimaryKey = true)]
        public string? Code { set; get; }
        public string? UserCode { set; get; }
        [Navigate(NavigateType.OneToOne, nameof(UserCode))] // 一对一
        public User User { set; get; } = new OneToOneInitializer<User>();
        public string? Explanation { set; get; }
        public DateOnly Date { set; get; } = DateOnly.FromDateTime(DateTime.Now);
        public string? Operator { set; get; }
        public DateTime InsertTime { set; get; } = DateTime.Now;
        public DateTime UpdateTime { set; get; } = DateTime.Now;

        // 购物车明细行 (一对多)
        [Navigate(NavigateType.OneToMany, nameof(CartItem.ShoppingCartCode))]
        public List<CartItem> Items { get; set; }

        public decimal TotalQuantity => Items.Sum(i => i.Quantity);

        public decimal TotalAmount => Items.Sum(i => i.Amount);

        public ShoppingCart()
        { 
        }

        public ShoppingCart(User user)
        {
            Id = SnowFlakeSingle.Instance.NextId();
            User = user;
            UserCode = user.Code;
            Code = "SC" + Id.ToString() ?? "no001";
            Items = new();
        }


        public void Insert()
        {
            SqlClient SSC = new();
            SSC.Db.InsertNav(this).Include(xx => xx.Items).ExecuteCommand();
        }

        public void InsertOrUpdate()
        {
            SqlClient SSC = new();
            ShoppingCart existingCart = SSC.Db.Queryable<ShoppingCart>()
                                        .Includes(xx=>xx.Items)
                                        .Where(sc => sc.Code == this.Code)
                                        .First();
            if (existingCart != null) {
                this.Update();
            }
            else {
                this.Insert();
            }
        }

        // 更新购物车
        public void Update()
        {
            SqlClient SSC = new();
            this.UpdateTime = DateTime.Now;
            SSC.Db.UpdateNav(this).Include(xx => xx.Items).ExecuteCommand();
        }

        // 删除购物车
        public void Delete(string cartCode)
        {
            SqlClient SSC = new();

            SSC.Db.Deleteable<ShoppingCart>()
                .Where(sc => sc.Code == cartCode)
                .ExecuteCommand();
        }

        // 获取用户购物车（带明细）
        public static ShoppingCart GetUserCart(string userCode)
        {
            SqlClient SSC = new();
            var res = SSC.Db.Queryable<ShoppingCart>()
                    .IncludesAllFirstLayer()
                    .IncludesAllSecondLayer(xx=>xx.Items)
                    .Includes(xx => xx.Items, xi => xi.Specification)
                    .Includes(xx => xx.Items, xi => xi.InventoryItem, xs => xs.Specifications)
                    .Where(sc => sc.UserCode == userCode)
                    .First();
            if (res == null) {
                res = new ShoppingCart(User.SelectByAccount(userCode));
            }
            return res;
        }

        // 添加商品到购物车
        public void AddItem(string cartCode, CartItem item)
        {
            SqlClient SSC = new();
            var cart = SSC.Db.Queryable<ShoppingCart>()
                        .IncludesAllFirstLayer()
                        .First(sc => sc.Code == cartCode);
            
            cart.Items.Add(item);
            cart.UpdateTime = DateTime.Now;
            SSC.Db.Updateable(cart).ExecuteCommand();
        }

        // 从购物车移除商品
        public void RemoveItem(long itemId)
        {
            SqlClient SSC = new();
            SSC.Db.Deleteable<CartItem>()
                      .Where(ci => ci.ShoppingCartCode == this.Code && ci.Id == itemId)
                      .ExecuteCommand();
        }

    }

    /// <summary>
    /// 购物车存货明细
    /// </summary>
    //public class ShoppingCart : InvInOut
    //{
    //    public string? SCCode { set; get; }
    //    public string? EmployeeCode { set; get; }
    //    [Navigate(NavigateType.OneToOne, nameof(EmployeeCode))]//一对一
    //    public Employee Employee { set; get; } = new OneToOneInitializer<Employee>();
    //    public string? Explanation { set; get; }
    //    public DateOnly Date { set; get; } = DateOnly.FromDateTime(DateTime.Now);
    //    public string? Operator { set; get; }
    //    public bool? Active { set; get; }  // 1: Active, 0: Inactive null: 作为调拨单有效但是不显示在界面，计算时计算调拨
    //    public DateTime InsertTime { set; get; } = DateTime.Now;
    //    public DateTime LastUpdateTime { set; get; } = DateTime.Now;
    //}

}
