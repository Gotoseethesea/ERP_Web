namespace ERP_Web.Repository
{
    public interface ICategory
    {
        List<Models.Category> GetTreeModel();
        List<Models.Category> GetList();
        List<string> GetMBXList(int caid);
        Models.Category GetModel(int caid);

        void Add(Models.Category model);
        void Delete(int id);
        void Update(Models.Category model);

    }
}
