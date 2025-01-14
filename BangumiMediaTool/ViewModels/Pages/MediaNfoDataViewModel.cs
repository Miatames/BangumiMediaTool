﻿using System.Collections;
using System.Collections.ObjectModel;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Page;
using BangumiMediaTool.Services.Program;
using BangumiMediaTool.ViewModels.Windows;
using BangumiMediaTool.Views.Windows;
using GongSolutions.Wpf.DragDrop;
using NaturalSort.Extension;
using Wpf.Ui.Controls;

namespace BangumiMediaTool.ViewModels.Pages;

public partial class MediaNfoDataViewModel : ObservableObject, INavigationAware, IDropTarget
{
    [ObservableProperty] private ObservableCollection<DataFilePath> _sourceFileList = [];
    [ObservableProperty] private ObservableCollection<DataEpisodesInfo> _nfoDataList = [];

    [ObservableProperty] private bool _isAddNfoFile = true;
    [ObservableProperty] private bool _isAddTmdbId = true;
    [ObservableProperty] private int _currentSearchMode = 0;
    [ObservableProperty] private int _currentFileOperateMode = 0;

    public void OnNavigatedTo() { }
    public void OnNavigatedFrom() { }

    private List<DataFilePath> sourceFileSelected = [];
    private List<DataEpisodesInfo> nfoDataSelected = [];

    #region DragDrop

    public void DragOver(IDropInfo dropInfo)
    {
        switch (dropInfo)
        {
            case { Data: DataFilePath, TargetItem: DataFilePath }:
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.All;
                break;
            }
            case { Data: DataEpisodesInfo, TargetItem: DataEpisodesInfo }:
            {
                dropInfo.DropTargetAdorner = DropTargetAdorners.Highlight;
                dropInfo.Effects = DragDropEffects.All;
                break;
            }
            case { Data: DataObject dataObject } when dataObject.GetDataPresent(DataFormats.FileDrop):
            {
                dropInfo.DropTargetAdorner = null;
                dropInfo.Effects = DragDropEffects.Copy;
                break;
            }
        }
    }

    public void Drop(IDropInfo dropInfo)
    {
        switch (dropInfo)
        {
            case { Data: DataFilePath sourceItem, TargetItem: DataFilePath targetItem }
                when SourceFileList.Contains(sourceItem)
                     && SourceFileList.Contains(targetItem)
                     && !sourceItem.Equals(targetItem):
            {
                SourceFileList.DragDropListItem(sourceItem, targetItem);
                break;
            }
            case { Data: DataEpisodesInfo sourceItem, TargetItem: DataEpisodesInfo targetItem }
                when NfoDataList.Contains(sourceItem)
                     && NfoDataList.Contains(targetItem)
                     && !sourceItem.Equals(targetItem):
            {
                NfoDataList.DragDropListItem(sourceItem, targetItem);
                break;
            }
            case { Data: DataObject dataObject } when dataObject.ContainsFileDropList():
            {
                var files = dataObject.GetFileDropList();
                var (mediaFiles, subFiles) = files.GetDropFileList();
                mediaFiles.ForEach(item => SourceFileList.AddUnique(item));
                App.GetService<ReNameFileViewModel>()?.OnDropSubFiles(subFiles);
                break;
            }
        }
    }

    #endregion

    /// <summary>
    /// 添加到Nfo数据列表
    /// </summary>
    /// <param name="datas"></param>
    public void AddToNfoData(List<DataEpisodesInfo> datas)
    {
        datas.ForEach(item => NfoDataList.AddUnique(item));
    }

    [RelayCommand]
    private async Task OnQuickSearch()
    {
        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);

        var results = await NfoDataService.SearchDataByFilesAsync(SourceFileList.ToList());
        results.ForEach(item => NfoDataList.Add(item));

        main?.SetGlobalProcess(false);
    }

    [RelayCommand]
    private void OnNavigateToPreviewWindow()
    {
        var list = NfoDataService.CreateNewFileList(SourceFileList.ToList(), NfoDataList.ToList(), CurrentSearchMode, CurrentFileOperateMode);
        var window = new FilePreviewWindow(new FilePreviewWindowViewModel(list))
        {
            Owner = App.GetService<MainWindow>(),
            ShowInTaskbar = false
        };
        window.ShowDialog();
    }

    [RelayCommand]
    private void OnClearAll()
    {
        SourceFileList.Clear();
        NfoDataList.Clear();
    }

    [RelayCommand]
    private async Task OnRunFileOperate()
    {
        var main = App.GetService<MainWindowViewModel>();
        main?.SetGlobalProcess(true);
        var newFileList = NfoDataService.CreateNewFileList(SourceFileList.ToList(), NfoDataList.ToList(), CurrentSearchMode, CurrentFileOperateMode);
        await NfoDataService.RunFileOperates(SourceFileList.ToList(), newFileList, CurrentFileOperateMode);
        if (IsAddNfoFile) await NfoDataService.RunCreateNfoFiles(NfoDataList.ToList(), newFileList, CurrentSearchMode,IsAddTmdbId);

        main?.SetGlobalProcess(false);

        //完成后自动添加到重命名文件页面
        App.GetService<ReNameFileViewModel>()?.AddSourceFiles(newFileList);
    }

    [RelayCommand]
    private void OnSourceFilesSelectedItemChanged(object sender)
    {
        if (sender is not IList list) return;
        sourceFileSelected = list.Cast<DataFilePath>().ToList();
    }

    [RelayCommand]
    private void OnNfoDataSelectedItemChange(object sender)
    {
        if (sender is not IList list) return;
        nfoDataSelected = list.Cast<DataEpisodesInfo>().ToList();
    }

    [RelayCommand]
    private void OnClearSourceFileList()
    {
        SourceFileList.Clear();
    }

    [RelayCommand]
    private void OnClearNfoDataList()
    {
        NfoDataList.Clear();
    }

    [RelayCommand]
    private void OnDelSourceFileItem()
    {
        for (var i = sourceFileSelected.Count - 1; i >= 0; i--)
        {
            SourceFileList.Remove(sourceFileSelected[i]);
        }
    }

    [RelayCommand]
    private void OnDelNfoDataItem()
    {
        for (var i = nfoDataSelected.Count - 1; i >= 0; i--)
        {
            NfoDataList.Remove(nfoDataSelected[i]);
        }
    }

    [RelayCommand]
    private void OnSortSourceFileList()
    {
        var list = SourceFileList.ToList()
            .OrderBy(path => path.FilePath, StringComparer.OrdinalIgnoreCase.WithNaturalSort());
        SourceFileList = new ObservableCollection<DataFilePath>(list);
    }
}
