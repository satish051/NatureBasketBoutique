using NatureBasketBoutique.Models;

namespace NatureBasketBoutique.Repository.IRepository
{
    public interface ICategoryRepository : IRepository<Category>
    {
        void Update(Category obj);
    }
}