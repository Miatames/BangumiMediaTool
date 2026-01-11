using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Windows;
using FFMpegCore;
using FFMpegCore.Pipes;

namespace BangumiMediaTool.Services.Page;

public static partial class ReNameFileService
{
    /// <summary>
    /// 获取匹配的字幕文件
    /// </summary>
    /// <param name="files"> 源文件列表</param>
    /// <param name="currentIndex">当前选中的扩展名序号</param>
    /// <param name="extensions">扩展名列表</param>
    /// <returns></returns>
    public static List<DataFilePath> GetMatchSubtitleFiles(this List<DataFilePath> files, int currentIndex, List<string> extensions)
    {
        List<DataFilePath> matchedFiles = [];

        if (currentIndex < 0 || currentIndex >= files.Count) return matchedFiles;
        var currentExtension = extensions[currentIndex];
        foreach (var file in files)
        {
            if (!file.FileName.EndsWith(currentExtension)) continue;
            bool isAdd = true;

            foreach (var extension in extensions
                         .Where(extension => currentExtension != extension
                                             && file.FileName.EndsWith(extension)
                                             && extension.Contains(currentExtension)))
            {
                isAdd = false;
            }

            if (isAdd) matchedFiles.Add(file);
        }

        return matchedFiles;
    }

    /// <summary>
    /// 创建新文件列表
    /// </summary>
    /// <param name="mediaFiles">媒体文件</param>
    /// <param name="subtitleFiles">字幕文件</param>
    /// <param name="selectExtension">筛选的扩展名,不筛选时为空</param>
    /// <param name="addExtension">添加的扩展名</param>
    /// <param name="fileOperateMode">文件操作模式 0:复制 1:重命名 2:转换为SRT 3:ASS子集化</param>
    /// <returns></returns>
    public static List<DataFilePath> CreateNewFilePaths(List<DataFilePath> mediaFiles, List<DataFilePath> subtitleFiles,
        string selectExtension, string addExtension, int fileOperateMode)
    {
        var newFiles = new List<DataFilePath>();

        if (mediaFiles.Count == 0 && subtitleFiles.Count > 0 && fileOperateMode == 2)
        {
            foreach (var file in subtitleFiles)
            {
                var folder = Path.GetDirectoryName(file.FilePath);
                if (string.IsNullOrEmpty(folder)) continue;

                if (string.IsNullOrEmpty(selectExtension))
                {
                    selectExtension = file.FileName.GetExtensionName(GlobalConfig.Instance.AppConfig.RegexRemoveSubtitleFiles);
                }

                var fileNameWithoutExtension = file.FilePath.Replace(selectExtension, string.Empty);
                newFiles.Add(new DataFilePath(fileNameWithoutExtension + addExtension + ".srt"));
            }

            return newFiles;
        }


        for (int i = 0; i < Math.Min(mediaFiles.Count, subtitleFiles.Count); i++)
        {
            var mediaFile = mediaFiles[i];
            var subtitleFile = subtitleFiles[i];
            switch (fileOperateMode)
            {
                case 0:
                case 3:
                    var folder1 = Path.GetDirectoryName(mediaFile.FilePath) ?? string.Empty;
                    var subExtension1 = Path.GetExtension(subtitleFile.FileName);
                    var fileName1 = Path.GetFileNameWithoutExtension(mediaFile.FileName) + addExtension + subExtension1;
                    newFiles.Add(new DataFilePath(Path.Combine(folder1, fileName1)));
                    break;
                case 1:
                    var folder2 = Path.GetDirectoryName(subtitleFile.FilePath) ?? string.Empty;
                    var subExtension2 = Path.GetExtension(subtitleFile.FileName);
                    var fileName2 = Path.GetFileNameWithoutExtension(mediaFile.FileName) + addExtension + subExtension2;
                    newFiles.Add(new DataFilePath(Path.Combine(folder2, fileName2)));
                    break;
            }
        }

        return newFiles;
    }

    /// <summary>
    /// 文件操作
    /// </summary>
    /// <param name="subtitleFiles">字幕文件路径</param>
    /// <param name="targetPaths">目标路径</param>
    /// <param name="fileOperateMode">文件操作模式 0:复制 1:重命名 2:转换为SRT 3:字体子集化</param>
    public static async Task<string> RunFileOperates(List<DataFilePath> subtitleFiles, List<DataFilePath> targetPaths, int fileOperateMode)
    {
        var main = App.GetService<MainWindowViewModel>();
        var count = Math.Min(subtitleFiles.Count, targetPaths.Count);
        var recordStr = new StringBuilder();

        if (fileOperateMode == 2)
        {
            if (!File.Exists(GlobalFFOptions.GetFFMpegBinaryPath()))
            {
                Logs.LogError($"未找到FFMpeg执行程序");
                return string.Empty;
            }

            for (int i = 0; i < count; i++)
            {
                main?.SetGlobalProcess(true, i + 1, count, "转换为srt");

                await ConvertAssFileToSrt(subtitleFiles[i].FilePath, targetPaths[i].FilePath);
                recordStr.AppendLine(targetPaths[i].FilePath);
                Logs.LogInfo($"转换为srt：{subtitleFiles[i].FilePath} >> {targetPaths[i].FilePath}");
            }

            return recordStr.ToString();
        }

        /*if (fileOperateMode == 3)
        {
            if (!File.Exists(GlobalConfig.Instance.AppConfig.AssFontsExePath))
            {
                Logs.LogError($"未找到assfonts.exe执行程序");
                return string.Empty;
            }

            for (int i = 0; i < count; i++)
            {
                main?.SetGlobalProcess(true, i + 1, count);

                await RunAssFonts(subtitleFiles[i], targetPaths[i]);
                recordStr.AppendLine(targetPaths[i].FilePath);
                Logs.LogInfo($"ass字体子集化：{subtitleFiles[i].FilePath} >> {targetPaths[i].FilePath}");
            }

            return recordStr.ToString();
        }*/

        await Task.Run(() =>
        {
            for (int i = 0; i < count; i++)
            {
                switch (fileOperateMode)
                {
                    case 0:
                        main?.SetGlobalProcess(true, i + 1, count, "复制");
                        File.Copy(subtitleFiles[i].FilePath, targetPaths[i].FilePath, true);
                        recordStr.AppendLine(targetPaths[i].FilePath);
                        Logs.LogInfo($"复制：{subtitleFiles[i].FilePath} >> {targetPaths[i].FilePath}");
                        break;
                    case 1:
                        main?.SetGlobalProcess(true, i + 1, count, "重命名");
                        File.Move(subtitleFiles[i].FilePath, targetPaths[i].FilePath, true);
                        recordStr.AppendLine(targetPaths[i].FilePath);
                        Logs.LogInfo($"重命名：{subtitleFiles[i].FilePath} >> {targetPaths[i].FilePath}");
                        break;
                }
            }
        });
        return recordStr.ToString();
    }

    /// <summary>
    /// 转换Ass到Srt （非Ass直接转换，Ass先去除特效后转换）
    /// </summary>
    /// <param name="assFilePath">Ass路径</param>
    /// <param name="srtFilePath">输出Srt路径</param>
    public static async Task ConvertAssFileToSrt(string assFilePath, string srtFilePath)
    {
        if (!File.Exists(assFilePath))
        {
            Logs.LogError($"字幕文件不存在: {assFilePath}");
            return;
        }

        //扩展名非ass时直接转换
        var ext = Path.GetExtension(assFilePath);
        if (ext != ".ass")
        {
            await FFMpegArguments.FromFileInput(assFilePath).OutputToFile(srtFilePath).ProcessAsynchronously();
            return;
        }

        var tempFilePath = assFilePath + ".temp";
        if (!File.Exists(tempFilePath))
        {
            File.Create(tempFilePath).Close();
        }

        //读取Ass文件,移除特效标签后保存到临时文件
        await using (var tempFile = new FileStream(tempFilePath, FileMode.Truncate, FileAccess.ReadWrite, FileShare.None))
        {
            var writer = new StreamWriter(tempFile);

            await Task.Run(() =>
            {
                using (var reader = new StreamReader(assFilePath))
                {
                    while (reader.ReadLine() is { } line)
                    {
                        if (AssRemoveLineRegex().IsMatch(line)) continue;

                        var newLine = AssEffectsRegex().Replace(line, string.Empty);
                        if (AssKaraokeRegex().IsMatch(newLine))
                        {
                            newLine = newLine.Replace("Comment: ", "Dialogue: ");
                        }

                        writer.WriteLine(newLine);
                    }

                    reader.Close();
                }
            });

            await writer.FlushAsync();
            writer.Close();
            tempFile.Close();
        }

        await FFMpegArguments.FromFileInput(tempFilePath).OutputToFile(srtFilePath).ProcessAsynchronously();

        if (File.Exists(tempFilePath))
        {
            File.Delete(tempFilePath);
        }
    }

    /*/// <summary>
    /// 使用assfonts进行子集化
    /// </summary>
    /// <param name="subtitleFile">原文件</param>
    /// <param name="targetPath">目标路径</param>
    public static async Task RunAssFonts(DataFilePath subtitleFile, DataFilePath targetPath)
    {
        var extension = Path.GetExtension(subtitleFile.FileName);
        if (extension != ".ass")
        {
            Logs.LogInfo($"{subtitleFile.FileName} 非ass文件，跳过转换");
            return;
        }

        var wordDir = Path.GetDirectoryName(subtitleFile.FilePath);
        if (string.IsNullOrEmpty(wordDir))
        {
            Logs.LogInfo($"{subtitleFile.FilePath} 目录错误，跳过转换");
            return;
        }

        using (var process = new Process())
        {
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.RedirectStandardInput = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardError = true;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.WorkingDirectory = wordDir;
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.StandardInput.AutoFlush = true;
            process.OutputDataReceived += (o, e) => { Logs.LogInfo(e.Data); };
            process.ErrorDataReceived += (o, e) => { Logs.LogInfo(e.Data); };
            var command = $"""{GlobalConfig.Instance.AppConfig.AssFontsExePath} -i "{subtitleFile.FilePath}"&exit""";
            Logs.LogInfo(command);
            await process.StandardInput.WriteLineAsync(command);
            await process.WaitForExitAsync();
            process.Close();
        }

        var newFilePath = subtitleFile.FilePath.Replace(".ass", ".assfonts.ass");
        if (!File.Exists(newFilePath))
        {
            Logs.LogInfo($"{newFilePath} 未找到转换后文件");
            return;
        }

        var tempDir = Path.Combine(wordDir, Path.GetFileNameWithoutExtension(subtitleFile.FileName) + "_subsetted");
        if (Directory.Exists(tempDir))
        {
            Logs.LogInfo($"{tempDir} 删除临时文件夹");
            Directory.Delete(tempDir, true);
        }

        await Task.Run(() =>
        {
            File.Move(newFilePath,targetPath.FilePath);
        });
    }*/

    #region Regex

    //匹配特效标签
    [GeneratedRegex("{[^}]+}")]
    private static partial Regex AssEffectsRegex();

    //匹配非必要行
    [GeneratedRegex("(Style:)|(,fx,{)")]
    private static partial Regex AssRemoveLineRegex();

    //匹配特效歌词
    [GeneratedRegex("Comment: .*,karaoke,.*")]
    private static partial Regex AssKaraokeRegex();

    #endregion
}