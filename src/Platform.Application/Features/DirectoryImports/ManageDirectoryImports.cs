using System.Text.Json;
using FluentValidation;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
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

public sealed record StartMicrosoftGraphAdminConsentCommand()
    : IRequest<Result<MicrosoftGraphAdminConsentStartResponse>>;

public sealed record CompleteMicrosoftGraphAdminConsentRequest(
    string? AdminConsent,
    string? Tenant,
    string? Scope,
    string? State,
    string? Error,
    string? ErrorDescription);

public sealed record CompleteMicrosoftGraphAdminConsentCommand(
    CompleteMicrosoftGraphAdminConsentRequest Request)
    : IRequest<Result<MicrosoftGraphAdminConsentCallbackResponse>>;

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

public sealed class StartMicrosoftGraphAdminConsentHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    IDataProtectionProvider dataProtectionProvider,
    IOptions<MicrosoftGraphDirectoryImportOptions> graphOptions)
    : IRequestHandler<StartMicrosoftGraphAdminConsentCommand, Result<MicrosoftGraphAdminConsentStartResponse>>
{
    public Task<Result<MicrosoftGraphAdminConsentStartResponse>> Handle(
        StartMicrosoftGraphAdminConsentCommand command,
        CancellationToken cancellationToken)
    {
        if (!actor.UserId.HasValue)
        {
            return Task.FromResult(Result.Failure<MicrosoftGraphAdminConsentStartResponse>(
                Error.Forbidden("actor.required", "Authenticated actor is required.")));
        }

        var options = graphOptions.Value;
        if (!options.IsAdminConsentConfigured)
        {
            return Task.FromResult(Result.Failure<MicrosoftGraphAdminConsentStartResponse>(
                Error.Conflict(
                    "directory_import.microsoft_graph_admin_consent_not_configured",
                    "Microsoft Graph admin consent settings are not configured.")));
        }

        var state = new MicrosoftGraphAdminConsentState(
            currentTenant.TenantId,
            actor.UserId.Value,
            DateTimeOffset.UtcNow);
        var protectedState = ProtectState(dataProtectionProvider, state);
        var tenant = string.IsNullOrWhiteSpace(options.AdminConsentTenant)
            ? "organizations"
            : options.AdminConsentTenant.Trim();
        var query = new Dictionary<string, string?>
        {
            ["client_id"] = options.ClientId!.Trim(),
            ["scope"] = "https://graph.microsoft.com/.default",
            ["redirect_uri"] = options.AdminConsentRedirectUri!.Trim(),
            ["state"] = protectedState
        };
        var authorizationUrl = QueryHelpers.AddQueryString(
            $"https://login.microsoftonline.com/{Uri.EscapeDataString(tenant)}/v2.0/adminconsent",
            query);

        return Task.FromResult(Result.Success(new MicrosoftGraphAdminConsentStartResponse(authorizationUrl)));
    }

    internal static string ProtectState(
        IDataProtectionProvider dataProtectionProvider,
        MicrosoftGraphAdminConsentState state)
    {
        var protector = dataProtectionProvider
            .CreateProtector("Platform.DirectoryImports.MicrosoftGraph.AdminConsent.v1")
            .ToTimeLimitedDataProtector();

        return protector.Protect(JsonSerializer.Serialize(state), TimeSpan.FromMinutes(30));
    }
}

public sealed class CompleteMicrosoftGraphAdminConsentHandler(
    IDataProtectionProvider dataProtectionProvider,
    IOptions<MicrosoftGraphDirectoryImportOptions> graphOptions,
    IDirectoryImportStore store)
    : IRequestHandler<CompleteMicrosoftGraphAdminConsentCommand, Result<MicrosoftGraphAdminConsentCallbackResponse>>
{
    private static readonly string[] DefaultGrantedScopes =
    [
        "User.Read.All",
        "Group.Read.All",
        "GroupMember.Read.All"
    ];

    public async Task<Result<MicrosoftGraphAdminConsentCallbackResponse>> Handle(
        CompleteMicrosoftGraphAdminConsentCommand command,
        CancellationToken cancellationToken)
    {
        var options = graphOptions.Value;
        if (string.IsNullOrWhiteSpace(options.PostConsentRedirectUrl))
        {
            return Result.Failure<MicrosoftGraphAdminConsentCallbackResponse>(
                Error.Conflict(
                    "directory_import.microsoft_graph_admin_consent_not_configured",
                    "Microsoft Graph admin consent return URL is not configured."));
        }

        if (!string.IsNullOrWhiteSpace(command.Request.Error))
        {
            return Result.Success(new MicrosoftGraphAdminConsentCallbackResponse(
                AppendConsentStatus(options.PostConsentRedirectUrl, "failed")));
        }

        if (!string.Equals(command.Request.AdminConsent, "True", StringComparison.Ordinal) ||
            string.IsNullOrWhiteSpace(command.Request.Tenant) ||
            string.IsNullOrWhiteSpace(command.Request.State))
        {
            return Result.Success(new MicrosoftGraphAdminConsentCallbackResponse(
                AppendConsentStatus(options.PostConsentRedirectUrl, "failed")));
        }

        var state = UnprotectState(dataProtectionProvider, command.Request.State);
        if (state is null)
        {
            return Result.Success(new MicrosoftGraphAdminConsentCallbackResponse(
                AppendConsentStatus(options.PostConsentRedirectUrl, "failed")));
        }

        var externalTenantId = command.Request.Tenant.Trim();
        var connectionRequest = new CreateDirectoryConnectionRequest(
            externalTenantId,
            $"Microsoft tenant {externalTenantId}",
            externalTenantId,
            ParseGrantedScopes(command.Request.Scope));
        var connectionResult = await store.CreateConnectionAsync(
            state.TenantId,
            connectionRequest,
            cancellationToken);

        return connectionResult.IsSuccess
            ? Result.Success(new MicrosoftGraphAdminConsentCallbackResponse(
                AppendConsentStatus(options.PostConsentRedirectUrl, "connected")))
            : Result.Success(new MicrosoftGraphAdminConsentCallbackResponse(
                AppendConsentStatus(options.PostConsentRedirectUrl, "failed")));
    }

    private static MicrosoftGraphAdminConsentState? UnprotectState(
        IDataProtectionProvider dataProtectionProvider,
        string protectedState)
    {
        try
        {
            var protector = dataProtectionProvider
                .CreateProtector("Platform.DirectoryImports.MicrosoftGraph.AdminConsent.v1")
                .ToTimeLimitedDataProtector();
            var json = protector.Unprotect(protectedState);
            return JsonSerializer.Deserialize<MicrosoftGraphAdminConsentState>(json);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ParseGrantedScopes(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return DefaultGrantedScopes;
        }

        var scopes = scope
            .Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(value =>
            {
                var lastSlashIndex = value.LastIndexOf('/');
                return lastSlashIndex >= 0 && lastSlashIndex < value.Length - 1
                    ? value[(lastSlashIndex + 1)..]
                    : value;
            })
            .Where(value => !string.IsNullOrWhiteSpace(value) && value != ".default")
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        return scopes.Length == 0 ? DefaultGrantedScopes : scopes;
    }

    private static string AppendConsentStatus(string returnUrl, string status)
    {
        return QueryHelpers.AddQueryString(
            returnUrl,
            "directoryConnection",
            status);
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

public sealed record MicrosoftGraphAdminConsentStartResponse(string AuthorizationUrl);

public sealed record MicrosoftGraphAdminConsentCallbackResponse(string RedirectUrl);

internal sealed record MicrosoftGraphAdminConsentState(
    Guid TenantId,
    Guid UserId,
    DateTimeOffset StartedAt);
