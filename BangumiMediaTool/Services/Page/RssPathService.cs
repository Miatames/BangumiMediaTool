using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using BangumiMediaTool.Models;
using Fluid;

namespace BangumiMediaTool.Services.Program;

public static partial class RssPathService
{
    public static string AnalyzeRssPath(string url)
    {
        var paramDict = GetUrlParams(url);

        if (url.Contains("nyaa.si/?page=rss") && paramDict.TryGetValue("q", out var nyaa_value))
        {
            return HttpUtility.UrlDecode(nyaa_value, Encoding.UTF8);
        }
        else if (url.Contains("share.dmhy.org/topics/rss/rss.xml") && paramDict.TryGetValue("keyword", out var dmhy_value))
        {
            return HttpUtility.UrlDecode(dmhy_value, Encoding.UTF8);
        }
        else if (url.Contains("ouo.si/feed") && paramDict.TryGetValue("q", out var ouo_value))
        {
            return HttpUtility.UrlDecode(ouo_value, Encoding.UTF8);
        }

        return string.Empty;
    }

    private static Dictionary<string, string> GetUrlParams(string url)
    {
        var paramDict = new Dictionary<string, string>();
        const string regStr = @"([^&?]*)=([^&]*)";
        var matches = Regex.Matches(url, regStr, RegexOptions.Multiline);
        foreach (Match match in matches)
        {
            var split = match.Value.Split('=');
            if (split.Length >= 2) paramDict.Add(split[0], split[1]);
        }

        return paramDict;
    }

    /// <summary>
    /// Rss剧集和规则名称
    /// </summary>
    /// <param name="info">条目数据</param>
    /// <param name="templateType">模板类型  0：剧集文件夹  1：规则</param>
    /// <returns></returns>
    public static string GetRssFolderName(DataSubjectsInfo info, int templateType)
    {
        var template = templateType switch
        {
            0 => GlobalConfig.Instance.AppConfig.RssFolderNameTemplate,
            1 => GlobalConfig.Instance.AppConfig.RssRuleNameTemplate,
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(template)) return string.Empty;

        var seasonYear = "0000";
        var seasonMonth = "00";
        if (DateTime.TryParse(info.AirDate, out DateTime airDate))
        {
            seasonYear = airDate.Month switch
            {
                12 => (airDate.Year + 1).ToString(),
                _ => airDate.Year.ToString()
            };
            seasonMonth = airDate.Month switch
            {
                12 or 1 or 2 => "01",
                3 or 4 or 5 => "04",
                6 or 7 or 8 => "07",
                9 or 10 or 11 => "10",
                _ => "00"
            };
        }

        var data = new
        {
            SubjectId = info.Id,
            SubjectName = info.Name,
            SubjectNameCn = info.NameCn,
            Year = airDate.Year,
            Month = airDate.Month.ToString().PadLeft(2, '0'),
            SeasonYear = seasonYear,
            SeasonMonth = seasonMonth,
        };

        var parser = new FluidParser();
        var name = string.Empty;
        if (parser.TryParse(template, out var fluidTemplate))
        {
            var context = new TemplateContext(data);
            name = fluidTemplate.Render(context);
        }

        return name;
    }
}