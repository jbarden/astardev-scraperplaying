using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenATagsProcessor
{
    private readonly ITagsQuery tagsQuery = Substitute.For<ITagsQuery>();
    private readonly IUnitOfWork unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly IRepository<TagEntity, TagId> tagRepository = Substitute.For<IRepository<TagEntity, TagId>>();
    private readonly IFileTagRepository fileTagRepository = Substitute.For<IFileTagRepository>();
    private readonly TagsProcessor processor;

    public GivenATagsProcessor()
    {
        unitOfWork.GetRepository<TagEntity, TagId>().Returns(tagRepository);
        processor = new(tagsQuery, unitOfWork, fileTagRepository);
    }

    [Fact]
    public async Task when_linking_tags_with_only_an_id_and_name_then_a_tag_is_created_with_default_metadata_and_linked()
    {
        tagsQuery.FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(ExistingTags());
        var createdTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 5, Name = "mountains" };
        tagRepository.Add(Arg.Any<TagEntity>()).Returns((Exceptional<TagEntity>)createdTag);
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());
        var fileId = FileId.Create();

        var result = await processor.LinkTagsAsync(fileId, [new WallpaperTag(5, "mountains")], CancellationToken.None);

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Received(1).Add(Arg.Is<TagEntity>(tag =>
            tag.WallhavenTagId == 5 && tag.Name == "mountains" && tag.Alias == string.Empty && tag.CategoryId == 0 && tag.Category == string.Empty && tag.Purity == string.Empty));
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.FileId == fileId && fileTag.TagId == createdTag.Id));
    }

    [Fact]
    public async Task when_the_same_new_tag_is_introduced_by_two_calls_then_it_is_created_once_and_cached_across_calls()
    {
        tagsQuery.FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(ExistingTags());
        var createdTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 6, Name = "shared" };
        tagRepository.Add(Arg.Any<TagEntity>()).Returns((Exceptional<TagEntity>)createdTag);
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());
        var firstFileId = FileId.Create();
        var secondFileId = FileId.Create();

        var firstResult = await processor.LinkTagsAsync(firstFileId, [new WallpaperTag(6, "shared")], CancellationToken.None);
        var secondResult = await processor.LinkTagsAsync(secondFileId, [new WallpaperTag(6, "shared")], CancellationToken.None);

        firstResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        secondResult.Match(_ => true, ex => throw ex).ShouldBeTrue();
        tagRepository.Received(1).Add(Arg.Is<TagEntity>(tag => tag.WallhavenTagId == 6));
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.FileId == firstFileId && fileTag.TagId == createdTag.Id));
        fileTagRepository.Received(1).Add(Arg.Is<FileTagEntity>(fileTag => fileTag.FileId == secondFileId && fileTag.TagId == createdTag.Id));
    }

    [Fact]
    public async Task when_the_same_tag_appears_twice_in_one_call_then_it_is_linked_only_once()
    {
        var existingTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 7, Name = "duplicate" };
        tagsQuery.FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(ExistingTags(existingTag));
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());

        var result = await processor.LinkTagsAsync(FileId.Create(), [new WallpaperTag(7, "duplicate"), new WallpaperTag(7, "duplicate")], CancellationToken.None);

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        fileTagRepository.Received(1).Add(Arg.Any<FileTagEntity>());
    }

    [Fact]
    public async Task when_a_wallpaper_has_several_uncached_tags_then_they_are_looked_up_in_a_single_query_and_only_missing_ones_created()
    {
        var existingTag = new TagEntity { Id = TagId.Create(), WallhavenTagId = 11, Name = "existing" };
        tagsQuery.FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(ExistingTags(existingTag));
        tagRepository.Add(Arg.Any<TagEntity>()).Returns(call => (Exceptional<TagEntity>)call.Arg<TagEntity>());
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());

        var result = await processor.LinkTagsAsync(FileId.Create(), [new WallpaperTag(10, "new-a"), new WallpaperTag(11, "existing"), new WallpaperTag(12, "new-b")], CancellationToken.None);

        result.Match(_ => true, ex => throw ex).ShouldBeTrue();
        await tagsQuery.Received(1).FindByWallhavenIdsAsync(Arg.Is<IReadOnlyCollection<int>>(ids => ids.Count == 3), Arg.Any<CancellationToken>());
        tagRepository.Received(2).Add(Arg.Any<TagEntity>());
        fileTagRepository.Received(3).Add(Arg.Any<FileTagEntity>());
    }

    [Fact]
    public async Task when_every_tag_is_already_cached_then_no_query_is_made()
    {
        tagsQuery.FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns(ExistingTags());
        tagRepository.Add(Arg.Any<TagEntity>()).Returns(call => (Exceptional<TagEntity>)call.Arg<TagEntity>());
        fileTagRepository.Add(Arg.Any<FileTagEntity>()).Returns(call => (Exceptional<FileTagEntity>)call.Arg<FileTagEntity>());
        await processor.LinkTagsAsync(FileId.Create(), [new WallpaperTag(20, "cached")], CancellationToken.None);
        tagsQuery.ClearReceivedCalls();

        await processor.LinkTagsAsync(FileId.Create(), [new WallpaperTag(20, "cached")], CancellationToken.None);

        await tagsQuery.DidNotReceive().FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task when_the_tag_lookup_fails_then_the_failure_is_returned()
    {
        var exception = new InvalidOperationException("db down");
        tagsQuery.FindByWallhavenIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>()).Returns((Exceptional<IReadOnlyList<TagEntity>>)exception);

        var result = await processor.LinkTagsAsync(FileId.Create(), [new WallpaperTag(30, "tag")], CancellationToken.None);

        result.Match(_ => (Exception?)null, ex => ex).ShouldBeSameAs(exception);
    }

    private static Exceptional<IReadOnlyList<TagEntity>> ExistingTags(params TagEntity[] tags)
        => Exceptional.Success<IReadOnlyList<TagEntity>>(tags);
}
