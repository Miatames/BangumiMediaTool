using System.Collections.ObjectModel;
using System.Text;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty] private AppConfig _appConfig = new();
    [ObservableProperty] private ObservableCollection<AppConfig_DefaultPathMap> tempPathMap = new();

    private ConsoleLogWindow? consoleLogWindow = null;

    public void OnNavigatedTo()
    {
        AppConfig = GlobalConfig.Instance.AppConfig;

        TempPathMap.Clear();
        foreach (var item in AppConfig.DefaultPathMap)
        {
            TempPathMap.Add(new AppConfig_DefaultPathMap()
            {
                Key = item.Key,
                Value = item.Value
            });
        }
    }

    public void OnNavigatedFrom() { }

    [RelayCommand]
    private void OnSetConfig()
    {
        var addDict = new Dictionary<string, string>();
        foreach (var item in TempPathMap)
        {
            if (!addDict.TryAdd(item.Key, item.Value))
            {
                Logs.LogError("路径表包含重复的索引");
                return;
            }
        }

        if (addDict.Count == 0)
        {
            Logs.LogError("至少需要有一条路径配置");
            return;
        }

        AppConfig.DefaultPathMap = addDict;

        GlobalConfig.Instance.WriteConfig(AppConfig);
        WeakReferenceMessenger.Default.Send(new DataSnackbarMessage("更新设置", string.Empty, ControlAppearance.Success));
    }

    [RelayCommand]
    private void OnShowConsoleLogWindow()
    {
        if (consoleLogWindow != null) return;

        consoleLogWindow = new ConsoleLogWindow()
        {
            ShowInTaskbar = false
        };
        consoleLogWindow.Show();
        consoleLogWindow.Closed += (sender, args) => consoleLogWindow = null;
    }

    [RelayCommand]
    private void OnAddPathItem()
    {
        var addKey = new StringBuilder("Key");
        while (AppConfig.DefaultPathMap.ContainsKey(addKey.ToString()))
        {
            addKey.Append("_1");
        }

        TempPathMap.Add(new AppConfig_DefaultPathMap()
        {
            Key = addKey.ToString(),
            Value = string.Empty
        });

        AppConfig.DefaultPathMap.Add(addKey.ToString(), string.Empty);
    }

    [RelayCommand]
    private void OnDeletePathItem(object? sender)
    {
        if (sender is not AppConfig_DefaultPathMap item) return;

        TempPathMap.Remove(item);
        AppConfig.DefaultPathMap.Remove(item.Key);
    }
}