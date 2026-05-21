using System.Net.Mail;
using FluentValidation;
using MediatR;
using Platform.Application.Tenancy;
using Platform.Domain.Campaigns;
using Platform.SharedKernel;

namespace Platform.Application.Features.Notifications;

public sealed record ListEmailSuppressionsQuery(int Limit = 50, bool IncludeReleased = false)
    : IRequest<Result<ListEmailSuppressionsResponse>>;

public sealed class ListEmailSuppressionsValidator : AbstractValidator<ListEmailSuppressionsQuery>
{
    public const int MaxLimit = 100;

    public ListEmailSuppressionsValidator()
    {
        RuleFor(query => query.Limit).InclusiveBetween(1, MaxLimit);
    }
}

public sealed class ListEmailSuppressionsHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<ListEmailSuppressionsQuery, Result<ListEmailSuppressionsResponse>>
{
    public Task<Result<ListEmailSuppressionsResponse>> Handle(
        ListEmailSuppressionsQuery query,
        CancellationToken cancellationToken)
    {
        return store.ListEmailSuppressionsAsync(
            currentTenant.TenantId,
            query.Limit,
            query.IncludeReleased,
            cancellationToken);
    }
}

public sealed record AddEmailSuppressionCommand(AddEmailSuppressionRequest Request)
    : IRequest<Result<EmailSuppressionResponse>>;

public sealed class AddEmailSuppressionValidator : AbstractValidator<AddEmailSuppressionCommand>
{
    public AddEmailSuppressionValidator()
    {
        RuleFor(command => command.Request.Recipient)
            .NotEmpty()
            .Must(BeEmailAddress)
            .WithMessage("Enter a valid email address to suppress.");
        RuleFor(command => command.Request.Reason)
            .MaximumLength(EmailSuppression.ReasonMaxLength);
        RuleFor(command => command.Request.Note)
            .MaximumLength(EmailSuppression.NoteMaxLength);
    }

    private static bool BeEmailAddress(string recipient)
    {
        try
        {
            return !string.IsNullOrWhiteSpace(new MailAddress(recipient.Trim()).Address);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}

public sealed class AddEmailSuppressionHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<AddEmailSuppressionCommand, Result<EmailSuppressionResponse>>
{
    public Task<Result<EmailSuppressionResponse>> Handle(
        AddEmailSuppressionCommand command,
        CancellationToken cancellationToken)
    {
        return store.AddEmailSuppressionAsync(
            currentTenant.TenantId,
            command.Request,
            cancellationToken);
    }
}

public sealed record ReleaseEmailSuppressionCommand(
    Guid SuppressionId,
    ReleaseEmailSuppressionRequest Request)
    : IRequest<Result<EmailSuppressionResponse>>;

public sealed class ReleaseEmailSuppressionValidator : AbstractValidator<ReleaseEmailSuppressionCommand>
{
    public ReleaseEmailSuppressionValidator()
    {
        RuleFor(command => command.SuppressionId).NotEmpty();
        RuleFor(command => command.Request.Reason)
            .MaximumLength(EmailSuppression.ReleaseReasonMaxLength);
    }
}

public sealed class ReleaseEmailSuppressionHandler(
    ICurrentTenant currentTenant,
    INotificationDeliveryStore store)
    : IRequestHandler<ReleaseEmailSuppressionCommand, Result<EmailSuppressionResponse>>
{
    public Task<Result<EmailSuppressionResponse>> Handle(
        ReleaseEmailSuppressionCommand command,
        CancellationToken cancellationToken)
    {
        return store.ReleaseEmailSuppressionAsync(
            currentTenant.TenantId,
            command.SuppressionId,
            command.Request,
            cancellationToken);
    }
}
