using System.IO;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Page;
using BangumiMediaTool.Services.Program;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.ViewModels.Pages;

public partial class QbtRssViewModel : ObservableRecipient, INavigationAware
{
    [ObservableProperty] private string _bangumiName = string.Empty;
    [ObservableProperty] private string _bangumiId = string.Empty;
    [ObservableProperty] private string _rssFeedPath = string.Empty;
    [ObservableProperty] private string _rssRuleName = string.Empty;
    [ObservableProperty] private string _mustContain = string.Empty;
    [ObservableProperty] private string _mustNotContain = string.Empty;
    [ObservableProperty] private bool _isUseRegex = false;
    [ObservableProperty] private bool _enableRule = true;

    private DataSubjectsInfo? dataSubjectsInfo = null;

    public void AddRssData(DataSubjectsInfo message)
    {
        dataSubjectsInfo = message;

        BangumiName = RssPathService.GetRssFolderName(dataSubjectsInfo,0);
        BangumiId = dataSubjectsInfo.Id.ToString();
        RssRuleName = RssPathService.GetRssFolderName(dataSubjectsInfo,1);
    }

    [RelayCommand]
    private async Task OnAddRssToQbt()
    {
        if (BangumiName.Length == 0 || RssFeedPath.Length == 0 || GlobalConfig.Instance.AppConfig.QbtDefaultDownloadPath.Length == 0) return;

        var folderPath = Path.Combine(GlobalConfig.Instance.AppConfig.QbtDefaultDownloadPath, BangumiName.RemoveInvalidFileNameChar());

        var dataAddRssRule = new DataAddRssRule()
        {
            Enabled = EnableRule,
            MustContain = MustContain,
            MustNotContain = MustNotContain,
            UseRegex = IsUseRegex,
            EpisodeFilter = "",
            SmartFilter = false,
            PreviouslyMatchedEpisodes = [],
            AffectedFeeds = [RssFeedPath],
            IgnoreDays = 0,
            LastMatch = "",
            AddPaused = false,
            AssignedCategory = "Bangumi",
            SavePath = Path.Combine(folderPath, "Season 1").RemoveInvalidFileNameChar()
        };

        var addRuleName = RssRuleName.Length == 0 ? BangumiName : RssRuleName;

        bool addFeedSuccess = await QbtApiService.Instance.QbtApi_AddFeed(RssFeedPath, addRuleName);

        if (addFeedSuccess)
        {
            bool addRuleSuccess = await QbtApiService.Instance.QbtApi_AddRule(dataAddRssRule, addRuleName);
            if (addRuleSuccess)
            {
                WeakReferenceMessenger.Default.Send<DataSnackbarMessage>(
                    new DataSnackbarMessage("添加Rss：",
                        $"{addRuleName} [{RssFeedPath}]",
                        ControlAppearance.Success));
                //全部添加成功后创建文件夹和元数据
                if (EnableRule)
                {
                    if (!Directory.Exists(folderPath))
                    {
                        Directory.CreateDirectory(folderPath);
                    }

                    if (dataSubjectsInfo != null)
                    {
                        var subjectsInfo = new NfoInfo_SubjectsRootTv
                        {
                            bangumiid = dataSubjectsInfo.Id.ToString(),
                            title = dataSubjectsInfo.NameCn,
                            originaltitle = dataSubjectsInfo.Name,
                            showtitle = dataSubjectsInfo.NameCn,
                            year = DateTime.Parse(dataSubjectsInfo.AirDate).Year.ToString(),
                        };
                        CreateFileService.CreateNfoFromData(subjectsInfo, Path.Combine(folderPath, "tvshow.nfo"));
                    }
                    else if (dataSubjectsInfo == null)
                    {
                        var subjectsInfo = new NfoInfo_SubjectsRootTv
                        {
                            bangumiid = BangumiId,
                            title = BangumiName,
                            originaltitle = BangumiName,
                            showtitle = BangumiName,
                            year = string.Empty
                        };
                        CreateFileService.CreateNfoFromData(subjectsInfo, Path.Combine(folderPath, "tvshow.nfo"));
                    }
                }
            }
            else
            {
                WeakReferenceMessenger.Default.Send(new DataSnackbarMessage(
                    "添加Rss失败：",
                    $"添加下载规则：{addRuleSuccess}",
                    ControlAppearance.Caution));
            }
        }
        else
        {
            WeakReferenceMessenger.Default.Send(new DataSnackbarMessage(
                "添加Rss失败：",
                $"添加订阅：{addFeedSuccess}",
                ControlAppearance.Caution));
        }
    }

    partial void OnRssFeedPathChanged(string value)
    {
        var title = RssPathService.AnalyzeRssPath(value);
        if (!string.IsNullOrEmpty(title) && string.IsNullOrEmpty(BangumiName))
        {
            BangumiName = title;
            App.GetService<SearchDataViewModel>()?.AddSearchText(title);
        }
    }

    public void OnNavigatedTo() { }

    public void OnNavigatedFrom() { }

    public void ReloadConfig()
    {
        if (dataSubjectsInfo!=null)
        {
            AddRssData(dataSubjectsInfo);
        }
    }
}