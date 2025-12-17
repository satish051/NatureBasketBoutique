using NatureBasketBoutique.Models;

namespace NatureBasketBoutique.Repository.IRepository
{
    public interface IShoppingCartRepository : IRepository<ShoppingCart>
    {
        void Update(ShoppingCart obj);
    }
}