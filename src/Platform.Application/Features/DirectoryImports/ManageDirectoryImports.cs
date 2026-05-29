using System.Text.Json;
using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.DirectoryImports;

public sealed record ListDirectoryImportWorkspaceQuery()
    : IRequest<Result<DirectoryImportWorkspaceResponse>>;

public sealed record CreateDirectoryConnectionRequest(
    string ExternalTenantId,
    string DisplayName,
    string PrimaryDomain,
    IReadOnlyList<string> GrantedScopes);

public sealed record CreateDirectoryConnectionCommand(CreateDirectoryConnectionRequest Request)
    : IRequest<Result<DirectoryConnectionResponse>>;

public sealed record CreateDirectoryImportRuleRequest(
    Guid ConnectionId,
    string Name,
    JsonElement Criteria,
    JsonElement FieldSelection,
    bool MirrorMode,
    string? MirrorConfirmation);

public sealed record CreateDirectoryImportRuleCommand(CreateDirectoryImportRuleRequest Request)
    : IRequest<Result<DirectoryImportRuleResponse>>;

public sealed class CreateDirectoryConnectionValidator
    : AbstractValidator<CreateDirectoryConnectionCommand>
{
    public CreateDirectoryConnectionValidator()
    {
        RuleFor(command => command.Request.ExternalTenantId)
            .NotEmpty()
            .MaximumLength(128);
        RuleFor(command => command.Request.DisplayName)
            .NotEmpty()
            .MaximumLength(256);
        RuleFor(command => command.Request.PrimaryDomain)
            .NotEmpty()
            .MaximumLength(256);
        RuleFor(command => command.Request.GrantedScopes)
            .NotEmpty();
    }
}

public sealed class CreateDirectoryImportRuleValidator
    : AbstractValidator<CreateDirectoryImportRuleCommand>
{
    public CreateDirectoryImportRuleValidator()
    {
        RuleFor(command => command.Request.ConnectionId)
            .NotEmpty();
        RuleFor(command => command.Request.Name)
            .NotEmpty()
            .MaximumLength(256);
        RuleFor(command => command.Request.Criteria)
            .Must(BeJsonObject)
            .WithMessage("Criteria must be a JSON object.");
        RuleFor(command => command.Request.FieldSelection)
            .Must(BeJsonObject)
            .WithMessage("Field selection must be a JSON object.");
    }

    private static bool BeJsonObject(JsonElement element)
    {
        return element.ValueKind == JsonValueKind.Object;
    }
}

public sealed class ListDirectoryImportWorkspaceHandler(
    ICurrentTenant currentTenant,
    IDirectoryImportStore store)
    : IRequestHandler<ListDirectoryImportWorkspaceQuery, Result<DirectoryImportWorkspaceResponse>>
{
    public Task<Result<DirectoryImportWorkspaceResponse>> Handle(
        ListDirectoryImportWorkspaceQuery request,
        CancellationToken cancellationToken)
    {
        return store.ListWorkspaceAsync(currentTenant.TenantId, cancellationToken);
    }
}

public sealed class CreateDirectoryConnectionHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IDirectoryImportStore store)
    : IRequestHandler<CreateDirectoryConnectionCommand, Result<DirectoryConnectionResponse>>
{
    public Task<Result<DirectoryConnectionResponse>> Handle(
        CreateDirectoryConnectionCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<DirectoryConnectionResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        return store.CreateConnectionAsync(currentTenant.TenantId, command.Request, cancellationToken);
    }
}

public sealed class CreateDirectoryImportRuleHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IDirectoryImportStore store)
    : IRequestHandler<CreateDirectoryImportRuleCommand, Result<DirectoryImportRuleResponse>>
{
    public const string MirrorConfirmationText = "MIRROR MICROSOFT DIRECTORY";

    public Task<Result<DirectoryImportRuleResponse>> Handle(
        CreateDirectoryImportRuleCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<DirectoryImportRuleResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        if (command.Request.MirrorMode &&
            !string.Equals(
                command.Request.MirrorConfirmation?.Trim(),
                MirrorConfirmationText,
                StringComparison.Ordinal))
        {
            return Task.FromResult(Result.Failure<DirectoryImportRuleResponse>(
                Error.Validation(
                    "directory_import_rule.mirror_confirmation_required",
                    $"Type {MirrorConfirmationText} to enable mirror mode.")));
        }

        return store.CreateRuleAsync(currentTenant.TenantId, command.Request, cancellationToken);
    }
}

public sealed record DirectoryImportWorkspaceResponse(
    Guid TenantId,
    IReadOnlyList<DirectoryConnectionResponse> Connections,
    IReadOnlyList<DirectoryImportRuleResponse> Rules,
    IReadOnlyList<DirectoryImportRunHistoryResponse> RecentRuns);

public sealed record DirectoryConnectionResponse(
    Guid Id,
    string Provider,
    string ExternalTenantId,
    string DisplayName,
    string PrimaryDomain,
    IReadOnlyList<string> GrantedScopes,
    string Status,
    DateTimeOffset? LastSuccessfulSyncAt,
    DateTimeOffset CreatedAt);

public sealed record DirectoryImportRuleResponse(
    Guid Id,
    Guid ConnectionId,
    string Name,
    JsonElement Criteria,
    JsonElement FieldSelection,
    bool MirrorMode,
    DateTimeOffset? MirrorConfirmedAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record DirectoryImportRunHistoryResponse(
    Guid Id,
    Guid RuleId,
    string RuleName,
    string Mode,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? FinishedAt,
    JsonElement Summary);
