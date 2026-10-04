using FluentAssertions;
using FoodTrack.Application.Common;

namespace FoodTrack.Tests.Application.Common;

public sealed class UtcOffsetTest
{
    [Theory]
    [InlineData(-180, -3, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(330, 5, 30)]
    [InlineData(-720, -12, 0)]
    [InlineData(840, 14, 0)]
    public void FromMinutes_ConvertsRealTimeZoneOffsets(int minutes, int expectedHours, int expectedMinutes)
    {
        var result = UtcOffset.FromMinutes(minutes);

        result.Should().Be(new TimeSpan(expectedHours, expectedMinutes, 0));
    }

    [Theory]
    [InlineData(-721)]
    [InlineData(841)]
    [InlineData(100000)]
    public void FromMinutes_Throws_WhenTheOffsetIsOutsideTheRealRange(int minutes)
    {
        var act = () => UtcOffset.FromMinutes(minutes);

        act.Should().Throw<ArgumentException>();
    }
}
