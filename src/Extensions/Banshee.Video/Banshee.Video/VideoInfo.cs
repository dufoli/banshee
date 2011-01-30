using System;
//using System.Data;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Collection;
using Hyena;

namespace Banshee.Video
{
    public enum videoType : int {
        SerieEpisode = 0,
        SerieSaison = 1,
        Serie = 2,
        Movie = 3,
        Other = 4
    }

    public class VideoInfo : CacheableItem
    {
        private static SqliteModelProvider<VideoInfo> provider;

        public static void Init() {
            provider= new SqliteModelProvider<VideoInfo> (ServiceManager.DbConnection, "Videos", true);
        }

        public static SqliteModelProvider<VideoInfo> Provider {
            get { return provider; }
        }

        public static readonly string UnknownTitle = Catalog.GetString ("Unknown Title");

        public static void Reset ()
        {
            //last_Video = null;
        }

        public VideoInfo ()
        {
        }

        public void Save ()
        {
            Provider.Save (this);
        }

        private string imdb_id;
        private DateTime release_date = DateTime.MinValue;
        private string language;
        private string title;
        private string original_title;
        private string alternative_title;
        private string info_url;
        private string homepage_url;
        private string trailer_url;
        private string summary;
        private string studios;
        private string country;
        private int parent;
        private string external_video_id;
        private int video_type;

        [DatabaseColumn("VideoID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int dbid;
        public int DbId {
            get { return dbid; }
        }

        [DatabaseColumn("Language")]
        public string Language {
            get { return language; }
            set { language = value; }
        }

        [DatabaseColumn("IMDBID")]
        public string ImDbId {
            get { return imdb_id; }
            set { imdb_id = value; }
        }

        [DatabaseColumn]
        public DateTime ReleaseDate {
            get { return release_date; }
            set { release_date = value; }
        }

        [DatabaseColumn]
        public string Title {
            get { return title; }
            set { title = value; }
        }

        [DatabaseColumn(Select = false)]
        internal string TitleLowered {
            get { return Hyena.StringUtil.SearchKey (Title); }
        }

        [DatabaseColumn]
        public string OriginalTitle {
            get { return original_title; }
            set { original_title = value; }
        }

        [DatabaseColumn(Select = false)]
        internal string OriginalTitleLowered {
            get { return Hyena.StringUtil.SearchKey (OriginalTitle); }
        }

        [DatabaseColumn]
        public string AlternativeTitle {
            get { return alternative_title; }
            set { alternative_title = value; }
        }

        [DatabaseColumn(Select = false)]
        internal string AlternativeTitleLowered {
            get { return Hyena.StringUtil.SearchKey (AlternativeTitle); }
        }

        [DatabaseColumn]
        public string InfoUrl {
            get { return info_url; }
            set { info_url = value; }
        }

        [DatabaseColumn]
        public string HomepageUrl {
            get { return homepage_url; }
            set { homepage_url = value; }
        }

        [DatabaseColumn]
        public string TrailerUrl {
            get { return trailer_url; }
            set { trailer_url = value; }
        }

        [DatabaseColumn]
        public string Summary {
            get { return summary; }
            set { summary = value; }
        }

        //MGM, miramax, dreamworks, ...)
        [DatabaseColumn]
        public string Studios {
            get { return studios; }
            set { studios = value; }
        }

        [DatabaseColumn]
        public string Country {
            get { return country; }
            set { country = value; }
        }

        // For tvshow episode link to main description
        [DatabaseColumn]
        public int ParentId {
            get { return parent; }
            set { parent = value; }
        }

        [DatabaseColumn]
        public string ExternalVideoId {
            get { return external_video_id; }
            set { external_video_id = value; }
        }

        [DatabaseColumn]
        public int VideoType {
            get { return video_type; }
            set { video_type = value; }
        }

        public string DisplayTrackTitle {
            get { return StringUtil.MaybeFallback (Title, UnknownTitle); }
        }

        public override string ToString ()
        {
            return String.Format ("<LibraryVideoInfo Title={0} DbId={1}>", Title, DbId);
        }

        public string ArtworkId {
            get {
                return String.Format ("video-{0}-{1}", VideoType, ExternalVideoId);
            }
        }
    }
}
