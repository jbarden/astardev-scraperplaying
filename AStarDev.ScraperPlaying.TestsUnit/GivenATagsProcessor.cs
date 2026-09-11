using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Home;
using AStarDev.ScraperPlaying.SearchAPI.DetailResponse;

namespace AStarDev.ScraperPlaying.TestsUnit;

public sealed class GivenATagsProcessor
{
    private readonly IJsonResponseProcessor jsonResponseProcessor = Substitute.For<IJsonResponseProcessor>();
    private readonly ITagsQuery tagsQuery = Substitute.For<ITagsQuery>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<TagEntity, TagId> tagRepository = Substitute.For<IRepository<TagEntity, TagId>>();
    private readonly IFileTagRepository fileTagRepository = Substitute.For<IFileTagRepository>();
    private readonly CapturingProgress progress = new();
    private readonly TagsProcessor processor;

    public GivenATagsProcessor()
    {
        unitOfWork.GetRepository<TagEntity, TagId>().Returns(tagRepository);
        processor = new(jsonResponseProcessor, tagsQuery, unitOfWork, fileTagRepository);
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_new_tag_then_it_is_created_and_linked()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 1, name: "landscape"));
        tagsQuery.TryFindByWallhavenIdAsync(1, Arg.Any<CancellationToken>()).Returns((Exceptional<Option<TagEntity>>)Option<TagEntity>.None.Instance);
        var createdTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 1, Name = "landscape" };
        tagRepository.Add(Arg.Any<TagEntity>()).Returns((Exceptional<TagEntity>)createdTag);
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Received(1).Add(Arg.Is<TagEntity>(tag => tag.WallhavenTagId == 1 && tag.Name == "landscape"));
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.TagId == createdTag.Id));
        progress.Messages.ShouldContain("Fetching tags for wallpaper wallpaper-1.");
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_that_already_exists_then_it_is_reused_and_linked()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 2, name: "space"));
        var existingTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 2, Name = "space" };
        tagsQuery.TryFindByWallhavenIdAsync(2, Arg.Any<CancellationToken>()).Returns((Exceptional<Option<TagEntity>>)(Option<TagEntity>)existingTag);
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.DidNotReceive().Add(Arg.Any<TagEntity>());
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.TagId == existingTag.Id));
    }

    [Fact]
    public async Task when_the_same_tag_appears_twice_in_one_detail_response_then_it_is_linked_only_once()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 3, name: "duplicate"), CreateTag(wallhavenTagId: 3, name: "duplicate"));
        var existingTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 3, Name = "duplicate" };
        tagsQuery.TryFindByWallhavenIdAsync(3, Arg.Any<CancellationToken>()).Returns((Exceptional<Option<TagEntity>>)(Option<TagEntity>)existingTag);
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        fileTagRepository.Received(1).Add(Arg.Any<FileTagEntity>());
    }

    [Fact]
    public async Task when_two_different_wallpapers_introduce_the_same_new_tag_then_it_is_created_once_and_cached_across_calls()
    {
        var tag = CreateTag(wallhavenTagId: 4, name: "shared");
        jsonResponseProcessor.GetFromJsonAsync<DetailResponse>($"{ApplicationConstants.WallhavenDetailPathTemplate}wallpaper-a", Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<DetailResponse>>)(Option<DetailResponse>)CreateDetailResponse(tag));
        jsonResponseProcessor.GetFromJsonAsync<DetailResponse>($"{ApplicationConstants.WallhavenDetailPathTemplate}wallpaper-b", Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<DetailResponse>>)(Option<DetailResponse>)CreateDetailResponse(tag));
        // Simulates the production race: neither call's DB lookup sees the other's not-yet-saved insert, so both return "not found".
        tagsQuery.TryFindByWallhavenIdAsync(4, Arg.Any<CancellationToken>()).Returns((Exceptional<Option<TagEntity>>)Option<TagEntity>.None.Instance);
        var createdTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 4, Name = "shared" };
        tagRepository.Add(Arg.Any<TagEntity>()).Returns((Exceptional<TagEntity>)createdTag);
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());
        var firstFileId = FileId.Create();
        var secondFileId = FileId.Create();

        var firstResult = await processor.FetchAndLinkTagsAsync("wallpaper-a", firstFileId, new HttpClient(), progress, CancellationToken.None);
        var secondResult = await processor.FetchAndLinkTagsAsync("wallpaper-b", secondFileId, new HttpClient(), progress, CancellationToken.None);

        firstResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        secondResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Received(1).Add(Arg.Is<TagEntity>(t => t.WallhavenTagId == 4));
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.FileId == firstFileId && fileTag.TagId == createdTag.Id));
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.FileId == secondFileId && fileTag.TagId == createdTag.Id));
    }

    [Fact]
    public async Task when_the_detail_fetch_fails_then_the_failure_is_returned_not_thrown()
    {
        var exception = new HttpRequestException("boom");
        jsonResponseProcessor.GetFromJsonAsync<DetailResponse>(Arg.Any<string>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<DetailResponse>>)exception);

        var result = await Run();

        var capturedException = result.Match(_ => (Exception?)null, ex => ex);
        capturedException.ShouldBeSameAs(exception);
        tagRepository.DidNotReceive().Add(Arg.Any<TagEntity>());
        fileTagRepository.DidNotReceive().Add(Arg.Any<FileTagEntity>());
    }

    [Fact]
    public async Task when_a_wallpaper_has_no_tags_then_it_is_a_no_op_success()
    {
        SetUpDetailResponse();

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.DidNotReceive().Add(Arg.Any<TagEntity>());
        fileTagRepository.DidNotReceive().Add(Arg.Any<FileTagEntity>());
    }

    private Task<Exceptional<UnitFp>> Run()
        => processor.FetchAndLinkTagsAsync("wallpaper-1", FileId.Create(), new HttpClient(), progress, CancellationToken.None);

    private void SetUpDetailResponse(params Tag[] tags)
        => jsonResponseProcessor.GetFromJsonAsync<DetailResponse>(Arg.Any<string>(), Arg.Any<HttpClient>(), Arg.Any<CancellationToken>())
            .Returns((Exceptional<Option<DetailResponse>>)(Option<DetailResponse>)CreateDetailResponse(tags));

    private static DetailResponse CreateDetailResponse(params Tag[] tags)
        => new(new Data(
            "wallpaper-1", "", "",
            new Uploader("", "", new Avatar("", "", "", "")),
            0, 0, "", "", "", 0, 0, "", "", 0, "", "", [], "",
            new Thumbs("", "", ""),
            tags));

    private static Tag CreateTag(int wallhavenTagId, string name)
        => new(wallhavenTagId, name, name, 1, "Nature", "sfw", "");

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
