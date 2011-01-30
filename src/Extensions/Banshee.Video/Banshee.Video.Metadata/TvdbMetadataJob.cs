// 
// TvdbMetadataProvider.cs
// 
// Author:
//   Olivier Dufour <olivier (dot) duff (at) gmail (dot) com>
// 
// Copyright 2011 
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Xml;
using System.Net;
using System.Web;

using Banshee.Base;
using Banshee.Web;
using Banshee.Metadata;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;

using Hyena;

namespace Banshee.Video.Metadata
{
    public class TvdbMetadataJob : MetadataServiceJob
    {
        private DatabaseTrackInfo track;

        public TvdbMetadataJob (IBasicTrackInfo track) : base ()
        {
            this.track = track as DatabaseTrackInfo;
        }

        public override void Run()
        {
            if (track == null || (track.MediaAttributes & TrackMediaAttributes.Podcast) != 0
                              || (track.MediaAttributes & TrackMediaAttributes.VideoStream) == 0)
                return;

            VideoService service = ServiceStack.ServiceManager.Get<VideoService> ();

            string cover_art_id = service.ArtworkIdFor (track);

            if (CoverArtSpec.CoverExists (cover_art_id)) {
                return;
            } else if (!InternetConnected) {
                return;
            }

            if ((track.MediaAttributes & TrackMediaAttributes.TvShow) == 0)
                return;

            GetMetadata (track);
        }

        void GetMetadata (DatabaseTrackInfo track)
        {
            string seriesid = GetSerieId (track);
            Log.Debug ("tv metadata job: serie id : " + seriesid);
            if (String.IsNullOrEmpty (seriesid))
                return;

            int parentid;
            VideoInfo parent = VideoInfo.Provider.FetchFirstMatching ("ExternalVideoId = ? AND VideoType = ?", seriesid, (int)videoType.Serie);
            if (parent == null) {
                Log.Debug ("no parent in db...");
                parent = GetSerieMetadata (track, seriesid);
                if (parent == null) {
                    Log.Debug ("Serie can not been created!");
                    return;
                }
            }
            parentid = parent.DbId;
            Log.Debug ("tv metadata job: parent id : " + parentid);
            GetEpisodeMetadata (track, seriesid, parentid);
            GetBanners (track, seriesid, parent.ArtworkId);

        }

        private readonly string API_KEY = "897F1CA903D4703A";

 #region Mirror (unactived for moment)
        private enum serverCapability : int
        {
            xmlfile= 1,
            bannerfile = 2,
            zipfile = 4
        }

        private class Mirror
        {
            public Mirror (string url, serverCapability capability)
            {
                this.url = url;
                this.capability = capability;
            }
            public string url;
            public serverCapability capability;
        }

        private List<Mirror> mirrors = new List<Mirror> ();
        void GetMirrorList ()
        {
            HttpRequest request = new HttpRequest (string.Format ("http://www.thetvdb.com/api/{0}/mirrors.xml", API_KEY));
            request.GetResponse ();
            XmlDocument doc = new XmlDocument();
            using (Stream stream = request.GetResponseStream ()) {
                doc.Load (stream);
            }
            foreach (XmlNode mirror_node in doc.DocumentElement.SelectNodes("/Mirrors/Mirror")) {
                mirrors.Add (new Mirror (mirror_node["mirrorpath"].InnerXml, (serverCapability)Int32.Parse(mirror_node["typemask"].InnerXml)));
            }
        }
#endregion

        //rarely updated so hardcoded
        private List<string> languages = new List<string> () {"da", "fi", "nl", "de", "it", "es", "fr", "pl", "hu", "el", "tr",
            "ru", "he", "ja", "pt", "zh", "cs", "sl", "hr", "ko", "en", "sv", "no"};
        /*void GetLanguageList ()
        {
            HttpRequest request = new HttpRequest (string.Format ("http://www.thetvdb.com/api/{0}/languages.xml", API_KEY));
            request.GetResponse ();
            XmlDocument doc = new XmlDocument();
            using (Stream stream = request.GetResponseStream ()) {
                doc.Load (stream);
            }

            foreach (XmlNode item_node in doc.DocumentElement.SelectNodes ("/Languages/Language")) {
                languages.Add (item_node["abbreviation"].InnerXml);
            }
        }*/

        private string GetSerieId (DatabaseTrackInfo track)
        {
            string lang;
            if (languages.Contains (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)) {
                lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            } else {
                lang = "All";
            }
            string serieName = track.ArtistName.Replace ('.', ' ').Replace ('_', ' ').Replace ('-', ' ');
            string url = string.Format("http://www.thetvdb.com/api/GetSeries.php?seriesname={0}&language={1}", HttpUtility.UrlEncode (serieName), lang);
            Log.Debug ("tvdb webservice ; " + url);
            HttpRequest request = new HttpRequest (url);
            XmlDocument doc = new XmlDocument();
            try {
                request.GetResponse ();
                using (Stream stream = request.GetResponseStream ()) {
                    doc.Load (stream);
                    Log.Debug (doc.InnerXml);
                }

                XmlNode item_node = doc.DocumentElement.SelectSingleNode ("/Data/Series");

                return item_node["seriesid"].InnerXml;
            } catch (WebException ex) {
                Log.Debug (ex.Status.ToString ());
                Log.DebugException (ex);
            } catch (Exception e) {
               Log.Error (doc.InnerXml);
               Log.DebugException (e);
            }
            return string.Empty;
        }

        private VideoInfo GetSerieMetadata (DatabaseTrackInfo track, string seriesid)
        {
            string lang;
            if (languages.Contains (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)) {
                lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            } else {
                lang = "en";
            }
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/{0}/series/{1}/{2}.xml", API_KEY, seriesid, lang));
            try {
                request.GetResponse ();

                XmlDocument doc = new XmlDocument();
                using (Stream stream = request.GetResponseStream ()) {
                    doc.Load (stream);
                }

                XmlNode serie_node = doc.DocumentElement.SelectSingleNode ("/Data/Series");
                if (serie_node == null) {
                    return null;
                }
                //create a new one as parent for serie
                VideoInfo video_info = new VideoInfo ();

                //serie_node["Genre"] //piped genre
                //video_info.SerieId = seriesid;
                video_info.Studios = serie_node["Network"].InnerXml;
                video_info.Summary = serie_node["Overview"].InnerXml;
                video_info.Title = serie_node["SeriesName"].InnerXml;
                video_info.Language = serie_node["Language"].InnerXml;
                video_info.ImDbId = serie_node["IMDB_ID"].InnerXml;
                video_info.VideoType = (int)videoType.Serie;
                video_info.ExternalVideoId = seriesid;
                video_info.ParentId = 0;
                video_info.ReleaseDate = DateTime.ParseExact (serie_node["FirstAired"].InnerXml, "yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat);

                video_info.Save ();

                string actors = serie_node["Actors"].InnerXml;
                if (actors != null) {
                    foreach (string actor in actors.Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries)) {
                        CastingMember member = new CastingMember ();
                        member.VideoID = video_info.DbId;
                        member.Name = actor;
                        member.Job = "actor";
                        member.Save ();
                    }
                }

                return video_info;
            }
            catch (Exception e) {
               Log.DebugException (e);
               return null;
            }
        }

        private void GetEpisodeMetadata (DatabaseTrackInfo track, string seriesid, int parentid)
        {
            string lang;
            if (languages.Contains (CultureInfo.CurrentCulture.TwoLetterISOLanguageName)) {
                lang = CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            } else {
                lang = "en";
            }
            string url = string.Format("http://www.thetvdb.com/api/{0}/series/{1}/default/{2}/{3}/{4}.xml", API_KEY, seriesid, HttpUtility.UrlEncode (track.AlbumTitle), track.TrackNumber, lang);
            Log.Debug (url);
            HttpRequest request = new HttpRequest (url);
            try {
                request.GetResponse ();
                XmlDocument doc = new XmlDocument();
                using (Stream stream = request.GetResponseStream ()) {
                    doc.Load (stream);
                }

                XmlNode episode_node = doc.DocumentElement.SelectSingleNode ("/Data/Episode");

                //track.TrackNumber
                VideoInfo video_info = (VideoInfo)track.ExternalObject;
                video_info.Title = episode_node["EpisodeName"].InnerXml;
                video_info.Language = episode_node["Language"].InnerXml;
                video_info.ImDbId = episode_node["IMDB_ID"].InnerXml;
                video_info.ReleaseDate = DateTime.ParseExact (episode_node["FirstAired"].InnerXml, "yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat);
                video_info.Summary = episode_node["Overview"].InnerXml;
                video_info.Title = episode_node["EpisodeName"].InnerXml;
                video_info.VideoType = (int)videoType.SerieEpisode;
                video_info.ExternalVideoId = episode_node["seriesid"].InnerXml;
                video_info.ParentId = parentid;
                video_info.Save ();

                CastingMember member;
                string members = episode_node["Director"].InnerXml;
                if (members != null) {
                    foreach (string director_name in members.Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries)) {
                        member = new CastingMember ();
                        member.VideoID = video_info.DbId;
                        member.Name = director_name;
                        member.Job = "Director";
                        member.Save ();
                    }
                }

                members = episode_node["GuestStars"].InnerXml;
                if (members != null) {
                    foreach (string guest_name in members.Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries)) {
                        member = new CastingMember ();
                        member.VideoID = video_info.DbId;
                        member.Name = guest_name;
                        member.Job = "GuestStar";
                        member.Save ();
                    }
                }

                members = episode_node["Writer"].InnerXml;
                if (members != null) {
                    foreach (string writer_name in members.Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries)) {
                        member = new CastingMember ();
                        member.VideoID = video_info.DbId;
                        member.Name = writer_name;
                        member.Job = "Writer";
                        member.Save ();
                    }
                }
                //not needed because ever done by regexp
                //track.TrackNumber = episode_node["EpisodeNumber"].InnerXml;
                //track.AlbumTitle = episode_node["SeasonNumber"].InnerXml;
                //TODO
                // <filename>episodes/80348-332179.jpg</filename>

            }
            catch (Exception e) {
               Log.DebugException (e);
            }
        }

        private void GetBanners (DatabaseTrackInfo track, string seriesid, string coverArtId)
        {
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/{0}/series/{1}/banners.xml", API_KEY, seriesid));
            try {
                request.GetResponse ();
                XmlDocument doc = new XmlDocument();
                using (Stream stream = request.GetResponseStream ()) {
                    doc.Load (stream);
                }

                foreach (XmlNode banner_node in doc.DocumentElement.SelectNodes ("/Banners/Banner")) {
                    LoadImage (banner_node, coverArtId);
                }
            }
            catch (Exception e) {
               Log.DebugException (e);
            }
        }

        private bool LoadImage (XmlNode obj, string cover_art_id)
        {
            string image_type = obj["BannerType"].InnerXml;
            string image_size = obj["BannerType2"].InnerXml;
            string image_url = obj["BannerPath"].InnerXml;

            if (image_type != "season" && image_size != "season")
                return false;

            image_url = "http://www.thetvdb.com/banners/" + image_url;
            Log.Debug (image_url);
            if (SaveHttpStreamCover (new Uri (image_url), cover_art_id, null)) {
                Banshee.Sources.Source src = ServiceManager.SourceManager.ActiveSource;
                if (src != null && (src is VideoLibrarySource || src.Parent is VideoLibrarySource)) {
                    (src as Banshee.Sources.DatabaseSource).Reload ();
                }
                return true;
            }
            return false;
        }
    }
}

