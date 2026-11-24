namespace Tracker.Transport;

using System.Net.Http.Headers;

internal sealed class LegacyHeadersHandler : DelegatingHandler
{
    private const string ContentType = "application/json; charset=utf-8";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        if (request.Content is null)
        {
            var content = new ByteArrayContent([]);
            content.Headers.ContentType = MediaTypeHeaderValue.Parse(ContentType);
            content.Headers.ContentLength = null;
            request.Content = content;
        }

        return base.SendAsync(request, cancellationToken);
    }
}
