using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using BangumiMediaTool.Models;
using BangumiMediaTool.Services.Program;

namespace BangumiMediaTool.Services.Api;

public class BangumiApiService
{
    public static BangumiApiService Instance { get; private set; } = null!;

    private readonly string bgmApiUrlBase = @"https://api.bgm.tv";

    private readonly HttpClient bgmApiClient;


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

        Logs.LogInfo("BangumiApiService Initialize");
    }

    /// <summary>
    /// Bangumi API 搜索剧集
    /// </summary>
    /// <param name="keywords">搜素关键词</param>
    /// <param name="offset">分页偏移参数 单页限制20</param>
    /// <returns>数据列表 + 总条目数</returns>
    public async Task<(List<DataSubjectsInfo>, long)> BangumiApi_Search(string keywords, long offset)
    {
        if (string.IsNullOrEmpty(keywords)) return ([], 0);

        var url = $"{bgmApiUrlBase}/v0/search/subjects?limit=20&offset={offset}";

        var data = new
        {
            keyword = keywords,
            filter = new
            {
                type = new List<int> { 2 },
                nsfw = true
            }
        };
        var dataString = JsonSerializer.Serialize(data);

        HttpResponseMessage response;
        try
        {
            if (!string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization == null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalConfig.Instance.AppConfig.BangumiAuthToken);
            }
            else if (string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization != null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = null;
            }

            response = await bgmApiClient.PostAsync(url, new StringContent(dataString, Encoding.UTF8));
        }
        catch (Exception e)
        {
            Logs.LogError($"{url} : Exception {e}");
            return ([], 0);
        }

        Logs.LogInfo($"请求: {url} : {response.StatusCode}");
        if (!response.IsSuccessStatusCode) return ([], 0);

        var result = await response.Content.ReadAsStringAsync();

        BgmApiJson_Search? jsonData;
        try
        {
            jsonData = JsonSerializer.Deserialize<BgmApiJson_Search>(result);
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
            return ([], 0);
        }

        if (jsonData == null) return ([], 0);

        var dataSubjectsInfos = new List<DataSubjectsInfo>();
        foreach (var item in jsonData.data)
        {
            if (string.IsNullOrEmpty(item.name) && !string.IsNullOrEmpty(item.name_cn))
            {
                item.name = item.name_cn;
            }
            else if (!string.IsNullOrEmpty(item.name) && string.IsNullOrEmpty(item.name_cn))
            {
                item.name_cn = item.name;
                item.name = string.Empty;
            }

            var addData = new DataSubjectsInfo()
            {
                Id = item.id,
                Name = item.name,
                NameCn = item.name_cn,
                EpsCount = item.eps,
                Desc = item.summary,
                AirDate = item.date,
                Platform = item.platform,
                ImageUrl = item.images.small
            };
            // addData.BuildShowText();
            dataSubjectsInfos.Add(addData);
        }

        return (dataSubjectsInfos, jsonData.total);
    }

    /// <summary>
    /// Bangumi API 剧集所有章节
    /// </summary>
    /// <param name="subjectInfo">条目数据</param>
    /// <param name="sourceList">初始数据列表</param>
    /// <returns></returns>
    public async Task<List<DataEpisodesInfo>> BangumiApi_Episodes(DataSubjectsInfo subjectInfo, List<DataEpisodesInfo>? sourceList = null)
    {
        long offset = 0;
        if (sourceList != null)
        {
            offset = sourceList.Count;
        }
        else
        {
            sourceList = [];
        }

        var url = $"{bgmApiUrlBase}/v0/episodes?subject_id={subjectInfo.Id}&limit=100&offset={offset}";

        BgmApiJson_EpisodesInfo? jsonData = null;

        try
        {
            if (!string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization == null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", GlobalConfig.Instance.AppConfig.BangumiAuthToken);
            }
            else if (string.IsNullOrEmpty(GlobalConfig.Instance.AppConfig.BangumiAuthToken) && bgmApiClient.DefaultRequestHeaders.Authorization != null)
            {
                bgmApiClient.DefaultRequestHeaders.Authorization = null;
            }

            var response = await bgmApiClient.GetAsync(url);
            Logs.LogInfo($"请求: {url} : {response.StatusCode}");
            if (!response.IsSuccessStatusCode) return sourceList;

            var result = await response.Content.ReadAsStringAsync();
            jsonData = JsonSerializer.Deserialize<BgmApiJson_EpisodesInfo>(WebUtility.HtmlDecode(result));
            if (jsonData == null) return sourceList;
        }
        catch (Exception e)
        {
            Logs.LogError(e.ToString());
            return sourceList;
        }

        foreach (var item in jsonData.data)
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
            sourceList.Add(addData);
        }

        if (jsonData.offset + 100 >= jsonData.total)
        {
            return sourceList.Where(item=>item.Type is 0 or 1).ToList();  //只获取正篇和SP
        }
        else
        {
            return await BangumiApi_Episodes(subjectInfo, sourceList);
        }
    }
}