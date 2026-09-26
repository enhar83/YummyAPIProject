using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace Yummy.Entity
{
    public class AppUser : IdentityUser<Guid>
    {
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string? ImageUrl { get; set; }
        public bool IsDeleted { get; set; } = false;
        public string? ActivationCode { get; set; }
        public DateTime? ActivationCodeExpiryTime { get; set; } // aktivasyon kodunun son geçerlilik zamanı (UTC). Bu tarihten sonra kod kabul edilmez.
        public DateTime? ActivationCodeSentAt { get; set; } // kodun en son gönderildiği zaman (UTC). Yeniden gönderme için bekleme süresi buna göre hesaplanır.
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedDate { get; set; }
        public string? RefreshToken { get; set; } // jwt yapısında access token süresi dolduğunda yeni bir token almak için kullanılan refresh token değeri. ? ile nullable'dır; kullanıcı giriş yapmamışsa bu alan boştur.
        public DateTime? RefreshTokenExpiryTime { get; set; } // refresh token'ın geçerlilik süresi. Bu tarihi geçen refresh token'lar geçersiz sayılır.

        public ICollection<Message> Messages { get; set; } = new HashSet<Message>();
        public ICollection<Reservation> Reservations { get; set; } = new HashSet<Reservation>();
        public ICollection<Testimonial> Testimonials { get; set; } = new HashSet<Testimonial>();
    }
}
