using System.Text;
using System.Text.RegularExpressions;
using System.Web;

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
}