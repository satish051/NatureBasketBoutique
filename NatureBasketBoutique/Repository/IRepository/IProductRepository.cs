using NatureBasketBoutique.Models;

namespace NatureBasketBoutique.Repository.IRepository
{
    public interface IProductRepository : IRepository<Product>
    {
        void Update(Product obj);
    }
}