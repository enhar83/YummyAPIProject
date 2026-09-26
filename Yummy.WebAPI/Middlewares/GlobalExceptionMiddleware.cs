using System.Net;
using System.Text.Json;
using Yummy.Core.Exceptions;

namespace Yummy.WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<GlobalExceptionMiddleware> _logger;
        private readonly IHostEnvironment _environment;

        public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger, IHostEnvironment environment)
        {
            _next = next;
            _logger = logger;
            _environment = environment;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                if (context.Response.HasStarted) // cevap istemciye yazılmaya başladıysa artık değiştirilemez; hata loglanıp tekrar fırlatılır.
                {
                    _logger.LogError(ex, "Cevap gönderilmeye başladıktan sonra hata oluştu: {Method} {Path}", context.Request.Method, context.Request.Path);
                    throw;
                }

                context.Response.ContentType = "application/json";

                if (ex is LogicException logicEx)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var response = new { PropertyName = logicEx.PropertyName, Message = logicEx.Message };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
                else
                {
                    // beklenmeyen hataların detayı (SQL hataları, tablo/kolon adları, dosya yolları vb.) sadece loglara yazılır.
                    _logger.LogError(ex, "Beklenmeyen hata: {Method} {Path}", context.Request.Method, context.Request.Path);

                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

                    // hata detayı sadece Development ortamında istemciye döndürülür; diğer ortamlarda genel bir mesaj gösterilir.
                    object response = _environment.IsDevelopment()
                        ? new
                        {
                            Message = ex.Message,
                            Detail = ex.InnerException != null ? ex.InnerException.Message : "Detay yok"
                        }
                        : new
                        {
                            Message = "Beklenmeyen bir hata oluştu. Lütfen daha sonra tekrar deneyin."
                        };

                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            }
        }
    }
}
