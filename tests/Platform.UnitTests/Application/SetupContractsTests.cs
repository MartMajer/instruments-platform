using System.Reflection;
using Platform.Application.Features.Setup;

namespace Platform.UnitTests.Application;

public sealed class SetupContractsTests
{
    [Fact]
    public void Create_campaign_identified_queue_access_request_has_no_destructive_switches()
    {
        var requestType = typeof(CreateCampaignIdentifiedQueueAccessRequest);

        Assert.DoesNotContain(
            requestType.GetProperties(BindingFlags.Instance | BindingFlags.Public),
            property => property.PropertyType == typeof(bool) ||
                property.Name.Contains("Rotate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Campaign_identified_queue_access_response_has_no_rotation_counters()
    {
        var responseType = typeof(CampaignIdentifiedQueueAccessResponse);

        Assert.DoesNotContain(
            responseType.GetProperties(BindingFlags.Instance | BindingFlags.Public),
            property => property.Name.Contains("Rotate", StringComparison.OrdinalIgnoreCase));
    }
}
