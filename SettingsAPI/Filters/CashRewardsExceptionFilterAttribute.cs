using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SettingsAPI.Error;
using System;

using System.Net;

namespace SettingsAPI.Filters
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class CashRewardsExceptionFilterAttribute : ExceptionFilterAttribute
    {
        /// <summary>
        /// The logger
        /// </summary>
        private readonly ILogger _logger;

        public CashRewardsExceptionFilterAttribute(ILoggerFactory logger)
        {
            _logger = logger.CreateLogger(typeof(ExceptionFilterAttribute));
        }

        public override void OnException(ExceptionContext context)
        {
            HttpStatusCode code = HttpStatusCode.InternalServerError;

            if (context.Exception is ValidationException ||
                context.Exception is BadRequestException)
                code = HttpStatusCode.BadRequest;


            if (context.Exception is TokenExpiredException)
                code = HttpStatusCode.Forbidden;

            if (context.Exception is UnauthorizedException)
                code = HttpStatusCode.Unauthorized;

            if (context.Exception is MemberNotFoundException)
                code = HttpStatusCode.NotFound;

            context.HttpContext.Response.ContentType = "application/json";
            context.HttpContext.Response.StatusCode = (int)code;

            Guid guid = Guid.NewGuid();
            JsonResult jsonResultFrontEnd = new JsonResult(new
            {
                Message = context.Exception.Message,
                errorId = guid,
            });

            JsonResult jsonResultLog = new JsonResult(new
            {
                error = context.Exception.Message,
                errorId = guid,
                stackTrace = context.Exception.StackTrace,
                innerException = context.Exception.InnerException,
            });
            context.Result = jsonResultFrontEnd;

            _logger.LogError(JsonConvert.SerializeObject(jsonResultLog.Value));
        }
    }
}