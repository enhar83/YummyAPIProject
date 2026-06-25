using System.Net;
using System.Text.Json;
using Yummy.Core.Exceptions;

namespace Yummy.WebAPI.Middlewares
{
    public class GlobalExceptionMiddleware
    {
        private readonly RequestDelegate _next;

        public GlobalExceptionMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                context.Response.ContentType = "application/json";

                if (ex is LogicException logicEx)
                {
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    var response = new { PropertyName = logicEx.PropertyName, Message = logicEx.Message };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
                else
                {
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    var response = new { Message = "Sunucu tarafında beklenmeyen bir hata oluştu." };
                    await context.Response.WriteAsync(JsonSerializer.Serialize(response));
                }
            }
        }
    }
}