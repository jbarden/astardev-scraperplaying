using AStarDev.ControlDb.TagDetail;
using AStarDev.FunctionalParadigm;

namespace AStarDev.ControlDb;

/// <summary>Implementation of the <see cref="IFileTagRepository"/> interface for managing file-tag link entities in the database.</summary>
/// <param name="context">The database context used for managing file-tag link entities.</param>
public class FileTagRepository(ControlDbContext context) : IFileTagRepository
{
    /// <inheritdoc/>
    public Exceptional<FileTagEntity> Add(FileTagEntity fileTag) =>
        Try.Run(() =>
        {
            context.FileTags.Add(fileTag);
            return fileTag;
        });
}
