using System.Collections.ObjectModel;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.ViewModels.Windows;

public partial class MainWindowViewModel : ObservableObject
{
    [ObservableProperty] private string _applicationTitle = "BangumiMediaTool";
    [ObservableProperty] private Visibility _isProcess = Visibility.Hidden;
    [ObservableProperty] private string _processText = string.Empty;

    [ObservableProperty] private ObservableCollection<object> _menuItems = new()
    {
        // new NavigationViewItemHeader(),
        new NavigationViewItem()
        {
            Content = "元数据",
            Icon = new SymbolIcon { Symbol = SymbolRegular.DataUsage24 },
            TargetPageType = typeof(Views.Pages.MediaNfoDataPage)
        },
        new NavigationViewItem()
        {
            Content = "搜索",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Search24 },
            TargetPageType = typeof(Views.Pages.SearchDataPage)
        },
        new NavigationViewItem()
        {
            Content = "重命名",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Rename24 },
            TargetPageType = typeof(Views.Pages.ReNameFilePage)
        },
        new NavigationViewItem()
        {
            Content = "RSS订阅",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Rss24 },
            TargetPageType = typeof(Views.Pages.QbtRssPage)
        },
    };

    [ObservableProperty] private ObservableCollection<object> _footerMenuItems = new()
    {
        new NavigationViewItem()
        {
            Content = "设置",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            TargetPageType = typeof(Views.Pages.SettingsPage)
        }
    };

    [ObservableProperty] private ObservableCollection<MenuItem> _trayMenuItems = new() { };

    public void SetGlobalProcess(bool isShow, int currentValue = 0, int totalValue = 0, string text = "")
    {
        IsProcess = isShow ? Visibility.Visible : Visibility.Hidden;
        if (totalValue == 0 && text == string.Empty)
        {
            ProcessText = string.Empty;
        }
        else if (totalValue != 0 && text == string.Empty)
        {
            ProcessText = $"{currentValue} / {totalValue}";
        }
        else
        {
            ProcessText = $"{text}：{currentValue} / {totalValue}";
        }
    }
}