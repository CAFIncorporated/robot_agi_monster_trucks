using System.Net;
using Xunit;
using FluentAssertions;
using CoordinateService.Client;

namespace CoordinateService.ClientTests;

public class MoveTests : IClassFixture<TestWebAppFactory>
{
    private readonly CoordinateServiceClient _client;

    public MoveTests(TestWebAppFactory factory)
    {
        var http = factory.CreateClient();
        _client = new CoordinateServiceClient(http);
    }

    private async Task<(Guid SystemId, Guid PointId)> CreateSystemWithPoint(int w, int h, int x, int y, Direction dir)
    {
        var (sys, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreateCoordinateSystemAsync(new CreateCoordinateSystemRequest { Name = "move-test", Width = w, Height = h }));
        var (pt, _) = await ClientTestHelper.RunAsync(() =>
            _client.CreatePointAsync(sys!.Id, new CreatePointRequest { X = x, Y = y, Direction = dir }));
        return (sys!.Id, pt!.Id);
    }

    [Fact]
    public async Task Move_SingleStep_East()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 5, 5, Direction.N);
        var (result, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "R", "M" } }));

        status.Should().Be(HttpStatusCode.OK);
        result!.X.Should().Be(6);
        result.Y.Should().Be(5);
        result.Direction.Should().Be(Direction.E);
    }

    [Fact]
    public async Task Move_SingleStep_North()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 5, 5, Direction.S);
        var (result, _) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "R", "R", "M" } }));

        result!.X.Should().Be(5);
        result.Y.Should().Be(4);
        result.Direction.Should().Be(Direction.N);
    }

    [Fact]
    public async Task Move_SingleStep_South()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 5, 5, Direction.N);
        var (result, _) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "R", "R", "M" } }));

        result!.Y.Should().Be(6);
        result.Direction.Should().Be(Direction.S);
    }

    [Fact]
    public async Task Move_SingleStep_West()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 5, 5, Direction.E);
        var (result, _) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "R", "R", "M" } }));

        result!.X.Should().Be(4);
        result.Direction.Should().Be(Direction.W);
    }

    [Fact]
    public async Task Move_MultipleSteps()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 5, 5, Direction.N);
        var (result, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "R", "M", "M", "R", "M", "M", "R", "M" } }));

        status.Should().Be(HttpStatusCode.OK);
        result!.X.Should().Be(6);
        result.Y.Should().Be(7);
        result.Direction.Should().Be(Direction.W);
    }

    [Fact]
    public async Task Move_OutOfBounds_North_Returns400()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 0, 0, Direction.N);
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "M" } }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_OutOfBounds_West_Returns400()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 0, 5, Direction.W);
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "M" } }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_OutOfBounds_South_Returns400()
    {
        var (_, ptId) = await CreateSystemWithPoint(5, 5, 2, 4, Direction.S);
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "M" } }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_OutOfBounds_East_Returns400()
    {
        var (_, ptId) = await CreateSystemWithPoint(5, 5, 4, 2, Direction.E);
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "M" } }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_OutOfBounds_MidSequence_Returns400()
    {
        var (_, ptId) = await CreateSystemWithPoint(5, 5, 3, 0, Direction.N);
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "M" } }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_EmptyCommands_Returns400()
    {
        var (_, ptId) = await CreateSystemWithPoint(10, 10, 5, 5, Direction.N);
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string>() }));

        status.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Move_PointNotFound_Returns404()
    {
        var (_, status) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(Guid.NewGuid(), new MovePointRequest { Commands = new List<string> { "M" } }));
        status.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Move_ToCornerAndBack()
    {
        var (_, ptId) = await CreateSystemWithPoint(3, 3, 1, 1, Direction.N);

        var (r1, _) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "L", "M", "R", "M" } }));
        r1!.X.Should().Be(0);
        r1.Y.Should().Be(0);

        var (r2, _) = await ClientTestHelper.RunAsync(() =>
            _client.MovePointAsync(ptId, new MovePointRequest { Commands = new List<string> { "R", "M", "M", "R", "M", "M", "L" } }));
        r2!.X.Should().Be(2);
        r2.Y.Should().Be(2);
        r2.Direction.Should().Be(Direction.E);
    }
}
