using FluentValidation;
using MediatR;
using Platform.Application.Auth;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.Application.Features.TestData;

public sealed record CreateCampaignTestRecipientsCommand(
    Guid CampaignId,
    CreateCampaignTestRecipientsRequest Request)
    : IRequest<Result<CreateCampaignTestRecipientsResponse>>;

public sealed class CreateCampaignTestRecipientsValidator
    : AbstractValidator<CreateCampaignTestRecipientsCommand>
{
    public CreateCampaignTestRecipientsValidator()
    {
        RuleFor(command => command.CampaignId).NotEmpty();
        RuleFor(command => command.Request.Count).InclusiveBetween(1, 1000);
        RuleFor(command => command.Request.GroupName).NotEmpty().MaximumLength(128);
        RuleFor(command => command.Request.EmailDomain)
            .NotEmpty()
            .MaximumLength(160)
            .Must(IsSafeTestDomain)
            .WithMessage("EmailDomain must be a non-routable test domain ending in .local or .invalid.");
        RuleFor(command => command.Request.Locale).NotEmpty().MaximumLength(16);
    }

    private static bool IsSafeTestDomain(string value)
    {
        var normalized = value.Trim().TrimStart('@').ToLowerInvariant();
        return normalized.EndsWith(".local", StringComparison.Ordinal) ||
            normalized.EndsWith(".invalid", StringComparison.Ordinal);
    }
}

public sealed class CreateCampaignTestRecipientsHandler(
    ICurrentTenant currentTenant,
    ICurrentActor actor,
    ITestDataSimulatorStore store)
    : IRequestHandler<CreateCampaignTestRecipientsCommand, Result<CreateCampaignTestRecipientsResponse>>
{
    public Task<Result<CreateCampaignTestRecipientsResponse>> Handle(
        CreateCampaignTestRecipientsCommand command,
        CancellationToken cancellationToken)
    {
        return store.CreateCampaignTestRecipientsAsync(
            currentTenant.TenantId,
            actor.UserId,
            command.CampaignId,
            command.Request,
            cancellationToken);
    }
}
