// 
// VideoService.cs
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
using System.Text.RegularExpressions;

using Banshee.Collection.Database;
using Banshee.Collection;
using Banshee.MediaEngine;
using Banshee.ServiceStack;


namespace Banshee.Video
{
    public class VideoService : IExtensionService
    {
        public VideoService ()
        {
        }


        public string ArtworkIdFor (DatabaseTrackInfo track)
        {
            VideoInfo vi = GetExternalObject (track);
            if (vi == null) return null;
            string digest = Banshee.Base.CoverArtSpec.Digest (vi.Title);
            return digest == null ? null : String.Format ("video-{0}", digest);
        }

        private VideoInfo GetExternalObject (DatabaseTrackInfo track)
        {
            return VideoInfo.Provider.FetchFirstMatching ("VideoID = ?", source.DbId, track.ExternalId);
        }

        private readonly Regex regexp = new Regex (@"(.*)[._][\[]?[s]?([0-9]+)[\]]?[._]?[\[]?[-EeXx]([0-9]+)[\]]?[._].*");
        private readonly Regex regexp2 = new Regex (@"(.*)[._]([0-9])([0-9][0-9])[._].*");

        private VideoLibrarySource source;

        public void Initialize ()
        {
            source = new VideoLibrarySource ();
            source.AddChildSource (new TvShowGroupSource (source));
            source.AddChildSource (new MovieGroupSource (source));
            ServiceManager.SourceManager.AddSource (source);
            RefreshTracks ();
            source.TracksAdded += OnTracksAdded;
        }

        void OnTracksAdded (Sources.Source sender, Sources.TrackEventArgs args)
        {
            RefreshTracks ();
            UpdateMetadata ();
        }

        void RefreshTracks ()
        {
            foreach (DatabaseTrackInfo track in DatabaseTrackInfo.Provider.FetchAllMatching ("PrimarySourceID = ? AND ExternalID = 0", source.DbId)) {
                var video = new VideoInfo ();
                // Temporary set title but will be updated by webservice
                video.Title = track.TrackTitle;
                ParseName (track, video, track.TrackTitle);
                video.Save ();
                track.ExternalId = video.DbId;
                track.Save ();
            }
        }

        public void ParseName (DatabaseTrackInfo track, VideoInfo video, string name)
        {
            int episode;
            Match match = regexp.Match (name);
            if (match.Success) {
                track.ArtistName = match.Captures[0].Value;
                track.AlbumTitle = String.Format ("Season {0}", match.Captures[1].Value);
                Int32.TryParse (match.Captures[2].Value, out episode);
                track.TrackNumber = episode;
                track.MediaAttributes |= TrackMediaAttributes.TvShow;
                return;
            }
            else {
                match = regexp2.Match (name);
                if (match.Success) {
                    track.ArtistName = match.Captures[0].Value;
                    track.AlbumTitle = String.Format ("Season {0}", match.Captures[1].Value);
                    Int32.TryParse (match.Captures[2].Value, out episode);
                    track.TrackNumber = episode;
                    track.MediaAttributes |= TrackMediaAttributes.TvShow;
                    return;
                }
            }
            //by default video is movie if not tv show
            track.MediaAttributes |= TrackMediaAttributes.Movie;

            //TODO multi-part tv show
            /*The defaults will match the following structures/file name formats
            foo.s01e01.*
            foo.s01.e01.*
            foo.s01_e01.*
            foo_[s01]_[e01]_*
            foo.1x01.*
            foo.101.*

            Defaults for two-part TV Show Episode will match
            foo.s01e01-02.*
            foo_[s01]_[e01-02]_*
            foo.1x01.1x02.*

            Same with three- or four-part TV Show Episode
            foo.s01e01-02-03-04.*
            foo_[s01]_[e01-02-03-04]_*
            foo.1x01.1x02.1x03.1x04.*
            */

        }

        void UpdateMetadata ()
        {

            //TODO call metadate service
            //ServiceStack.ServiceManager.JobScheduler.Add (job);
        }

        public void Dispose ()
        {
            ServiceManager.SourceManager.RemoveSource (source);
        }

        public string ServiceName {get{ return "VideoService";} }
    }
}

