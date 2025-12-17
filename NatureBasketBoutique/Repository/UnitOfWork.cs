using NatureBasketBoutique.Data;
using NatureBasketBoutique.Repository.IRepository;

namespace NatureBasketBoutique.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private ApplicationDbContext _db;
        public ICategoryRepository Category { get; private set; }
        public IProductRepository Product { get; private set; }
        public IShoppingCartRepository ShoppingCart { get; private set; }

        // Add Properties
        public IRepository<ApplicationUser> ApplicationUser { get; private set; }
        public IRepository<OrderHeader> OrderHeader { get; private set; }
        public IRepository<OrderDetail> OrderDetail { get; private set; }

        // Update Constructor
        public UnitOfWork(ApplicationDbContext db)
        {
            _db = db;
            Category = new CategoryRepository(_db);
            Product = new ProductRepository(_db);
            ShoppingCart = new ShoppingCartRepository(_db);

            // Generic Repos are fine for these
            ApplicationUser = new Repository<ApplicationUser>(_db);
            OrderHeader = new Repository<OrderHeader>(_db);
            OrderDetail = new Repository<OrderDetail>(_db);
        }

        public void Save()
        {
            _db.SaveChanges();
        }
    }
}