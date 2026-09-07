using AStarDev.Utilities;
using AStarDev.ControlDb.TestsUnit.TestDataFactories;
using static AStarDev.ControlDb.TestsUnit.Utilities.RegexHelpers;

namespace AStarDev.ControlDb.TestsUnit.FileDetail;

public partial class GivenAFileDetailEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = FileDetailEntityFactory.CreateFileDetailEntity().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
    }
}

public partial class GivenAFileAccessDetailEntity
{
    [Fact]
    public void when_properties_are_set_correctly_the_properties_are_assigned_as_expected()
    {
        string sut = FileAccessDetailEntityFactory.CreateFileAccessDetailEntity().ToJson() + Environment.NewLine;
        sut.ShouldMatchApproved(c => c.WithScrubber(s => DateTimeFormatRegex().Replace(s, "<date>")));
    }
}
