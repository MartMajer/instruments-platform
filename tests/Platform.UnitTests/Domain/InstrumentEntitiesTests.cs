using Platform.Domain.Instruments;

namespace Platform.UnitTests.Domain;

public sealed class InstrumentEntitiesTests
{
    [Fact]
    public void Canonical_instrument_is_global_locked_and_has_no_tenant()
    {
        var instrument = Instrument.CreateCanonical(
            Guid.NewGuid(),
            "olbi",
            "1.0.0",
            "Oldenburg Burnout Inventory",
            InstrumentDomains.Psychometric,
            "Demerouti et al. (2003)",
            InstrumentLicenseTypes.FreeAcademic);

        Assert.Null(instrument.TenantId);
        Assert.Null(instrument.ParentInstrumentId);
        Assert.True(instrument.IsGlobal);
        Assert.True(instrument.IsLocked);
        Assert.Equal(InstrumentValidityStatuses.Canonical, instrument.ValidityStatus);
    }

    [Fact]
    public void Derivative_instrument_is_tenant_owned_and_points_to_parent()
    {
        var tenantId = Guid.NewGuid();
        var parentId = Guid.NewGuid();
        var derivative = Instrument.CreateDerivative(
            Guid.NewGuid(),
            tenantId,
            parentId,
            "olbi-custom",
            "1.0.0",
            "Custom OLBI derivative",
            InstrumentDomains.Psychometric,
            "Derived from OLBI canonical");

        Assert.Equal(tenantId, derivative.TenantId);
        Assert.Equal(parentId, derivative.ParentInstrumentId);
        Assert.False(derivative.IsGlobal);
        Assert.False(derivative.IsLocked);
        Assert.Equal(InstrumentValidityStatuses.Derived, derivative.ValidityStatus);
    }

    [Fact]
    public void Canonical_instrument_records_platform_rights_and_official_label()
    {
        var instrument = Instrument.CreateCanonical(
            Guid.NewGuid(),
            "public-tool",
            "1.0.0",
            "Public Tool",
            InstrumentDomains.Psychometric,
            "Author citation",
            InstrumentLicenseTypes.Free);

        Assert.Equal(InstrumentRightsScopes.PlatformGranted, instrument.RightsScope);
        Assert.Equal(InstrumentRightsStatuses.Verified, instrument.RightsStatus);
        Assert.Equal(InstrumentValidityLabels.Official, instrument.ValidityLabel);
        Assert.Null(instrument.ProvenanceNote);
    }

    [Fact]
    public void Derivative_instrument_records_tenant_rights_provenance_and_adapted_label()
    {
        var tenantId = Guid.NewGuid();
        var parentId = Guid.NewGuid();

        var derivative = Instrument.CreateDerivative(
            Guid.NewGuid(),
            tenantId,
            parentId,
            "custom-workload",
            "1.0.0",
            "Custom Workload Instrument",
            InstrumentDomains.Psychometric,
            "Tenant adapted from private source");

        Assert.Equal(InstrumentRightsScopes.TenantProvided, derivative.RightsScope);
        Assert.Equal(InstrumentRightsStatuses.AttestedByTenant, derivative.RightsStatus);
        Assert.Equal(InstrumentValidityLabels.Adapted, derivative.ValidityLabel);
        Assert.Equal("Tenant adapted from private source", derivative.ProvenanceNote);
    }

    [Fact]
    public void Tenant_attested_private_import_with_unknown_license_can_start_campaign()
    {
        var instrument = Instrument.CreateTenantImport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "private-import",
            "1.0.0",
            "Private Import",
            InstrumentDomains.Psychometric,
            "Tenant provided item source",
            rightsStatus: InstrumentRightsStatuses.AttestedByTenant,
            validityLabel: InstrumentValidityLabels.TenantProvided);

        Assert.True(instrument.CanStartNewCampaign(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Unverified_internal_demo_import_cannot_start_campaign()
    {
        var instrument = Instrument.CreateTenantImport(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "demo-import",
            "1.0.0",
            "Demo Import",
            InstrumentDomains.Psychometric,
            "Demo-only import; rights unverified",
            rightsStatus: InstrumentRightsStatuses.UnverifiedInternalDemo,
            validityLabel: InstrumentValidityLabels.RightsUnverified);

        Assert.False(instrument.CanStartNewCampaign(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Expired_license_blocks_new_campaign_use()
    {
        var instrument = Instrument.CreateCanonical(
            Guid.NewGuid(),
            "paid-tool",
            "1.0.0",
            "Paid Tool",
            InstrumentDomains.Psychometric,
            "Vendor manual",
            InstrumentLicenseTypes.Paid,
            licenseExpiresAt: new DateTimeOffset(2026, 5, 5, 0, 0, 0, TimeSpan.Zero));

        Assert.False(instrument.CanStartNewCampaign(new DateTimeOffset(2026, 5, 6, 0, 0, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Unknown_license_blocks_new_campaign_use()
    {
        var instrument = Instrument.CreateCanonical(
            Guid.NewGuid(),
            "unclear",
            "1.0.0",
            "Unclear License Tool",
            InstrumentDomains.Psychometric,
            "Unclear source",
            InstrumentLicenseTypes.Unknown);

        Assert.False(instrument.CanStartNewCampaign(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Subscale_rejects_reliability_alpha_outside_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new InstrumentSubscale(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EX",
            "Exhaustion",
            8,
            InstrumentScoringMethods.Mean,
            reliabilityAlphaPublished: 1.2m));
    }

    [Fact]
    public void Norm_rejects_non_object_percentile_json()
    {
        Assert.Throws<ArgumentException>(() => new InstrumentNorm(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "EX",
            InstrumentNormTypes.PublishedInstrument,
            "general workforce",
            100,
            "en",
            percentiles: """[1,2,3]"""));
    }

    [Fact]
    public void Translation_requires_exactly_one_target()
    {
        Assert.Throws<ArgumentException>(() => new InstrumentTranslation(
            Guid.NewGuid(),
            instrumentId: Guid.NewGuid(),
            instrumentSubscaleId: Guid.NewGuid(),
            instrumentItemId: null,
            field: "name",
            locale: "hr",
            text: "Naziv",
            status: TranslationStatuses.DraftTranslation));
    }
}
