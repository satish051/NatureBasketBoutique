using NatureBasketBoutique.Models;

namespace NatureBasketBoutique.Repository.IRepository
{
    public interface IOrderHeaderRepository : IRepository<OrderHeader>
    {
        void Update(OrderHeader obj);
    }
}