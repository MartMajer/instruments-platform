using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Integrations;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record PreviewLiveMicrosoftGraphDirectoryImportRuleCommand(Guid RuleId)
    : IRequest<Result<MicrosoftGraphImportRulePreviewResponse>>;

public sealed record ApplyLiveMicrosoftGraphDirectoryImportRuleCommand(
    Guid RuleId,
    LiveApplyMicrosoftGraphImportRuleRequest Request)
    : IRequest<Result<MicrosoftGraphImportRuleApplyResponse>>;

public sealed class PreviewLiveMicrosoftGraphDirectoryImportRuleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceReadStore readStore,
    IProductSurfaceWriteStore writeStore,
    IMicrosoftGraphDirectorySnapshotConnector connector)
    : IRequestHandler<PreviewLiveMicrosoftGraphDirectoryImportRuleCommand, Result<MicrosoftGraphImportRulePreviewResponse>>
{
    public async Task<Result<MicrosoftGraphImportRulePreviewResponse>> Handle(
        PreviewLiveMicrosoftGraphDirectoryImportRuleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        var context = await readStore.GetMicrosoftGraphDirectoryImportRuleExecutionContextAsync(
            currentTenant.TenantId,
            command.RuleId,
            cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(context.Error);
        }

        var snapshot = await connector.FetchSnapshotAsync(context.Value.MicrosoftTenantId, cancellationToken);
        if (snapshot.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(snapshot.Error);
        }

        var plan = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(
            snapshot.Value with
            {
                MarkMissingUsersStale = context.Value.StalePolicy == DirectoryImportStalePolicies.MarkStale
            },
            dryRun: true);
        if (plan.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(plan.Error);
        }

        var import = await writeStore.ImportSubjectDirectoryCsvAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            plan.Value.Request with
            {
                DryRun = true,
                PreviewImportRunId = null,
                DirectoryImportRuleId = context.Value.RuleId
            },
            cancellationToken);
        if (import.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(import.Error);
        }

        return Result.Success(new MicrosoftGraphImportRulePreviewResponse(
            currentTenant.TenantId,
            context.Value.RuleId,
            context.Value.DirectoryConnectionId,
            import.Value,
            plan.Value.IncludedUserCount,
            plan.Value.IncludedMembershipCount,
            plan.Value.Warnings));
    }
}

public sealed class ApplyLiveMicrosoftGraphDirectoryImportRuleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceReadStore readStore,
    IProductSurfaceWriteStore writeStore,
    IMicrosoftGraphDirectorySnapshotConnector connector)
    : IRequestHandler<ApplyLiveMicrosoftGraphDirectoryImportRuleCommand, Result<MicrosoftGraphImportRuleApplyResponse>>
{
    public async Task<Result<MicrosoftGraphImportRuleApplyResponse>> Handle(
        ApplyLiveMicrosoftGraphDirectoryImportRuleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<MicrosoftGraphImportRuleApplyResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        if (command.Request.PreviewImportRunId == Guid.Empty)
        {
            return Result.Failure<MicrosoftGraphImportRuleApplyResponse>(
                Error.Validation("directory_import_run.preview_required", "A completed preview run is required."));
        }

        var context = await readStore.GetMicrosoftGraphDirectoryImportRuleExecutionContextAsync(
            currentTenant.TenantId,
            command.RuleId,
            cancellationToken);
        if (context.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRuleApplyResponse>(context.Error);
        }

        var snapshot = await connector.FetchSnapshotAsync(context.Value.MicrosoftTenantId, cancellationToken);
        if (snapshot.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRuleApplyResponse>(snapshot.Error);
        }

        var plan = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(
            snapshot.Value with
            {
                MarkMissingUsersStale = context.Value.StalePolicy == DirectoryImportStalePolicies.MarkStale
            },
            dryRun: false);
        if (plan.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRuleApplyResponse>(plan.Error);
        }

        var import = await writeStore.ImportSubjectDirectoryCsvAsync(
            currentTenant.TenantId,
            actor.UserId.Value,
            plan.Value.Request with
            {
                DryRun = false,
                PreviewImportRunId = command.Request.PreviewImportRunId,
                DirectoryImportRuleId = context.Value.RuleId
            },
            cancellationToken);
        if (import.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRuleApplyResponse>(import.Error);
        }

        return Result.Success(new MicrosoftGraphImportRuleApplyResponse(
            currentTenant.TenantId,
            context.Value.RuleId,
            context.Value.DirectoryConnectionId,
            import.Value,
            plan.Value.IncludedUserCount,
            plan.Value.IncludedMembershipCount,
            plan.Value.Warnings));
    }
}
