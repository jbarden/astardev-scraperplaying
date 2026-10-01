using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using AStarDev.ScraperPlaying.Scraping.WallhavenResponses.DetailResponse;
using AStarDev.ScraperPlaying.TestsUnit.Fakes;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenATagsProcessor
{
    private readonly FakeJsonResponseProcessor jsonResponseProcessor = new();
    private readonly FakeTagsQuery tagsQuery = new();
    private readonly FakeUnitOfWork unitOfWork = new();
    private readonly FakeRepository<TagEntity, TagId> tagRepository;
    private readonly FakeFileTagRepository fileTagRepository = new();
    private readonly CapturingProgress progress = new();
    private readonly TagsProcessor processor;

    public GivenATagsProcessor()
    {
        tagRepository = unitOfWork.Register<TagEntity, TagId>();
        processor = new(jsonResponseProcessor, tagsQuery, unitOfWork, fileTagRepository);
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_new_tag_then_it_is_created_and_linked()
    {
        var createdId = TagId.Create();
        tagRepository.Store = tag =>
        {
            tag.Id = createdId;

            return tag;
        };
        SetUpDetailResponse(CreateTag(wallhavenTagId: 1, name: "landscape"));

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        var created = tagRepository.Added.Single();
        (created.WallhavenTagId, created.Name).ShouldBe((1, "landscape"));
        fileTagRepository.Added.Single().TagId.ShouldBe(createdId);
        progress.Messages.ShouldContain("Fetching tags for wallpaper wallpaper-1.");
    }

    [Fact]
    public async Task when_a_wallpaper_has_a_tag_that_already_exists_then_it_is_reused_and_linked()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 2, name: "space"));
        var existingTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 2, Name = "space" };
        tagsQuery.Existing[2] = existingTag;

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Added.ShouldBeEmpty();
        fileTagRepository.Added.Single().TagId.ShouldBe(existingTag.Id);
    }

    [Fact]
    public async Task when_the_same_tag_appears_twice_in_one_detail_response_then_it_is_linked_only_once()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 3, name: "duplicate"), CreateTag(wallhavenTagId: 3, name: "duplicate"));
        tagsQuery.Existing[3] = new TagEntity { Id = TagId.Create(), WallhavenTagId = 3, Name = "duplicate" };

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        fileTagRepository.Added.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_two_different_wallpapers_introduce_the_same_new_tag_then_it_is_created_once_and_cached_across_calls()
    {
        var tag = CreateTag(wallhavenTagId: 4, name: "shared");
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(tag);
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(tag);
        // The tags query never sees the other call's not-yet-saved insert, simulating the production race: both lookups return "not found".
        var createdId = TagId.Create();
        tagRepository.Store = created =>
        {
            created.Id = createdId;

            return created;
        };
        var firstFileId = FileId.Create();
        var secondFileId = FileId.Create();

        var firstResult = await FetchAndLink("wallpaper-a", firstFileId);
        var secondResult = await FetchAndLink("wallpaper-b", secondFileId);

        firstResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        secondResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Added.Single().WallhavenTagId.ShouldBe(4);
        fileTagRepository.Added.Select(fileTag => (fileTag.FileId, fileTag.TagId)).ShouldBe([(firstFileId, createdId), (secondFileId, createdId)]);
    }

    [Fact]
    public async Task when_a_wallpaper_has_several_tags_then_the_existing_ones_are_looked_up_with_a_single_query_of_the_distinct_ids()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 5, name: "one"), CreateTag(wallhavenTagId: 6, name: "two"), CreateTag(wallhavenTagId: 5, name: "one"), CreateTag(wallhavenTagId: 7, name: "three"));
        tagsQuery.Existing[6] = new TagEntity { Id = TagId.Create(), WallhavenTagId = 6, Name = "two" };

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        (tagsQuery.Queries.Count, string.Join(",", tagsQuery.Queries.Single().Order()), string.Join(",", tagRepository.Added.Select(tag => tag.WallhavenTagId).Order())).ShouldBe((1, "5,6,7", "5,7"));
    }

    [Fact]
    public async Task when_every_tag_of_a_later_wallpaper_is_already_cached_then_no_query_is_made()
    {
        var tag = CreateTag(wallhavenTagId: 8, name: "cached");
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(tag);
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(tag);

        _ = await FetchAndLink("wallpaper-a", FileId.Create());
        _ = await FetchAndLink("wallpaper-b", FileId.Create());

        tagsQuery.Queries.Count.ShouldBe(1);
    }

    [Fact]
    public async Task when_tags_are_fetched_then_they_are_returned_with_their_categories()
    {
        SetUpDetailResponse(new Tag(9, "Max Verstappen", "max-verstappen", 51, "Other Figures", "sfw"));

        using var client = new HttpClient();

        var result = await processor.FetchTagsAsync("wallpaper-1", client, progress, CancellationToken.None);

        result.Match(tags => tags, ex => throw ex).ShouldBe([new Tag(9, "Max Verstappen", "max-verstappen", 51, "Other Figures", "sfw")]);
        tagRepository.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_fetched_tag_is_flagged_to_ignore_images_then_it_is_returned_flagged()
    {
        SetUpDetailResponse(CreateTag(wallhavenTagId: 20, name: "unwanted"), CreateTag(wallhavenTagId: 21, name: "wanted"));
        tagsQuery.Ignored.Add(20);

        using var client = new HttpClient();
        var result = await processor.FetchTagsAsync("wallpaper-1", client, progress, CancellationToken.None);

        result.Match(tags => tags.Select(tag => (tag.Id, tag.IgnoreImage)).ToList(), ex => throw ex).ShouldBe([(20, true), (21, false)]);
    }

    [Fact]
    public async Task when_tags_are_fetched_for_several_wallpapers_then_the_ignored_tags_are_only_loaded_once()
    {
        jsonResponseProcessor.Responses["wallpaper-a"] = CreateDetailResponse(CreateTag(wallhavenTagId: 22, name: "one"));
        jsonResponseProcessor.Responses["wallpaper-b"] = CreateDetailResponse(CreateTag(wallhavenTagId: 23, name: "two"));

        using var client = new HttpClient();
        _ = await processor.FetchTagsAsync("wallpaper-a", client, progress, CancellationToken.None);
        _ = await processor.FetchTagsAsync("wallpaper-b", client, progress, CancellationToken.None);

        tagsQuery.IgnoredQueryCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_the_detail_fetch_fails_then_the_failure_is_returned_not_thrown()
    {
        var exception = new HttpRequestException("boom");
        jsonResponseProcessor.Failure = Option.Some<Exception>(exception);

        var result = await Run();

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
        tagRepository.Added.ShouldBeEmpty();
        fileTagRepository.Added.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_wallpaper_has_no_tags_then_it_is_a_no_op_success()
    {
        SetUpDetailResponse();

        var result = await Run();

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Added.ShouldBeEmpty();
        fileTagRepository.Added.ShouldBeEmpty();
    }

    private Task<Exceptional<Unit>> Run()
        => FetchAndLink("wallpaper-1", FileId.Create());

    private async Task<Exceptional<Unit>> FetchAndLink(string wallpaperId, FileId fileId)
    {
        using var client = new HttpClient();

        return await (await processor.FetchTagsAsync(wallpaperId, client, progress, CancellationToken.None))
            .Match(tags => processor.LinkTagsAsync(fileId, tags, CancellationToken.None), exception => Task.FromResult((Exceptional<Unit>)exception));
    }

    private void SetUpDetailResponse(params Tag[] tags)
        => jsonResponseProcessor.Responses["wallpaper-1"] = CreateDetailResponse(tags);

    private static DetailResponse CreateDetailResponse(params Tag[] tags)
        => new(new Data(tags));

    private static Tag CreateTag(int wallhavenTagId, string name)
        => new(wallhavenTagId, name, name, 1, "Nature", "sfw");

    private sealed class FakeJsonResponseProcessor : IJsonResponseProcessor
    {
        public Dictionary<string, DetailResponse> Responses { get; } = [];

        public Option<Exception> Failure { get; set; } = Option.None<Exception>();

        public Task<Exceptional<Option<T>>> GetFromJsonAsync<T>(Uri url, HttpClient client, CancellationToken cancellationToken)
        {
            if (Failure is Option<Exception>.Some failure) return Task.FromResult<Exceptional<Option<T>>>(failure.Value);

            var wallpaperId = url.OriginalString[ApplicationConstants.WallhavenDetailPathTemplate.Length..];
            Option<T> response = Responses.TryGetValue(wallpaperId, out var detail) ? (Option<T>)(T)(object)detail : Option<T>.None.Instance;

            return Task.FromResult<Exceptional<Option<T>>>(response);
        }
    }

    private sealed class FakeTagsQuery : ITagsQuery
    {
        public Dictionary<int, TagEntity> Existing { get; } = [];

        public List<IReadOnlyCollection<int>> Queries { get; } = [];

        public HashSet<int> Ignored { get; } = [];

        public int IgnoredQueryCount { get; private set; }

        public Task<Exceptional<IReadOnlyList<TagEntity>>> ListAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Exceptional<IReadOnlyList<TagEntity>>>(Existing.Values.ToList());

        public Task<Exceptional<IReadOnlyCollection<int>>> GetIgnoredWallhavenIdsAsync(CancellationToken cancellationToken = default)
        {
            IgnoredQueryCount++;

            return Task.FromResult<Exceptional<IReadOnlyCollection<int>>>(Ignored.ToList());
        }

        public Task<Exceptional<IReadOnlyList<TagEntity>>> FindByWallhavenIdsAsync(IReadOnlyCollection<int> wallhavenTagIds, CancellationToken cancellationToken = default)
        {
            Queries.Add(wallhavenTagIds);

            return Task.FromResult<Exceptional<IReadOnlyList<TagEntity>>>(wallhavenTagIds.Where(Existing.ContainsKey).Select(id => Existing[id]).ToList());
        }
    }

    private sealed class FakeFileTagRepository : IFileTagRepository
    {
        public List<FileTagEntity> Added { get; } = [];

        public Exceptional<FileTagEntity> Add(FileTagEntity fileTag)
        {
            Added.Add(fileTag);

            return fileTag;
        }
    }

    private sealed class CapturingProgress : IProgress<string>
    {
        public List<string> Messages { get; } = [];

        public void Report(string value) => Messages.Add(value);
    }
}
