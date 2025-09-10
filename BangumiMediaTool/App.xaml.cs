using System.IO;
using System.Reflection;
using System.Windows.Threading;
using BangumiMediaTool.Services;
using BangumiMediaTool.Services.Api;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Pages;
using BangumiMediaTool.ViewModels.Windows;
using BangumiMediaTool.Views.Pages;
using BangumiMediaTool.Views.Windows;
using BangumiSubReNamer.Services;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wpf.Ui;
using Wpf.Ui.Controls;

namespace BangumiMediaTool;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App
{
    // The.NET Generic Host provides dependency injection, configuration, logging, and other services.
    // https://docs.microsoft.com/dotnet/core/extensions/generic-host
    // https://docs.microsoft.com/dotnet/core/extensions/dependency-injection
    // https://docs.microsoft.com/dotnet/core/extensions/configuration
    // https://docs.microsoft.com/dotnet/core/extensions/logging
    private static readonly IHost _host = Host
        .CreateDefaultBuilder()
        /*.ConfigureAppConfiguration(config =>
        {
            config.AddJsonFile("config.json", optional: false, reloadOnChange: true);
        })*/
        .ConfigureServices((context, services) =>
        {
            services.AddHostedService<ApplicationHostService>();

            // Page resolver service
            services.AddSingleton<IPageService, PageService>();

            // Theme manipulation
            services.AddSingleton<IThemeService, ThemeService>();

            // TaskBar manipulation
            services.AddSingleton<ITaskBarService, TaskBarService>();

            // Service containing navigation, same as INavigationWindow... but without window
            services.AddSingleton<INavigationService, NavigationService>();

            // Main window with navigation
            services.AddSingleton<INavigationWindow, MainWindow>();
            services.AddSingleton<MainWindowViewModel>();

            services.AddSingleton<MediaNfoDataViewModel>();
            services.AddSingleton<MediaNfoDataPage>();
            services.AddSingleton<SearchDataViewModel>();
            services.AddSingleton<SearchDataPage>();
            services.AddSingleton<ReNameFileViewModel>();
            services.AddSingleton<ReNameFilePage>();
            services.AddSingleton<QbtRssViewModel>();
            services.AddSingleton<QbtRssPage>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<SettingsPage>();
        }).Build();

    /// <summary>
    /// Gets registered service.
    /// </summary>
    /// <typeparam name="T">Type of the service to get.</typeparam>
    /// <returns>Instance of the service or <see langword="null"/>.</returns>
    public static T? GetService<T>()
        where T : class
    {
        return _host.Services.GetService(typeof(T)) as T;
    }

    /// <summary>
    /// Occurs when the application is loading.
    /// </summary>
    private void OnStartup(object sender, StartupEventArgs e)
    {
        _host.Start();

        _ = new GlobalConfig();
        _ = new BangumiApiService();
        _ = new TmdbApiService();
        _ = new QbtApiService();
    }

    /// <summary>
    /// Occurs when the application is closing.
    /// </summary>
    private async void OnExit(object sender, ExitEventArgs e)
    {
        await _host.StopAsync();

        _host.Dispose();
    }

    /// <summary>
    /// Occurs when an exception is thrown by an application but not handled.
    /// </summary>
    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // For more info see https://docs.microsoft.com/en-us/dotnet/api/system.windows.application.dispatcherunhandledexception?view=windowsdesktop-6.0
        // WeakReferenceMessenger.Default.Send(new DataSnackbarMessage("错误", e.Exception.Message, ControlAppearance.Caution));
        Logs.LogError(e.Exception.Message);
        e.Handled = true;
    }
}
