using NatureBasketBoutique.Data;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;

namespace NatureBasketBoutique.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;

        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }

        // keeping these generic is fine for now
        public IRepository<ApplicationUser> ApplicationUser { get; private set; }
        public IRepository<OrderDetail> OrderDetail { get; private set; }

        // --- CHANGE 1: Use the Interface that has the Update method ---
        public IOrderHeaderRepository OrderHeader { get; private set; }
        // -------------------------------------------------------------

        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            ShoppingCart = new ShoppingCartRepository(_db);

            ApplicationUser = new Repository<ApplicationUser>(_db);
            OrderDetail = new Repository<OrderDetail>(_db);

            // --- CHANGE 2: Use the Class you just showed me ---
            OrderHeader = new OrderHeaderRepository(_db);
            // --------------------------------------------------
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}