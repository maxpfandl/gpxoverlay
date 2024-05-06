using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Prometheus;
namespace gpxoverlay
{
    public class PrometheusMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger _logger;


        public PrometheusMiddleware(
            RequestDelegate next
            , ILoggerFactory loggerFactory
            )
        {
            this._next = next;
            this._logger = loggerFactory.CreateLogger<PrometheusMiddleware>();
        }

        public async Task Invoke(HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value;
            var method = httpContext.Request.Method;

            var totalRequests = Metrics.CreateCounter($"dotnet_request_total", "HTTP Requests Total", new CounterConfiguration
            {
                LabelNames = new[] { "path", "method", "status" }
            });

            var concurrentRequests = Metrics.CreateGauge($"dotnet_request_current", "HTTP Requests Current", new GaugeConfiguration
            {
                LabelNames = new[] { "path", "method" }
            });

            var requestTime = Metrics.CreateHistogram($"dotnet_request_time", "HTTP Request Time", new HistogramConfiguration
            {
                LabelNames = new[] { "path", "method" }
            });

            var requestSize = Metrics.CreateHistogram($"dotnet_request_size", "HTTP Request Size in Byte", new HistogramConfiguration
            {
                LabelNames = new[] { "path", "method" }
            });

            var responseSize = Metrics.CreateHistogram($"dotnet_response_size", "HTTP Response Size in Byte", new HistogramConfiguration
            {
                LabelNames = new[] { "path", "method", "status" }
            });


            try
            {
                if (httpContext.Request.ContentLength.HasValue)
                {
                    requestSize.Labels(path, method).Observe((double)httpContext.Request.ContentLength);
                }
                using (concurrentRequests.WithLabels(path, method).TrackInProgress())
                using (requestTime.WithLabels(path, method).NewTimer())
                {
                    await _next.Invoke(httpContext);
                }
            }
            catch (Exception)
            {
                totalRequests.Labels(path, method, httpContext.Response.StatusCode.ToString()).Inc();
                if (httpContext.Response.ContentLength.HasValue)
                {
                    responseSize.Labels(path, method, httpContext.Response.StatusCode.ToString()).Observe((double)httpContext.Response.ContentLength);
                }
                throw;
            }

            if (path != "/metrics")
            {
                totalRequests.Labels(path, method, httpContext.Response.StatusCode.ToString()).Inc();
                if (httpContext.Response.ContentLength.HasValue)
                {
                    responseSize.Labels(path, method, httpContext.Response.StatusCode.ToString()).Observe((double)httpContext.Response.ContentLength);
                }
            }

        }
    }

    public static class PrometheusMiddlewareExtensions
    {
        public static IApplicationBuilder UsePrometheusMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<PrometheusMiddleware>();
        }
    }
}
