namespace Platform.SharedKernel;

public static class PlatformIds
{
    public static Guid NewId()
    {
        return Guid.CreateVersion7();
    }
}
