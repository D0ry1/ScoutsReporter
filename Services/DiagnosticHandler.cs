using System.Diagnostics;
using System.Net.Http;

namespace ScoutsReporter.Services;

public class DiagnosticHandler : DelegatingHandler
{
    private readonly DiagnosticLogger _logger;

    public DiagnosticHandler(DiagnosticLogger logger, HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!_logger.IsEnabled)
            return await base.SendAsync(request, cancellationToken);

        var method = request.Method.Method;
        var url = DiagnosticLogger.SanitizeUrl(request.RequestUri?.ToString() ?? "");
        var sw = Stopwatch.StartNew();

        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            long? size = response.Content.Headers.ContentLength;

            _logger.Log(new DiagnosticEntry(
                DateTime.Now,
                method,
                url,
                (int)response.StatusCode,
                sw.ElapsedMilliseconds,
                size,
                null));

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();

            _logger.Log(new DiagnosticEntry(
                DateTime.Now,
                method,
                url,
                null,
                sw.ElapsedMilliseconds,
                null,
                ex.GetType().Name + ": " + ex.Message));

            throw;
        }
    }
}
