using AStarDev.ControlDb.TagDetail;
using AStarDev.EFCoreSqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AStarDev.ControlDb.Configurations.Tags;

/// <summary>EF Core configuration for <see cref="TagEntity"/>.</summary>
public sealed class TagEntityConfiguration : IEntityTypeConfiguration<TagEntity>
{
    /// <inheritdoc />
    public void Configure(EntityTypeBuilder<TagEntity> builder)
    {
        builder.ToTable("Tags");

        builder.HasKey(tag => tag.Id);
        builder.Property(tag => tag.Id).ValueGeneratedOnAdd();
        builder.Property(tag => tag.Id).HasConversion(id => id.Value, guid => new TagId(guid));
        builder.Property(tag => tag.Id).HasValueGenerator((_, _) => new StrongGuidIdValueGenerator<TagId>(value => new TagId(value)));

        builder.HasIndex(tag => tag.WallhavenTagId).IsUnique();
    }
}
