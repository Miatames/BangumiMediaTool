using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Pages;
using CommunityToolkit.Mvvm.Messaging;
using NaturalSort.Extension;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.Services.Page;

public static class DragDropService
{
    /// <summary>
    /// 获取拖入的文件
    /// </summary>
    /// <param name="fileList">拖入文件列表</param>
    /// <returns></returns>
    public static (List<DataFilePath> mediaFileList, List<DataFilePath> subFileList) GetDropFileList(this StringCollection fileList)
    {
        var config = GlobalConfig.Instance.AppConfig;
        var regexMedia = config.RegexMatchMediaFiles;
        var regexSub = config.RegexMatchSubtitleFiles;
        var regexIgnoreFolder = config.RegexIgnoreFolderName;

        var filePathList = new List<string>();
        foreach (var file in fileList)
        {
            if (File.Exists(file))
            {
                filePathList.Add(file);
            }
            else if (Directory.Exists(file))
            {
                filePathList.AddRange(Directory.GetFiles(file));
                foreach (var directory in Directory.GetDirectories(file))
                {
                    if (!Regex.IsMatch(directory, regexIgnoreFolder))
                    {
                        filePathList.AddRange(Directory.GetFiles(directory));
                    }
                }
            }
            else
            {
                Logs.LogError($"未知文件: {file}");
            }
        }

        filePathList.Sort(StringComparer.OrdinalIgnoreCase.WithNaturalSort());

        if (string.IsNullOrEmpty(regexMedia) || string.IsNullOrEmpty(regexSub))
        {
            Logs.LogError("未知的文件匹配规则");
            return ([], []);
        }

        List<DataFilePath> mediaFileList = [];
        List<DataFilePath> subFileList = [];

        foreach (var filePath in filePathList)
        {
            var addFilePath = new DataFilePath(filePath);
            if (Regex.IsMatch(Path.GetExtension(filePath), regexMedia))
            {
                mediaFileList.AddUnique(addFilePath);
            }
            else if (Regex.IsMatch(Path.GetExtension(filePath), regexSub))
            {
                subFileList.AddUnique(addFilePath);
            }
            else
            {
                Logs.LogInfo($"未知文件类型: {filePath}");
            }
        }

        return (mediaFileList, subFileList);
    }

    /// <summary>
    /// 列表拖动事件
    /// </summary>
    public static void DragDropListItem<T>(this ObservableCollection<T> list, T sourceItem, T targetItem)
    {
        var sourceIndex = list.IndexOf(sourceItem);
        list.RemoveAt(sourceIndex);
        var targetIndex = list.IndexOf(targetItem);
        if (targetIndex < 0 || targetIndex >= list.Count) return;

        if (sourceIndex <= targetIndex)
        {
            list.Insert(targetIndex + 1, sourceItem);
        }
        else
        {
            list.Insert(targetIndex, sourceItem);
        }
    }
}