//
// ContextPage.cs
//
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright (C) 2011 Olivier Dufour
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;

using Banshee.ContextPane;
using Gtk;
using Banshee.I18n;

namespace Banshee.Video
{
    public class ContextPage : BaseContextPage
    {
        public ContextPage ()
        {
            Id = "Video";
            Name = Catalog.GetString ("Video Detail");
            IconNames = new string[] { "video", "browser", "internet" };
        }

        public override void SetTrack (Banshee.Collection.TrackInfo track)
        {
            VideoInfo video = track.ExternalObject as VideoInfo;
            view.VideoInfo = video;
            view.Members = CastingMember.Provider.FetchAllMatching ("VideoID = ?", video.DbId);
            view.Refresh ();
        }

        internal void SetState (ContextState state)
        {
            State = state;
        }

        private VideoDetailPanel view;
        public override Widget Widget {
            get { return view ?? (view = new VideoDetailPanel (this)); }
        }
    }
}
