using System.Threading;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Yummy.Core.DTOs.CategoryDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Microsoft.EntityFrameworkCore;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class CategoryManager : ICategoryService
    {
        private readonly IGenericRepository<Category> _categoryRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;

        public CategoryManager(IGenericRepository<Category> categoryRepository, IUnitOfWork uow, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _uow = uow;
            _mapper = mapper;
        }


        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var entities = await _categoryRepository.GetAllAsync(cancellationToken);
            return _mapper.Map<IEnumerable<CategoryResponseDto>>(entities);
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                throw new LogicException("CategoryId", "Aradığınız kategori bulunamadı.");

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task AddAsync(CategoryCreateDto dto, CancellationToken cancellationToken = default)
        {
            var category = _mapper.Map<Category>(dto);

            bool isNameExist = await _categoryRepository.AnyAsync(c => c.CategoryName == dto.CategoryName, cancellationToken);
            if (isNameExist)
                throw new LogicException("CategoryName", "Bu kategori ismi zaten sistemde kullanılıyor.");

            category.CategoryId = Guid.NewGuid();

            await _categoryRepository.AddAsync(category, cancellationToken);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task UpdateAsync(CategoryUpdateDto dto, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId, cancellationToken);
            if (category == null)
                throw new LogicException("CategoryId", "Güncellenmek istenen kategori bulunamadı.");

            bool isNameExist = await _categoryRepository.AnyAsync(x =>
                x.CategoryName == dto.CategoryName &&
                x.CategoryId != dto.CategoryId, cancellationToken);

            if (isNameExist)
                throw new LogicException("CategoryName", "Bu kategori ismi zaten sistemde kullanılıyor.");

            _mapper.Map(dto, category);
            _categoryRepository.Update(category);
            await _uow.SaveAsync(cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var category = await _categoryRepository.GetByIdAsync(id, cancellationToken);
            if (category == null)
                throw new LogicException("CategoryId", "Silinmek istenen kategori bulunamadı.");

            _categoryRepository.Remove(category);
            await _uow.SaveAsync(cancellationToken);

        }
    }
}