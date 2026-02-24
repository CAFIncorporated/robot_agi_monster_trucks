namespace CoordinateService.Client;

public class RequestIdHandler : DelegatingHandler
{
    public const string HeaderName = "X-Request-Id";

    public string? LastRequestId { get; private set; }
    public string? LastResponseId { get; private set; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequestId = Guid.NewGuid().ToString("N");
        request.Headers.Remove(HeaderName);
        request.Headers.Add(HeaderName, LastRequestId);

        var response = await base.SendAsync(request, cancellationToken);

        LastResponseId = response.Headers.TryGetValues(HeaderName, out var vals)
            ? vals.FirstOrDefault()
            : null;

        return response;
    }
}
