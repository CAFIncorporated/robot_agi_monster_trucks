using Xunit;
using FluentAssertions;
using CoordinateService.Models.Domain;
using CoordinateService.Models.Request;
using CoordinateService.Models.Response;

namespace CoordinateService.Tests;

public class ModelTests
{
    [Fact]
    public void Direction_N_S_E_W_AreDefined()
    {
        Enum.IsDefined(Direction.N).Should().BeTrue();
        Enum.IsDefined(Direction.S).Should().BeTrue();
        Enum.IsDefined(Direction.E).Should().BeTrue();
        Enum.IsDefined(Direction.W).Should().BeTrue();
    }

    [Fact]
    public void CoordinateSystem_RecordEquality()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var a = new CoordinateSystem(id, "test", 10, 10, t);
        var b = new CoordinateSystem(id, "test", 10, 10, t);
        a.Should().Be(b);
    }

    [Fact]
    public void CreateCoordinateSystemRequest_Properties()
    {
        var req = new CreateCoordinateSystemRequest("grid", 5, 8);
        req.Name.Should().Be("grid");
        req.Width.Should().Be(5);
        req.Height.Should().Be(8);
    }

    [Fact]
    public void CreatePointRequest_Properties()
    {
        var req = new CreatePointRequest(3, 7, Direction.E);
        req.X.Should().Be(3);
        req.Y.Should().Be(7);
        req.Direction.Should().Be(Direction.E);
    }

    [Fact]
    public void MovePointRequest_ContainsCommands()
    {
        var req = new MovePointRequest(["M", "R", "L"]);
        req.Commands.Should().HaveCount(3);
        req.Commands[1].Should().Be("R");
    }

    [Fact]
    public void MovePointResponse_Properties()
    {
        var id = Guid.NewGuid();
        var sysId = Guid.NewGuid();
        var resp = new MovePointResponse(id, 4, 6, Direction.W, sysId);
        resp.Id.Should().Be(id);
        resp.X.Should().Be(4);
        resp.Y.Should().Be(6);
        resp.Direction.Should().Be(Direction.W);
        resp.SystemId.Should().Be(sysId);
    }

    [Fact]
    public void ErrorResponse_Properties()
    {
        var err = new ErrorResponse("Not found", "System does not exist");
        err.Error.Should().Be("Not found");
        err.Detail.Should().Be("System does not exist");
    }

    [Fact]
    public void PointDetail_RecordEquality()
    {
        var id = Guid.NewGuid();
        var t = DateTime.UtcNow;
        var a = new PointDetail(id, 1, 2, Direction.N, t);
        var b = new PointDetail(id, 1, 2, Direction.N, t);
        a.Should().Be(b);
    }
}
