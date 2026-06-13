using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.Domain.Integrations;
using Platform.SharedKernel;

namespace Platform.Application.Features.ProductSurfaces;

public sealed record PreviewMicrosoftGraphDirectoryImportRuleCommand(
    Guid RuleId,
    PreviewMicrosoftGraphImportRuleRequest Request)
    : IRequest<Result<MicrosoftGraphImportRulePreviewResponse>>;

public sealed class PreviewMicrosoftGraphDirectoryImportRuleValidator
    : AbstractValidator<PreviewMicrosoftGraphDirectoryImportRuleCommand>
{
    public PreviewMicrosoftGraphDirectoryImportRuleValidator()
    {
        RuleFor(command => command.RuleId)
            .NotEmpty();
        RuleFor(command => command.Request.MicrosoftTenantId)
            .NotEmpty()
            .MaximumLength(128);
        RuleFor(command => command.Request.Users)
            .NotNull();
        RuleFor(command => command.Request.Groups)
            .NotNull();
        RuleFor(command => command.Request.Memberships)
            .NotNull();
    }
}

public sealed class PreviewMicrosoftGraphDirectoryImportRuleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IProductSurfaceReadStore readStore,
    IProductSurfaceWriteStore writeStore)
    : IRequestHandler<PreviewMicrosoftGraphDirectoryImportRuleCommand, Result<MicrosoftGraphImportRulePreviewResponse>>
{
    public async Task<Result<MicrosoftGraphImportRulePreviewResponse>> Handle(
        PreviewMicrosoftGraphDirectoryImportRuleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required."));
        }

        var rules = await readStore.ListMicrosoftGraphDirectoryImportRulesAsync(
            currentTenant.TenantId,
            cancellationToken);
        var rule = rules.Rules.SingleOrDefault(candidate =>
            candidate.Id == command.RuleId &&
            candidate.Status == DirectoryImportRuleStatuses.Active);
        if (rule is null)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(
                Error.NotFound("directory_import_rule.not_found", "Graph import rule was not found."));
        }

        var snapshot = new MicrosoftGraphDirectoryImportSnapshot(
            command.Request.MicrosoftTenantId,
            command.Request.Users,
            command.Request.Groups,
            command.Request.Memberships,
            command.Request.AllowUserPrincipalNameEmailFallback,
            command.Request.ExcludeGuests,
            command.Request.ExcludeDisabledAccounts,
            command.Request.ManagerRelationships,
            MarkMissingUsersStale: rule.StalePolicy == DirectoryImportStalePolicies.MarkStale);
        var plan = MicrosoftGraphDirectoryImportAdapter.CreateCsvImportPlan(snapshot, dryRun: true);
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
                DirectoryImportRuleId = rule.Id
            },
            cancellationToken);
        if (import.IsFailure)
        {
            return Result.Failure<MicrosoftGraphImportRulePreviewResponse>(import.Error);
        }

        return Result.Success(new MicrosoftGraphImportRulePreviewResponse(
            currentTenant.TenantId,
            rule.Id,
            rule.DirectoryConnectionId,
            import.Value,
            plan.Value.IncludedUserCount,
            plan.Value.IncludedMembershipCount,
            plan.Value.Warnings));
    }
}
