namespace BangumiMediaTool.Models;

/// <summary>
/// Bangumi条目数据
/// </summary>
public class DataSubjectsInfo
{
    /// <summary>
    /// 条目ID
    /// </summary>
    public long Id { get; init; } = 0;

    /// <summary>
    /// 条目名（原文）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 条目名（中文）
    /// </summary>
    public string NameCn { get; init; } = string.Empty;

    /// <summary>
    /// 话数
    /// </summary>
    public long EpsCount { get; init; } = 0;

    /// <summary>
    /// 条目简介
    /// </summary>
    public string Desc { get; init; } = string.Empty;

    /// <summary>
    /// 放送时间
    /// </summary>
    public string AirDate { get; init; } = string.Empty;

    /// <summary>
    /// 分类
    /// </summary>
    public string Platform {get; init;} = string.Empty;

    /// <summary>
    /// 剧集封面图
    /// </summary>
    public string ImageUrl { get; init; } = string.Empty;

    private const double Tolerance = 1e-6;

    public override bool Equals(object? other)
    {
        if (other is not DataSubjectsInfo info) return false;
        return Id == info.Id
               && Name == info.Name
               && NameCn == info.NameCn
               && EpsCount == info.EpsCount
               && Desc == info.Desc
               && AirDate == info.AirDate
               && Platform == info.Platform
               && ImageUrl == info.ImageUrl;
    }

    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Id);
        hashCode.Add(Name);
        hashCode.Add(NameCn);
        hashCode.Add(EpsCount);
        hashCode.Add(Desc);
        hashCode.Add(AirDate);
        hashCode.Add(Platform);
        hashCode.Add(ImageUrl);
        return hashCode.ToHashCode();
    }
}
