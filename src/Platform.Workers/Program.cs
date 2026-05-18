using Microsoft.Extensions.Hosting;

namespace Platform.Workers;

public static class Program
{
    public static async Task Main(string[] args)
    {
        await WorkerHostBuilder.Build(args).RunAsync();
    }
}
