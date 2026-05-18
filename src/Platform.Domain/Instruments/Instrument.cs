namespace Platform.Domain.Instruments;

public sealed class Instrument
{
    private Instrument()
    {
    }

    private Instrument(
        Guid id,
        Guid? tenantId,
        string code,
        string version,
        string fullName,
        string domain,
        string citationApa,
        string licenseType,
        bool isLocked,
        bool isGlobal,
        string validityStatus,
        Guid? parentInstrumentId,
        string? constructCategory,
        IEnumerable<string>? developers,
        int? yearFirstPublished,
        string? doi,
        string? licenseTermsUrl,
        DateTimeOffset? licenseExpiresAt,
        string rightsScope,
        string rightsStatus,
        string validityLabel,
        string? provenanceNote,
        string? vendor,
        Guid? canonicalTemplateVersionId)
    {
        ValidateShape(tenantId, isGlobal, validityStatus, parentInstrumentId);
        ValidateDomain(domain);
        ValidateLicenseType(licenseType);
        ValidateRightsScope(rightsScope);
        ValidateRightsStatus(rightsStatus);
        ValidateValidityLabel(validityLabel);
        ValidateRightsShape(isGlobal, rightsScope, validityStatus, validityLabel);

        Id = id;
        TenantId = tenantId;
        Code = NormalizeCode(code);
        Version = NormalizeRequired(version, nameof(version));
        FullName = NormalizeRequired(fullName, nameof(fullName));
        Domain = domain;
        ConstructCategory = NormalizeOptional(constructCategory);
        Developers = developers?.Select(NormalizeDeveloper).Where(value => value.Length > 0).Distinct().ToArray() ?? [];
        YearFirstPublished = yearFirstPublished;
        CitationApa = NormalizeRequired(citationApa, nameof(citationApa));
        Doi = NormalizeOptional(doi);
        LicenseType = licenseType;
        LicenseTermsUrl = NormalizeOptional(licenseTermsUrl);
        LicenseExpiresAt = licenseExpiresAt;
        RightsScope = rightsScope;
        RightsStatus = rightsStatus;
        ValidityLabel = validityLabel;
        ProvenanceNote = NormalizeOptional(provenanceNote);
        Vendor = NormalizeOptional(vendor);
        IsLocked = isLocked;
        IsGlobal = isGlobal;
        ValidityStatus = validityStatus;
        ParentInstrumentId = parentInstrumentId;
        CanonicalTemplateVersionId = canonicalTemplateVersionId;
        CreatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = CreatedAt;
    }

    public Guid Id { get; private set; }

    public Guid? TenantId { get; private set; }

    public string Code { get; private set; } = string.Empty;

    public string Version { get; private set; } = string.Empty;

    public string FullName { get; private set; } = string.Empty;

    public string Domain { get; private set; } = string.Empty;

    public string? ConstructCategory { get; private set; }

    public string[] Developers { get; private set; } = [];

    public int? YearFirstPublished { get; private set; }

    public string CitationApa { get; private set; } = string.Empty;

    public string? Doi { get; private set; }

    public string LicenseType { get; private set; } = string.Empty;

    public string? LicenseTermsUrl { get; private set; }

    public DateTimeOffset? LicenseExpiresAt { get; private set; }

    public string RightsScope { get; private set; } = string.Empty;

    public string RightsStatus { get; private set; } = string.Empty;

    public string ValidityLabel { get; private set; } = string.Empty;

    public string? ProvenanceNote { get; private set; }

    public string? Vendor { get; private set; }


    public bool IsLocked { get; private set; }

    public bool IsGlobal { get; private set; }

    public string ValidityStatus { get; private set; } = string.Empty;

    public Guid? ParentInstrumentId { get; private set; }

    public Guid? CanonicalTemplateVersionId { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public static Instrument CreateCanonical(
        Guid id,
        string code,
        string version,
        string fullName,
        string domain,
        string citationApa,
        string licenseType,
        string? constructCategory = null,
        IEnumerable<string>? developers = null,
        int? yearFirstPublished = null,
        string? doi = null,
        string? licenseTermsUrl = null,
        DateTimeOffset? licenseExpiresAt = null,
        string? vendor = null,
        Guid? canonicalTemplateVersionId = null)
    {
        return new Instrument(
            id,
            tenantId: null,
            code,
            version,
            fullName,
            domain,
            citationApa,
            licenseType,
            isLocked: true,
            isGlobal: true,
            InstrumentValidityStatuses.Canonical,
            parentInstrumentId: null,
            constructCategory,
            developers,
            yearFirstPublished,
            doi,
            licenseTermsUrl,
            licenseExpiresAt,
            InstrumentRightsScopes.PlatformGranted,
            InstrumentRightsStatuses.Verified,
            InstrumentValidityLabels.Official,
            provenanceNote: null,
            vendor,
            canonicalTemplateVersionId);
    }

    public static Instrument CreateDerivative(
        Guid id,
        Guid tenantId,
        Guid parentInstrumentId,
        string code,
        string version,
        string fullName,
        string domain,
        string provenanceNote)
    {
        return new Instrument(
            id,
            tenantId,
            code,
            version,
            fullName,
            domain,
            citationApa: provenanceNote,
            licenseType: InstrumentLicenseTypes.Unknown,
            isLocked: false,
            isGlobal: false,
            InstrumentValidityStatuses.Derived,
            parentInstrumentId,
            constructCategory: null,
            developers: null,
            yearFirstPublished: null,
            doi: null,
            licenseTermsUrl: null,
            licenseExpiresAt: null,
            InstrumentRightsScopes.TenantProvided,
            InstrumentRightsStatuses.AttestedByTenant,
            InstrumentValidityLabels.Adapted,
            provenanceNote,
            vendor: null,
            canonicalTemplateVersionId: null);
    }

    public static Instrument CreateTenantImport(
        Guid id,
        Guid tenantId,
        string code,
        string version,
        string fullName,
        string domain,
        string provenanceNote,
        string rightsStatus,
        string validityLabel,
        string licenseType = InstrumentLicenseTypes.Unknown,
        string? citationApa = null)
    {
        return new Instrument(
            id,
            tenantId,
            code,
            version,
            fullName,
            domain,
            citationApa: citationApa ?? provenanceNote,
            licenseType,
            isLocked: false,
            isGlobal: false,
            InstrumentValidityStatuses.PrivateImport,
            parentInstrumentId: null,
            constructCategory: null,
            developers: null,
            yearFirstPublished: null,
            doi: null,
            licenseTermsUrl: null,
            licenseExpiresAt: null,
            InstrumentRightsScopes.TenantProvided,
            rightsStatus,
            validityLabel,
            provenanceNote,
            vendor: null,
            canonicalTemplateVersionId: null);
    }

    public bool CanStartNewCampaign(DateTimeOffset now)
    {
        if (ValidityStatus is InstrumentValidityStatuses.Draft or InstrumentValidityStatuses.Retired)
        {
            return false;
        }

        if (RightsStatus is InstrumentRightsStatuses.UnverifiedInternalDemo or InstrumentRightsStatuses.Expired)
        {
            return false;
        }

        if (RightsScope == InstrumentRightsScopes.PlatformGranted && LicenseType == InstrumentLicenseTypes.Unknown)
        {
            return false;
        }

        if (RightsScope == InstrumentRightsScopes.TenantProvided &&
            RightsStatus != InstrumentRightsStatuses.AttestedByTenant)
        {
            return false;
        }

        return !LicenseExpiresAt.HasValue || LicenseExpiresAt.Value > now;
    }

    private static void ValidateShape(
        Guid? tenantId,
        bool isGlobal,
        string validityStatus,
        Guid? parentInstrumentId)
    {
        if (!InstrumentValidityStatuses.IsKnown(validityStatus))
        {
            throw new ArgumentException("Unknown instrument validity status.", nameof(validityStatus));
        }

        if (isGlobal && tenantId.HasValue)
        {
            throw new ArgumentException("Global instruments cannot have a tenant.", nameof(tenantId));
        }

        if (!isGlobal && !tenantId.HasValue)
        {
            throw new ArgumentException("Tenant instruments require a tenant.", nameof(tenantId));
        }

        if (validityStatus == InstrumentValidityStatuses.Derived && !parentInstrumentId.HasValue)
        {
            throw new ArgumentException("Derivative instruments require a parent instrument.", nameof(parentInstrumentId));
        }

        if (validityStatus == InstrumentValidityStatuses.PrivateImport && parentInstrumentId.HasValue)
        {
            throw new ArgumentException("Private imports cannot have a parent instrument.", nameof(parentInstrumentId));
        }

        if (validityStatus == InstrumentValidityStatuses.Canonical && parentInstrumentId.HasValue)
        {
            throw new ArgumentException("Canonical instruments cannot have a parent instrument.", nameof(parentInstrumentId));
        }
    }

    private static void ValidateDomain(string domain)
    {
        if (!InstrumentDomains.IsKnown(domain))
        {
            throw new ArgumentException("Unknown instrument domain.", nameof(domain));
        }
    }

    private static void ValidateLicenseType(string licenseType)
    {
        if (!InstrumentLicenseTypes.IsKnown(licenseType))
        {
            throw new ArgumentException("Unknown instrument license type.", nameof(licenseType));
        }
    }

    private static void ValidateRightsScope(string rightsScope)
    {
        if (!InstrumentRightsScopes.IsKnown(rightsScope))
        {
            throw new ArgumentException("Unknown instrument rights scope.", nameof(rightsScope));
        }
    }

    private static void ValidateRightsStatus(string rightsStatus)
    {
        if (!InstrumentRightsStatuses.IsKnown(rightsStatus))
        {
            throw new ArgumentException("Unknown instrument rights status.", nameof(rightsStatus));
        }
    }

    private static void ValidateValidityLabel(string validityLabel)
    {
        if (!InstrumentValidityLabels.IsKnown(validityLabel))
        {
            throw new ArgumentException("Unknown instrument validity label.", nameof(validityLabel));
        }
    }

    private static void ValidateRightsShape(
        bool isGlobal,
        string rightsScope,
        string validityStatus,
        string validityLabel)
    {
        if (isGlobal && rightsScope != InstrumentRightsScopes.PlatformGranted)
        {
            throw new ArgumentException("Global instruments require platform-granted rights.", nameof(rightsScope));
        }

        if (!isGlobal && rightsScope != InstrumentRightsScopes.TenantProvided)
        {
            throw new ArgumentException("Tenant instruments require tenant-provided rights.", nameof(rightsScope));
        }

        if (validityStatus == InstrumentValidityStatuses.Canonical &&
            validityLabel != InstrumentValidityLabels.Official)
        {
            throw new ArgumentException("Canonical instruments require the official validity label.", nameof(validityLabel));
        }
    }

    private static string NormalizeCode(string code)
    {
        return NormalizeRequired(code, nameof(code)).ToLowerInvariant();
    }

    private static string NormalizeRequired(string value, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        return value.Trim();
    }

    private static string NormalizeDeveloper(string value)
    {
        return value.Trim();
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
