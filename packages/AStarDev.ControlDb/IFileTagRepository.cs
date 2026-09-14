using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>
/// Adds <see cref="FileTagEntity"/> link rows. Separate from <see cref="IRepository{TAggregate,TKey}"/> since a
/// file-tag link is keyed by the (FileId, TagId) pair, not a single <c>TKey</c>.
/// </summary>
public interface IFileTagRepository
{
    /// <summary>Adds a new file-tag link to the database.</summary>
    /// <param name="fileTag">The file-tag link entity to add.</param>
    /// <returns>An exceptional result containing the added file-tag link entity.</returns>
    Exceptional<FileTagEntity> Add(FileTagEntity fileTag);
}
