using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class CategoryManager : ICategoryService
    {
        private readonly IGenericRepository<Category> _categoryRepository;
        private readonly IUnitOfWork _uow;

        public CategoryManager(IGenericRepository<Category> categoryRepository, IUnitOfWork uow)
        {
            _categoryRepository = categoryRepository;
            _uow = uow;
        }

        public async Task<IEnumerable<Category>> GetAllAsync()
        {
            return await _categoryRepository.GetAllAsync();
        }

        public async Task<Category?> GetByIdAsync(Guid id)
        {
            return await _categoryRepository.GetByIdAsync(id);
        }

        public async Task AddAsync(Category category)
        {
            await _categoryRepository.AddAsync(category);
            await _uow.SaveAsync();
        }

        public async Task UpdateAsync(Category category)
        {
            _categoryRepository.Update(category);
            await _uow.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category != null)
            {
                _categoryRepository.Remove(category);
                await _uow.SaveAsync();
            }
        }
    }
}
