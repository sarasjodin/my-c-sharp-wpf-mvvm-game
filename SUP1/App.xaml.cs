using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using SUP.Services;
using SUP.Services.Eats;
using SUP.Services.Monsters;
using SUP.Services.SuperCell;
using SUP.ViewModels;
using System.Windows;

namespace SUP;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IHost _host;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var builder = Host.CreateDefaultBuilder()
       .ConfigureAppConfiguration(cfg => cfg.AddUserSecrets<App>())
       .ConfigureServices((ctx, services) =>
       {
           // Stateful resurser DB pool stateful som äger tillstånd / resurser
           services.AddSingleton(sp =>
           {
               var cs = sp.GetRequiredService<IConfiguration>()
                          .GetConnectionString("Development")
                        ?? throw new InvalidOperationException("ConnectionStrings:Development saknas.");
               return NpgsqlDataSource.Create(cs); // stateful connection pool
           });

           // https://abdelmajid-baco.medium.com/exploring-scopes-in-c-7d44eaeb8d51
           // https://www.bytehide.com/blog/scoped-transient-singleton-csharp
           // Stores & state singletons
           services.AddSingleton<NavigationStore>(); // stateful CurrentViewModel och History
           services.AddSingleton<GameState>();

           // Stateless services
           services.AddSingleton<INavigationService, NavigationService>(); // manipulerar store men har ingen egen state - stateless över store
           services.AddSingleton<IDbService, PgDbService>(); // stateless wrapper
           services.AddSingleton<IMonsterService, PancakeMonsterService>();
           services.AddSingleton<ISuperCellService, SuperCellService>();
           services.AddSingleton<IEatService, EatService>();

           // Per-VM state
           services.AddTransient<ITimerService, TimerService>(); // en timer / BoardVM

           // VMs Transient
           services.AddTransient<StartViewModel>();
           services.AddTransient<BoardViewModel>();
           services.AddTransient<ScoreboardViewModel>();
           services.AddTransient<RulesViewModel>();
           services.AddTransient<EndViewModel>();

           // Fönster
           services.AddSingleton<MainWindow>(sp => new MainWindow { DataContext = sp.GetRequiredService<NavigationStore>() });
       });

        _host = builder.Build();

        // Första sidan
        var nav = _host.Services.GetRequiredService<INavigationService>();
        nav.NavigateTo<StartViewModel>();

        // Visa fönstret och bind till store
        var store = _host.Services.GetRequiredService<NavigationStore>();
        var window = _host.Services.GetRequiredService<MainWindow>();
        window.DataContext = store;
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Stoppa och disposa hosten
        // Stänger alla IDisposable/IAsyncDisposable singletons
        _host?.StopAsync().GetAwaiter().GetResult();
        _host?.Dispose();

        base.OnExit(e);
    }
}