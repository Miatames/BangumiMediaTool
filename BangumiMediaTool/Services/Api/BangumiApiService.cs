using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;

namespace BangumiMediaTool.Services.Api;

public class BangumiApiService
{
    public static BangumiApiService Instance { get; private set; } = null!;

    private readonly string bgmApiUrlBase = @"https://api.bgm.tv";

    private readonly HttpClient bgmApiClient;
    private readonly HttpClient tmdbApiClient;

    public BangumiApiService()
    {
        Instance = this;

        bgmApiClient = new HttpClient(new HttpClientHandler()
        {
            ServerCertificateCustomValidationCallback = (_, _, _, _) => true
        });
        bgmApiClient.DefaultRequestHeaders.Add("Accept", "application/json");
        bgmApiClient.DefaultRequestHeaders.Add("User-Agent", "miatames/bangumi-media-tool (https://github.com/Miatames/BangumiMediaTool)");
        if (!string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken))
        {
            bgmApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalConfig.Instance.AppConfig.BangumiAuthToken);
        }
        bgmApiClient.Timeout = TimeSpan.FromSeconds(10);

        tmdbApiClient = new HttpClient();
        tmdbApiClient.DefaultRequestHeaders.Add("Accept", "application/json");
        tmdbApiClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + TmdbInfo.Authorization);
        bgmApiClient.Timeout = TimeSpan.FromSeconds(10);

        Logs.LogInfo("BangumiApiService Initialize");
    }

    /// <summary>
    /// TMDB API 搜索
    /// </summary>
    /// <param name="keywords">搜索关键词</param>
    /// <returns>原文标题 TmdbID</returns>
    public async Task<(string title, long? id)> TmdbApi_Search(string keywords)
    {
        if (string.IsNullOrEmpty(keywords) || string.IsNullOrEmpty(TmdbInfo.Authorization)) return (keywords, null);

        var url = $"https://api.themoviedb.org/3/search/multi?query={Uri.EscapeDataString(keywords)}&include_adult=false&page=1";

        HttpResponseMessage? response = null;
        try
        {
            response = await tmdbApiClient.GetAsync(url);
        }
        catch (Exception e)
        {
            Logs.LogError($"{url} : Exception {e}");
            return (keywords, null);
        }

        Logs.LogInfo($"请求: {url} : {response.StatusCode}");
        if (!response.IsSuccessStatusCode) return (keywords, null);

        TmdbApiJson_Search? jsonRoot;
        try
        {
            jsonRoot = JsonSerializer.Deserialize<TmdbApiJson_Search>(await response.Content.ReadAsStringAsync());
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
            return (keywords, null);
        }

        var resultTitle = keywords;
        long? resultId = null;
        if (jsonRoot is { results.Count: > 0 })
        {
            foreach (var resultsItem in jsonRoot.results)
            {
                if (resultsItem.media_type == "tv")
                {
                    resultTitle = resultsItem.original_name;
                    resultId = resultsItem.id;
                    break;
                }
                else if (resultsItem.media_type == "movie")
                {
                    resultTitle = resultsItem.original_title;
                    resultId = resultsItem.id;
                    break;
                }
            }
        }

        return (resultTitle, resultId);
    }

    /// <summary>
    /// Bangumi API 搜索
    /// </summary>
    /// <param name="keywords">搜索关键词</param>
    /// <param name="getAllResults">是否获取全部结果</param>
    /// <returns>剧集数据列表</returns>
    public async Task<List<DataSubjectsInfo>> BangumiApi_Search(string keywords, bool getAllResults = false)
    {
        var results = new List<string>();
        if (string.IsNullOrEmpty(keywords)) return [];

        var url = $"{bgmApiUrlBase}/search/subject/{Uri.EscapeDataString(keywords)}?type=2&responseGroup=large&start=0&max_results=25";

        HttpResponseMessage response;
        try
        {
            if (!string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization == null)
            {
               bgmApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalConfig.Instance.AppConfig.BangumiAuthToken);
            }
            else if (string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization !=null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = null;
            }

            response = await bgmApiClient.GetAsync(url);
        }
        catch (Exception e)
        {
            Logs.LogError($"{url} : Exception {e}");
            return [];
        }

        Logs.LogInfo($"请求: {url} : {response.StatusCode}");
        if (!response.IsSuccessStatusCode) return [];

        var result = await response.Content.ReadAsStringAsync();

        JsonDocument? document = null;
        try
        {
            document = JsonDocument.Parse(result);
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
            return [];
        }

        var root = document.RootElement;

        //请求出错直接返回
        if (root.TryGetProperty("error", out _)) return [];

        results.Add(WebUtility.HtmlDecode(result));

        if (getAllResults && root.TryGetProperty("results", out JsonElement maxCount) && maxCount.GetInt32() >= 25)
        {
            for (int i = 1; i < Math.Ceiling(maxCount.GetSingle() / 25.0f); i++)
            {
                var urlLoop = $"{bgmApiUrlBase}/search/subject/{Uri.EscapeDataString(keywords)}?type=2&responseGroup=large&start={i * 25}&max_results=25";
                Console.WriteLine("请求: " + urlLoop);
                var responseLoop = await bgmApiClient.GetAsync(urlLoop);
                if (!responseLoop.IsSuccessStatusCode) continue;

                var resultLoop = await responseLoop.Content.ReadAsStringAsync();
                results.Add(WebUtility.HtmlDecode(resultLoop));
            }
        }

        var dataSubjectsInfos = new List<DataSubjectsInfo>();
        foreach (var r in results)
        {
            BgmApiJson_Search? jsonData;
            try
            {
                jsonData = JsonSerializer.Deserialize<BgmApiJson_Search>(r);
            }
            catch (Exception e)
            {
                Logs.LogError(e.ToString());
                continue;
            }

            if (jsonData == null) continue;
            foreach (var item in jsonData.list)
            {
                var addData = new DataSubjectsInfo
                {
                    Id = item.id,
                    Name = item.name,
                    NameCn = item.name_cn,
                    EpsCount = item.eps_count,
                    Desc = item.summary,
                    AirDate = item.air_date,
                };
                addData.BuildShowText();
                dataSubjectsInfos.Add(addData);
            }
        }

        return dataSubjectsInfos;
    }

    /// <summary>
    /// Bangumi API 剧集所有章节
    /// </summary>
    /// <param name="subjectInfo">条目数据</param>
    /// <returns></returns>
    public async Task<List<DataEpisodesInfo>> BangumiApi_Episodes(DataSubjectsInfo subjectInfo)
    {
        var url = $"{bgmApiUrlBase}/v0/episodes?subject_id={subjectInfo.Id}";

        BgmApiJson_EpisodesInfo? jsonData = null;

        try
        {
            if (!string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization == null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalConfig.Instance.AppConfig.BangumiAuthToken);
            }
            else if (string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization !=null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = null;
            }

            var response = await bgmApiClient.GetAsync(url);
            Logs.LogInfo($"请求: {url} : {response.StatusCode}");
            if (!response.IsSuccessStatusCode) return [];

            var result = await response.Content.ReadAsStringAsync();
            jsonData = JsonSerializer.Deserialize<BgmApiJson_EpisodesInfo>(WebUtility.HtmlDecode(result));
            if (jsonData == null) return [];
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
            return [];
        }

        var dataEpisodesInfos = new List<DataEpisodesInfo>();
        foreach (var item in jsonData.data.Where(item => item.type is 0 or 1)) //只获取正篇和SP
        {
            var addData = new DataEpisodesInfo
            {
                Id = item.id,
                Name = item.name,
                NameCn = item.name_cn,
                SubjectName = subjectInfo.Name,
                SubjectNameCn = subjectInfo.NameCn,
                Ep = item.ep,
                Sort = item.sort,
                SubjectId = item.subject_id,
                Type = item.type,
                Year = DateTime.Parse(subjectInfo.AirDate).Year.ToString()
            };
            addData.BuildShowText();
            dataEpisodesInfos.Add(addData);
        }

        return dataEpisodesInfos;
    }
}