using System.Net;
using System.Text.Json;
using Computational_Practice.Common;
using Computational_Practice.Exceptions;

namespace Computational_Practice.Middleware
{
    public class ExceptionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionMiddleware> _logger;

        public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Необроблена помилка: {Message}", ex.Message);
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            context.Response.ContentType = "application/json";

            object response;

            switch (exception)
            {
                case ValidationException validationEx:
                    response = new ValidationErrorResponse
                    {
                        Message = "Помилки валідації",
                        StatusCode = (int)HttpStatusCode.BadRequest,
                        Errors = validationEx.Errors
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case EntityNotFoundException:
                    response = new ErrorResponse
                    {
                        Message = exception.Message,
                        StatusCode = (int)HttpStatusCode.NotFound
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case BusinessLogicException:
                    response = new ErrorResponse
                    {
                        Message = exception.Message,
                        StatusCode = (int)HttpStatusCode.BadRequest
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case ArgumentNullException:
                case ArgumentException:
                    response = new ErrorResponse
                    {
                        Message = "Некоректні параметри запиту",
                        StatusCode = (int)HttpStatusCode.BadRequest
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                case KeyNotFoundException:
                case FileNotFoundException:
                    response = new ErrorResponse
                    {
                        Message = "Ресурс не знайдено",
                        StatusCode = (int)HttpStatusCode.NotFound
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    break;

                case UnauthorizedAccessException:
                    response = new ErrorResponse
                    {
                        Message = "Доступ заборонено",
                        StatusCode = (int)HttpStatusCode.Unauthorized
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
                    break;

                case InvalidOperationException:
                    response = new ErrorResponse
                    {
                        Message = "Некоректна операція",
                        StatusCode = (int)HttpStatusCode.BadRequest
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
                    break;

                default:
                    response = new ErrorResponse
                    {
                        Message = "Внутрішня помилка сервера",
                        StatusCode = (int)HttpStatusCode.InternalServerError
                    };
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    break;
            }

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}
