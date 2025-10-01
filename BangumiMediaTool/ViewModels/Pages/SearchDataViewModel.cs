using System.Collections.ObjectModel;
using System.ComponentModel;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Api;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Windows;
using BangumiMediaTool.Views.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.ViewModels.Pages;

public partial class SearchDataViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty] private string _searchText = string.Empty;

    [ObservableProperty] private DataSubjectsInfo? _searchListSelectItem;
    [ObservableProperty] private ObservableCollection<DataSubjectsInfo> _dataSearchResultList = [];
    [ObservableProperty] private bool _isSearchAllResults = false;

    [ObservableProperty] private ObservableCollection<DataEpisodesInfo> _dataEpisodesInfoList = [];
    [ObservableProperty] private Visibility _isEpisodes = Visibility.Hidden;

    [ObservableProperty] private string _subjectInfoName = string.Empty;
    [ObservableProperty] private string _subjectInfoCount = string.Empty;
    [ObservableProperty] private string _subjectInfoDate = string.Empty;
    [ObservableProperty] private string _subjectInfoUrl = string.Empty;
    [ObservableProperty] private string _subjectInfoDesc = string.Empty;

    [ObservableProperty] private string _showPageText = string.Empty;
    [ObservableProperty] private Visibility _isPages = Visibility.Collapsed;
    private long _currentPageNum = 0;
    private long _totalPageNum = 0;
    private string _currentSearchText = string.Empty;

    private Dictionary<long, List<DataEpisodesInfo>> _tempDataList = new();

    public void OnNavigatedTo()
    {
        if (DataSearchResultList.Count == 0 || SearchListSelectItem is null)
        {
            IsEpisodes = Visibility.Hidden;
            IsPages = Visibility.Collapsed;
            ShowPageText = string.Empty;
            _currentPageNum = 0;
            _totalPageNum = 0;
            _currentSearchText = string.Empty;
        }
    }

    public void OnNavigatedFrom() { }

    /// <summary>
    /// 添加文本到搜索框
    /// </summary>
    /// <param name="str"></param>
    public void AddSearchText(string str)
    {
        SearchText = str;
    }

    [RelayCommand]
    private async Task OnSearch()
    {
        IsEpisodes = Visibility.Hidden;
        IsPages = Visibility.Collapsed;
        DataEpisodesInfoList.Clear();
        DataSearchResultList.Clear();
        SearchListSelectItem = null;
        ShowPageText = string.Empty;
        _currentPageNum = 0;
        _totalPageNum = 0;
        _currentSearchText = string.Empty;

        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);

        var (list, total) = await BangumiApiService.Instance.BangumiApi_Search(SearchText, 0);

        main?.SetGlobalProcess(false);

        DataSearchResultList = new ObservableCollection<DataSubjectsInfo>(list);
        if (list.Count > 0)
        {
            IsEpisodes = Visibility.Visible;
            IsPages = Visibility.Visible;
            SearchListSelectItem = list[0];
            _currentSearchText = SearchText;
            _currentPageNum = 1;
            _totalPageNum = (long)Math.Ceiling(total / 20.0f);
            SetPageText();
        }
        else
        {
            Logs.LogError("搜索无结果");
        }
    }

    [RelayCommand]
    private async Task OnPagePrevious()
    {
        if (_currentPageNum - 1 <= 0) return;

        DataEpisodesInfoList.Clear();
        DataSearchResultList.Clear();
        _currentPageNum = _currentPageNum - 1;
        SetPageText();

        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);
        var (list, _) = await BangumiApiService.Instance.BangumiApi_Search(SearchText, (_currentPageNum - 1) * 20);
        main?.SetGlobalProcess(false);

        DataSearchResultList = new ObservableCollection<DataSubjectsInfo>(list);
        if (list.Count > 0)
        {
            SearchListSelectItem = list[0];
            _currentSearchText = SearchText;
        }
        else
        {
            SearchListSelectItem = null;
        }
    }

    [RelayCommand]
    private async Task OnPageNext()
    {
        if (_currentPageNum + 1 > _totalPageNum) return;

        DataEpisodesInfoList.Clear();
        DataSearchResultList.Clear();

        _currentPageNum = _currentPageNum + 1;
        SetPageText();

        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);
        var (list, _) = await BangumiApiService.Instance.BangumiApi_Search(SearchText, (_currentPageNum - 1) * 20);
        main?.SetGlobalProcess(false);

        DataSearchResultList = new ObservableCollection<DataSubjectsInfo>(list);
        if (list.Count > 0)
        {
            SearchListSelectItem = list[0];
            _currentSearchText = SearchText;
        }
        else
        {
            SearchListSelectItem = null;
        }
    }

    private void SetPageText()
    {
        if (_totalPageNum == 0)
        {
            ShowPageText = string.Empty;
        }
        else
        {
            ShowPageText = $"{_currentPageNum}/{_totalPageNum}";
        }
    }

    partial void OnSearchTextChanged(string? oldValue, string newValue)
    {
        IsPages = newValue.Equals(_currentSearchText) ? Visibility.Visible : Visibility.Collapsed;
    }

    partial void OnSearchListSelectItemChanged(DataSubjectsInfo? value)
    {
        DataEpisodesInfoList.Clear();

        if (value != null)
        {
            Logs.LogInfo($"{value.NameCn} ({value.Name})  id:{value.Id}  话数:{value.EpsCount}  放送时间：{value.AirDate}");

            if (string.IsNullOrEmpty(value.Name))
            {
                SubjectInfoName = value.NameCn;
            }
            else
            {
                SubjectInfoName = $"{value.NameCn} ({value.Name})";
            }

            SubjectInfoCount = value.EpsCount.ToString();
            SubjectInfoDate = value.AirDate;
            SubjectInfoDesc = value.Desc;
            SubjectInfoUrl = @"https://bgm.tv/subject/" + value.Id;


            if (_tempDataList.TryGetValue(value.Id, out var list))
            {
                DataEpisodesInfoList = new ObservableCollection<DataEpisodesInfo>(list);
            }
        }
        else
        {
            SubjectInfoName = string.Empty;
            SubjectInfoCount = string.Empty;
            SubjectInfoDate = string.Empty;
            SubjectInfoDesc = string.Empty;
            SubjectInfoUrl = string.Empty;
        }
    }

    [RelayCommand]
    private async Task OnGetEpisodeInfoList()
    {
        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);

        if (SearchListSelectItem != null)
        {
            var list = await BangumiApiService.Instance.BangumiApi_Episodes(SearchListSelectItem);
            DataEpisodesInfoList = new ObservableCollection<DataEpisodesInfo>(list);

            if (!_tempDataList.TryAdd(SearchListSelectItem.Id, list))
            {
                _tempDataList[SearchListSelectItem.Id] = list;
            }
        }

        main?.SetGlobalProcess(false);
    }

    [RelayCommand]
    private async Task OnAddToReName(object? sender)
    {
        if (SearchListSelectItem is null) return;
        var nfoPage = App.GetService<MediaNfoDataViewModel>();

        if (DataEpisodesInfoList.Count == 0)
        {
            var main = App.GetService<MainWindowViewModel>();
            main?.SetGlobalProcess(true);
            var list = await BangumiApiService.Instance.BangumiApi_Episodes(SearchListSelectItem);
            main?.SetGlobalProcess(false);
            if (list.Count != 0)
            {
                DataEpisodesInfoList = new ObservableCollection<DataEpisodesInfo>(list);
                nfoPage?.AddToNfoData(list);
                WeakReferenceMessenger.Default.Send(new DataSnackbarMessage("添加到元数据", string.Empty, ControlAppearance.Success));
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new DataSnackbarMessage("获取剧集信息失败", string.Empty, ControlAppearance.Caution));
            }
        }
        else
        {
            if (sender is not ListView listView) return;
            var episodesInfoSelectItems = listView.SelectedItems.Cast<DataEpisodesInfo>().ToList();

            if (episodesInfoSelectItems.Count == 0)
            {
                nfoPage?.AddToNfoData(DataEpisodesInfoList.ToList());
            }
            else
            {
                nfoPage?.AddToNfoData(episodesInfoSelectItems.ToList());
            }

            WeakReferenceMessenger.Default.Send(new DataSnackbarMessage("添加到元数据", string.Empty, ControlAppearance.Success));
        }
    }

    [RelayCommand]
    private void OnAddToRss()
    {
        var rss = App.GetService<QbtRssViewModel>();
        if (rss == null || SearchListSelectItem == null) return;
        rss.AddRssData(SearchListSelectItem);

        WeakReferenceMessenger.Default.Send(new DataSnackbarMessage("添加到RSS", SearchListSelectItem.NameCn, ControlAppearance.Success));
    }
}