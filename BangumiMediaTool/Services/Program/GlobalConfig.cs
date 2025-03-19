using System.IO;
using System.Text.Json;
using BangumiMediaTool.Models;
using FFMpegCore;

namespace BangumiMediaTool.Services.Program;

public class GlobalConfig
{
    public static GlobalConfig Instance { get; private set; } = null!;
    public AppConfig AppConfig { get; set; } = new AppConfig();

    public GlobalConfig()
    {
        Instance = this;
        ReadConfig();

        Logs.LogInfo("GlobalConfig Initialize");
    }

    public void ReadConfig()
    {
        if (File.Exists("config.json"))
        {
            Logs.LogInfo("读取配置");

            var jsonString = File.ReadAllText("config.json");
            AppConfig = JsonSerializer.Deserialize<AppConfig>(jsonString) ?? new AppConfig();
            Logs.LogInfo(jsonString);
            GlobalFFOptions.Configure(new FFOptions() { BinaryFolder = AppConfig.FFmpegPath });
        }
        else
        {
            Logs.LogInfo("未找到配置文件，生成默认配置");

            var config = new AppConfig
            {
                RegexMatchSubtitleFiles = ".ass|.srt",
                RegexRemoveSubtitleFiles = @"\[|\]|\(|\)|[\u4e00-\u9fa5]",
                RegexMatchMediaFiles = ".mp4|.mkv|.flv",
                DefaultAddSubtitleFilesExtensions = "|.chs|.cht",
                DefaultHardLinkPath = "Media",
                CreateFolderNameTemplate = "{{SubjectNameCn}} ({{Year}})",
                CreateBangumiFileNameTemplate = "{{SubjectNameCn}} - {{EpisodesSort}} - {{EpisodeNameCn}} - {{SourceFileName}}",
                CreateMovieFileNameTemplate = "{{SourceFileName}}",
                QbtWebServerUrl = "http://127.0.0.1:8080/",
                BangumiAuthToken = "",
                CustomSpName = "SP",
                QbtDefaultDownloadPath = "G:\\Media",
                FFmpegPath = ""
            };
            WriteConfig(config);
        }
    }

    private readonly JsonSerializerOptions defaultWriteOptions = new() { WriteIndented = true };

    public void WriteConfig(AppConfig setConfig)
    {
        AppConfig = setConfig;
        var jsonString = JsonSerializer.Serialize(AppConfig, defaultWriteOptions);
        File.WriteAllText("config.json", jsonString);
        GlobalFFOptions.Configure(new FFOptions() { BinaryFolder = AppConfig.FFmpegPath });
    }
}