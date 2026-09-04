using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Shouldly;
using Xunit;

namespace AStarDev.EFCoreSqlite.TestsUnit;

public sealed class GivenAConfiguredModel
{
    [Fact]
    public void when_sqlite_conversions_are_applied_then_special_properties_have_sqlite_metadata()
    {
        var modelBuilder = new ModelBuilder(new Microsoft.EntityFrameworkCore.Metadata.Conventions.ConventionSet());
        modelBuilder.Entity<ConversionEntity>();

        modelBuilder.UseSqliteFriendlyConversions([typeof(ConversionEntity)]);

        var entity = modelBuilder.Model.FindEntityType(typeof(ConversionEntity))!;
        entity.FindProperty(nameof(ConversionEntity.When))!.GetColumnName().ShouldBe("When_Ticks");
        entity.FindProperty(nameof(ConversionEntity.When))!.GetColumnType().ShouldBe("INTEGER");
        entity.FindProperty(nameof(ConversionEntity.Identifier))!.GetColumnType().ShouldBe("BLOB");
        entity.FindProperty(nameof(ConversionEntity.Status))!.GetColumnType().ShouldBe("INTEGER");
        entity.FindProperty(nameof(ConversionEntity.Amount))!.GetColumnType().ShouldBe("INTEGER");
    }

    private sealed class ConversionEntity
    {
        public int Id { get; set; }
        public DateTimeOffset When { get; set; }
        public Guid Identifier { get; set; }
        public ConversionStatus Status { get; set; }
        public decimal Amount { get; set; }
    }

    private enum ConversionStatus
    {
        Pending,
        Complete
    }
}