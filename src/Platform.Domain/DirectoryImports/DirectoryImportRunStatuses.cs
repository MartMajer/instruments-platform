namespace Platform.Domain.DirectoryImports;

public static class DirectoryConnectionProviders
{
    public const string MicrosoftGraph = "microsoft_graph";

    public static bool IsKnown(string value) => value is MicrosoftGraph;
}

public static class DirectoryConnectionStatuses
{
    public const string Active = "active";
    public const string Revoked = "revoked";
    public const string Failed = "failed";

    public static bool IsKnown(string value) => value is Active or Revoked or Failed;
}

public static class DirectoryImportRunModes
{
    public const string Preview = "preview";
    public const string Apply = "apply";

    public static bool IsKnown(string value) => value is Preview or Apply;
}

public static class DirectoryImportRunStatuses
{
    public const string Planned = "planned";
    public const string Previewed = "previewed";
    public const string Applying = "applying";
    public const string Applied = "applied";
    public const string Failed = "failed";

    public static bool IsKnown(string value) => value is Planned or Previewed or Applying or Applied or Failed;
}

public static class DirectoryImportRunItemActions
{
    public const string CreateSubject = "create_subject";
    public const string UpdateSubject = "update_subject";
    public const string CreateGroup = "create_group";
    public const string AddMembership = "add_membership";
    public const string SetManager = "set_manager";
    public const string DeactivateSubject = "deactivate_subject";
    public const string NoChange = "no_change";
    public const string Warning = "warning";

    public static bool IsKnown(string value) =>
        value is CreateSubject
            or UpdateSubject
            or CreateGroup
            or AddMembership
            or SetManager
            or DeactivateSubject
            or NoChange
            or Warning;
}

public static class DirectoryImportRunItemStatuses
{
    public const string Planned = "planned";
    public const string Applied = "applied";
    public const string Skipped = "skipped";
    public const string Warning = "warning";
    public const string Failed = "failed";

    public static bool IsKnown(string value) => value is Planned or Applied or Skipped or Warning or Failed;
}
