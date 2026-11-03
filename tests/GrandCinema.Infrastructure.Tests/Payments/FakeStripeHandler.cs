using System.Net;
using System.Text;

namespace GrandCinema.Infrastructure.Tests.Payments;

public sealed class FakeStripeHandler : HttpMessageHandler
{
    private readonly string _response;

    public FakeStripeHandler(string response)
    {
        _response = response;
    }

    public HttpRequestMessage? Request { get; private set; }

    // The form fields sent to Stripe, decoded: "client_reference_id=…", "line_items[0][price_data][unit_amount]=1900"…
    public string Body { get; private set; } = string.Empty;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Request = request;
        Body = request.Content is null ? string.Empty : Uri.UnescapeDataString(await request.Content.ReadAsStringAsync(cancellationToken));
        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(_response, Encoding.UTF8, "application/json") };
    }
}
