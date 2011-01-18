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
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.Video
{
    public class VideoService : IExtensionService
    {
        public VideoService ()
        {
        }

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
        }

        void RefreshTracks ()
        {
            //TODO filter to get only track with no infos
            //source.DatabaseTrackModel
            //DatabaseTackInfo tra;
            //TODO get all tracks which have not artwork/data
            //source.DatabaseTrackModel.
            // Use custom 
        }

        public void Dispose ()
        {
            ServiceManager.SourceManager.RemoveSource (source);
        }

        public string ServiceName {get{ return "VideoService";} }
    }
}

