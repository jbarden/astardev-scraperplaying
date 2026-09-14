namespace AStarDev.Utilities.TestsUnit;

public class GivenByteSizeExtensions
{
    [Fact]
    public void when_the_byte_count_is_zero_then_to_file_size_string_returns_zero_bytes()
        => 0.ToFileSizeString().ShouldBe("0 B");

    [Fact]
    public void when_the_byte_count_is_negative_then_to_file_size_string_returns_zero_bytes()
        => (-1).ToFileSizeString().ShouldBe("0 B");

    [Fact]
    public void when_the_byte_count_is_under_a_kilobyte_then_to_file_size_string_formats_in_bytes()
        => 512.ToFileSizeString().ShouldBe("512 B");

    [Fact]
    public void when_the_byte_count_is_a_whole_number_of_kilobytes_then_to_file_size_string_formats_in_kilobytes()
        => (42 * 1024).ToFileSizeString().ShouldBe("42 KB");

    [Fact]
    public void when_the_byte_count_is_a_fractional_number_of_megabytes_then_to_file_size_string_formats_in_megabytes()
        => (3 * 1024 * 1024 + 512 * 1024).ToFileSizeString().ShouldBe("3.5 MB");

    [Fact]
    public void when_the_byte_count_is_a_whole_number_of_gigabytes_then_to_file_size_string_formats_in_gigabytes()
        => (1024 * 1024 * 1024).ToFileSizeString().ShouldBe("1 GB");
}
