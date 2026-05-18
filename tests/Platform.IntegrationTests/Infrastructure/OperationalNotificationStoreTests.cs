using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Platform.Application.Features.Notifications;
using Platform.Domain.Operations;
using Platform.Domain.Reports;
using Platform.Domain.Tenancy;
using Platform.Infrastructure.Data;
using Platform.Infrastructure.Notifications;
using Platform.Infrastructure.Tenancy;
using Platform.IntegrationTests.Support;
using Testcontainers.PostgreSql;

namespace Platform.IntegrationTests.Infrastructure;

public sealed class OperationalNotificationStoreTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("instruments_platform")
        .WithUsername("platform_app")
        .WithPassword("platform_app")
        .Build();

    [DockerFact]
    public async Task RecordReportPdfArtifactTerminalStateAsync_inserts_idempotent_operational_notification()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = Guid.NewGuid();
        var exportArtifactId = Guid.NewGuid();
        var campaignSeriesId = Guid.NewGuid();
        await SeedTenantAsync(options, tenantId);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        var store = new OperationalNotificationStore(db, tenantDbScope);

        var first = await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            exportArtifactId,
            campaignSeriesId,
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);
        var second = await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            exportArtifactId,
            campaignSeriesId,
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.Equal(first.Value.Id, second.Value.Id);

        await using var verifyTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        var notification = Assert.Single(await db.OperationalNotifications.ToListAsync());
        await verifyTransaction.CommitAsync();

        Assert.Equal(OperationalNotification.ReportPdfArtifactTerminalNotificationType, notification.NotificationType);
        Assert.Equal(OperationalNotification.SeverityWarning, notification.Severity);
        Assert.Equal(OperationalNotification.StatusUnread, notification.Status);
        Assert.Equal(exportArtifactId, notification.SourceAggregateId);
        Assert.Equal(OperationalNotification.SourceAggregateTypeExportArtifact, notification.SourceAggregateType);
        Assert.Equal(
            OperationalNotification.SourceEventTypeReportPdfArtifactTerminalStateReached,
            notification.SourceEventType);
        using var payload = JsonDocument.Parse(notification.PayloadJson);
        Assert.Equal(1, payload.RootElement.GetProperty("schemaVersion").GetInt32());
        Assert.Equal(exportArtifactId, payload.RootElement.GetProperty("exportArtifactId").GetGuid());
        Assert.Equal(campaignSeriesId, payload.RootElement.GetProperty("campaignSeriesId").GetGuid());
        Assert.Equal(ExportArtifactStatuses.Failed, payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "export_artifact.object_store_unavailable",
            payload.RootElement.GetProperty("failureReasonCode").GetString());
        Assert.DoesNotContain("storage", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("X-Amz", notification.PayloadJson, StringComparison.OrdinalIgnoreCase);
    }

    [DockerFact]
    public async Task RecordReportPdfArtifactTerminalStateAsync_reuses_existing_tenant_transaction()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = Guid.NewGuid();
        var exportArtifactId = Guid.NewGuid();
        await SeedTenantAsync(options, tenantId);

        await using var db = new ApplicationDbContext(options);
        var tenantDbScope = new TenantDbScope(db);
        var store = new OperationalNotificationStore(db, tenantDbScope);
        await using var outerTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);

        var recorded = await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            exportArtifactId,
            Guid.NewGuid(),
            ExportArtifactStatuses.Succeeded,
            failureReasonCode: null,
            CancellationToken.None);

        Assert.True(recorded.IsSuccess, recorded.Error.ToString());
        Assert.Equal(exportArtifactId, recorded.Value.SourceAggregateId);
        await outerTransaction.CommitAsync();

        await using var verifyTransaction = await tenantDbScope.BeginTransactionAsync(tenantId);
        Assert.Equal(1, await db.OperationalNotifications.CountAsync());
        await verifyTransaction.CommitAsync();
    }

    [DockerFact]
    public async Task ListOperationalNotificationsAsync_returns_latest_safe_pointer_rows_for_tenant()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = Guid.NewGuid();
        var exportArtifactId = Guid.NewGuid();
        var campaignSeriesId = Guid.NewGuid();
        await SeedTenantAsync(options, tenantId);

        await using var db = new ApplicationDbContext(options);
        var store = new OperationalNotificationStore(db, new TenantDbScope(db));

        await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Succeeded,
            failureReasonCode: null,
            CancellationToken.None);
        await Task.Delay(10);
        await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            exportArtifactId,
            campaignSeriesId,
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);

        var listed = await store.ListOperationalNotificationsAsync(
            tenantId,
            limit: 1,
            CancellationToken.None);

        Assert.True(listed.IsSuccess, listed.Error.ToString());
        Assert.Equal(1, listed.Value.RequestedLimit);
        var notification = Assert.Single(listed.Value.Notifications);
        Assert.Equal(exportArtifactId, notification.SourceAggregateId);
        Assert.Equal(campaignSeriesId, notification.CampaignSeriesId);
        Assert.Equal(ExportArtifactStatuses.Failed, notification.ArtifactStatus);
        Assert.Equal(ExportArtifactStatuses.Failed, notification.SourceStatus);
        Assert.Equal("export_artifact.object_store_unavailable", notification.FailureReasonCode);
    }

    [DockerFact]
    public async Task Operational_notification_mark_read_is_idempotent_and_scoped_to_tenant()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        var exportArtifactId = Guid.NewGuid();
        await SeedTenantAsync(options, tenantId);
        await SeedTenantAsync(options, otherTenantId);

        await using var db = new ApplicationDbContext(options);
        var store = new OperationalNotificationStore(db, new TenantDbScope(db));
        var created = await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            exportArtifactId,
            Guid.NewGuid(),
            ExportArtifactStatuses.Succeeded,
            failureReasonCode: null,
            CancellationToken.None);
        Assert.True(created.IsSuccess, created.Error.ToString());
        var firstReadAt = DateTimeOffset.Parse("2026-05-18T21:05:00+00:00");
        var secondReadAt = firstReadAt.AddMinutes(5);

        var wrongTenant = await store.MarkOperationalNotificationReadAsync(
            otherTenantId,
            created.Value.Id,
            firstReadAt,
            CancellationToken.None);
        var first = await store.MarkOperationalNotificationReadAsync(
            tenantId,
            created.Value.Id,
            firstReadAt,
            CancellationToken.None);
        var second = await store.MarkOperationalNotificationReadAsync(
            tenantId,
            created.Value.Id,
            secondReadAt,
            CancellationToken.None);

        Assert.True(wrongTenant.IsFailure);
        Assert.Equal("operational_notification.not_found", wrongTenant.Error.Code);
        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.Equal("read", first.Value.Status);
        Assert.Equal("read", second.Value.Status);
        Assert.Equal(firstReadAt, first.Value.ReadAt);
        Assert.Equal(firstReadAt, second.Value.ReadAt);
        Assert.Equal(first.Value.UpdatedAt, second.Value.UpdatedAt);
    }

    [DockerFact]
    public async Task Operational_notification_mark_all_read_is_idempotent_and_scoped_to_tenant()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await SeedTenantAsync(options, tenantId);
        await SeedTenantAsync(options, otherTenantId);

        await using var db = new ApplicationDbContext(options);
        var store = new OperationalNotificationStore(db, new TenantDbScope(db));
        await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Succeeded,
            failureReasonCode: null,
            CancellationToken.None);
        await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);
        await store.RecordReportPdfArtifactTerminalStateAsync(
            otherTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);
        var firstReadAt = DateTimeOffset.Parse("2026-05-18T21:35:00+00:00");
        var secondReadAt = firstReadAt.AddMinutes(5);

        var first = await store.MarkAllOperationalNotificationsReadAsync(
            tenantId,
            firstReadAt,
            CancellationToken.None);
        var second = await store.MarkAllOperationalNotificationsReadAsync(
            tenantId,
            secondReadAt,
            CancellationToken.None);
        var tenantSummary = await store.GetOperationalNotificationSummaryAsync(
            tenantId,
            CancellationToken.None);
        var otherSummary = await store.GetOperationalNotificationSummaryAsync(
            otherTenantId,
            CancellationToken.None);

        Assert.True(first.IsSuccess, first.Error.ToString());
        Assert.True(second.IsSuccess, second.Error.ToString());
        Assert.Equal(2, first.Value.MarkedReadCount);
        Assert.Equal(firstReadAt, first.Value.ReadAt);
        Assert.Equal(0, second.Value.MarkedReadCount);
        Assert.Equal(secondReadAt, second.Value.ReadAt);
        Assert.True(tenantSummary.IsSuccess, tenantSummary.Error.ToString());
        Assert.True(otherSummary.IsSuccess, otherSummary.Error.ToString());
        Assert.Equal(0, tenantSummary.Value.UnreadCount);
        Assert.Equal(1, otherSummary.Value.UnreadCount);
    }

    [DockerFact]
    public async Task GetOperationalNotificationSummaryAsync_counts_unread_notifications_for_tenant()
    {
        var options = CreateOptions();
        await PrepareDatabaseAsync(options);
        var tenantId = Guid.NewGuid();
        var otherTenantId = Guid.NewGuid();
        await SeedTenantAsync(options, tenantId);
        await SeedTenantAsync(options, otherTenantId);

        await using var db = new ApplicationDbContext(options);
        var store = new OperationalNotificationStore(db, new TenantDbScope(db));
        var readNotification = await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Succeeded,
            failureReasonCode: null,
            CancellationToken.None);
        await store.RecordReportPdfArtifactTerminalStateAsync(
            tenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);
        await store.RecordReportPdfArtifactTerminalStateAsync(
            otherTenantId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            ExportArtifactStatuses.Failed,
            "export_artifact.object_store_unavailable",
            CancellationToken.None);
        Assert.True(readNotification.IsSuccess, readNotification.Error.ToString());
        await store.MarkOperationalNotificationReadAsync(
            tenantId,
            readNotification.Value.Id,
            DateTimeOffset.Parse("2026-05-18T21:05:00+00:00"),
            CancellationToken.None);

        var summary = await store.GetOperationalNotificationSummaryAsync(
            tenantId,
            CancellationToken.None);

        Assert.True(summary.IsSuccess, summary.Error.ToString());
        Assert.Equal(1, summary.Value.UnreadCount);
        Assert.Equal(0, summary.Value.InfoUnreadCount);
        Assert.Equal(1, summary.Value.WarningUnreadCount);
        Assert.NotNull(summary.Value.LatestUnreadAt);
    }

    public Task InitializeAsync()
    {
        return _postgres.StartAsync();
    }

    public Task DisposeAsync()
    {
        return _postgres.DisposeAsync().AsTask();
    }

    private static async Task PrepareDatabaseAsync(DbContextOptions<ApplicationDbContext> options)
    {
        await using var db = new ApplicationDbContext(options);
        await db.Database.MigrateAsync();
    }

    private static async Task SeedTenantAsync(
        DbContextOptions<ApplicationDbContext> options,
        Guid tenantId)
    {
        await using var db = new ApplicationDbContext(options);
        db.Tenants.Add(new Tenant(tenantId, $"tenant-{tenantId:N}"[..15], "Operational notification test"));
        await db.SaveChangesAsync();
    }

    private DbContextOptions<ApplicationDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;
    }
}
