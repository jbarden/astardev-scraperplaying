using AStarDev.FunctionalParadigm;

namespace AStarDev.ScraperPlaying.Scraping;

/// <summary>One search to scrape: a category search or the "Top Wallpapers" scrape.</summary>
/// <param name="LogLabel">A label used to identify the search in progress messages.</param>
/// <param name="CategoryName">The search category name when a category search is being performed, or <see cref="Option{T}.None"/> for the "Top Wallpapers" scrape.</param>
/// <param name="PageUrlFactory">A function that generates the search's page URLs based on the page number.</param>
public sealed record SearchRequest(string LogLabel, Option<string> CategoryName, Func<int, string> PageUrlFactory);
