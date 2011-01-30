// 
// VideoSourceContents.cs
// 
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
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
using Banshee.Sources.Gui;
using Banshee.Collection.Gui;
using Banshee.Sources;
using Hyena.Data.Gui;
using Banshee.Collection;
using Hyena.Data;
namespace Banshee.Video
{
    public class VideoSourceContents : FilteredListSourceContents, ITrackModelSourceContents
    {
        private TrackListView track_view;
        private VideoInfoView video_view;

        public VideoSourceContents () : base ("video")
        {
        }

        protected override void InitializeViews ()
        {
            SetupMainView (track_view = new TrackListView ());
            SetupFilterView (video_view = new VideoInfoView ());
        }

        protected override void ClearFilterSelections ()
        {
            if (video_view.Model != null) {
                video_view.Selection.Clear ();
            }
        }

        protected override bool ActiveSourceCanHasBrowser {
            get { return true; }
        }

        protected override string ForcePosition {
            get { return "top"; }
        }

        #region Implement ISourceContents

        public override bool SetSource (ISource source)
        {
            DatabaseSource track_source = source as DatabaseSource;
            if (track_source == null) {
                return false;
            }

            this.source = source;

            SetModel (track_view, track_source.TrackModel);

            foreach (IListModel model in track_source.CurrentFilters) {
                if (model is VideoInfoModel) {
                    Hyena.Log.Debug ("SET MODEL");
                    SetModel (video_view, (model as IListModel<VideoInfo>));
                }
            }

            return true;
        }

        public override void ResetSource ()
        {
            source = null;
            SetModel (track_view, null);
            SetModel (video_view, null);
        }

        #endregion

        #region ITrackModelSourceContents implementation

        public IListView<TrackInfo> TrackView {
            get { return track_view; }
        }

        #endregion
    }
}
