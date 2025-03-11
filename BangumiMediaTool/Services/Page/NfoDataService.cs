using System.IO;
using System.Text;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Api;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Windows;
using CommunityToolkit.Mvvm.Messaging;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.Services.Page;

public static class NfoDataService
{
    /// <summary>
    /// 从文件名搜索元数据
    /// </summary>
    /// <param name="files">文件列表</param>
    public static async Task<List<DataEpisodesInfo>> SearchDataByFilesAsync(List<DataFilePath> files)
    {
        if (files.Count == 0) return [];

        string searchStr = files[0].FileName;
        List<DataEpisodesInfo> results = [];

        //快速搜索只按文件列表第一条进行搜索
        //解析标题
        var aniParse = AnitomySharp.AnitomySharp.Parse(files[0].FileName);
        foreach (var element in aniParse)
        {
            if (element.Category.ToString() == "ElementAnimeTitle" && !string.IsNullOrEmpty(element.Value))
            {
                searchStr = element.Value;
            }
        }

        //文件识别到英文名称时用Bgm搜索可能无结果，使用Tmdb提高匹配准确率
        var (searchStrOrigin, tmdbId) = await BangumiApiService.Instance.TmdbApi_Search(searchStr);
        var dataSubjectsInfos = await BangumiApiService.Instance.BangumiApi_Search(searchStrOrigin);

        if (dataSubjectsInfos.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new DataSnackbarMessage(
                "搜索无结果",
                $"搜索：{searchStr}",
                ControlAppearance.Caution));
            return [];
        }

        //默认获取第一条
        var dataEpisodesInfos = await BangumiApiService.Instance.BangumiApi_Episodes(dataSubjectsInfos[0]);
        if (dataSubjectsInfos.Count == 0)
        {
            WeakReferenceMessenger.Default.Send(new DataSnackbarMessage(
                "搜索无结果",
                $"搜索：{searchStr} ID: {dataSubjectsInfos[0].Id}",
                ControlAppearance.Caution));
            return [];
        }

        dataEpisodesInfos.ForEach(item =>
        {
            item.TmdbSubjectId = tmdbId;
            results.Add(item);
        });

        return results;
    }

    /// <summary>
    /// 创建新文件列表
    /// </summary>
    /// <param name="sourceFileList">源文件列表</param>
    /// <param name="infoList">元数据列表</param>
    /// <param name="searchMode">搜索模式 0:剧集 1:电影</param>
    /// <param name="fileOperateMode">文件操作模式 0:硬链接 1:复制 2:重命名 3:仅生成元数据</param>
    /// <param name="nfoExtraSettings">额外设置</param>
    /// <returns></returns>
    public static List<DataFilePath> CreateNewFileList(
        List<DataFilePath> sourceFileList, List<DataEpisodesInfo> infoList,
        int searchMode, int fileOperateMode, NfoExtraSettings nfoExtraSettings)
    {
        //集数补零
        var padLeft = Math.Min(sourceFileList.Count, infoList.Count).ToString().Length;
        var newFileList = new List<DataFilePath>();

        for (int i = 0; i < Math.Min(sourceFileList.Count, infoList.Count); i++)
        {
            var sourcePath = sourceFileList[i].FilePath;
            var sourceName = sourceFileList[i].FileName;
            var info = infoList[i];

            var rootPath = Path.GetPathRoot(sourcePath);
            if (string.IsNullOrEmpty(rootPath))
            {
                Logs.LogError($"获取根目录失败：{sourcePath}");
                continue;
            }

            //重命名使用原文件夹，其他使用配置中的文件夹
            var targetFolder = fileOperateMode switch
            {
                2 or 3 => Path.GetDirectoryName(sourcePath),
                _ => Path.Combine(rootPath, GlobalConfig.Instance.AppConfig.DefaultHardLinkPath)
            };
            if (string.IsNullOrEmpty(targetFolder))
            {
                Logs.LogError($"获取目标文件夹失败：{sourcePath}");
                continue;
            }

            var newPath = string.Empty;
            var newName = string.Empty;

            var seasonNum = 1 + nfoExtraSettings.SeasonOffset;
            if (seasonNum < 0) seasonNum = 0;

            switch (searchMode)
            {
                case 0:
                    newName = fileOperateMode switch
                    {
                        3 => sourceFileList[i].FileName,
                        _ => CreateFileService.BangumiNewFileName(info, sourceFileList[i], nfoExtraSettings, padLeft)
                    };
                    newPath = fileOperateMode switch
                    {
                        0 or 1 => Path.Combine(targetFolder, CreateFileService.NewFolderName(info), info.Type == 0 ? $"Season {seasonNum}" : "SP")
                            .RemoveInvalidPathNameChar(),
                        2 or 3 => targetFolder,
                        _ => newPath
                    };
                    break;
                case 1:
                    newName = fileOperateMode switch
                    {
                        3 => sourceFileList[i].FileName,
                        _ => CreateFileService.MovieNewFileName(info, sourceFileList[i], nfoExtraSettings)
                    };
                    newPath = fileOperateMode switch
                    {
                        0 or 1 => Path.Combine(
                            targetFolder,
                            CreateFileService.NewFolderName(info)).RemoveInvalidPathNameChar(),
                        2 or 3 => targetFolder,
                        _ => newPath
                    };
                    break;
            }

            newFileList.Add(new DataFilePath(Path.Combine(newPath, newName)));
        }

        return newFileList;
    }

    /// <summary>
    /// 文件操作
    /// </summary>
    /// <param name="sourceFileList">原文件路径</param>
    /// <param name="newFileList">目标路径</param>
    /// <param name="fileOperateMode">0:硬链接 1:复制 2:重命名 3:仅生成元数据</param>
    public static async Task<string> RunFileOperates(List<DataFilePath> sourceFileList, List<DataFilePath> newFileList, int fileOperateMode)
    {
        var main = App.GetService<MainWindowViewModel>();
        var count = Math.Min(sourceFileList.Count, newFileList.Count);
        var record = new StringBuilder();

        await Task.Run(() =>
        {
            for (int i = 0; i < count; i++)
            {
                if (!File.Exists(sourceFileList[i].FilePath)) continue;

                var targetPath = newFileList[i].FilePath;
                var targetDirectory = Path.GetDirectoryName(targetPath);
                if (targetDirectory == null) continue;

                if (!Directory.Exists(targetDirectory))
                {
                    try
                    {
                        Directory.CreateDirectory(targetDirectory);
                        Logs.LogInfo($"创建文件夹：{targetDirectory}");
                    }
                    catch (Exception e)
                    {
                        Logs.LogError(e.ToString());
                        continue;
                    }
                }

                try
                {
                    main?.SetGlobalProcess(true, i + 1, count);
                    switch (fileOperateMode)
                    {
                        case 0: //硬链接
                            ExtensionTools.CreateHardLink(newFileList[i].FilePath, sourceFileList[i].FilePath, IntPtr.Zero);
                            Logs.LogInfo($"硬链接：{newFileList[i].FilePath}");
                            record.AppendLine(newFileList[i].FilePath);
                            break;
                        case 1: //复制
                            File.Copy(sourceFileList[i].FilePath, newFileList[i].FilePath, true);
                            Logs.LogInfo($"复制：{newFileList[i].FilePath}");
                            record.AppendLine(newFileList[i].FilePath);
                            break;
                        case 2: //重命名
                            File.Move(sourceFileList[i].FilePath, newFileList[i].FilePath, true);
                            Logs.LogInfo($"重命名：{newFileList[i].FilePath}");
                            record.AppendLine(newFileList[i].FilePath);
                            break;
                        case 3: //仅生成元数据
                            record.AppendLine(newFileList[i].FilePath);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Logs.LogError(e.ToString());
                }
            }
        });

        return record.ToString();
    }

    /// <summary>
    /// 创建Nfo文件
    /// </summary>
    /// <param name="infoList">元数据列表</param>
    /// <param name="newFileList">新媒体文件路径</param>
    /// <param name="searchMode">搜索模式 0:剧集 1:电影</param>
    /// <param name="isAddTmdbId">添加TmdbId</param>
    /// <param name="nfoExtraSettings">额外设置</param>
    public static async Task RunCreateNfoFiles(List<DataEpisodesInfo> infoList, List<DataFilePath> newFileList,
        int searchMode, bool isAddTmdbId, NfoExtraSettings nfoExtraSettings)
    {
        var count = Math.Min(infoList.Count, newFileList.Count);

        await Task.Run(() =>
        {
            for (int i = 0; i < count; i++)
            {
                var info = infoList[i];
                var newMediaFile = newFileList[i];
                if (!File.Exists(newMediaFile.FilePath)) continue;

                var folder = Path.GetDirectoryName(newMediaFile.FilePath);
                if (searchMode == 0)
                {
                    var subjectFolder = Path.GetDirectoryName(folder);
                    if (!string.IsNullOrEmpty(subjectFolder) && !File.Exists(Path.Combine(subjectFolder, "tvshow.nfo")))
                    {
                        var tmdbId = info.TmdbSubjectId.ToString() ?? string.Empty;
                        var subjectNfoData = new NfoInfo_SubjectsRootTv
                        {
                            bangumiid = info.SubjectId.ToString(),
                            tmdbid = isAddTmdbId ? tmdbId : string.Empty,
                            title = info.SubjectNameCn,
                            originaltitle = info.SubjectName,
                            showtitle = info.SubjectNameCn,
                            year = info.Year
                        };
                        CreateFileService.CreateNfoFromData(subjectNfoData, Path.Combine(subjectFolder, "tvshow.nfo"));
                    }
                }

                var epFile = Path.GetFileNameWithoutExtension(newMediaFile.FileName) + ".nfo";
                if (string.IsNullOrEmpty(folder)) continue;
                var epNfoPath = Path.Combine(folder, epFile);

                var seasonNum = 1 + nfoExtraSettings.SeasonOffset;
                if (info.Type != 0) seasonNum = nfoExtraSettings.SeasonOffset;
                if (seasonNum < 0) seasonNum = 0;

                switch (searchMode)
                {
                    case 0:
                    {
                        var nfoData = new NfoInfo_EpisodesRoot
                        {
                            bangumiid = info.Id.ToString(),
                            title = info.NameCn,
                            originaltitle = info.Name,
                            showtitle = info.NameCn,
                            episode = (info.Sort + nfoExtraSettings.EpisodeOffset).ToString(),
                            season = seasonNum.ToString(),
                        };
                        CreateFileService.CreateNfoFromData(nfoData, epNfoPath);
                        break;
                    }
                    case 1:
                    {
                        var nfoData = new NfoInfo_SubjectsRootMovie
                        {
                            bangumiid = info.SubjectId.ToString(),
                            tmdbid = string.Empty,
                            title = info.SubjectNameCn,
                            originaltitle = info.SubjectName,
                            year = info.Year
                        };
                        CreateFileService.CreateNfoFromData(nfoData, epNfoPath);
                        break;
                    }
                }

                Logs.LogInfo($"创建元数据：{epNfoPath}");
            }
        });
    }
}