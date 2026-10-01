using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>Interface for processing pages by fetching and handling their content asynchronously.</summary>
public interface IPagesProcessor
{
    /// <summary>Fetches and processes pages asynchronously based on the provided parameters.</summary>
    /// <param name="logLabel">A label used for logging purposes.</param>
    /// <param name="categoryName">The search category name when a category search is being performed, or <see cref="Option{T}.None"/> when fetching the "Top Wallpapers" scrape.</param>
    /// <param name="previousProgress">The progress recorded by the previous scrape of the category, or <see cref="Option{T}.None"/> when there is none or it is the "Top Wallpapers" scrape, which is never skipped. When the previous scrape stopped part-way and the category is unchanged, paging resumes after the last page visited.</param>
    /// <param name="onPageCompleted">Called after each page is ingested, before it is saved, with the progress so far, so an interrupted scrape can resume from the last page visited.</param>
    /// <param name="pageUrlFactory">A function that generates page URLs based on the page number.</param>
    /// <param name="connection">The Wallhaven API credentials and target host to fetch pages from.</param>
    /// <param name="personCategories">The tag category names whose tags are person names when naming saved files.</param>
    /// <param name="progress">The progress reporter to report the fetching and processing progress.</param>
    /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
    /// <returns>The progress observed by this scrape, or <see cref="Option{T}.None"/> when the search was skipped because nothing had changed since the previous scrape.</returns>
    Task<Option<SearchCategoryProgress>> FetchAndProcessPagesAsync(string logLabel, Option<string> categoryName, Option<SearchCategoryProgress> previousProgress, Action<SearchCategoryProgress> onPageCompleted, Func<int, Uri> pageUrlFactory, WallhavenConnection connection, IReadOnlyList<string> personCategories, IProgress<string> progress, CancellationToken cancellationToken);
}