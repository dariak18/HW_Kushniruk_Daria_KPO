using Microsoft.Extensions.DependencyInjection;
using VyshKat;
using VyshKat.Services;

var services = new ServiceCollection();
services.AddSingleton<Fleet>();
services.AddSingleton<IServiceCenter, ServiceCenter>();
services.AddSingleton<ParkService>();
services.AddSingleton<ReportService>();
services.AddSingleton<ConsoleApp>();

using var provider = services.BuildServiceProvider(new ServiceProviderOptions
{
    ValidateOnBuild = true,
    ValidateScopes = true
});
provider.GetRequiredService<ConsoleApp>().Run(Console.In, Console.Out);
