using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Yummy.Core.Constants;
using Yummy.Entity;

namespace Yummy.Business.Seeding
{
    // yetkilendirme sistem rollerinin isimlerine bağlı olduğu için ([Authorize(Roles = ...)]) bu roller uygulama açılışında garanti altına alınır.
    // rol yoksa oluşturulur; daha önce pasife alınmışsa (IsDeleted) tekrar aktif edilir. aksi halde pasif rol query filter nedeniyle
    // "yok" görünür ve aynı isimle yeni rol oluşturmak unique index'e takılır.
    public static class SystemRoleSeeder
    {
        public static async Task SeedAsync(RoleManager<AppRole> roleManager, CancellationToken cancellationToken = default)
        {
            foreach (var roleName in RoleNames.SystemRoles)
            {
                var normalizedName = roleManager.NormalizeKey(roleName);
                var existingRole = await roleManager.Roles
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(r => r.NormalizedName == normalizedName, cancellationToken);

                if (existingRole == null)
                {
                    var result = await roleManager.CreateAsync(new AppRole { Name = roleName, Description = $"{roleName} sistem rolü." });
                    EnsureSucceeded(result, roleName);
                }
                else if (existingRole.IsDeleted)
                {
                    existingRole.IsDeleted = false;
                    var result = await roleManager.UpdateAsync(existingRole);
                    EnsureSucceeded(result, roleName);
                }
            }
        }

        private static void EnsureSucceeded(IdentityResult result, string roleName)
        {
            if (!result.Succeeded)
                throw new InvalidOperationException($"'{roleName}' sistem rolü oluşturulamadı: {string.Join(" | ", result.Errors.Select(e => e.Description))}");
        }
    }
}
