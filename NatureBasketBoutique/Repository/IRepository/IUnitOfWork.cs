namespace NatureBasketBoutique.Repository.IRepository
{
    public interface IUnitOfWork
    {
        ICategoryRepository Category { get; }
        IProductRepository Product { get; }
        IShoppingCartRepository ShoppingCart { get; } // Changed from IRepository to IShoppingCartRepository

        IRepository<ApplicationUser> ApplicationUser { get; }
        IRepository<OrderHeader> OrderHeader { get; }
        IRepository<OrderDetail> OrderDetail { get; }
        void Save();
    }
}