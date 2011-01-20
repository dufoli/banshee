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
using System.Net;
using System.IO;
using System.Globalization;
using System.Collections.Generic;

using Banshee.Base;
using Banshee.Web;
using Banshee.Metadata;
using Banshee.Collection.Database;

using Hyena.Json;
using Hyena;

namespace Banshee.Video.Metadata
{
    public class TvdbMetadataProvider : MetadataServiceJob
    {
        private DatabaseTrackInfo track;

        public TvdbMetadataProvider (DatabaseTrackInfo track) : base ()
        {
            this.track = track;
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

            GetMetadata (track);

        }
        //private readonly string API_KEY = "897F1CA903D4703A";

        private enum serverCapability : int
        {
            xmlfile= 1,
            bannerfile = 2,
            zipfile = 4
        }

        private struct Mirror
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
            //TODO
            /*
            <Mirrors>
              <Mirror>
                <id>1</id>
                <mirrorpath>http://thetvdb.com</mirrorpath>
                <typemask>7</typemask>
              </Mirror>
            </Mirrors>*/

            //HttpWebRequest request = HttpWebRequest.Create (string.Format ("http://www.thetvdb.com/api/{0}/mirrors.xml", API_KEY));
            mirrors.Add (new Mirror("http://thetvdb.com", serverCapability.bannerfile | serverCapability.xmlfile | serverCapability.zipfile));
        }

        private string GetSerieId (DatabaseTrackInfo track)
        {
            //TODO get language
            //CultureInfo.CurrentCulture.

            //TODO get title from videoInfo.title to remove season/episode part
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/GetSeries.php?seriesname={0}&language={1}", track.TrackTitle, "all"));
            try {
                Stream response_stream = request.GetResponseStream ();
                ICSharpCode.SharpZipLib.Zip.ZipInputStream stream = new ICSharpCode.SharpZipLib.Zip.ZipInputStream (response_stream);
                //TODO How get the good file if the ziped file contain more than one file ?
                Deserializer deserializer = new Deserializer (stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null)
                    return string.Empty;

                var items = json_obj["Items"] as JsonObject;
                var obj_item = items["Item"];
                JsonObject json_item = null;

                if (obj_item is JsonArray)
                    json_item = (JsonObject) (((JsonArray)obj_item)[0]);
                else if (obj_item is JsonObject)
                    json_item = (JsonObject)obj_item;

                //store imdbid in musicbrainzid
                track.MusicBrainzId = json_item["seriesid"] as string;
                track.Save ();
                return track.MusicBrainzId;
            }
            catch (Exception e) {
               Log.DebugException (e);
               return string.Empty;
            }
        }

        private string GetSerieMetadata (DatabaseTrackInfo track)
        {
            //TODO get title from videoInfo.title to remove season/episode part
            HttpRequest request = new HttpRequest (string.Format("http://www.thetvdb.com/api/GetSeries.php?seriesname={0}&language={1}", track.TrackTitle, "all"));
            // <mirrorpath_zip>/api/<apikey>/series/<seriesid>/all/<language>.zip

            try {
                Stream response_stream = request.GetResponseStream ();

                Deserializer deserializer = new Deserializer (response_stream);
                object obj = deserializer.Deserialize ();
                JsonObject json_obj = obj as Hyena.Json.JsonObject;

                if (json_obj == null)
                    return string.Empty;

                var items = json_obj["Items"] as JsonObject;
                var obj_item = items["Item"];
                JsonObject json_item = null;

                if (obj_item is JsonArray)
                    json_item = (JsonObject) (((JsonArray)obj_item)[0]);
                else if (obj_item is JsonObject)
                    json_item = (JsonObject)obj_item;

                //store imdbid in musicbrainzid
                track.MusicBrainzId = json_item["seriesid"] as string;
                track.Save ();
                return track.MusicBrainzId;
            }
            catch (Exception e) {
               Log.DebugException (e);
               return string.Empty;
            }
        }

        private string GetEpisodeMetadata (DatabaseTrackInfo track)
        {
            return string.Empty;
            //TODO
        }

        void GetMetadata (DatabaseTrackInfo track)
        {
            //string imageUrl;
            //string serieId = GetSerieId (track);
            GetSerieMetadata (track);
            GetEpisodeMetadata (track);
            //TODO
            /*imageUrl = string.Empty;
            if (SaveHttpStreamCover (new Uri (imageUrl), cover_art_id, null)) {
                Banshee.Sources.Source src = ServiceManager.SourceManager.ActiveSource;
                if (src != null && (src is VideoLibrarySource || src.Parent is VideoLibrarySource)) {
                    (src as Banshee.Sources.DatabaseSource).Reload ();
                }
                return;
            }*/
        }
    }
}

