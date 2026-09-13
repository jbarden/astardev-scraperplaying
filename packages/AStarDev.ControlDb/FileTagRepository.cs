using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>
/// Adds <see cref="FileTagEntity"/> link rows. Separate from <see cref="IRepository{TAggregate,TKey}"/> since a
/// file-tag link is keyed by the (FileId, TagId) pair, not a single <c>TKey</c>.
/// </summary>
public interface IFileTagRepository
{
    Exceptional<FileTagEntity> Add(FileTagEntity fileTag);
}

public class FileTagRepository(ControlDbContext context) : IFileTagRepository
{
    public Exceptional<FileTagEntity> Add(FileTagEntity fileTag) =>
        Try.Run(() =>
        {
            context.FileTags.Add(fileTag);
            return fileTag;
        });
}
