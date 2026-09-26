using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yummy.Core.Services;

namespace Yummy.Business.BackgroundServices
{
    // bitiş saati geçen rezervasyonların durumlarını periyodik olarak günceller.
    // iş kuralları ReservationManager.ProcessPastReservationsAsync içerisindedir; bu sınıf sadece zamanlama ve hata yönetiminden sorumludur.
    public class ReservationStatusWorker : BackgroundService
    {
        private static readonly TimeSpan Interval = TimeSpan.FromMinutes(10);

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ReservationStatusWorker> _logger;

        public ReservationStatusWorker(IServiceProvider serviceProvider, ILogger<ReservationStatusWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // BackgroundService singleton'dır; scoped servisler (DbContext, repository, manager) her çalışmada yeni bir scope içerisinden alınır.
                    await using (var scope = _serviceProvider.CreateAsyncScope())
                    {
                        var reservationService = scope.ServiceProvider.GetRequiredService<IReservationService>();
                        var processedCount = await reservationService.ProcessPastReservationsAsync(stoppingToken);

                        if (processedCount > 0)
                            _logger.LogInformation("Zamanı geçen {Count} rezervasyonun durumu güncellendi.", processedCount);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    // uygulama kapanıyor; hata olarak loglanmaz.
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rezervasyon durum kontrol servisinde beklenmeyen bir hata oluştu.");
                }

                try
                {
                    await Task.Delay(Interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
        }
    }
}
