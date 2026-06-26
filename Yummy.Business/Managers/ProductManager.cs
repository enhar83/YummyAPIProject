using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AutoMapper;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Yummy.Core.DTOs.ProductDTOs;
using Yummy.Core.Exceptions;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Core.Services;
using Yummy.Entity;

namespace Yummy.Business.Managers
{
    public class ProductManager : IProductService
    {
        private readonly IGenericRepository<Product> _productRepository;
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly IWebHostEnvironment _environment;

        public ProductManager(IGenericRepository<Product> productRepository, IUnitOfWork uow, IMapper mapper, IWebHostEnvironment environment)
        {
            _productRepository = productRepository;
            _uow = uow;
            _mapper = mapper;
            _environment = environment;
        }

        public async Task AddAsync(ProductCreateDto dto)
        {
            var product = _mapper.Map<Product>(dto);

            var productIsExist = await _productRepository.GetSingleAsync(p=> p.ProductName.ToLower() == dto.ProductName.ToLower());
            if (productIsExist != null)
                throw new LogicException("ProductName", "Bu ürün ismi zaten sistemde kullanılıyor.");

            product.ProductId = Guid.NewGuid();

            if (dto.Image != null)
                product.ImageUrl = await SaveFileAsync(dto.Image);

            await _productRepository.AddAsync(product);
            await _uow.SaveAsync();
        }

        public async Task DeleteAsync(Guid id)
        {
            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                throw new LogicException("ProductId", "Silinmek istenen ürün bulunamadı.");
            DeleteFile(product.ImageUrl);

            _productRepository.Remove(product);
            await _uow.SaveAsync();
        }

        public async Task<IEnumerable<ProductResponseDto>> GetAllAsync()
        {
            var products = await _productRepository.GetAllAsync(p=>p.Category);
            return _mapper.Map<IEnumerable<ProductResponseDto>>(products);
        }

        public async Task<ProductResponseDto?> GetByIdAsync(Guid id)
        {
            var product = await _productRepository.GetSingleAsync(p => p.ProductId == id, p => p.Category);
            if (product == null)
                throw new LogicException("ProductId", "Aradığınız ürün bulunamadı.");

            return _mapper.Map<ProductResponseDto>(product);
        }

        public async Task UpdateAsync(ProductUpdateDto dto)
        {
            var product = await _productRepository.GetByIdAsync(dto.ProductId);
            if (product == null)
                throw new LogicException("ProductId", "Güncellenmek istenen ürün bulunamadı.");

            var productIsExist = await _productRepository.GetSingleAsync(p => p.ProductName.ToLower() == dto.ProductName.ToLower() && p.ProductId != dto.ProductId);
            if (productIsExist != null)
                throw new LogicException("ProductName", "Bu ürün ismi zaten sistemde kullanılıyor.");

            _mapper.Map(dto, product);
            if (dto.Image != null)
            {
                DeleteFile(product.ImageUrl);
                product.ImageUrl = await SaveFileAsync(dto.Image);
            }

            _productRepository.Update(product);
            await _uow.SaveAsync();
        }

        #region Dosya İşlemleri
        private async Task<string> SaveFileAsync(IFormFile file)
        {
            var uploadsFolder = Path.Combine(_environment.WebRootPath, "images", "products");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(fileStream);
            }

            return $"/images/products/{uniqueFileName}";
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
