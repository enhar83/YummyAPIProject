namespace Yummy.Core.DTOs.ChefDTOs
{
    // admin'in bir şef profilini sisteme giriş yapacak kullanıcı hesabına bağlaması için.
    public record ChefLinkUserDto
    {
        public Guid UserId { get; init; }
    }
}
