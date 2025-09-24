using System.Collections.ObjectModel;

namespace BangumiMediaTool.Models;

public class AppConfig : ObservableObject
{
    //匹配字幕文件扩展名
    public string RegexMatchSubtitleFiles { get; set; } = string.Empty;

    //排除字幕文件扩展名
    public string RegexRemoveSubtitleFiles { get; set; } = string.Empty;

    //匹配媒体文件扩展名
    public string RegexMatchMediaFiles { get; set; } = string.Empty;

    //默认添加字幕文件扩展名
    public string DefaultAddSubtitleFilesExtensions { get; set; } = string.Empty;

    //元数据和转换媒体默认路径
    public Dictionary<string, string> DefaultPathMap { get; set; } = new();

    //文件夹名称模板
    public string CreateFolderNameTemplate { get; set; } = string.Empty;

    //剧集文件名模板
    public string CreateBangumiFileNameTemplate { get; set; } = string.Empty;

    //电影文件名模板
    public string CreateMovieFileNameTemplate { get; set; } = string.Empty;

    //qBittorrent网页地址
    public string QbtWebServerUrl { get; set; } = string.Empty;

    //qBittorrent下载路径
    public string QbtDefaultDownloadPath { get; set; } = string.Empty;

    //Bangumi Api Assess Token
    public string BangumiAuthToken { get; set; } = string.Empty;

    //自定义特别季名称
    public string CustomSpName { get; set; } = string.Empty;

    //FFmpeg文件夹路径
    public string FFmpegPath { get; set; } = string.Empty;

    /*//assfonts.exe路径
    public string AssFontsExePath {get; set;} = string.Empty;*/
}

public class AppConfig_DefaultPathMap : ObservableObject
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}