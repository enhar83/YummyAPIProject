using System;
using System.Collections.Generic;

namespace Yummy.Entity
{
    public class Chef : BaseEntity
    {
        public Guid ChefId { get; set; }
        public string Name { get; set; } = null!;
        public string Surname { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Description { get; set; } = null!;
        public string ImageUrl { get; set; } = null!;

        // şefin sisteme giriş yaptığı kullanıcı hesabı. her şefin hesabı olmak zorunda değildir (sadece vitrinde görünen şefler).
        // bir kullanıcı en fazla bir şef profiline bağlanabilir (unique index). bağlantıyı admin kurar (ChefManager.LinkUserAsync).
        public Guid? AppUserId { get; set; }
        public AppUser? AppUser { get; set; }
    }
}
