using AStarDev.ControlDb.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.ScrapeConfiguration;

/// <summary>The validated user settings of a scrape configuration.</summary>
/// <param name="EmailAddress">The email address of the user, empty when not supplied.</param>
/// <param name="Username">The username of the user.</param>
/// <param name="Password">The password of the user.</param>
/// <param name="ApiKey">The API key of the user.</param>
public sealed record UserSettings(string EmailAddress, string Username, string Password, string ApiKey) : IScrapeConfigurationSectionEdit
{
    /// <summary>Copies the user settings from the specified scrape configuration.</summary>
    /// <param name="entity">The scrape configuration to read.</param>
    public static UserSettings From(ScrapeConfigurationEntity entity) => new(
        entity.UserConfiguration.EmailAddress,
        entity.UserConfiguration.Username,
        entity.UserConfiguration.Password,
        entity.UserConfiguration.ApiKey);

    /// <inheritdoc/>
    public void ApplyTo(ScrapeConfigurationEntity entity)
    {
        entity.UserConfiguration.EmailAddress = EmailAddress;
        entity.UserConfiguration.Username = Username;
        entity.UserConfiguration.Password = Password;
        entity.UserConfiguration.ApiKey = ApiKey;
    }
}
