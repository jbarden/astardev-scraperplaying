using AStarDev.ControlDb.ScrapeConfiguration;
using AStarDev.ScraperPlaying.SearchAPI;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenASearchCategoryMapping
{
    [Fact]
    public void when_an_entity_is_mapped_then_all_fields_are_copied()
    {
        var entity = new SearchCategoryEntity
        {
            Id = "1",
            Name = "General",
            LastKnownImageCount = 42,
            LastPageVisited = 3,
            TotalPages = 10,
            IncludeInSearch = false,
            IsFamous = true,
            IsInternet = true
        };

        var dto = entity.ToDto();

        dto.Id.ShouldBe("1");
        dto.Name.ShouldBe("General");
        dto.LastKnownImageCount.ShouldBe(42);
        dto.LastPageVisited.ShouldBe(3);
        dto.TotalPages.ShouldBe(10);
        dto.IncludeInSearch.ShouldBeFalse();
        dto.IsFamous.ShouldBeTrue();
        dto.IsInternet.ShouldBeTrue();
    }

    [Fact]
    public void when_a_collection_is_mapped_then_every_entity_is_converted_in_order()
    {
        ICollection<SearchCategoryEntity> entities =
        [
            new() { Id = "1", Name = "General" },
            new() { Id = "2", Name = "Anime" }
        ];

        var dtos = entities.ToDtos();

        dtos.Length.ShouldBe(2);
        dtos[0].Id.ShouldBe("1");
        dtos[1].Id.ShouldBe("2");
    }

    [Fact]
    public void when_the_collection_is_empty_then_an_empty_array_is_returned()
    {
        ICollection<SearchCategoryEntity> entities = [];

        var dtos = entities.ToDtos();

        dtos.ShouldBeEmpty();
    }
}
