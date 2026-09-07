using AStarDev.Utilities;
using static AStarDev.ControlDb.TestsUnit.Utilities.RegexHelpers;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenAFileId
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = FileId.Create().ToJson();
        sut.ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
    }
}
