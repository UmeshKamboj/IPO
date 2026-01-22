using IPOClient.Data;
using IPOClient.Repositories.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IPOClient.Repositories.Implementations
{
    public class BaseRepository<T> : IRepository<T> where T : class
    {
        protected readonly IPOClientDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public BaseRepository(IPOClientDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public virtual async Task<T?> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }

        public virtual async Task<IEnumerable<T>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public virtual async Task<T> AddAsync(T entity)
        {
            await _dbSet.AddAsync(entity);
            await SaveAsync();
            return entity;
        }

        public virtual async Task UpdateAsync(T entity)
        {
            _dbSet.Update(entity);
            await SaveAsync();
        }

        public virtual async Task DeleteAsync(T entity)
        {
            _dbSet.Remove(entity);
            await SaveAsync();
        }

        public virtual async Task SaveAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
