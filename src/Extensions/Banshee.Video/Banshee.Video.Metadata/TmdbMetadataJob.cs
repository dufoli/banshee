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
using System.Xml;
using System.Net;
using System.Web;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Metadata;
using Banshee.Web;

using Hyena;

namespace Banshee.Video.Metadata
{
    public class TmdbMetadataJob : MetadataServiceJob
    {
        private DatabaseTrackInfo track;

        public TmdbMetadataJob (IBasicTrackInfo track)
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

            string tmdb_id = SearchMovie (track);
            GetInfo (track, tmdb_id);
        }

        private readonly string API_KEY = "0e4632ee0881dce13dfc053ec42b819b";

        private string SearchMovie (DatabaseTrackInfo track)
        {
            //TODO remove all not needed part or parasit char
            string name = track.TrackTitle.Replace ('.', ' ').Replace ('_', ' ');
            string url = string.Format("http://api.themoviedb.org/2.1/Movie.search/{0}/xml/{1}/{2}", CultureInfo.CurrentCulture.Name, API_KEY, HttpUtility.UrlEncode (name));
            Log.Debug (url);
            HttpRequest request = new HttpRequest (url);
            XmlDocument doc = new XmlDocument();
            try {
                request.GetResponse ();

                using (Stream stream = request.GetResponseStream ()) {
                    doc.Load (stream);
                }
                Log.Debug (doc.OuterXml);

                XmlNode movie_node = doc.DocumentElement.SelectSingleNode ("/OpenSearchDescription/movies/movie");
                if (movie_node == null) {
                    return String.Empty;
                }
                VideoInfo video_info = (VideoInfo)track.ExternalObject;
                video_info.Title = movie_node["name"].InnerXml;
                video_info.OriginalTitle = movie_node["original_name"].InnerXml;
                video_info.AlternativeTitle = movie_node["alternative_name"].InnerXml;
                video_info.Language = movie_node["language"].InnerXml;
                video_info.InfoUrl = movie_node["url"].InnerXml;
                video_info.ImDbId = movie_node["imdb_id"].InnerXml;
                video_info.ReleaseDate = DateTime.ParseExact (movie_node["released"].InnerXml, "yyyy-MM-dd", CultureInfo.InvariantCulture.DateTimeFormat);
                video_info.Summary = movie_node["overview"].InnerXml;
                video_info.VideoType = (int)videoType.Movie;
                video_info.ExternalVideoId = movie_node["id"].InnerXml;
                video_info.ParentId = 0;
                video_info.Save ();

                foreach (XmlNode n in movie_node.SelectNodes ("//image")) {
                    if (LoadImage (n, video_info.ArtworkId))
                        break;
                }

                return movie_node["id"].InnerXml;
            } catch (WebException ex) {
                Log.Error (ex.Status.ToString ());
                Log.DebugException (ex);
            } catch (Exception e) {
               Log.Error (doc.InnerXml);
               Log.DebugException (e);
            }
            return String.Empty;
        }

        private bool LoadImage (XmlNode obj, string cover_art_id)
        {
            string image_type = obj.Attributes["type"].Value;
            string image_size = obj.Attributes["size"].Value;
            string image_url = obj.Attributes["url"].Value;

            if (image_type != "poster" && image_size != "thumb")
                return false;
            Log.Debug ("Load movie image");
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
                XmlDocument doc = new XmlDocument();
                using (Stream stream = request.GetResponseStream ()) {
                    doc.Load (stream);
                }

                XmlNode movie_node = doc.DocumentElement.SelectSingleNode ("TODO");


            //TODO to get casting and few other things...
                
            }
            catch (Exception e) {
               Log.DebugException (e);
            }*/
        }
    }
}

