using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.IRepositories;
using Yummy.Data.Context;
using Yummy.Entity;

namespace Yummy.Data.Repositories
{
    public class GenericRepository<T> : IGenericRepository<T> where T : class
    {
        protected readonly YummyDbContext _context;
        protected readonly DbSet<T> _dbSet;

        public GenericRepository(YummyDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>(); // T tipindeki nesneler için sorgu ve yazma arayüzü.
        }
        // _context.Set<T>() ile T = Product için Product tablosuna erişilir. T'nin ne olduğunu EF Core Runtime anında dinamik olarak anlar.

        // cancellation token: asenkron işlemlerin iptal edilmesini sağlar. Kullanıcı tarayıcı sekmesini kaparsa işlem durdurulur ve kesilir.
        // params içerisinde ise Navigation Prop'lar sorguya dahil edilir. params anahtar kelimesi ile 0 ile birden fazla sayıda navigation prop eklenebilir.
        // IQueryable kullanılmasının nedeni tek bir ToList ile her şeyin belleğe alınabilmesini sağlamaktır. Böylece n+1 sorgu problemi ortadan kaldırılır.
        // AsNoTracking: salt okunur sorgularda EF Core'un Change Tracker'ı devre dışı bırakır. Bellek kullanımını azaltır ve sorgu performansını artırır.
        public async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.ToListAsync(cancellationToken);
        }

        // Expression<Func<T, bool>> predicate ile SQL ifadesi koşul alır.
        public async Task<IEnumerable<T>> GetWhereAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }
            return await query.Where(predicate).ToListAsync(cancellationToken);
        }

        // FindAsync: önce Change Tracker'a bakar, yoksa DB'ye gider. ID bazlı tekil sorgu için en verimli yöntemdir.
        // ⚠️ FindAsync, Global Query Filter'ı bypass eder. Bu nedenle BaseEntity türevleri için IsDeleted manuel kontrol edilir.
        public async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _dbSet.FindAsync(new object[] { id }, cancellationToken);

            if (entity is BaseEntity baseEntity && baseEntity.IsDeleted)
            {
                // Soft-delete edilmiş kayıt bulundu; Change Tracker'dan çıkarılır ve null döndürülür.
                _context.Entry(entity).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                return null;
            }

            return entity;
        }

        // COUNT ve sayfa sorgusu ayrı çalışır; Include sadece sayfa sorgusuna eklenir (COUNT'a gereksiz JOIN eklenmez).
        public async Task<(IReadOnlyList<T> Items, int TotalCount)> GetPagedAsync(Expression<Func<T, bool>>? predicate, Func<IQueryable<T>, IOrderedQueryable<T>> orderBy, int page, int pageSize, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            if (predicate != null)
                query = query.Where(predicate);

            var totalCount = await query.CountAsync(cancellationToken);

            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            var items = await orderBy(query)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(cancellationToken);

            return (items, totalCount);
        }

        // FirstOrDefaultAsync: koşula uyan ilk kaydı döner. Include desteği vardır; navigation property gerektiğinde kullanılır.
        public async Task<T?> GetSingleAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes)
        {
            IQueryable<T> query = _dbSet.AsNoTracking();
            foreach (var include in includes)
            {
                query = query.Include(include);
            }

            return await query.FirstOrDefaultAsync(predicate, cancellationToken);
        }

        // AddAsync / Update / Remove veritabanına HEMEN yazmaz; Change Tracker'a ekler.
        // UnitOfWork.SaveAsync() çağrıldığında tüm değişiklikler tek bir transaction'da commit edilir.
        public async Task AddAsync(T entity, CancellationToken cancellationToken = default) =>
            await _dbSet.AddAsync(entity, cancellationToken);

        public void Update(T entity)
        {
            // BaseEntity türevlerinde UpdatedDate otomatik olarak güncellenir.
            // Bu sayede her manager'da ayrıca UpdatedDate = DateTime.UtcNow yazmak gerekmez.
            if (entity is BaseEntity baseEntity)
                baseEntity.UpdatedDate = DateTime.UtcNow;

            _dbSet.Update(entity);
        }

        public void Remove(T entity)
        {
            // BaseEntity türevleri soft delete ile işaretlenir; fiziksel silme yapılmaz.
            // Diğer entity'ler (örn. Identity tabloları) fiziksel olarak silinir.
            if (entity is BaseEntity baseEntity)
            {
                baseEntity.IsDeleted = true;
                baseEntity.UpdatedDate = DateTime.UtcNow;
                _dbSet.Update(entity);
            }
            else
            {
                _dbSet.Remove(entity);
            }
        }

        // AnyAsync: varlık kontrolü için en performanslı yöntem. Tüm koleksiyonu belleğe çekmez, EXISTS sorgusu üretir.
        public async Task<bool> AnyAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default) =>
            await _dbSet.AnyAsync(predicate, cancellationToken);
    }
}
