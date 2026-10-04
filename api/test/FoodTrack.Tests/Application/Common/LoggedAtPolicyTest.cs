using FluentAssertions;
using FoodTrack.Application.Common;

namespace FoodTrack.Tests.Application.Common;

public sealed class LoggedAtPolicyTest
{
    private static readonly DateTimeOffset Now = new(2026, 10, 3, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Resolve_ReturnsNow_WhenNoTimeWasRequested()
    {
        var result = LoggedAtPolicy.Resolve(null, Now);

        result.Should().Be(Now);
    }

    [Fact]
    public void Resolve_ReturnsTheRequestedTime_WhenItIsInThePast()
    {
        var requested = Now.AddDays(-3);

        var result = LoggedAtPolicy.Resolve(requested, Now);

        result.Should().Be(requested);
    }

    [Fact]
    public void Resolve_ReturnsTheRequestedTime_WhenItIsAheadByLessThanTheTolerance()
    {
        var requested = Now + LoggedAtPolicy.FutureTolerance - TimeSpan.FromSeconds(1);

        var result = LoggedAtPolicy.Resolve(requested, Now);

        result.Should().Be(requested);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(60 * 24)]
    public void Resolve_Throws_WhenTheRequestedTimeIsBeyondTheTolerance(int minutesBeyondTolerance)
    {
        var requested = Now + LoggedAtPolicy.FutureTolerance + TimeSpan.FromMinutes(minutesBeyondTolerance);

        var act = () => LoggedAtPolicy.Resolve(requested, Now);

        act.Should().Throw<ArgumentException>().WithMessage("*future*");
    }
}
