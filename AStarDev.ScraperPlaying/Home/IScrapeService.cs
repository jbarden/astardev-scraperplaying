namespace AStarDev.ScraperPlaying.Home;

public interface IScrapeService
{
    Task RunScraperAsync(IProgress<string> progress);
}
