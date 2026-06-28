using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.DTOs.ChefDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class ChefManager : IChefService
    {
        private readonly IGenericRepository<Chef> _chefRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private IWebHostEnvironment _environment;

        public ChefManager(IGenericRepository<Chef> chefRepository, IUnitOfWork uow, IMapper mapper, IWebHostEnvironment environment)
        {
            _chefRepository = chefRepository;
            _uow = uow;
            _mapper = mapper;
            _environment = environment;
        }

        public async Task AddAsync(ChefCreateDto dto)
        {
            var chef = _mapper.Map<Chef>(dto);
            chef.ChefId = Guid.NewGuid();

            if (dto.Image != null)
                chef.ImageUrl = await SaveFileAsync(dto.Image);

            await _chefRepository.AddAsync(chef);
            await _uow.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var chef = await _chefRepository.GetByIdAsync(id);
            if (chef == null)
                throw new LogicException("ChefId", "Silinmek istenen şef bulunamadı.");
            DeleteFile(chef.ImageUrl);

            _chefRepository.Remove(chef);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<ChefResponseDto>> GetAllAsync()
        {
            return await _chefRepository.GetAsQueryable()
                .ProjectTo<ChefResponseDto>(_mapper.ConfigurationProvider)
                .ToListAsync();
        }

        public async Task<ChefResponseDto?> GetByIdAsync(Guid id)
        {
            var chef = await _chefRepository.GetByIdAsync(id);
            if (chef == null)
                throw new LogicException("ChefId", "Aradığınız şef bulunamadı.");

            return _mapper.Map<ChefResponseDto>(chef);
        }

        public async Task UpdateAsync(ChefUpdateDto dto)
        {
            var chef = await _chefRepository.GetByIdAsync(dto.ChefId);
            if (chef == null)
                throw new LogicException("ChefId", "Güncellenmek istenen şef bulunamadı.");

            _mapper.Map(dto, chef);

            if (dto.Image != null)
            {
                DeleteFile(chef.ImageUrl);
                chef.ImageUrl = await SaveFileAsync(dto.Image);
            }

            _chefRepository.Update(chef);
            await _uow.SaveAsync();
        }

        #region Dosya İşlemleri
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "chefs");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/chefs/{uniqueFileName}";
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
