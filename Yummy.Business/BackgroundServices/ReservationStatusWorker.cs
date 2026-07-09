using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Yummy.Core.IRepositories;
using Yummy.Core.IUnitOfWork;
using Yummy.Entity;
using Yummy.Entity.Enums;

namespace Yummy.Business.BackgroundServices
{
    public class ReservationStatusWorker : BackgroundService
    {
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
                _logger.LogInformation("Rezervasyon durum kontrol servisi tetiklendi: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var reservationRepository = scope.ServiceProvider.GetRequiredService<IGenericRepository<Reservation>>();
                        var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        var activeReservations = await reservationRepository.GetAsQueryable()
                            .Where(r => r.ReservationStatus == ReservationStatus.Approved || r.ReservationStatus == ReservationStatus.Pending)
                            .ToListAsync(stoppingToken);

                        bool hasChanges = false;

                        foreach (var reservation in activeReservations)
                        {
                            if (TimeSpan.TryParse(reservation.ReservationTime, out TimeSpan parsedTime))
                            {
                                var exactReservationDateTime = reservation.ReservationDate.Date.Add(parsedTime);

                                var thresholdTime = exactReservationDateTime.AddHours(2);

                                if (DateTime.Now >= thresholdTime)
                                {
                                    if (reservation.ReservationStatus == ReservationStatus.Approved)
                                    {
                                        reservation.ReservationStatus = ReservationStatus.Completed;
                                        reservationRepository.Update(reservation);
                                        hasChanges = true;

                                        _logger.LogInformation($"Rezervasyon ID: {reservation.ReservationId} 'Completed' olarak güncellendi.");
                                    }
                                    else if (reservation.ReservationStatus == ReservationStatus.Pending)
                                    {
                                        reservation.ReservationStatus = ReservationStatus.Cancelled;
                                        reservationRepository.Update(reservation);
                                        hasChanges = true;

                                        _logger.LogInformation($"Rezervasyon ID: {reservation.ReservationId} süresi dolduğu için 'Cancelled' olarak güncellendi.");
                                    }
                                }
                            }
                        }

                        if (hasChanges)
                        {
                            await uow.SaveAsync();
                            _logger.LogInformation("Zamanı geçen rezervasyonların durumları veritabanına başarıyla kaydedildi.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rezervasyon durum kontrol servisinde beklenmeyen bir hata oluştu.");
                }
                await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
            }
        }
    }
}
