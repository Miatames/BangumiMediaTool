using System.Collections.ObjectModel;

namespace BangumiMediaTool.Models;

public class AppConfig : ObservableObject
{
    //匹配字幕文件扩展名
    public string RegexMatchSubtitleFiles { get; set; } = ".ass|.srt";

    //排除字幕文件扩展名
    public string RegexRemoveSubtitleFiles { get; set; } = @"\[|\]|\(|\)|[\u4e00-\u9fa5]";

    //匹配媒体文件扩展名
    public string RegexMatchMediaFiles { get; set; } = ".mp4|.mkv|.flv|.strm";

    //默认添加字幕文件扩展名
    public string DefaultAddSubtitleFilesExtensions { get; set; } = "|.chs|.cht";

    //元数据和转换媒体默认路径
    public Dictionary<string, string> DefaultPathMap { get; set; } = new() { { "默认文件夹", "Media" } };

    //文件夹名称模板
    public string CreateFolderNameTemplate { get; set; } = "{{SubjectNameCn}} ({{Year}})";

    //剧集文件名模板
    public string CreateBangumiFileNameTemplate { get; set; } = "{{SubjectNameCn}} - {{EpisodesSort}} - {{EpisodeNameCn}} - {{SourceFolderName}}";

    //电影文件名模板
    public string CreateMovieFileNameTemplate { get; set; } = "{{SourceFileName}}";

    //RSS下载文件夹模板
    public string RssFolderNameTemplate { get; set; } = "{{SubjectNameCn}}";

    //RSS规则名模板
    public string RssRuleNameTemplate { get; set; } = "{{SubjectNameCn}}";

    //qBittorrent网页地址
    public string QbtWebServerUrl { get; set; } = "http://127.0.0.1:8080/";

    //qBittorrent下载路径
    public string QbtDefaultDownloadPath { get; set; } = "G:\\Media";

    //Bangumi Api Assess Token
    public string BangumiAuthToken { get; set; } = string.Empty;

    //自定义特别季名称
    public string CustomSpName { get; set; } = "SP";

    //FFmpeg文件夹路径
    public string FFmpegPath { get; set; } = string.Empty;
}

public class AppConfig_DefaultPathMap : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}