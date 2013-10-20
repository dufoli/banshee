//
// MediaPanelContents.cs
// 
// Author:
//   Aaron Bockover <abockover@novell.com>
// 
// Copyright 2010 Novell, Inc.
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
using System.Collections.Generic;

using Mono.Unix;
using Gtk;

using Hyena;
using Hyena.Data;
using Hyena.Gui;
using Hyena.Data.Gui;

using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Collection;
using Banshee.Collection.Gui;
using Banshee.MediaEngine;
using Banshee.Gui;
using Banshee.Gui.Widgets;

namespace Banshee.MediaPanel
{
    public class MediaPanelContents : Grid, ITrackModelSourceContents
    {
        private ArtistListView artist_view;
        private AlbumListView album_view;
        private TerseTrackListView track_view;

        private SourceComboBox source_combo_box;
        private SearchEntry search_entry;
        private TrackInfoDisplay track_info_display;

        private ISource source;
        private Dictionary<object, double> model_positions = new Dictionary<object, double> ();

        protected MediaPanelContents (IntPtr raw) : base (raw)
        {
        }

        public MediaPanelContents () : base ()
        {
            BorderWidth = 5;
            RowSpacing = 6;
            ColumnSpacing = 7;
            RedrawOnAllocate = true;
            AppPaintable = true;

            BuildLibrary ();
            BuildNowPlaying ();
            ConnectEvents ();

            SetSource (ServiceManager.SourceManager.ActiveSource);
        }

        private void ConnectEvents ()
        {
            ServiceManager.SourceManager.ActiveSourceChanged += OnActiveSourceChanged;

            ServiceManager.Get<InterfaceActionService> ().TrackActions ["SearchForSameArtistAction"].Activated += OnProgrammaticSearch;
            ServiceManager.Get<InterfaceActionService> ().TrackActions ["SearchForSameAlbumAction"].Activated += OnProgrammaticSearch;

            ServiceManager.PlayerEngine.ConnectEvent ((args) => track_info_display.Visible =
                ServiceManager.PlayerEngine.CurrentState != PlayerState.Idle,
                PlayerEvent.StateChange | PlayerEvent.StartOfStream);

            source_combo_box.Model.Filter = (source) => source is ITrackModelSource;
            source_combo_box.Model.Refresh ();
            source_combo_box.UpdateActiveSource ();

            search_entry.Changed += OnSearchEntryChanged;

            artist_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
            album_view.SelectionProxy.Changed += OnBrowserViewSelectionChanged;
        }

#region UI Construction

        private void BuildLibrary ()
        {
            var box = new HeaderBox () { Title = Catalog.GetString ("Library") };

            // Build the Library Header
            var header = new HBox () {
                Spacing = 5,
                BorderWidth = 5
            };

            var app_button = new Button (new Image () {
                IconSize = (int)IconSize.LargeToolbar,
                IconName = "media-player-banshee"
            }) {
                TooltipText = Catalog.GetString ("Launch the Banshee Media Player")
            };
            app_button.Clicked += (o, e) => {
                ServiceManager.SourceManager.SetActiveSource (ServiceManager.SourceManager.MusicLibrary);
                ServiceManager.Get<MediaPanelService> ().PresentPrimaryInterface ();
            };

            header.PackStart (source_combo_box = new SourceComboBox (), false, false, 0);
            header.PackStart (app_button, false, false, 0);
            header.PackStart (search_entry = new SearchEntry (), true, true, 0);
            box.PackStartHighlighted (header, false, false, 0, HeaderBox.HighlightFlags.Background);

            // Build the Library Views
            var pane = new Paned (Orientation.Horizontal);
            pane.Pack1 (SetupView (artist_view = new ArtistListView () {
                    Name = "media-panel-artists",
                    WidthRequest = 150,
                    DoNotRenderNullModel = true
                }), false, false);
            pane.Pack2 (SetupView (album_view = new AlbumListView () {
                Name = "media-panel-albums",
                DoNotRenderNullModel = true
            }), true, true);
            box.PackStart (pane, true, true, 0);
            box.Hexpand = box.Vexpand = true;
            box.Halign = box.Valign = Align.Fill;

            Attach (box, 0, 0, 1, 1);
        }

        private void BuildNowPlaying ()
        {
            var box = new HeaderBox () { Title = Catalog.GetString ("Now Playing") };

            var seek_slider = new ConnectedSeekSlider (SeekSliderLayout.Horizontal);
            seek_slider.StreamPositionLabel.FormatString = "<small>{0}</small>";

            track_info_display = new TrackInfoDisplay () {
                HeightRequest = 64,
                NoShowAll = true
            };

            track_view = new TerseTrackListView () {
                Name = "media-panel-tracks",
                WidthRequest = 220
            };
            track_view.ColumnController.Insert (new Column (null, "indicator",
                new ColumnCellStatusIndicator (null), 0.05, true, 20, 20), 0);

            box.PackStartHighlighted (track_info_display, false, false, 0, HeaderBox.HighlightFlags.Background);
            box.PackStartHighlighted (seek_slider, false, false, 0, HeaderBox.HighlightFlags.Background);
            box.PackStart (SetupView (track_view), true, true, 0);
            box.PackStartHighlighted (new PlaybackBox (), false, false, 0, HeaderBox.HighlightFlags.TopLine);
            box.Vexpand = true;
            box.Valign = Align.Fill;

            Attach (box, 1, 0, 1, 1);
        }

        private ScrolledWindow SetupView (Widget view)
        {
            var scrolled = new ScrolledWindow () {
                VscrollbarPolicy = PolicyType.Automatic,
                HscrollbarPolicy = PolicyType.Never,
                ShadowType = ShadowType.None
            };
            scrolled.Add (view);
            return scrolled;
        }

#endregion

#region Event Handlers

        private void OnProgrammaticSearch (object o, EventArgs args)
        {
            Source source = ServiceManager.SourceManager.ActiveSource;
            search_entry.Ready = false;
            search_entry.Query = source.FilterQuery;
            search_entry.Ready = true;
        }

        private void OnBrowserViewSelectionChanged (object o, EventArgs args)
        {
            // Scroll the raising filter view to the top if "all" is selected
            var selection = (Hyena.Collections.Selection)o;
            if (!selection.AllSelected) {
                return;
            }

            if (artist_view.Selection == selection) {
                artist_view.ScrollTo (0);
            } else if (album_view.Selection == selection) {
                album_view.ScrollTo (0);
            }
        }

        private void OnSearchEntryChanged (object o, EventArgs args)
        {
            var source = ServiceManager.SourceManager.ActiveSource;
            if (source == null) {
                return;
            }

            source.FilterType = (TrackFilterType)search_entry.ActiveFilterID;
            source.FilterQuery = search_entry.Query;
        }

        private void OnActiveSourceChanged (SourceEventArgs args)
        {
            ThreadAssist.ProxyToMain (delegate {
                var source = ServiceManager.SourceManager.ActiveSource;

                search_entry.Ready = false;
                search_entry.CancelSearch ();
                search_entry.SearchSensitive = source != null && source.CanSearch;

                if (source != null && source.FilterQuery != null) {
                    search_entry.Query = source.FilterQuery;
                    search_entry.ActivateFilter ((int)source.FilterType);
                }

                ResetSource ();
                SetSource (source);

                search_entry.Ready = true;

                if (source != null && source != ServiceManager.SourceManager.MusicLibrary
                     && source.Parent != ServiceManager.SourceManager.MusicLibrary) {
                    ServiceManager.Get<MediaPanelService> ().PresentPrimaryInterface ();
                }
            });
        }

        internal void SyncSearchEntry ()
        {
            OnActiveSourceChanged (null);
        }

#endregion

#region View<->Model binding

        private void SetModel<T> (IListModel<T> model)
        {
            ListView<T> view = FindListView <T> ();
            if (view != null) {
                SetModel (view, model);
            } else {
                Hyena.Log.DebugFormat ("Unable to find view for model {0}", model);
            }
        }

        private void SetModel<T> (ListView<T> view, IListModel<T> model)
        {
            if (view.Model != null) {
                model_positions[view.Model] = view.Vadjustment.Value;
            }

            if (model == null) {
                view.SetModel (null);
                return;
            }

            if (!model_positions.ContainsKey (model)) {
                model_positions[model] = 0.0;
            }

            view.SetModel (model, model_positions[model]);
        }

        private ListView<T> FindListView<T> ()
        {
            foreach (var view in new IListView [] { artist_view, album_view, track_view }) {
                if (view is ListView<T>) {
                    return (ListView<T>)view;
                }
            }
            return null;
        }

#endregion

#region ISourceContents

        public bool SetSource (ISource source)
        {
            var track_source = source as ITrackModelSource;
            var filterable_source = source as IFilterableSource;
            if (track_source == null) {
                return false;
            }

            this.source = source;

            SetModel (track_view, track_source.TrackModel);

            if (filterable_source != null && filterable_source.CurrentFilters != null) {
                foreach (var model in filterable_source.CurrentFilters) {
                    if (model is IListModel<ArtistInfo>) {
                        SetModel (artist_view, (model as IListModel<ArtistInfo>));
                    } else if (model is IListModel<AlbumInfo>) {
                        SetModel (album_view, (model as IListModel<AlbumInfo>));
                    }
                }
            }

            return true;
        }

        public void ResetSource ()
        {
            source = null;
            SetModel (track_view, null);
            SetModel (artist_view, null);
            SetModel (album_view, null);
            track_view.HeaderVisible = false;
        }

        public ISource Source {
            get { return source; }
        }

        public Widget Widget {
            get { return this; }
        }

#endregion

#region ITrackModelSourceContents

        public IListView<TrackInfo> TrackView {
            get { return track_view; }
        }

#endregion

    }
}
