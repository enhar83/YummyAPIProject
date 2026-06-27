using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Yummy.Core.DTOs.FeatureDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class FeatureManager : IFeatureService
    {
        private readonly IGenericRepository<Feature> _featureRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public FeatureManager(IGenericRepository<Feature> featureRepository, IUnitOfWork uow, IMapper mapper, IWebHostEnvironment environment)
        {
            _featureRepository = featureRepository;
            _uow = uow;
            _mapper = mapper;
            _environment = environment;
        }

        public async Task AddAsync(FeatureCreateDto dto)
        {
            var feature = _mapper.Map<Feature>(dto);
            var isFeatureExist = await _featureRepository.GetSingleAsync(f => f.Title == dto.Title);
            if (isFeatureExist != null)
                throw new LogicException("Title", "Bu başlık zaten sistem içerisinde kullanılıyor.");

            feature.FeatureId = Guid.NewGuid();

            if (dto.Image != null)
                feature.ImageUrl = await SaveFileAsync(dto.Image);

            await _featureRepository.AddAsync(feature);
            await _uow.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var feature = await _featureRepository.GetByIdAsync(id);
            if (feature == null)
                throw new LogicException("FeatureId", "Silinmek istenen özellik Id'si bulunamadı.");

            DeleteFile(feature.ImageUrl);
            _featureRepository.Remove(feature);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<FeatureResponseDto>> GetAllAsync()
        {
            var features = await _featureRepository.GetAllAsync();
            return _mapper.Map<IEnumerable<FeatureResponseDto>>(features);
        }

        public async Task<FeatureResponseDto?> GetByIdAsync(Guid id)
        {
            var feature = await _featureRepository.GetByIdAsync(id);
            if (feature == null)
                throw new LogicException("FeatureId", "İstenilen Id'ye sahip özellik bulunamadı.");

            return _mapper.Map<FeatureResponseDto>(feature);
        }

        public async Task UpdateAsync(FeatureUpdateDto dto)
        {
            var feature = await _featureRepository.GetByIdAsync(dto.FeatureId);
            if (feature == null)
                throw new LogicException("FeatureId", "Güncellenmek istenen özelliğe ait Id bulunamadı.");

            var isFeatureExist = await _featureRepository.GetSingleAsync(f => f.Title == dto.Title && f.FeatureId != dto.FeatureId);
            if (isFeatureExist != null)
                throw new LogicException("FeatureId", "Bu özellik başlığı zaten sistem içerisinde kullanılıyor.");

            _mapper.Map(dto, feature);

            if (dto.Image != null)
            {
                DeleteFile(feature.ImageUrl);
                feature.ImageUrl = await SaveFileAsync(dto.Image);
            }

            _featureRepository.Update(feature);
            await _uow.SaveAsync();
        }

        #region Dosya İşlemleri
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "features");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/features/{uniqueFileName}";
        }

        private void DeleteFile(string? imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return;
            var filePath = Path.Combine(_environment.WebRootPath, imageUrl.TrimStart('/'));

            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        #endregion
    }
}
