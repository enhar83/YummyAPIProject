using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Yummy.Core.DTOs.ContactDTOs;
using Yummy.Core.Services;

namespace Yummy.WebAPI.Controllers.AdminControllers
{
    [Authorize]
    [Route("api/admin/contacts")]
    [ApiController]
    public class AdminContactsController : ControllerBase
    {
        private readonly IContactService _contactService;

        public AdminContactsController(IContactService contactService)
        {
            _contactService = contactService;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var contacts = await _contactService.GetAllAsync();
            return Ok(contacts);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var contact = await _contactService.GetByIdAsync(id);
            return Ok(contact);
        }

        [HttpPost]
        public async Task<IActionResult> Add(ContactCreateDto dto)
        {
            await _contactService.AddAsync(dto);
            return StatusCode(201, "İletişim bilgileri başarıyla oluşturuldu.");
        }

        [HttpPut]
        public async Task<IActionResult> Update(ContactUpdateDto dto)
        {
            await _contactService.UpdateAsync(dto);
            return Ok("İletişim bilgileri başarıyla güncellendi.");
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            await _contactService.DeleteAsync(id);
            return Ok("İletişim bilgileri başarıyla silindi.");
        }
    }
}
