using AntDesign;
using ERP_Web.Models;
using ERP_Web.Repository;

namespace ERP_Web.Repository.Implement
{
    public class CategoryRepositoryMssql : ICategory
    {
 
        public void Add(Category model)
        {
           SqlSugarHelperOld.Db.Insertable(model).ExecuteCommand();
        }

        public void Delete(int id)
        {
            //有下级时不能删除
            int xjcount = SqlSugarHelperOld.Db.Queryable<Category>().Where(a=>a.ParentId == id).Count();
            if (xjcount>0)
            {
                throw new Exception("该分类下还有下级,不可删除!");
            }
            //有商品时不能删除
            //int procount = SqlSugarHelperOld.Db.Queryable<IcInv>().Where(p=>p.id == id).Count();
            //if (procount>0)
            //{
            //    throw new Exception("该分类下还有商品,不可删除!");
            //}
            SqlSugarHelperOld.Db.Deleteable<Category>(a=>a.Id == id).ExecuteCommand();
        }

        public List<Category> GetList()
        {
            return SqlSugarHelperOld.Db.Queryable<Category>().ToList();
        }

        public List<string> GetMBXList(int caid)
        {
            if (caid == 0)
            {
                return new List<string>() { "全部商品" };
            }
            List<string> list = new List<string>();
            Models.Category ca = GetModel(caid);
            string[] caids = ca.CategoryPath.Split(',');
            foreach (var item in caids)
            {
                if (string.IsNullOrEmpty(item))
                {
                    continue;
                }
                Models.Category temp = GetModel(int.Parse(item));
                list.Add(temp.Name);
            }
            list.Add(ca.Name);
            return list;
        }

        public Category GetModel(int caid)
        {
            return SqlSugarHelperOld.Db.Queryable<Category>().Single(ca => ca.Id == caid);
        }

        public List<Category> GetTreeModel()
        {
            List<Category> categories = GetList();
            List<Category> list = new List<Category>();
            //var top = categories.Where(ca => ca.ParentId == 0).OrderBy(a=>a.Sort).ToList();
            //foreach (var oneca in top)
            //{
            //    oneca.Items.Clear();
            //    DiGui(oneca, categories);
            //    list.Add(oneca);
            //}
            return list;
        }

        public void Update(Category model)
        {
            SqlSugarHelperOld.Db.Updateable<Category>(model).ExecuteCommand() ;
        }

        /// <summary>
        /// 递归添加下级节点
        /// </summary>
        /// <param name="oneca"></param>
        //private void DiGui(Category oneca, List<Category> categories)
        //{
        //    var sub = categories.Where(ca => ca.ParentId == oneca.Id).OrderBy(a=>a.Sort).ToList();
        //    foreach (var oneca2 in sub)
        //    {
        //        oneca2.Items.Clear();
        //        DiGui(oneca2, categories);
        //        oneca.Items.Add(oneca2);
        //    }
        //}
    }
}
