using System.Net;
using Xunit;
using FluentAssertions;
using CoordinateService.Client;

namespace CoordinateService.ClientTests;

public class CoordinateSystemTests : IClassFixture<TestWebAppFactory>
{
    private readonly CoordinateServiceClient _client;
    private readonly RequestIdHandler _reqIdHandler;

    public CoordinateSystemTests(TestWebAppFactory factory)
    {
        _reqIdHandler = new RequestIdHandler { InnerHandler = factory.Server.CreateHandler() };
        var http = new HttpClient(_reqIdHandler) { BaseAddress = factory.Server.BaseAddress };
        _client = new CoordinateServiceClient(http);
    }

    [Fact]
    public async Task CreateSystem_ReturnsId()
    {
        var (data, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "grid", Width = 10, Height = 10 }));

        status.Should().Be(HttpStatusCode.OK);
        data!.Id.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateSystem_InvalidWidth_Returns400()
    {
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "bad", Width = 0, Height = 10 }));
        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSystem_NegativeHeight_Returns400()
    {
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "bad", Width = 5, Height = -1 }));
        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateSystem_EmptyName_Returns400()
    {
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "", Width = 10, Height = 10 }));
        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetSystem_NotFound_Returns404()
    {
        var (data, status) = await ClientTestHelper.RunAsync(() =>
            _client.GetCoordinateSystemAsync(Guid.NewGuid()));

        status.Should().Be(HttpStatusCode.NotFound);
        data.Should().BeNull();
    }

    [Fact]
    public async Task GetSystem_Exists_ReturnsDetail()
    {
        var (created, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "test-sys", Width = 20, Height = 20 }));
        var (detail, status) = await ClientTestHelper.RunAsync(() =>
            _client.GetCoordinateSystemAsync(created!.Id));

        status.Should().Be(HttpStatusCode.OK);
        detail!.Name.Should().Be("test-sys");
        detail.Width.Should().Be(20);
        detail.Point.Should().BeNull();
    }

    [Fact]
    public async Task DeleteSystem_Exists_Returns204()
    {
        var (created, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "to-delete", Width = 5, Height = 5 }));
        var status = await ClientTestHelper.RunAsync(() => _client.DeleteCoordinateSystemAsync(created!.Id));
        status.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeleteSystem_NotFound_Returns404()
    {
        var status = await ClientTestHelper.RunAsync(() => _client.DeleteCoordinateSystemAsync(Guid.NewGuid()));
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteSystem_AlsoDeletesPoint()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "will-delete", Width = 10, Height = 10 }));
        var (pt, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 0, Y = 0, Direction = Direction.N }));
        await _client.DeleteCoordinateSystemAsync(sys.Id);

        var (_, status) = await ClientTestHelper.RunAsync(() => _client.GetPointAsync(pt!.Id));
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task RequestId_IsReturnedInResponse()
    {
        await _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "reqid-test", Width = 5, Height = 5 });

        _reqIdHandler.LastRequestId.Should().NotBeNullOrEmpty();
        _reqIdHandler.LastResponseId.Should().Be(_reqIdHandler.LastRequestId);
    }
}
