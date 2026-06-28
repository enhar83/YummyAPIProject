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


        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            return await _categoryRepository.GetAsQueryable()
                .ProjectTo<CategoryResponseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new LogicException("CategoryId", "Aradığınız kategori bulunamadı.");

            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task AddAsync(CategoryCreateDto dto)
        {
            var category = _mapper.Map<Category>(dto);

            bool isNameExist = await _categoryRepository.AnyAsync(c => c.CategoryName == dto.CategoryName);
            if (isNameExist)
                throw new LogicException("CategoryName", "Bu kategori ismi zaten sistemde kullanılıyor.");

            category.CategoryId = Guid.NewGuid();

            await _categoryRepository.AddAsync(category);
            await _uow.SaveAsync();
        }

        public async Task UpdateAsync(CategoryUpdateDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category == null)
                throw new LogicException("CategoryId", "Güncellenmek istenen kategori bulunamadı.");

            bool isNameExist = await _categoryRepository.AnyAsync(x =>
                x.CategoryName == dto.CategoryName &&
                x.CategoryId != dto.CategoryId);

            if (isNameExist)
                throw new LogicException("CategoryName", "Bu kategori ismi zaten sistemde kullanılıyor.");

            _mapper.Map(dto, category);
            _categoryRepository.Update(category);
            await _uow.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            if (category == null)
                throw new LogicException("CategoryId", "Silinmek istenen kategori bulunamadı.");

            _categoryRepository.Remove(category);
            await _uow.SaveAsync();

        }
    }
}