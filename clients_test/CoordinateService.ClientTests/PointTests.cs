using System.Net;
using Xunit;
using FluentAssertions;
using CoordinateService.Client;

namespace CoordinateService.ClientTests;

public class PointTests : IClassFixture<TestWebAppFactory>
{
    private readonly CoordinateServiceClient _client;

    public PointTests(TestWebAppFactory factory)
    {
        var http = factory.CreateClient();
        _client = new CoordinateServiceClient(http);
    }

    [Fact]
    public async Task CreatePoint_ValidPosition_Returns200()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "pt-grid", Width = 10, Height = 10 }));
        var (pt, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 5, Y = 5, Direction = Direction.N }));

        status.Should().Be(HttpStatusCode.OK);
        pt!.X.Should().Be(5);
        pt.Y.Should().Be(5);
        pt.Direction.Should().Be(Direction.N);
        pt.SystemId.Should().Be(sys.Id);
    }

    [Fact]
    public async Task CreatePoint_OutOfBounds_Returns400()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "small", Width = 3, Height = 3 }));
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 5, Y = 0, Direction = Direction.E }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePoint_NegativeCoords_Returns400()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "neg", Width = 10, Height = 10 }));
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = -1, Y = 0, Direction = Direction.N }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreatePoint_SystemNotFound_Returns404()
    {
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(Guid.NewGuid(), new CreatePointRequest { X = 0, Y = 0, Direction = Direction.N }));
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task CreatePoint_DuplicateInSystem_Returns400()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "one-pt", Width = 10, Height = 10 }));
        await _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 0, Y = 0, Direction = Direction.N });
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys.Id, new CreatePointRequest { X = 1, Y = 1, Direction = Direction.S }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPoint_Exists_ReturnsDetail()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "get-pt", Width = 10, Height = 10 }));
        var (created, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 3, Y = 7, Direction = Direction.W }));
        var (pt, status) = await ClientTestHelper.RunAsync(() => _client.GetPointAsync(created!.Id));

        status.Should().Be(HttpStatusCode.OK);
        pt!.X.Should().Be(3);
        pt.Y.Should().Be(7);
        pt.Direction.Should().Be(Direction.W);
    }

    [Fact]
    public async Task GetPoint_NotFound_Returns404()
    {
        var (_, status) = await ClientTestHelper.RunAsync(() => _client.GetPointAsync(Guid.NewGuid()));
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePoint_Exists_Returns204()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "del-pt", Width = 10, Height = 10 }));
        var (pt, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 0, Y = 0, Direction = Direction.N }));
        var status = await ClientTestHelper.RunAsync(() => _client.DeletePointAsync(pt!.Id));

        status.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeletePoint_NotFound_Returns404()
    {
        var status = await ClientTestHelper.RunAsync(() => _client.DeletePointAsync(Guid.NewGuid()));
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetSystem_IncludesPoint()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "with-pt", Width = 10, Height = 10 }));
        await _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 2, Y = 3, Direction = Direction.E });
        var (detail, _) = await ClientTestHelper.RunAsync(() => _client.GetCoordinateSystemAsync(sys.Id));

        detail!.Point.Should().NotBeNull();
        detail.Point!.X.Should().Be(2);
        detail.Point.Y.Should().Be(3);
    }

    [Fact]
    public async Task CreatePoint_AtBoundaryEdge_Returns200()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "edge", Width = 5, Height = 5 }));
        var (pt, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 4, Y = 4, Direction = Direction.S }));

        status.Should().Be(HttpStatusCode.OK);
        pt!.X.Should().Be(4);
        pt.Y.Should().Be(4);
    }

    [Fact]
    public async Task CreatePoint_ExactlyAtWidth_Returns400()
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "at-w", Width = 5, Height = 5 }));
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = 5, Y = 0, Direction = Direction.N }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }
}
