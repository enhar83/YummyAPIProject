using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;

namespace Yummy.Tests.Infrastructure
{
    // dosya yükleyen/silen manager'lar (ChefManager, AppUserManager) için. dosyalar her testte ayrı bir geçici klasöre yazılır.
    public class FakeWebHostEnvironment : IWebHostEnvironment
    {
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "yummy-tests", Guid.NewGuid().ToString("N"));
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
        public string ApplicationName { get; set; } = "Yummy.Tests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public string EnvironmentName { get; set; } = "Testing";
    }
}
