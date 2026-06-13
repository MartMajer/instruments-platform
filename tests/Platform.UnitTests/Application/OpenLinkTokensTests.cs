using Platform.Application.Features.Responses;

namespace Platform.UnitTests.Application;

public sealed class OpenLinkTokensTests
{
    [Fact]
    public void Issue_embeds_tenant_routing_hint_and_hashes_raw_token()
    {
        var tenantId = Guid.NewGuid();

        var issued = OpenLinkTokens.Issue(tenantId);

        Assert.StartsWith($"opn_{tenantId:N}_", issued.RawToken, StringComparison.Ordinal);
        Assert.Equal(64, issued.TokenHash.Length);
        Assert.Equal(issued.TokenHash, OpenLinkTokens.Hash(issued.RawToken));
    }

    [Fact]
    public void IssueInvitation_embeds_tenant_routing_hint_and_hashes_raw_token()
    {
        var tenantId = Guid.NewGuid();

        var issued = OpenLinkTokens.IssueInvitation(tenantId);

        Assert.StartsWith($"inv_{tenantId:N}_", issued.RawToken, StringComparison.Ordinal);
        Assert.Equal(64, issued.TokenHash.Length);
        Assert.Equal(issued.TokenHash, OpenLinkTokens.Hash(issued.RawToken));
    }

    [Fact]
    public void IssueIdentifiedEntry_embeds_tenant_routing_hint_and_hashes_raw_token()
    {
        var tenantId = Guid.NewGuid();

        var issued = OpenLinkTokens.IssueIdentifiedEntry(tenantId);

        Assert.StartsWith($"idn_{tenantId:N}_", issued.RawToken, StringComparison.Ordinal);
        Assert.Equal(64, issued.TokenHash.Length);
        Assert.Equal(issued.TokenHash, OpenLinkTokens.Hash(issued.RawToken));
    }

    [Fact]
    public void IssueIdentifiedQueue_embeds_tenant_routing_hint_and_hashes_raw_token()
    {
        var tenantId = Guid.NewGuid();

        var issued = OpenLinkTokens.IssueIdentifiedQueue(tenantId);

        Assert.StartsWith($"idq_{tenantId:N}_", issued.RawToken, StringComparison.Ordinal);
        Assert.Equal(64, issued.TokenHash.Length);
        Assert.Equal(issued.TokenHash, OpenLinkTokens.Hash(issued.RawToken));
    }

    [Fact]
    public void ParseTenant_returns_tenant_id_for_valid_token()
    {
        var tenantId = Guid.NewGuid();
        var issued = OpenLinkTokens.Issue(tenantId);

        var parsed = OpenLinkTokens.ParseTenant(issued.RawToken);

        Assert.True(parsed.IsSuccess, parsed.Error.ToString());
        Assert.Equal(tenantId, parsed.Value.TenantId);
    }

    [Fact]
    public void ParseTenant_returns_tenant_id_for_valid_invitation_token()
    {
        var tenantId = Guid.NewGuid();
        var issued = OpenLinkTokens.IssueInvitation(tenantId);

        var parsed = OpenLinkTokens.ParseTenant(issued.RawToken);

        Assert.True(parsed.IsSuccess, parsed.Error.ToString());
        Assert.Equal(tenantId, parsed.Value.TenantId);
    }

    [Fact]
    public void ParseTenant_rejects_malformed_tokens_without_echoing_raw_token()
    {
        const string rawToken = "not-a-valid-open-link-token";

        var parsed = OpenLinkTokens.ParseTenant(rawToken);

        Assert.True(parsed.IsFailure);
        Assert.Equal("open_link.invalid_token", parsed.Error.Code);
        Assert.DoesNotContain(rawToken, parsed.Error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Hash_is_deterministic_for_exact_raw_token()
    {
        var issued = OpenLinkTokens.Issue(Guid.NewGuid());

        Assert.Equal(OpenLinkTokens.Hash(issued.RawToken), OpenLinkTokens.Hash(issued.RawToken));
    }

    [Fact]
    public void Public_session_handle_issue_embeds_tenant_hint_and_hashes_raw_handle()
    {
        var tenantId = Guid.NewGuid();

        var issued = OpenLinkSessionHandles.Issue(tenantId);

        Assert.StartsWith($"rsh_{tenantId:N}_", issued.RawHandle, StringComparison.Ordinal);
        Assert.Equal(64, issued.HandleHash.Length);
        Assert.Equal(issued.HandleHash, OpenLinkSessionHandles.Hash(issued.RawHandle));
    }

    [Fact]
    public void Public_session_handle_parse_tenant_returns_tenant_for_valid_handle()
    {
        var tenantId = Guid.NewGuid();
        var issued = OpenLinkSessionHandles.Issue(tenantId);

        var parsed = OpenLinkSessionHandles.ParseTenant(issued.RawHandle);

        Assert.True(parsed.IsSuccess, parsed.Error.ToString());
        Assert.Equal(tenantId, parsed.Value.TenantId);
    }

    [Fact]
    public void ParseTenant_returns_tenant_id_for_valid_identified_entry_token()
    {
        var tenantId = Guid.NewGuid();
        var issued = OpenLinkTokens.IssueIdentifiedEntry(tenantId);

        var parsed = OpenLinkTokens.ParseTenant(issued.RawToken);

        Assert.True(parsed.IsSuccess, parsed.Error.ToString());
        Assert.Equal(tenantId, parsed.Value.TenantId);
    }

    [Fact]
    public void ParseTenant_returns_tenant_id_for_valid_identified_queue_token()
    {
        var tenantId = Guid.NewGuid();
        var issued = OpenLinkTokens.IssueIdentifiedQueue(tenantId);

        var parsed = OpenLinkTokens.ParseTenant(issued.RawToken);

        Assert.True(parsed.IsSuccess, parsed.Error.ToString());
        Assert.Equal(tenantId, parsed.Value.TenantId);
    }

    [Fact]
    public void Public_session_handle_parse_tenant_rejects_malformed_handles_without_echoing_raw_handle()
    {
        const string rawHandle = "not-a-valid-public-session-handle";

        var parsed = OpenLinkSessionHandles.ParseTenant(rawHandle);

        Assert.True(parsed.IsFailure);
        Assert.Equal("public_session.invalid_handle", parsed.Error.Code);
        Assert.DoesNotContain(rawHandle, parsed.Error.Message, StringComparison.Ordinal);
    }
}
