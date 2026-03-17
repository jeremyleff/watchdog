using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Watchdog.Configuration;
using Watchdog.Orchestration;
using Watchdog.State;

IHost host = Host.CreateDefaultBuilder(args)
    .UseWindowsService()
    .ConfigureServices((context, services) =>
    {
        services.Configure<NotificationMonitorOptions>(
            context.Configuration.GetSection(NotificationMonitorOptions.SectionName));

        services.AddHttpClient();
        services.AddSingleton<IStateStore, FileStateStore>();
        services.AddSingleton<MonitorFactory>();
        services.AddSingleton<NotificationChannelFactory>();
        services.AddHostedService<MonitorOrchestrator>();
    })
    .Build();

await host.RunAsync();
