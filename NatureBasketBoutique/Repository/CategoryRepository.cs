using NatureBasketBoutique.Data;
using NatureBasketBoutique.Models;
using NatureBasketBoutique.Repository.IRepository;

namespace NatureBasketBoutique.Repository
{
    public class CategoryRepository : Repository<Category>, ICategoryRepository
    {
        private ApplicationDbContext _db;
        public CategoryRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public void Update(Category obj)
        {
            _db.Categories.Update(obj);
        }
    }
}