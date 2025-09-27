using System.Windows.Input;
using BangumiMediaTool.Models;
using BangumiMediaTool.ViewModels.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.Views.Windows;

public partial class MainWindow : INavigationWindow, IRecipient<DataSnackbarMessage>
{
    public MainWindowViewModel ViewModel { get; }

    private readonly SnackbarService snackbarService;

    public MainWindow(
        MainWindowViewModel viewModel,
        IPageService pageService,
        INavigationService navigationService
    )
    {
        ViewModel = viewModel;
        DataContext = this;

        SystemThemeWatcher.Watch(this);

        InitializeComponent();
        SetPageService(pageService);

        navigationService.SetNavigationControl(RootNavigation);
        snackbarService = new SnackbarService();
        snackbarService.SetSnackbarPresenter(UI_SnackbarPresenter);

        WeakReferenceMessenger.Default.Register<DataSnackbarMessage>(this);
    }

    #region INavigationWindow methods

    public INavigationView GetNavigation() => RootNavigation;

    public bool Navigate(Type pageType) => RootNavigation.Navigate(pageType);

    public void SetPageService(IPageService pageService) => RootNavigation.SetPageService(pageService);

    public void ShowWindow() => Show();

    public void CloseWindow() => Close();

    #endregion INavigationWindow methods

    /// <summary>
    /// Raises the closed event.
    /// </summary>
    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);

        // Make sure that closing this window will begin the process of closing the application.
        Application.Current.Shutdown();
    }

    INavigationView INavigationWindow.GetNavigation()
    {
        return RootNavigation;
    }

    public void SetServiceProvider(IServiceProvider serviceProvider) { }

    public void Receive(DataSnackbarMessage message)
    {
        snackbarService.Show(message.Title, message.Message, message.ControlAppearance,
            null,
            TimeSpan.FromSeconds(message.Seconds));
    }


    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // 获取当前的焦点控件
        var element = FocusManager.GetFocusedElement(this);

        // 如果当前有焦点控件，且点击的区域不在该控件上，则清除焦点
        if (element != null && !element.IsMouseOver && element is TextBox)
        {
            // 将焦点设置到窗口本身或其他透明控件
            FocusManager.SetFocusedElement(this, this);
        }

        base.OnPreviewMouseDown(e);
    }
}
