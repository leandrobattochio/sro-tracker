namespace API;

using System.Net.Http.Headers;

// O cliente legado (requests) manda Content-Type em toda request, inclusive nos GET sem corpo.
// HttpClient só emite um header de conteúdo quando há conteúdo, então anexamos um corpo vazio
// sem Content-Length para carregar o Content-Type, igual ao requests (header sem corpo).
public sealed class LegacyHeadersHandler : DelegatingHandler
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
