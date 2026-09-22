using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.IRepositories;
using Yummy.Data.Context;

namespace Yummy.Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly YummyDbContext _context; // db ile iletişim kurmamızı sağlayan sınıf
        protected readonly DbSet<T> _dbSet; // T türündeki nesneler için db üzerinde işlem yapmayı sağlayan EF nesnesidir.

        public GenericRepository(YummyDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }

        // cancellationToken: async metotlarda işlem yarıda kaldığında operasyonu sonlandırmak için kullanılır. varsayılan olarak null döner. 
        // params Expression<Func<T, object>>[] includes: EF Core'da ilişkisel verileri (foreign key) çekmek için kullanılır. "includes" ifadesi, ilgili verilerin de getirilmesini sağlar.
        // IQueryable: DB'den veri çekme sorgusu, henüz veritabanına gönderilmemiş halidir. Bu sayede LINQ ifadeleri ile sorgu oluşturulabilir.
        // .Include(): İlişkisel verileri çekmek için kullanılır. Örneğin, bir Category'nin Products'larını çekmek için kullanılır.
        // .ToListAsync(): Sorguyu DB'ye gönderir ve sonucu belleğe getirir.
        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync(cancellationToken);
        }

        // predicate: LINQ sorgusu, koşul ifadesi.
        // .Where(): Koşula uyan verileri filtreler.
        // .ToListAsync(): Filtrelenmiş verileri DB'den çeker.
        public async Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
            await _dbSet.Where(predicate).ToListAsync(cancellationToken);


        // .FindAsync(): ID'ye göre veri arar. Eğer kayıt yoksa null döner.
        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            await _dbSet.FindAsync(new object[] { id }, cancellationToken);

        // .FirstOrDefaultAsync(): Koşula uyan ilk veriyi döner. Eğer kayıt yoksa null döner.
        public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet;
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
            await _dbSet.AddAsync(entity, cancellationToken);

        public void Update(T entity) =>
            _dbSet.Update(entity);

        public void Remove(T entity) =>
            _dbSet.Remove(entity);

        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
            await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}
