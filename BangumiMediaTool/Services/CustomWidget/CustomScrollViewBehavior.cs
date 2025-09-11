using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.Xaml.Behaviors;

namespace BangumiMediaTool.Services.CustomWidget;

/// <summary>
/// 自定义ListView，刷新内容时自动滚动到顶端，自定义滚轮滚动距离
/// </summary>
public class CustomScrollViewBehavior : Behavior<ListView>
{
    // 滚动距离（默认 50 像素）
    public double ScrollAmount
    {
        get => (double)GetValue(ScrollAmountProperty);
        set => SetValue(ScrollAmountProperty, value);
    }

    public static readonly DependencyProperty ScrollAmountProperty =
        DependencyProperty.Register(
            nameof(ScrollAmount),
            typeof(double),
            typeof(CustomScrollViewBehavior),
            new PropertyMetadata(0.0));

    private DependencyPropertyDescriptor? _itemsSourceDescriptor;

    protected override void OnAttached()
    {
        base.OnAttached();

        // 监听 ItemsSource 属性变化
        _itemsSourceDescriptor = DependencyPropertyDescriptor.FromProperty(
            ItemsControl.ItemsSourceProperty, typeof(ItemsControl));
        _itemsSourceDescriptor?.AddValueChanged(AssociatedObject, OnItemsSourceChanged);

        // 监听滚轮事件
        AssociatedObject.PreviewMouseWheel += OnPreviewMouseWheel;
    }

    protected override void OnDetaching()
    {
        if (_itemsSourceDescriptor is not null)
        {
            _itemsSourceDescriptor.RemoveValueChanged(AssociatedObject, OnItemsSourceChanged);
            _itemsSourceDescriptor = null;
        }

        AssociatedObject.PreviewMouseWheel -= OnPreviewMouseWheel;
        base.OnDetaching();
    }

    private void OnItemsSourceChanged(object? sender, EventArgs e)
    {
        // 每次 ItemsSource 被替换时，滚到顶部
        TryScrollToTopAsync();
    }

    private void TryScrollToTopAsync()
    {
        AssociatedObject?.Dispatcher?.BeginInvoke((Action)(() =>
        {
            if (AssociatedObject?.Items.Count > 0)
            {
                var first = AssociatedObject.Items[0];
                if (first is not null)
                {
                    AssociatedObject.ScrollIntoView(first);
                }
            }
        }), DispatcherPriority.Loaded);
    }

    private void OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (ScrollAmount != 0 && FindScrollViewer(AssociatedObject) is ScrollViewer sv)
        {
            if (e.Delta < 0)
                sv.ScrollToVerticalOffset(sv.VerticalOffset + ScrollAmount);
            else
                sv.ScrollToVerticalOffset(sv.VerticalOffset - ScrollAmount);

            e.Handled = true;
        }
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject? d)
    {
        if (d is ScrollViewer sv) return sv;

        if (d == null) return null;

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
        {
            var child = VisualTreeHelper.GetChild(d, i);
            var result = FindScrollViewer(child);
            if (result is not null) return result;
        }

        return null;
    }
}