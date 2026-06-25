using AutoMapper;
using Yummy.Core.DTOs.CategoryDTOs; 
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
        private readonly IMapper _mapper; 

        public CategoryManager(IGenericRepository<Category> categoryRepository, IUnitOfWork uow, IMapper mapper)
        {
            _categoryRepository = categoryRepository;
            _uow = uow;
            _mapper = mapper;
        }


        public async Task<IEnumerable<CategoryResponseDto>> GetAllAsync()
        {
            var categories = await _categoryRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<CategoryResponseDto>>(categories);
        }

        public async Task<CategoryResponseDto?> GetByIdAsync(Guid id)
        {
            var category = await _categoryRepository.GetByIdAsync(id);
            return _mapper.Map<CategoryResponseDto>(category);
        }

        public async Task AddAsync(CategoryCreateDto dto)
        {
            var category = _mapper.Map<Category>(dto);

            await _categoryRepository.AddAsync(category);
            await _uow.SaveAsync();
        }

        public async Task UpdateAsync(CategoryUpdateDto dto)
        {
            var category = await _categoryRepository.GetByIdAsync(dto.CategoryId);
            if (category != null)
            {
                _mapper.Map(dto, category);

                _categoryRepository.Update(category);
                await _uow.SaveAsync();
            }
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