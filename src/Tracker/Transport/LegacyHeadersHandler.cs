namespace Tracker.Transport;

using System.Net.Http.Headers;

// The legacy client (requests) sends Content-Type on every request, including GETs with
// no body. HttpClient only emits a content header when there is content, so we attach an
// empty body without Content-Length just to carry the Content-Type, matching requests.
internal sealed class LegacyHeadersHandler : DelegatingHandler
{
    const string ContentType = "application/json; charset=utf-8";

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
