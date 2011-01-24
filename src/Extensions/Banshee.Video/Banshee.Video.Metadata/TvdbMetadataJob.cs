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

using Banshee.Base;
using Banshee.Web;
using Banshee.Metadata;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;

using Hyena.Json;
using Hyena;

namespace Banshee.Video.Metadata
{
    public class TvdbMetadataJob : MetadataServiceJob
    {
        private DatabaseTrackInfo track;

        public TvdbMetadataJob (IBasicTrackInfo track) : base ()
        {
            this.track = (DatabaseTrackInfo)track;
        }
        //http://thetvdb.com/

        public override void Run()
        {
            VideoService service = ServiceStack.ServiceManager.Get<VideoService> ();

            string cover_art_id = service.ArtworkIdFor (track);

            if (cover_art_id == null) {
                return;
            } else if (CoverArtSpec.CoverExists (cover_art_id)) {
                return;
            } else if (!InternetConnected) {
                return;
            }

            if ((track.MediaAttributes & TrackMediaAttributes.TvShow) == 0)
                return;

            GetMetadata (track, cover_art_id);

        }

        private readonly string API_KEY = "897F1CA903D4703A";

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
            Stream stream = request.GetResponseStream ();
            Deserializer deserializer = new Deserializer (stream);
            object obj = deserializer.Deserialize ();
            JsonObject json_obj = obj as Hyena.Json.JsonObject;

            if (json_obj == null)
                return;

            var items = json_obj["Mirrors"] as JsonObject;
            var obj_item = items["Mirror"];
            JsonObject json_item = null;

            if (obj_item is JsonArray) {
                foreach (object o in ((JsonArray)obj_item)) {
                    json_item = (JsonObject)o;
                    mirrors.Add (new Mirror (json_item["mirrorpath"] as string, (serverCapability)Int32.Parse(json_item["typemask"] as string)));
                }
            }
            else if (obj_item is JsonObject) {
                json_item = (JsonObject)obj_item;
                mirrors.Add (new Mirror (json_item["mirrorpath"] as string, (serverCapability)Int32.Parse(json_item["typemask"] as string)));
            }
        }

        //rarely updated so hardcoded
        private List<string> languages = new List<string> () {"da", "fi", "nl", "de", "it", "es", "fr", "pl", "hu", "el", "tr",
            "ru", "he", "ja", "pt", "zh", "cs", "sl", "hr", "ko", "en", "sv", "no"};
        /*void GetLanguageList ()
        {
            HttpRequest request = new HttpRequest (string.Format ("http://www.thetvdb.com/api/{0}/languages.xml", API_KEY));
            request.GetResponse ();
            Stream s = request.GetResponseStream ();
            Deserializer deserializer = new Deserializer (stream);
            object obj = deserializer.Deserialize ();
            JsonObject json_obj = obj as Hyena.Json.JsonObject;

            if (json_obj == null)
                return string.Empty;

            var items = json_obj["Languages"] as JsonObject;
            var obj_item = items["Language"];
            JsonObject json_item = null;

            if (obj_item is JsonArray) {
                foreach (object o in ((JsonArray)obj_item)) {
                    json_item = (JsonObject)o;
                    languages.Add (json_item["abbreviation"] as string);
                }
            }
            else if (obj_item is JsonObject) {
                json_item = (JsonObject)obj_item;
                languages.Add (json_item["abbreviation"] as string);
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

            //TODO get title from videoInfo.title to remove season/episode part
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/GetSeries.php?seriesname={0}&language={1}", track.ArtistName, lang));
            try {
                Stream stream = request.GetResponseStream ();
                Deserializer deserializer = new Deserializer (stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null || !json_obj.ContainsKey ("Items"))
                    return string.Empty;

                var items = json_obj["Items"] as JsonObject;

                if (items == null || !items.ContainsKey ("Item"))
                    return string.Empty;

                var obj_item = items["Item"];
                JsonObject json_item = null;

                if (obj_item is JsonArray)
                    json_item = (JsonObject) (((JsonArray)obj_item)[0]);
                else if (obj_item is JsonObject)
                    json_item = (JsonObject)obj_item;

                //store imdbid in musicbrainzid
                return json_item["seriesid"] as string;
            }
            catch (Exception e) {
               Log.DebugException (e);
               return string.Empty;
            }
        }

        private VideoInfo GetSerieMetadata (DatabaseTrackInfo track, string seriesid)
        {
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/{0}/series/{1}/language.xml", API_KEY, seriesid));
            try {
                Stream response_stream = request.GetResponseStream ();

                Deserializer deserializer = new Deserializer (response_stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null || !json_obj.ContainsKey ("Series"))
                    return null;

                var json_item = json_obj["Series"] as JsonObject;

                if (json_item == null)
                    return null;

                //create a new one as parent for serie
                VideoInfo video_info = new VideoInfo ();

                //json_item["Genre"] //piped genre
                //video_info.SerieId = seriesid;
                video_info.Studios = json_item["Network"] as string;
                video_info.Summary = json_item["Overview"] as string;
                video_info.Title = json_item["SeriesName"] as string;
                video_info.Language = json_item["Language"] as string;
                video_info.ImDbId = json_item["IMDB_ID"] as string;
                video_info.VideoType = (int)videoType.Serie;
                video_info.ReleaseDate = DateTime.ParseExact (json_item["FirstAired"] as string, "yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat);

                video_info.Save ();

                string actors = json_item["Actors"] as string;
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
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/{0}/series/{1}/default/{2}/{3}/language.xml", API_KEY, seriesid, track.AlbumTitle, track.TrackNumber));
            try {
                Stream response_stream = request.GetResponseStream ();

                Deserializer deserializer = new Deserializer (response_stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null || !json_obj.ContainsKey ("Episode"))
                    return;

                var json_item = json_obj["Episode"] as JsonObject;

                //track.TrackNumber
                VideoInfo video_info = (VideoInfo)track.ExternalObject;
                video_info.Title = json_item["EpisodeName"] as string;
                video_info.Language = json_item["Language"] as string;
                video_info.ImDbId = json_item["IMDB_ID"] as string;
                video_info.ReleaseDate = DateTime.ParseExact (json_item["FirstAired"] as string, "yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat);
                video_info.Summary = json_item["Overview"] as string;
                video_info.Title = json_item["EpisodeName"] as string;
                video_info.VideoType = (int)videoType.SerieEpisode;
                video_info.ExternalVideoId = json_item["seriesid"] as string;
                video_info.ParentId = parentid;
                video_info.Save ();

                CastingMember member;
                string members = json_item["Director"] as string;
                if (members != null) {
                    foreach (string director_name in members.Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries)) {
                        member = new CastingMember ();
                        member.VideoID = video_info.DbId;
                        member.Name = director_name;
                        member.Job = "Director";
                        member.Save ();
                    }
                }

                members = json_item["GuestStars"] as string;
                if (members != null) {
                    foreach (string guest_name in members.Split(new string[] {"|"}, StringSplitOptions.RemoveEmptyEntries)) {
                        member = new CastingMember ();
                        member.VideoID = video_info.DbId;
                        member.Name = guest_name;
                        member.Job = "GuestStar";
                        member.Save ();
                    }
                }

                members = json_item["Writer"] as string;
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
                //track.TrackNumber = json_item["EpisodeNumber"] as string;
                //track.AlbumTitle = json_item["SeasonNumber"] as string;
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
                Stream response_stream = request.GetResponseStream ();

                Deserializer deserializer = new Deserializer (response_stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null)
                    return;

                if (json_obj == null || !json_obj.ContainsKey ("Banners"))
                    return;

                var json_item = json_obj["Banners"] as JsonObject;

                var obj_item = json_item["Banner"];
                json_item = null;

                if (obj_item is JsonArray) {
                    foreach (object o in (JsonArray)obj_item) {
                        if (LoadImage ((JsonObject)o, coverArtId))
                            break;
                    }
                } else if (obj_item is JsonObject)
                    LoadImage ((JsonObject)obj_item, coverArtId);
            }
            catch (Exception e) {
               Log.DebugException (e);
            }
        }

        private bool LoadImage (JsonObject obj, string cover_art_id)
        {
            string image_type = obj["BannerType"] as string;
            string image_size = obj["BannerType2"] as string;
            string image_url = obj["BannerPath"] as string;

            if (image_type != "season" && image_size != "season")
                return false;

            if (SaveHttpStreamCover (new Uri (image_url), cover_art_id, null)) {
                Banshee.Sources.Source src = ServiceManager.SourceManager.ActiveSource;
                if (src != null && (src is VideoLibrarySource || src.Parent is VideoLibrarySource)) {
                    (src as Banshee.Sources.DatabaseSource).Reload ();
                }
                return true;
            }
            return false;
        }

        void GetMetadata (DatabaseTrackInfo track, string coverArtId)
        {
            string seriesid = GetSerieId (track);

            int parentid;
            VideoInfo parent = VideoInfo.Provider.FetchFirstMatching ("SerieId = {0} AND VideoType = {1}", seriesid, videoType.Serie);
            if (parent == null) {
                parent = GetSerieMetadata (track, seriesid);
                if (parent == null) {
                    return;
                }
            }
            parentid = parent.DbId;
            GetEpisodeMetadata (track, seriesid, parentid);
            GetBanners (track, seriesid, coverArtId);

        }
    }
}

