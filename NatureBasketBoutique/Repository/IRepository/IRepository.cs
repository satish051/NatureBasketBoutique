using System.Linq.Expressions;

namespace NatureBasketBoutique.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        // UPDATE THIS LINE: Add 'Expression<Func<T, bool>>? filter = null'
        IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null);

        T Get(Expression<Func<T, bool>> filter, string? includeProperties = null);
        void Add(T entity);
        void Remove(T entity);
        void RemoveRange(IEnumerable<T> entity);
    }
}