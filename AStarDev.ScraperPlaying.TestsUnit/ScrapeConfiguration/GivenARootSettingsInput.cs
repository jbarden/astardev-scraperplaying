using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.ScrapeConfiguration;

namespace AStarDev.ScraperPlaying.TestsUnit.ScrapeConfiguration;

public sealed class GivenARootSettingsInput
{
    private static readonly RootSettingsInput ValidInput = new(
        "https://wallhaven.cc/api/v1",
        "https://wallhaven.cc/login",
        "api-key",
        "cats",
        "top",
        "prefix-",
        "-suffix",
        "subscriptions",
        5,
        1,
        20,
        2,
        30,
        3,
        40,
        true,
        Option.Some(250f));

    [Fact]
    public void when_the_input_is_valid_then_the_settings_carry_every_value()
    {
        var result = ValidInput.Validate();

        var settings = result.ShouldBeOfType<Valid<RootSettings>>().Value;
        settings.BaseUrl.ShouldBe(new Uri("https://wallhaven.cc/api/v1"));
        settings.LoginUrl.ShouldBe("https://wallhaven.cc/login");
        settings.ApiKey.ShouldBe("api-key");
        settings.SearchString.ShouldBe("cats");
        settings.TopWallpapers.ShouldBe("top");
        settings.SearchStringPrefix.ShouldBe("prefix-");
        settings.SearchStringSuffix.ShouldBe("-suffix");
        settings.Subscriptions.ShouldBe("subscriptions");
        settings.ImagePauseInSeconds.ShouldBe(5);
        settings.StartingPageNumber.ShouldBe(1);
        settings.TotalPages.ShouldBe(20);
        settings.SubscriptionsStartingPageNumber.ShouldBe(2);
        settings.SubscriptionsTotalPages.ShouldBe(30);
        settings.TopWallpapersStartingPageNumber.ShouldBe(3);
        settings.TopWallpapersTotalPages.ShouldBe(40);
        settings.UseHeadless.ShouldBeTrue();
        settings.SlowMotionDelay.ShouldBe(Option.Some(250f));
    }

    [Fact]
    public void when_the_starting_page_equals_the_total_pages_then_the_input_is_valid() =>
        (ValidInput with { StartingPageNumber = 20 }).Validate().ShouldBeOfType<Valid<RootSettings>>();

    [Fact]
    public void when_the_slow_motion_delay_is_absent_then_the_input_is_valid() =>
        (ValidInput with { SlowMotionDelay = Option.None<float>() }).Validate().ShouldBeOfType<Valid<RootSettings>>();

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a url")]
    [InlineData("relative/path")]
    [InlineData("ftp://example.com")]
    public void when_the_base_url_is_not_an_absolute_http_url_then_it_is_rejected(string enteredText) =>
        ErrorsFor(ValidInput with { BaseUrl = enteredText }).ShouldContain(error => error.Property == nameof(RootSettingsInput.BaseUrl));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("login")]
    [InlineData("not a url, just text: /login?x=1 & more")]
    [InlineData("ftp://example.com/login")]
    public void when_the_login_url_is_any_text_then_it_is_valid_and_stored_exactly_as_entered(string enteredText) =>
        (ValidInput with { LoginUrl = enteredText }).Validate().ShouldBeOfType<Valid<RootSettings>>().Value.LoginUrl.ShouldBe(enteredText);

    [Fact]
    public void when_the_image_pause_is_negative_then_it_is_rejected() =>
        ErrorsFor(ValidInput with { ImagePauseInSeconds = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.ImagePauseInSeconds));

    [Fact]
    public void when_the_image_pause_is_zero_then_the_input_is_valid() =>
        (ValidInput with { ImagePauseInSeconds = 0 }).Validate().ShouldBeOfType<Valid<RootSettings>>();

    [Fact]
    public void when_a_page_count_is_negative_then_it_is_rejected()
    {
        ErrorsFor(ValidInput with { StartingPageNumber = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.StartingPageNumber));
        ErrorsFor(ValidInput with { TotalPages = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.TotalPages));
        ErrorsFor(ValidInput with { SubscriptionsStartingPageNumber = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.SubscriptionsStartingPageNumber));
        ErrorsFor(ValidInput with { SubscriptionsTotalPages = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.SubscriptionsTotalPages));
        ErrorsFor(ValidInput with { TopWallpapersStartingPageNumber = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.TopWallpapersStartingPageNumber));
        ErrorsFor(ValidInput with { TopWallpapersTotalPages = -1 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.TopWallpapersTotalPages));
    }

    [Fact]
    public void when_a_starting_page_exceeds_its_total_pages_then_it_is_rejected()
    {
        ErrorsFor(ValidInput with { StartingPageNumber = 21 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.StartingPageNumber));
        ErrorsFor(ValidInput with { SubscriptionsStartingPageNumber = 31 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.SubscriptionsStartingPageNumber));
        ErrorsFor(ValidInput with { TopWallpapersStartingPageNumber = 41 }).ShouldContain(error => error.Property == nameof(RootSettingsInput.TopWallpapersStartingPageNumber));
    }

    [Fact]
    public void when_the_slow_motion_delay_is_negative_then_it_is_rejected() =>
        ErrorsFor(ValidInput with { SlowMotionDelay = Option.Some(-1f) }).ShouldContain(error => error.Property == nameof(RootSettingsInput.SlowMotionDelay));

    [Fact]
    public void when_several_values_are_invalid_then_every_error_is_reported() =>
        ErrorsFor(ValidInput with { BaseUrl = string.Empty, ImagePauseInSeconds = -1 }).Count.ShouldBe(2);

    private static IReadOnlyList<ValidationError> ErrorsFor(RootSettingsInput input) =>
        input.Validate().ShouldBeOfType<Invalid<RootSettings>>().Errors;
}
