using System.Net.Http;
using System.Text.Json;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;

namespace BangumiMediaTool.Services.Api;

public class TmdbApiService
{
    public static TmdbApiService Instance { get; private set; } = null!;

    private readonly string tmdbUrlBase = @"https://api.themoviedb.org/3";

    private readonly HttpClient tmdbApiClient;

    public TmdbApiService()
    {
        Instance = this;

        tmdbApiClient = new HttpClient();
        tmdbApiClient.DefaultRequestHeaders.Add("Accept", "application/json");
        tmdbApiClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + TmdbInfo.Authorization);
        tmdbApiClient.Timeout = TimeSpan.FromSeconds(10);

        Logs.LogInfo("TmdbApiService Initialize");
    }

    /// <summary>
    /// TMDB API 搜索
    /// </summary>
    /// <param name="keywords">搜索关键词</param>
    /// <returns>原文标题 TmdbID</returns>
    public async Task<(string title, long? id)> TmdbApi_Search(string keywords)
    {
        if (string.IsNullOrEmpty(keywords) || string.IsNullOrEmpty(TmdbInfo.Authorization)) return (keywords, null);

        var url = $"{tmdbUrlBase}/search/multi?query={Uri.EscapeDataString(keywords)}&include_adult=false&page=1";

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
    /// TMDB API 寻找剧集关联的id
    /// </summary>
    /// <param name="tmdbId">tmdb_id</param>
    /// <returns>thetvdb_id</returns>
    public async Task<long> TmdbApi_ExternalIds(long tmdbId)
    {
        var url = $"{tmdbUrlBase}/tv/{tmdbId}/external_ids";

        HttpResponseMessage? response = null;

        try
        {
            response = await tmdbApiClient.GetAsync(url);
        }
        catch (Exception e)
        {
            Logs.LogError($"{url} : Exception {e}");
            return 0;
        }

        Logs.LogInfo($"请求: {url} : {response.StatusCode}");
        if (!response.IsSuccessStatusCode) return 0;

        Logs.LogInfo(await response.Content.ReadAsStringAsync());
        return 1;
    }
}