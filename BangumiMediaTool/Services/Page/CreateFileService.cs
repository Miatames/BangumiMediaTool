using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Windows;
using Fluid;
using Microsoft.WindowsAPICodePack.Shell;

namespace BangumiMediaTool.Services.Page;

public static class CreateFileService
{
    /// <summary>
    /// 新文件夹名称
    /// </summary>
    /// <param name="info">元数据</param>
    /// <returns></returns>
    public static string NewFolderName(DataEpisodesInfo info)
    {
        var folderName = "文件夹";
        var templateFolderName = GlobalConfig.Instance.AppConfig.CreateFolderNameTemplate;

        var data = new
        {
            SubjectId = info.SubjectId,
            SubjectName = info.SubjectName,
            SubjectNameCn = info.SubjectNameCn,
            Year = info.Year
        };

        var fileNameParser = new FluidParser();
        if (fileNameParser.TryParse(templateFolderName, out var template))
        {
            var context = new TemplateContext(data);

            folderName = template.Render(context);
        }

        return folderName.RemoveInvalidFileNameChar();
    }

    /// <summary>
    /// 新剧集文件名称
    /// </summary>
    /// <param name="info">元数据</param>
    /// <param name="sourceFileName">源文件名</param>
    /// <param name="nfoExtraSettings">额外设置</param>
    /// <param name="padLeft">剧集编号左侧填0的数量</param>
    /// <returns></returns>
    public static string BangumiNewFileName(DataEpisodesInfo info, DataFilePath sourceFileName, NfoExtraSettings nfoExtraSettings, int padLeft)
    {
        var fileName = sourceFileName.FileName;
        var extensionName = Path.GetExtension(fileName);
        var templateFileName = GlobalConfig.Instance.AppConfig.CreateBangumiFileNameTemplate;
        if (padLeft < 2) padLeft = 2;

        var seasonNum = 1 + nfoExtraSettings.SeasonOffset;
        if (info.Type != 0) seasonNum = nfoExtraSettings.SeasonOffset;
        if (seasonNum < 0) seasonNum = 0;
        var seasonNumStr = seasonNum.ToString();
        if (seasonNum < 10) seasonNumStr = "0" + seasonNumStr;

        var data = new
        {
            SubjectId = info.SubjectId,
            SubjectName = info.SubjectName,
            SubjectNameCn = info.SubjectNameCn,
            EpisodeId = info.Id,
            EpisodeName = info.Name,
            EpisodeNameCn = string.IsNullOrEmpty(info.NameCn) ? info.Name : info.NameCn,
            EpisodesSort = $"S{seasonNumStr}E" + (info.Sort + nfoExtraSettings.EpisodeOffset).ToString().PadLeft(padLeft, '0'),
            Year = info.Year,
            SourceFileName = Path.GetFileNameWithoutExtension(sourceFileName.FileName),
            SourceFolderName = Path.GetFileName(Path.GetDirectoryName(sourceFileName.FilePath)),
            SpecialText = nfoExtraSettings.SpecialText,
        };

        var fileNameParser = new FluidParser();
        if (fileNameParser.TryParse(templateFileName, out var template))
        {
            var context = new TemplateContext(data);

            fileName = template.Render(context) + extensionName;
        }

        return fileName.RemoveInvalidFileNameChar();
    }

    /// <summary>
    /// 新电影文件名称
    /// </summary>
    /// <param name="info">元数据</param>
    /// <param name="sourceFileName">源文件名</param>
    /// <param name="nfoExtraSettings">额外设置</param>
    /// <returns></returns>
    public static string MovieNewFileName(DataEpisodesInfo info, DataFilePath sourceFileName, NfoExtraSettings nfoExtraSettings)
    {
        var fileName = sourceFileName.FileName;
        var extensionName = Path.GetExtension(fileName);
        var templateFileName = GlobalConfig.Instance.AppConfig.CreateMovieFileNameTemplate;

        var data = new
        {
            SubjectId = info.SubjectId,
            SubjectName = info.SubjectName,
            SubjectNameCn = info.SubjectNameCn,
            EpisodeId = info.Id,
            EpisodeName = info.Name,
            EpisodeNameCn = string.IsNullOrEmpty(info.NameCn) ? info.Name : info.NameCn,
            Year = info.Year,
            SourceFileName = Path.GetFileNameWithoutExtension(sourceFileName.FileName),
            SourceFolderName = Path.GetFileName(Path.GetDirectoryName(sourceFileName.FilePath)),
            SpecialText = nfoExtraSettings.SpecialText,
        };

        var fileNameParser = new FluidParser();
        if (fileNameParser.TryParse(templateFileName, out var template))
        {
            var context = new TemplateContext(data);

            fileName = template.Render(context) + extensionName;
        }

        return fileName.RemoveInvalidFileNameChar();
    }

    /// <summary>
    /// 创建Nfo文件
    /// </summary>
    /// <param name="info">数据</param>
    /// <param name="filePath">文件路径</param>
    public static void CreateNfoFromData<T>(T info, string filePath)
    {
        try
        {
            var serializer = new XmlSerializer(typeof(T));
            using var writer = new XmlTextWriter(filePath, Encoding.UTF8);
            writer.Formatting = Formatting.Indented;
            var namespaces = new XmlSerializerNamespaces([new XmlQualifiedName(string.Empty, string.Empty)]);
            serializer.Serialize(writer, info, namespaces);
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
        }
    }

    /// <summary>
    /// 生成视频预览图
    /// </summary>
    /// <param name="sourceFileList">源文件路径</param>
    /// <param name="newFileList">目标文件路径</param>
    public static async Task RunCreateThumbFiles(List<DataFilePath> sourceFileList, List<DataFilePath> newFileList)
    {
        var main = App.GetService<MainWindowViewModel>();
        var count = Math.Min(sourceFileList.Count, newFileList.Count);

        await Task.Run(() =>
        {
            for (int i = 0; i < count; i++)
            {
                main?.SetGlobalProcess(true, i + 1, count);

                var sourceMediaFile = sourceFileList[i].FilePath;
                var newThumbFile = Path.Combine(
                    Path.GetDirectoryName(newFileList[i].FilePath) ?? string.Empty,
                    Path.GetFileNameWithoutExtension(newFileList[i].FileName) + "-thumb.png");

                try
                {
                    var shellFile = ShellFile.FromFilePath(sourceMediaFile);
                    var thumbData = shellFile.Thumbnail.ExtraLargeBitmap;
                    thumbData?.Save(newThumbFile, ImageFormat.Png);
                }
                catch (Exception e)
                {
                    Logs.LogError(e.ToString());
                }
            }
        });
    }
}