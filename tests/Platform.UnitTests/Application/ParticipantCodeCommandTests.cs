using Platform.Application.Features.ParticipantCodes;
using Platform.Application.Tenancy;
using Platform.SharedKernel;

namespace Platform.UnitTests.Application;

public sealed class ParticipantCodeCommandTests
{
    [Fact]
    public void Validator_rejects_empty_campaign_series_id_and_blank_raw_code()
    {
        var validator = new ResolveParticipantCodeValidator();

        var result = validator.Validate(new ResolveParticipantCodeCommand(
            Guid.Empty,
            new ResolveParticipantCodeRequest("  ")));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, failure => failure.PropertyName == "CampaignSeriesId");
        Assert.Contains(result.Errors, failure => failure.PropertyName == "Request.RawCode");
    }

    [Fact]
    public async Task Handler_passes_current_tenant_campaign_series_and_raw_code_to_store()
    {
        var tenantId = Guid.NewGuid();
        var campaignSeriesId = Guid.NewGuid();
        var currentTenant = new CurrentTenant();
        currentTenant.SetTenant(tenantId, "test");
        var response = new ParticipantCodeResponse(Guid.NewGuid(), campaignSeriesId);
        var store = new RecordingParticipantCodeStore(response);
        var handler = new ResolveParticipantCodeHandler(currentTenant, store);
        var request = new ResolveParticipantCodeRequest("  Cable Horse Battery  ");

        var result = await handler.Handle(
            new ResolveParticipantCodeCommand(campaignSeriesId, request),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Error.ToString());
        Assert.Equal(response, result.Value);
        Assert.Equal(tenantId, store.TenantId);
        Assert.Equal(campaignSeriesId, store.CampaignSeriesId);
        Assert.Equal(request.RawCode, store.RawCode);
    }

    [Fact]
    public void Response_contract_does_not_expose_raw_code_or_created_flag()
    {
        Assert.DoesNotContain(
            typeof(ParticipantCodeResponse).GetProperties(),
            property =>
                property.Name.Contains("Raw", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Normalized", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Created", StringComparison.OrdinalIgnoreCase) ||
                property.Name.Contains("Existing", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class RecordingParticipantCodeStore(ParticipantCodeResponse response) : IParticipantCodeStore
    {
        public Guid TenantId { get; private set; }

        public Guid CampaignSeriesId { get; private set; }

        public string? RawCode { get; private set; }

        public Task<Result<ParticipantCodeResponse>> ResolveAsync(
            Guid tenantId,
            Guid campaignSeriesId,
            string rawCode,
            CancellationToken cancellationToken)
        {
            TenantId = tenantId;
            CampaignSeriesId = campaignSeriesId;
            RawCode = rawCode;

            return Task.FromResult(Result.Success(response));
        }
    }
}
