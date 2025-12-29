using NatureBasketBoutique.Data; // Ensure this namespace matches your DbContext location
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;

namespace NatureBasketBoutique.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private ApplicationDbContext _db;

        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(OrderHeader obj)
        {
            _db.OrderHeaders.Update(obj);
        }
    }
}