// 
// TmdbMetadataProvider.cs
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
using System.Globalization;
using System.IO;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Metadata;
using Banshee.Web;

using Hyena;
using Hyena.Json;

namespace Banshee.Video.Metadata
{
    public class TmdbMetadataJob : MetadataServiceJob
    {
        private DatabaseTrackInfo track;

        public TmdbMetadataJob (IBasicTrackInfo track)
        {
            this.track = (DatabaseTrackInfo)track;
        }
        
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

            if ((track.MediaAttributes & TrackMediaAttributes.Movie) == 0)
                return;

            string tmdb_id = SearchMovie (track, cover_art_id);
            GetInfo (track, tmdb_id);
        }

        private readonly string API_KEY = "0e4632ee0881dce13dfc053ec42b819b";

        private string SearchMovie (DatabaseTrackInfo track, string coverArtId)
        {
            HttpRequest request = new HttpRequest (string.Format("http://api.themoviedb.org/2.1/Movie.search/{0}/xml/{1}/{2}", CultureInfo.CurrentCulture.Name, API_KEY, track.TrackTitle));
            try {
                Stream stream = request.GetResponseStream ();
                Deserializer deserializer = new Deserializer (stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null || !json_obj.ContainsKey ("OpenSearchDescription"))
                    return string.Empty;

                var head = json_obj["OpenSearchDescription"] as JsonObject;

                if (head == null || !head.ContainsKey ("movies"))
                    return string.Empty;

                var movies_item = head["movies"] as JsonObject;

                if (movies_item == null || !movies_item.ContainsKey ("movie"))
                    return string.Empty;

                var obj_item = movies_item["movie"];

                if (obj_item == null)
                    return string.Empty;

                JsonObject json_item = null;

                if (obj_item is JsonArray)
                    json_item = (JsonObject) (((JsonArray)obj_item)[0]);
                else if (obj_item is JsonObject)
                    json_item = (JsonObject)obj_item;

                VideoInfo video_info = (VideoInfo)track.ExternalObject;
                video_info.Title = json_item["name"] as string;
                video_info.OriginalTitle = json_item["original_name"] as string;
                video_info.AlternativeTitle = json_item["alternative_name"] as string;
                video_info.Language = json_item["language"] as string;
                video_info.InfoUrl = json_item["url"] as string;
                video_info.ImDbId = json_item["imdb_id"] as string;
                video_info.ReleaseDate = DateTime.ParseExact (json_item["released"] as string, "yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat);
                video_info.Summary = json_item["overview"] as string;
                video_info.VideoType = (int)videoType.Movie;
                video_info.ExternalVideoId = json_item["id"] as string;
                video_info.Save ();

                json_item = (JsonObject)json_item["images"];
                obj_item = json_item["image"];

                if (obj_item == null)
                    return string.Empty;

                json_item = null;

                if (obj_item is JsonArray) {
                    foreach (object o in (JsonArray)obj_item) {
                        if (LoadImage ((JsonObject)o, coverArtId))
                            break;
                    }
                } else if (obj_item is JsonObject)
                    LoadImage ((JsonObject)obj_item, coverArtId);

                return json_item["id"] as string;
            }
            catch (Exception e) {
               Log.DebugException (e);
               return string.Empty;
            }
        }

        private bool LoadImage (JsonObject obj, string cover_art_id)
        {
            var attr = (JsonObject)obj["@attr"];

            string image_type = attr["type"] as string;
            string image_size = attr["size"] as string;
            string image_url = attr["url"] as string;

            if (image_type != "poster" && image_size != "thumb")
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

        private void GetInfo (DatabaseTrackInfo track, string tmdb_id)
        {
            /*HttpRequest request = new HttpRequest (string.Format("http://api.themoviedb.org/2.1/Movie.getInfo/{0}/xml/{1}/{2}", CultureInfo.CurrentCulture.TwoLetterISOLanguageName, API_KEY, tmdb_id));
            try {
                Stream response_stream = request.GetResponseStream ();

                Deserializer deserializer = new Deserializer (response_stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

            //TODO to get casting and few other things...
                
            }
            catch (Exception e) {
               Log.DebugException (e);
            }*/
        }
    }
}

