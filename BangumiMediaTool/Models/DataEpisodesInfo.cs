namespace BangumiMediaTool.Models;

/// <summary>
/// Bangumi章节数据
/// </summary>
public class DataEpisodesInfo
{
    /// <summary>
    /// 章节ID
    /// </summary>
    public long Id { get; init; } = 0;

    /// <summary>
    /// 章节名（原文）
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// 章节名（中文）
    /// </summary>
    public string NameCn { get; init; } = string.Empty;

    /// <summary>
    /// 所属条目名（原文）
    /// </summary>
    public string SubjectName { get; init; } = string.Empty;

    /// <summary>
    /// 所属条目名（中文）
    /// </summary>
    public string SubjectNameCn { get; init; } = string.Empty;

    /// <summary>
    /// 序号（特别篇固定为0）
    /// </summary>
    public float Ep { get; init; } = 0;

    /// <summary>
    /// 序号
    /// </summary>
    public float Sort { get; init; } = 0;

    /// <summary>
    /// 所属条目ID
    /// </summary>
    public long SubjectId { get; init; } = 0;

    /// <summary>
    /// TMDB条目Id
    /// </summary>
    public long? TmdbSubjectId { get; set; }

    /// <summary>
    /// 类型 本篇：0 SP：1 其他：2
    /// </summary>
    public int Type { get; init; } = 0;

    /// <summary>
    /// 年份
    /// </summary>
    public string Year { get; init; } = string.Empty;

    public string ShowText { get; private set; } = string.Empty;

    public void BuildShowText()
    {
        ShowText = Type switch
        {
            0 => $"{SubjectNameCn}  本篇:{Sort}  {NameCn} ({Name})",
            1 => $"{SubjectNameCn}  SP:{Sort}  {NameCn} ({Name})",
            _ => $"{SubjectNameCn}  其他:{Ep}-{Sort}  {NameCn} ({Name})"
        };
    }

    public override bool Equals(object? obj)
    {
        if (obj is not DataEpisodesInfo other) return false;

        return Id == other.Id
               && Name == other.Name
               && NameCn == other.NameCn
               && SubjectName == other.SubjectName
               && SubjectNameCn == other.SubjectNameCn
               && Math.Abs(Ep - other.Ep) < 1e-6
               && Math.Abs(Sort - other.Sort) < 1e-6
               && SubjectId == other.SubjectId
               && TmdbSubjectId == other.TmdbSubjectId
               && Type == other.Type
               && Year == other.Year;
    }


    public override int GetHashCode()
    {
        var hashCode = new HashCode();
        hashCode.Add(Id);
        hashCode.Add(Name);
        hashCode.Add(NameCn);
        hashCode.Add(SubjectName);
        hashCode.Add(SubjectNameCn);
        hashCode.Add(Ep);
        hashCode.Add(Sort);
        hashCode.Add(SubjectId);
        hashCode.Add(Type);
        hashCode.Add(Year);
        return hashCode.ToHashCode();
    }
}
