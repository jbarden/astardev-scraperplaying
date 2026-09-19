using AStarDev.ControlDb;
using AStarDev.ControlDb.FileDetail;
using AStarDev.ScraperPlaying.Scraping;
using Microsoft.EntityFrameworkCore;

namespace AStarDev.ScraperPlaying.TestsUnit.Scraping;

public sealed class GivenAnIngestionStore
{
    private readonly IRepository<FileEntity, FileId> fileRepository = Substitute.For<IRepository<FileEntity, FileId>>();
    private readonly FakeUnitOfWork unitOfWork;
    private readonly List<string> messages = [];
    private readonly IngestionStore store;

    public GivenAnIngestionStore()
    {
        unitOfWork = new(fileRepository);
        store = new(unitOfWork);
    }

    [Fact]
    public void when_the_file_repository_is_requested_then_the_units_file_repository_is_returned()
        => store.FileRepository.ShouldBeSameAs(fileRepository);

    [Fact]
    public async Task when_a_page_is_saved_then_the_changes_are_saved_with_the_given_token()
    {
        using var cancellationTokenSource = new CancellationTokenSource();

        await store.SavePageAsync(cancellationTokenSource.Token);

        unitOfWork.SaveTokens.ShouldBe([cancellationTokenSource.Token]);
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
    }

    private sealed class Progress(List<string> messages) : IProgress<string>
    {
        public void Report(string value) => messages.Add(value);
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

        public Task InTransactionAsync(Func<Task> work, CancellationToken cancellationToken = default) => work();
    }
}
