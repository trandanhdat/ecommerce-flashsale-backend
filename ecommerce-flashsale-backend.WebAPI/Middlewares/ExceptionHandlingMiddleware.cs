using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FlashSale.Domain.SeedWork;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using FlashSale.Domain.Users.Exceptions;
namespace ecommerce_flashsale_backend.Middlewares
{
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            try
            {
                await _next(httpContext);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred.");
                await HandleExceptionAsync(httpContext, ex);
            }
        }

        private static Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            var statusCode = HttpStatusCode.InternalServerError;
            var message = "Lỗi hệ thống không xác định.";
            string innerMessage = null;

            switch (exception)
            {
                case InvalidCredentialsException e:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = e.Message;
                    break;
                case DomainException e:
                    // Thường lỗi Not Found thì ném NotFoundException, nhưng project hiện tại hay dùng chung DomainException
                    statusCode = e.Message.Contains("Không tìm thấy") ? HttpStatusCode.NotFound : HttpStatusCode.BadRequest; 
                    message = e.Message;
                    break;
                case ValidationException e:
                    statusCode = HttpStatusCode.BadRequest;
                    message = e.Message;
                    break;
                case UnauthorizedAccessException e:
                    statusCode = HttpStatusCode.Unauthorized;
                    message = e.Message;
                    break;
            }

            context.Response.StatusCode = (int)statusCode;

            var result = JsonSerializer.Serialize(new
            {
                statusCode = context.Response.StatusCode,
                message = message,
                error = exception.GetType().Name
            });

            return context.Response.WriteAsync(result);
        }
    }
}
