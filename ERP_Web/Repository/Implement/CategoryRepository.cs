using ERP_Web.Models;

namespace ERP_Web.Repository.Implement
{
    public class CategoryRepository : ICategory
    {
        //private List<Models.Category> categories = new List<Models.Category>();
        //public CategoryRepository()
        //{
        //    categories = new List<Category>() {
        //        new Models.Category(){ Id=1, CategoryName = "电子产品", ParentId=0, CategoryPath = "" },
        //        new Models.Category(){ Id=11,CategoryName="手机", ParentId=1,CategoryPath=",1,"},
        //        new Models.Category(){ Id=12,CategoryName="音箱", ParentId=1,CategoryPath=",1,"},
        //        new Models.Category(){ Id=13,CategoryName="鼠标", ParentId=1,CategoryPath=",1,"},
        //        new Models.Category(){ Id=131,CategoryName="蓝牙鼠标", ParentId=13,CategoryPath=",1,13,"},
        //        new Models.Category(){ Id=14,CategoryName="电脑", ParentId=1,CategoryPath=",1,"},
        //        new Models.Category(){ Id = 2 ,CategoryName="生活用品", ParentId=0, CategoryPath="" },
        //         new Models.Category(){ Id=21,CategoryName="抽纸", ParentId=2,CategoryPath = ",2,"},
        //        new Models.Category(){ Id=22,CategoryName="牙签", ParentId=2 ,CategoryPath = ",2,"},
        //        new Models.Category(){ Id=3,CategoryName="食品", ParentId=0 ,CategoryPath = ""},
        //        new Models.Category(){ Id=31,CategoryName="粉", ParentId=3 ,CategoryPath = ",3,"},
        //        new Models.Category(){ Id=4,CategoryName="书籍", ParentId=0 ,CategoryPath = ""},
        //        new Models.Category(){ Id=41,CategoryName="计算机", ParentId=4 ,CategoryPath = ",4,"},
        //    };
        //}

        public void Add(Models.Category model)
        {
            SqlSugarHelper.Db.Insertable(model).ExecuteCommand();
            //throw new NotImplementedException();
        }

        public void Delete(int id)
        {
            SqlSugarHelper.Db.Deleteable<Models.Category>().In(id).ExecuteCommand();
            //throw new NotImplementedException();
        }

        public List<Models.Category> GetList(string searchKey = "")
        {
            //SqlSugarHelper.Db.Insertable(model).ExecuteCommand();
            List<Models.Category> list = SqlSugarHelper.Db.Queryable<Models.Category>().ToList();
            return list;
            //throw new NotImplementedException();
        }

        public List<Models.Category> GetListByCaId(int caid)
        {
            throw new NotImplementedException();
        }

        public List<Models.Category> GetListPage(string searchKey = "", int caId = 0, int pageSize = 8, int pageIndex = 1)
        {
            throw new NotImplementedException();
        }

        public Models.Category GetModel(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Models.Category model)
        {
            if (model != null)
            {
                var result = SqlSugarHelper.Db.Updateable(model).UpdateColumns(it => new { it.Id, it.Name }).ExecuteCommand();
                //throw new NotImplementedException();
            }
        }

        List<Category> ICategory.GetList()
        {
            throw new NotImplementedException();
        }

        List<string> ICategory.GetMBXList(int caid)
        {
            throw new NotImplementedException();
        }

        List<Category> ICategory.GetTreeModel()
        {
            throw new NotImplementedException();
        }
    }
    }
