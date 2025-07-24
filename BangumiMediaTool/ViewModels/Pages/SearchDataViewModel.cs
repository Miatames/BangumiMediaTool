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

    public void OnNavigatedTo()
    {
        if (DataSearchResultList.Count == 0 || SearchListSelectItem is null)
        {
            IsEpisodes = Visibility.Hidden;
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
        DataEpisodesInfoList.Clear();
        SearchListSelectItem = null;

        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);

        var list = await BangumiApiService.Instance.BangumiApi_Search(SearchText, IsSearchAllResults);

        main?.SetGlobalProcess(false);

        DataSearchResultList = new ObservableCollection<DataSubjectsInfo>(list);
        if (DataSearchResultList.Count > 0)
        {
            IsEpisodes = Visibility.Visible;
            SearchListSelectItem = DataSearchResultList[0];
        }
        else
        {
            Logs.LogError("搜索无结果");
        }
    }

    partial void OnSearchListSelectItemChanged(DataSubjectsInfo? value)
    {
        if (value != null)
        {
            Logs.LogInfo(value.ShowText);

            SubjectInfoName = $"{value.NameCn} ({value.Name})";
            SubjectInfoCount = value.EpsCount.ToString();
            SubjectInfoDate = value.AirDate;
            SubjectInfoDesc = value.Desc;
            SubjectInfoUrl = @"https://bgm.tv/subject/" + value.Id;
        }
        else
        {
            SubjectInfoName = string.Empty;
            SubjectInfoCount = string.Empty;
            SubjectInfoDate = string.Empty;
            SubjectInfoDesc = string.Empty;
            SubjectInfoUrl = string.Empty;
        }

        DataEpisodesInfoList.Clear();
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