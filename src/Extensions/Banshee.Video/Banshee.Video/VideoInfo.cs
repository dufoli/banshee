using System;
//using System.Data;

using Mono.Unix;

using Hyena.Data;
using Hyena.Data.Sqlite;

using Banshee.Database;
using Banshee.ServiceStack;
using Banshee.Collection;

namespace Banshee.Video
{
    public class VideoInfo : CacheableItem
    {
        private static BansheeModelProvider<VideoInfo> provider = new BansheeModelProvider<VideoInfo> (
            ServiceManager.DbConnection, "Videos"
        );

        public static BansheeModelProvider<VideoInfo> Provider {
            get { return provider; }
        }

        private static HyenaSqliteCommand default_select_command = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreVideos.Title = ?",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static HyenaSqliteCommand select_command_with_imdb_id = new HyenaSqliteCommand (String.Format (
            "SELECT {0} FROM {1} WHERE {2} AND CoreVideos.Title = ? AND CoreVideos.ImDbId = ? ",
            provider.Select, provider.From,
            (String.IsNullOrEmpty (provider.Where) ? "1=1" : provider.Where)
        ));

        private static VideoInfo last_Video;

        public static void Reset ()
        {
            last_Video = null;
        }

        public static VideoInfo FindOrCreate (string title, VideoInfo parent)
        {
            return FindOrCreate (title, parent, null);
        }

        public static VideoInfo FindOrCreate (string title, VideoInfo parent, string imdb_id)
        {
            VideoInfo Video = new VideoInfo ();

            Video.Title = title;
            Video.ParentId = -1;
            if (parent != null) {
                Video.ParentId = parent.DbId;
            }
            Video.ImDbId = imdb_id;
            return FindOrCreate (Video, parent);
        }

        private static IDataReader FindExistingVideo (string title, string imdb_id)
        {
            HyenaSqliteConnection db = ServiceManager.DbConnection;
            if (imdb_id != null) {
                return db.Query (select_command_with_imdb_id, title, imdb_id);
            }
            return db.Query (default_select_command, title);
        }

        public static VideoInfo FindOrCreate (VideoInfo Video, VideoInfo tvshow)
        {
            if (last_Video != null && Video.Title == last_Video.Title && Video.ImDbId == last_Video.ImDbId) {
                return last_Video;
            }

            if (String.IsNullOrEmpty (Video.Title) || Video.Title.Trim () == String.Empty) {
                Video.Title = null;
            }

            using (IDataReader reader = FindExistingVideo (Video.Title, Video.ImDbId)) {
                if (reader.Read ()) {
                    bool save = false;
                    last_Video = provider.Load (reader);

                    if (last_Video.Language != Video.Language && !String.IsNullOrEmpty (Video.Language)) {
                        last_Video.Language = Video.Language;
                        save = true;
                    }

                    if (last_Video.ImDbId != Video.ImDbId && !String.IsNullOrEmpty (Video.ImDbId)) {
                        last_Video.ImDbId = Video.ImDbId;
                        save = true;
                    }
                    
                    if ((last_Video.ReleaseDate != Video.ReleaseDate) && (Video.ReleaseDate != DateTime.MinValue)) {
                        last_Video.ReleaseDate = Video.ReleaseDate;
                        save = true;
                    }
                    
                    if (last_Video.Title != Video.Title && !String.IsNullOrEmpty (Video.Title)) {
                        last_Video.Title = Video.Title;
                        save = true;
                    }
                    
                    if (last_Video.OriginalTitle != Video.OriginalTitle && !String.IsNullOrEmpty (Video.OriginalTitle)) {
                        last_Video.OriginalTitle = Video.OriginalTitle;
                        save = true;
                    }
                    
                    if (last_Video.AlternativeTitle != Video.AlternativeTitle && !String.IsNullOrEmpty (Video.AlternativeTitle)) {
                        last_Video.AlternativeTitle = Video.AlternativeTitle;
                        save = true;
                    }
                    
                    if (last_Video.InfoUrl != Video.InfoUrl && !String.IsNullOrEmpty (Video.InfoUrl)) {
                        last_Video.InfoUrl = Video.InfoUrl;
                        save = true;
                    }
                    
                    if (last_Video.HomepageUrl != Video.HomepageUrl && !String.IsNullOrEmpty (Video.HomepageUrl)) {
                        last_Video.HomepageUrl = Video.HomepageUrl;
                        save = true;
                    }

                    if (last_Video.TrailerUrl != Video.TrailerUrl && !String.IsNullOrEmpty (Video.TrailerUrl)) {
                        last_Video.TrailerUrl = Video.TrailerUrl;
                        save = true;
                    }

                    if (last_Video.Summary != Video.Summary && !String.IsNullOrEmpty (Video.Summary)) {
                        last_Video.Summary = Video.Summary;
                        save = true;
                    }

                    if (last_Video.Studios != Video.Studios && !String.IsNullOrEmpty (Video.Studios)) {
                        last_Video.Studios = Video.Studios;
                        save = true;
                    }

                    if (last_Video.Country != Video.Country && !String.IsNullOrEmpty (Video.Country)) {
                        last_Video.Country = Video.Country;
                        save = true;
                    }

                    if (last_Video.ParentId != Video.ParentId && Video.ParentId != -1) {
                        last_Video.ParentId = Video.ParentId;
                        save = true;
                    }

                    if (save) {
                        last_Video.Save ();
                    }
                } else {
                    Video.Save ();
                    last_Video = Video;
                }
            }
            return last_Video;
        }

        public static VideoInfo UpdateOrCreate (VideoInfo Video)
        {
            VideoInfo found = FindOrCreate (Video, null);
            if (found != Video) {
                // Overwrite the found Video
                Video.dbid = found.DbId;
                Video.Language = found.Language;
                Video.ImDbId = found.ImDbId;
                Video.ReleaseDate = found.ReleaseDate;
                Video.Title = found.Title;
                Video.OriginalTitle = found.OriginalTitle;
                Video.AlternativeTitle = found.AlternativeTitle;
                Video.InfoUrl = found.InfoUrl;
                Video.HomepageUrl = found.HomepageUrl;
                Video.TrailerUrl = found.TrailerUrl;
                Video.Summary = found.Summary;
                Video.Studios = found.Studios;
                Video.Country = found.Country;
                Video.ParentId = found.ParentId;
                Video.Save ();
            }
            return Video;
        }

        public VideoInfo () : base ()
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
        private string serie_id;
        private bool is_main_serie;

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

        [DatabaseColumn]
        public string OriginalTitle {
            get { return original_title; }
            set { original_title = value; }
        }

        [DatabaseColumn]
        public string AlternativeTitle {
            get { return alternative_title; }
            set { alternative_title = value; }
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
        public string SerieId {
            get { return serie_id; }
            set { serie_id = value; }
        }

        [DatabaseColumn]
        public bool IsMainSerie {
            get { return is_main_serie; }
            set { is_main_serie = value; }
        }

        public override string ToString ()
        {
            return String.Format ("<LibraryVideoInfo Title={0} DbId={1}>", Title, DbId);
        }
    }
}

//TODO Link to track
//TODO on DBTrackInfo:
/*
trackmediaattribute //add episode flag
categories (like tags) crime, thriller, drama, comedy, ... //map to genre
artworkid //generate from id
season //map to album
episode number //map to tracknumber
??? parent for tvshow // map to artist?


DatabaseTrackListModel
DatabaseFilterListModel

//TODO 
Handle Video track added and add externalid item and link it.
Maybe can be done with source.TrackExternalObjectHandler to add external object add search for data.
==> TODO migrate old video track with 

on call to TrackExternalObjectHandler load the VideoInfo with ExternalId.

On track added: RefreshMetadata ();

launch a query to load all track with no externalid in the videosource as RescanPipeline
and add an external object when found and this one will contain 

delete orphelan videoInfo.

Another thread will update this data in low priority.
==> check how music part done it.
*/