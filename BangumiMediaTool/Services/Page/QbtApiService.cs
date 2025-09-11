using System.Net.Http;
using System.Text.Json;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;

namespace BangumiMediaTool.Services.Page;

public class QbtApiService
{
    public static QbtApiService Instance { get; private set; } = null!;

    private readonly HttpClient qbtApiClient;

    public QbtApiService()
    {
        Instance = this;

        qbtApiClient = new HttpClient();
        qbtApiClient.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <summary>
    /// 添加订阅
    /// </summary>
    /// <param name="feedUrl">订阅地址</param>
    /// <param name="feedName">订阅名称</param>
    /// <returns></returns>
    public async Task<bool> QbtApi_AddFeed(string feedUrl, string feedName)
    {
        var api = new Uri(new Uri(GlobalConfig.Instance.AppConfig.QbtWebServerUrl), "api/v2/rss/addFeed");
        var param = new Dictionary<string, string>
        {
            { "url", feedUrl },
            { "path", feedName }
        };

        try
        {
            var response = await qbtApiClient.PostAsync(api, new FormUrlEncodedContent(param));
            Logs.LogInfo("qBittorrent Add Feed " + response.StatusCode);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) return true;
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
        }

        return false;
    }

    /// <summary>
    /// 添加下载规则
    /// </summary>
    /// <param name="addRssRuleData">下载规则</param>
    /// <param name="ruleName">规则名称</param>
    /// <returns></returns>
    public async Task<bool> QbtApi_AddRule(DataAddRssRule addRssRuleData, string ruleName)
    {
        var api = new Uri(new Uri(GlobalConfig.Instance.AppConfig.QbtWebServerUrl), "api/v2/rss/setRule");
        try
        {
            var param = new Dictionary<string, string>
            {
                { "ruleName", ruleName },
                { "ruleDef", JsonSerializer.Serialize(addRssRuleData) }
            };

            var response = await qbtApiClient.PostAsync(api, new FormUrlEncodedContent(param));
            Logs.LogInfo("qBittorrent Set Rule " + response.StatusCode);
            if (response.StatusCode == System.Net.HttpStatusCode.OK) return true;
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
        }

        return false;
    }
}