using AStarDev.Utilities;
using static AStarDev.ControlDb.TestsUnit.Utilities.RegexHelpers;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenAImageDetailEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = ImageDetailEntityFactory.CreateImageDetailEntity().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
    }
}
