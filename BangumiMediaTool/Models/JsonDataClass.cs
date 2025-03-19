using System.Xml.Serialization;

namespace BangumiMediaTool.Models;

#region bgm api search

public class BgmApiJson_SearchListItem
{
    public long id { get; set; }
    public string name { get; set; }
    public string name_cn { get; set; }
    public long eps_count { get; set; }
    public string summary { get; set; }
    public string air_date { get; set; }
}

public class BgmApiJson_Search
{
    public long results { get; set; }
    public List<BgmApiJson_SearchListItem> list { get; set; }
}

#endregion

#region tmdb api search

public class TmdbApiJson_SearchResultsItem
{
    public long id { get; set; }
    public string original_title { get; set; } = string.Empty;
    public string original_name { get; set; } = string.Empty;
    public string media_type { get; set; } = string.Empty;
}

public class TmdbApiJson_Search
{
    public long page { get; set; }
    public List<TmdbApiJson_SearchResultsItem> results { get; set; }
    public long total_pages { get; set; }
    public long total_results { get; set; }
}

#endregion

#region episodes

public class BgmApiJson_EpisodesInfoListItem
{
    public string name { get; set; } = string.Empty;
    public string name_cn { get; set; } = string.Empty;
    public float ep { get; set; }
    public float sort { get; set; }
    public long id { get; set; }
    public long subject_id { get; set; }
    public int type { get; set; }
}

public class BgmApiJson_EpisodesInfo
{
    public List<BgmApiJson_EpisodesInfoListItem> data { get; set; }
    public long total { get; set; }
    public long limit { get; set; }
    public long offset { get; set; }
}

#endregion

#region nfo info

[XmlRoot("episodedetails")]
public class NfoInfo_EpisodesRoot
{
    public string bangumiid { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string originaltitle { get; set; } = string.Empty;
    public string showtitle { get; set; } = string.Empty;
    public string episode { get; set; } = string.Empty;
    public string season { get; set; } = string.Empty;
}

[XmlRoot("tvshow")]
public class NfoInfo_SubjectsRootTv
{
    public string bangumiid { get; set; } = string.Empty;
    public string tmdbid { get; set; } = string.Empty;
    public string title { get; set; } = string.Empty;
    public string originaltitle { get; set; } = string.Empty;
    public string showtitle { get; set; } = string.Empty;
    public string year{ get; set; } = string.Empty;
}

[XmlRoot("movie")]
public class NfoInfo_SubjectsRootMovie
{
    public string bangumiid { get; set; } = string.Empty;
    public string tmdbid { get; set; } = string.Empty;
    public string title { get; set; }
    public string originaltitle { get; set; } = string.Empty;
    public string year{ get; set; } = string.Empty;
}

#endregion
