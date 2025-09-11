using System.Xml.Serialization;

namespace BangumiMediaTool.Models;

#region bgm api search old

public class BgmApiJson_SearchListItem_Old
{
    public long id { get; set; }
    public string name { get; set; }
    public string name_cn { get; set; }
    public long eps_count { get; set; }
    public string summary { get; set; }
    public string air_date { get; set; }
}

public class BgmApiJson_Search_Old
{
    public long results { get; set; }
    public List<BgmApiJson_SearchListItem_Old> list { get; set; }
}

#endregion

#region bgm api search

public class BgmApiJson_SearchListItem_Images
{
    public string small { get; set; }  = string.Empty;
}

public class BgmApiJson_SearchListItem
{
    public long id { get; set; }
    public string name { get; set; } = string.Empty;
    public string name_cn { get; set; } = string.Empty;
    public long eps { get; set; }
    public string summary { get; set; } = string.Empty;
    public string date { get; set; } = string.Empty;
    public string platform { get; set; } = string.Empty;
    public BgmApiJson_SearchListItem_Images images { get; set; } = new();
}

public class BgmApiJson_Search
{
    public List<BgmApiJson_SearchListItem> data { get; set; } = new();

    public long total { get; set; }
    public long limit { get; set; }
    public long offset { get; set; }
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

[XmlRoot("season")]
public class NfoInfo_SubjectsRootSeason
{
    public string bangumiid { get; set; } = string.Empty;
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
