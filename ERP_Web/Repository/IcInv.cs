namespace ERP_Web.Repository
{
    public interface IcInvifc
    {
        void Add(Models.IcInv model);
        void Delete(string Code);
        void Update(Models.IcInv model);
        List<Models.IcInv> GetList(string searchKey = "");
        List<Models.IcInv> GetListByCaId(int caid);
        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="searchKey"></param>
        /// <param name="caId"></param>
        /// <param name="pageSize"></param>
        /// <param name="pageIndex"></param>
        /// <returns></returns>
        List<Models.IcInv> GetListPage(string searchKey = "", int caId = 0, int pageSize = 8, int pageIndex = 1);
        Models.IcInv GetModel(int id);
        int CalcCount(int caid);
        int CalcCountPage(string searchKey = "", int caId = 0);
    }

}
