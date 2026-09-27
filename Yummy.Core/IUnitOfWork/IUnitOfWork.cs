using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Yummy.Core.IUnitOfWork
{
    public interface IUnitOfWork : IAsyncDisposable
    {
        Task<int> SaveAsync(CancellationToken cancellationToken = default);

        // action, lockKey için alınan özel (exclusive) bir kilit altında tek bir transaction içerisinde çalıştırılır.
        // aynı lockKey ile gelen eşzamanlı istekler sıraya girer; "kontrol et → kaydet" adımları arasına başka bir istek giremez.
        Task ExecuteInLockedTransactionAsync(string lockKey, Func<Task> action, CancellationToken cancellationToken = default);

        // birden fazla kilit gerektiğinde kilitler verilen sırayla alınır. deadlock oluşmaması için tüm çağıranlar aynı sırayı kullanmalıdır
        // (örn. rezervasyonlarda: önce kullanıcı, sonra gün kilidi).
        Task ExecuteInLockedTransactionAsync(IReadOnlyList<string> lockKeys, Func<Task> action, CancellationToken cancellationToken = default);
    }
}



/*
    ExecuteInLockedTransactionAsync metotlarının amacı bir işi başka hiçbir isteğin araya giremeyeceği şekilde çalıştırmaktır. 

    iki kişinin aynı anda aynı rezervasyonu yapmaya çalışması büyük bir problemdir ve mantığı tamamen çökertebilir.
    sadece SaveAsync bulunsaydı bu problem çözülmüş olmazdı. SaveAsync yazma anını korur. Okuma ile yazma arasındaki boşluğu korumamış olur.

    ÇÖZÜM: tek bir anahtar kullanımı. tek kişilik bir soyunma kabini gibi düşün.
        - içeri girmek isteyen anahtarı alır, kabine girer, işini bitirir, çıkınca anahtarı yerine koyar.
        - anahtar yoksa kapıda bekler.

    SİSTEMDEKİ KULLANIMI:
        - Kabin: 29 Eylül rezervasyonları
        - Anahtar: reservation:2026-09-29 adında bir kilit
        - kabinde yapılan is: oku, karar ver, yaz

    ZAMAN                   AYŞE'nin İSTEĞİ                 ALİ'nin İSTEĞİ
    ------                  ---------------                 --------------
    1                       29 eylül kilidini al               
    2                                                       29 eylül kilidini al (bekle)
    3                       Oku: Masa 1 boş -> EVET 
    4                       Yaz: Masa 1 -> ONAYLA, anahtarı bırak     
    5                                                       29 eylül kilidini al (artık boş)
    6                                                       Oku: Masa 1 boş mu? -> HAYIR (Ayşe'de)
    7                                                       Masa 2'yi ver ya da NoTable de

                                            BÖYLECE AYNI MASA İKİ KİŞİYE VERİLMEDİ

    farklı günlerin anahtarları farklıdır. 29 eylüle yapılan rezervasyon 30 eylülü hiç bekletmez.

    IReadOnlyList<string> lockKeys parametresi hangi anahtarların alınacağının cevabıdır. 
    Func<Task> action parametresi kilit altında yapılacak işin cevabıdır.
    


 */
