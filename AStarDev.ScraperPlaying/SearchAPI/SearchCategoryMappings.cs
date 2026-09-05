using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.SearchAPI;

public static class SearchCategoryMappings
{
    public static SearchCategory ToDto(this SearchCategoryEntity entity)
        => new()
        {
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt,
            Id = entity.Id,
            Name = entity.Name,
            LastKnownImageCount = entity.LastKnownImageCount,
            LastPageVisited = entity.LastPageVisited,
            TotalPages = entity.TotalPages,
            IncludeInSearch = entity.IncludeInSearch,
            IsFamous = entity.IsFamous,
            IsInternet = entity.IsInternet
        };

    public static SearchCategory[] ToDtos(this ICollection<SearchCategoryEntity> entities)
        => [.. entities.Select(e => e.ToDto())];
}
