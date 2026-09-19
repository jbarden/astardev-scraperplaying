using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.FunctionalParadigm;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAnIngestionStore
{
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly FakeTagsProcessor tagsProcessor = new();
    private readonly List<string> messages = [];
    private readonly IngestionStore store;

    public GivenAnIngestionStore()
    {
        unitOfWork = new(fileRepository);
        store = new(unitOfWork, tagsProcessor);
    }

    [Fact]
    public void when_the_file_repository_is_requested_then_the_units_file_repository_is_returned()
        => store.FileRepository.ShouldBeSameAs(fileRepository);

    [Fact]
    public async Task when_a_page_is_saved_then_the_changes_are_saved_with_the_given_token()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        await store.SavePageAsync(new Progress(messages), cancellationTokenSource.Token);

        unitOfWork.SaveTokens.ShouldBe([cancellationTokenSource.Token]);
        messages.ShouldBeEmpty();
    }

    [Fact]
    public async Task when_a_page_is_saved_then_the_change_tracker_is_cleared_and_its_new_tags_are_accepted()
    {
        await store.SavePageAsync(new Progress(messages), TestContext.Current.CancellationToken);

        unitOfWork.ClearCount.ShouldBe(1);
        tagsProcessor.AcceptedCount.ShouldBe(1);
        tagsProcessor.DiscardedCount.ShouldBe(0);
    }

    [Fact]
    public async Task when_saving_a_page_fails_then_it_is_reported_the_pending_changes_and_tags_are_discarded_and_nothing_is_thrown()
    {
        unitOfWork.FailWith = new DbUpdateException("unique violation");

        await store.SavePageAsync(new Progress(messages), TestContext.Current.CancellationToken);

        messages.ShouldBe(["Failed to save the wallpapers ingested from this page, continuing with the next page: unique violation"]);
        unitOfWork.ClearCount.ShouldBe(1);
        tagsProcessor.AcceptedCount.ShouldBe(0);
        tagsProcessor.DiscardedCount.ShouldBe(1);
    }

    [Fact]
    public async Task when_saving_a_page_is_cancelled_then_the_cancellation_propagates()
    {
        unitOfWork.FailWith = new OperationCanceledException();

        await Should.ThrowAsync<OperationCanceledException>(() => store.SavePageAsync(new Progress(messages), TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task when_saving_after_cancellation_then_the_changes_are_saved_without_a_token_and_success_is_reported()
    {
        await store.SaveAfterCancellationAsync(new Progress(messages));

        unitOfWork.SaveTokens.ShouldBe([CancellationToken.None]);
        messages.ShouldBe(["Scrape cancelled - saved wallpapers downloaded so far this page."]);
    }

    [Fact]
    public async Task when_saving_after_cancellation_fails_then_the_failure_is_reported_not_thrown()
    {
        unitOfWork.FailWith = new DbUpdateException("save failed");

        await store.SaveAfterCancellationAsync(new Progress(messages));

        messages.ShouldBe(["Scrape cancelled - failed to save wallpapers downloaded so far this page: save failed"]);
        unitOfWork.ClearCount.ShouldBe(1);
    }

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
    }

    private sealed class FakeTagsProcessor : ITagsProcessor
    {
        public int AcceptedCount { get; private set; }

        public int DiscardedCount { get; private set; }

        public Task<Exceptional<Unit>> LinkTagsAsync(FileId fileId, IReadOnlyList<WallpaperTag> tags, CancellationToken cancellationToken) => Task.FromResult(Exceptional.Success(Unit.Instance));

        public void AcceptPendingTags() => AcceptedCount++;

        public void DiscardPendingTags() => DiscardedCount++;
    }

    private sealed class FakeUnitOfWork(IRepository<FileEntity, FileId> fileRepository) : IUnitOfWork
    {
        public List<CancellationToken> SaveTokens { get; } = [];

        public Exception? FailWith { get; set; }

        public IRepository<TAggregate, TKey> GetRepository<TAggregate, TKey>() where TAggregate : IAggregateRoot
            => (IRepository<TAggregate, TKey>)fileRepository;

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveTokens.Add(cancellationToken);

            return FailWith is null ? Task.FromResult(1) : throw FailWith;
        }

        public int ClearCount { get; private set; }

        public void ClearChangeTracker() => ClearCount++;

        public Task InTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default) => work();
    }
}
