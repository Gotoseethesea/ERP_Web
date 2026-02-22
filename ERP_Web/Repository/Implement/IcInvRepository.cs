using ERP_Web.Models;
using ERP_Web.Repository;

namespace ERP_Web.Repository.Implement
{
    public class IcInvRepository : IcInv
    {
        public void Add(Models.IcInv model)
        {
            SqlSugarHelper.Db.Insertable(model).ExecuteCommand();
            //throw new NotImplementedException();
        }
        public void Add(List<Models.IcInv> models)
        {
            SqlSugarHelper.Db.Insertable(models).ExecuteCommand();
            //throw new NotImplementedException();
        }

        public int CalcCount(int caid)
        {
            throw new NotImplementedException();
        }

        public int CalcCountPage(string searchKey = "", int caId = 0)
        {
            throw new NotImplementedException();
        }

        public void Delete(string Code)
        {
            SqlSugarHelper.Db.Deleteable<Models.IcInv>().In(Code).ExecuteCommand();
            //throw new NotImplementedException();
        }

        public void Delete(Models.IcInv model)
        {

            //SqlSugarHelper.Db.Deleteable<Models.IcInv>().In(id).ExecuteCommand();
            //throw new NotImplementedException();
        }

        public List<Models.IcInv> GetList(string searchKey = "")
        {
            //SqlSugarHelper.Db.Insertable(model).ExecuteCommand();
            List<Models.IcInv> list = SqlSugarHelper.Db.Queryable<Models.IcInv>()
                .Includes(model => model.Category)
                .ToList();
            return list;
            //throw new NotImplementedException();
        }

        public List<Models.IcInv> GetListByCaId(int caid)
        {
            throw new NotImplementedException();
        }

        public List<Models.IcInv> GetListPage(string searchKey = "", int caId = 0, int pageSize = 8, int pageIndex = 1)
        {
            throw new NotImplementedException();
        }

        public Models.IcInv GetModel(int id)
        {
            throw new NotImplementedException();
        }

        public void Update(Models.IcInv model)
        {
            if (model != null) {
                var result = SqlSugarHelper.Db.Updateable(model).UpdateColumns(it => new { it.Code, it.Name, it.LastUpdateTime }).ExecuteCommand();
                //throw new NotImplementedException();
            }
        }
    }

}
