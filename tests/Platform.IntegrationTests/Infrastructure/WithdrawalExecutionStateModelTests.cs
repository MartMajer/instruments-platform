using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Platform.Domain.Consent;
using Platform.Infrastructure.Data;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class WithdrawalExecutionStateModelTests
{
    [Fact]
    public void Withdrawal_event_status_constraint_allows_execution_lifecycle_statuses()
    {
        using var db = new ApplicationDbContextFactory().CreateDbContext([]);
        var entity = db.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(WithdrawalEvent));

        Assert.NotNull(entity);
        var constraint = Assert.Single(
            entity!.GetCheckConstraints(),
            check => check.Name == "ck_withdrawal_event_status");

        Assert.Contains("'planned'", constraint.Sql);
        Assert.Contains("'processing'", constraint.Sql);
        Assert.Contains("'completed'", constraint.Sql);
        Assert.Contains("'failed'", constraint.Sql);

        var countConstraint = Assert.Single(
            entity.GetCheckConstraints(),
            check => check.Name == "ck_withdrawal_event_counts_non_negative");
        Assert.Contains("score_run_count", countConstraint.Sql);
    }

    [Fact]
    public void Withdrawal_event_status_migration_updates_database_constraint()
    {
        var migrationPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Platform.Infrastructure",
            "Migrations",
            "20260517153000_ExpandWithdrawalEventStatuses.cs");
        var migration = File.ReadAllText(Path.GetFullPath(migrationPath));

        Assert.Contains("DROP CONSTRAINT IF EXISTS ck_withdrawal_event_status", migration);
        Assert.Contains("'processing'", migration);
        Assert.Contains("'completed'", migration);
        Assert.Contains("'failed'", migration);

        var scoreRunMigrationPath = Path.Combine(
            AppContext.BaseDirectory,
            "..",
            "..",
            "..",
            "..",
            "..",
            "src",
            "Platform.Infrastructure",
            "Migrations",
            "20260517162000_AddWithdrawalEventScoreRunCount.cs");
        var scoreRunMigration = File.ReadAllText(Path.GetFullPath(scoreRunMigrationPath));
        Assert.Contains("score_run_count", scoreRunMigration);
        Assert.Contains("ck_withdrawal_event_counts_non_negative", scoreRunMigration);
    }
}
