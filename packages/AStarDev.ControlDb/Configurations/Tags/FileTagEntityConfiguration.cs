using AStarDev.ControlDb.FileDetail;
using AStarDev.ControlDb.TagDetail;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Tags;

/// <summary>EF Core configuration for <see cref="FileTagEntity"/>.</summary>
public sealed class FileTagEntityConfiguration : IEntityTypeConfiguration<FileTagEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<FileTagEntity> builder)
    {
        builder.ToTable("FileTags");

        builder.HasKey(fileTag => new { fileTag.FileId, fileTag.TagId });
        builder.Property(fileTag => fileTag.FileId).HasConversion(id => id.Value, guid => new FileId(guid));
        builder.Property(fileTag => fileTag.TagId).HasConversion(id => id.Value, guid => new TagId(guid));

        builder.HasOne(fileTag => fileTag.FileDetail)
            .WithMany(file => file.FileTags)
            .HasForeignKey(fileTag => fileTag.FileId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(fileTag => fileTag.Tag)
            .WithMany(tag => tag.FileTags)
            .HasForeignKey(fileTag => fileTag.TagId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
