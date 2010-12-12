//
// NowPlayingInterface.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2008-2010 Novell, Inc.
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

using Mono.Unix;
using Gtk;

using Banshee.ServiceStack;
using Banshee.PlatformServices;
using Banshee.Sources;
using Banshee.Gui;
using Banshee.Sources.Gui;

namespace Banshee.NowPlaying
{
    public class NowPlayingInterface : VBox, ISourceContents
    {
        private NowPlayingSource source;
        private Hyena.Widgets.RoundedFrame frame;
        private Gtk.Window video_window;
        private Gtk.Window primary_window;
        private FullscreenAdapter fullscreen_adapter;
        private ScreensaverManager screensaver;
        
        internal NowPlayingContents Contents { get; private set; }

        public NowPlayingInterface ()
        {
            GtkElementsService service = ServiceManager.Get<GtkElementsService> ();
            primary_window = service.PrimaryWindow;

            Contents = new NowPlayingContents ();
            Contents.ButtonPressEvent += (o, a) => {
                switch (a.Event.Type) {
                case Gdk.EventType.TwoButtonPress:
                    var iaservice = ServiceManager.Get<InterfaceActionService> ();
                    var action = iaservice.ViewActions["FullScreenAction"] as Gtk.ToggleAction;
                    if (action != null && action.Sensitive) {
                        action.Active = !action.Active;
                    }
                    break;
                case Gdk.EventType.ButtonPress:
                    if (ServiceManager.PlayerEngine.IsMenu) {
                        ServiceManager.PlayerEngine.NotifyMouseButtonPressed ((int)a.Event.Button, a.Event.X, a.Event.Y);
                    }
                    break;
                case Gdk.EventType.ButtonRelease:
                    if (ServiceManager.PlayerEngine.IsMenu) {
                        ServiceManager.PlayerEngine.NotifyMouseButtonReleased ((int)a.Event.Button, a.Event.X, a.Event.Y);
                    }
                    break;
                }
            };

            //TODO stop tracking mouse when no more in menu
            Contents.MotionNotifyEvent += (o, args) => {
                if (ServiceManager.PlayerEngine.IsMenu) {
                    ServiceManager.PlayerEngine.NotifyMouseMove (args.Event.X, args.Event.Y);
                }
            };

            Contents.KeyPressEvent += delegate(object o, KeyPressEventArgs args) {
                if (!ServiceManager.PlayerEngine.IsMenu) {
                    return;
                }
                switch (args.Event.Key) {
                    case Gdk.Key.leftarrow:
                    case Gdk.Key.KP_Left:
                    case Gdk.Key.Left:
                        ServiceManager.PlayerEngine.NavigateToLeftMenu ();
                        break;
                    case Gdk.Key.rightarrow:
                    case Gdk.Key.KP_Right:
                    case Gdk.Key.Right:
                        ServiceManager.PlayerEngine.NavigateToRightMenu ();
                        break;
                    case Gdk.Key.uparrow:
                    case Gdk.Key.KP_Up:
                    case Gdk.Key.Up:
                        ServiceManager.PlayerEngine.NavigateToUpMenu ();
                        break;
                    case Gdk.Key.downarrow:
                    case Gdk.Key.KP_Down:
                    case Gdk.Key.Down:
                        ServiceManager.PlayerEngine.NavigateToDownMenu ();
                        break;
                    case Gdk.Key.Break:
                    case Gdk.Key.KP_Enter:
                    case Gdk.Key.Return:
                        ServiceManager.PlayerEngine.ActivateCurrentMenu ();
                        break;
                }
            };

            // This is my really sweet hack - it's where the video widget
            // is sent when the source is not active. This keeps the video
            // widget from being completely destroyed, causing problems with
            // its internal windowing and GstXOverlay. It's also conveniently
            // the window that is used to do fullscreen video. Sweeeeeeeeeet.
            video_window = new FullscreenWindow (service.PrimaryWindow);
            video_window.Hidden += OnFullscreenWindowHidden;
            video_window.Realize ();
            video_window.Add (Contents);

            frame = new Hyena.Widgets.RoundedFrame ();
            frame.SetFillColor (new Cairo.Color (0, 0, 0));
            frame.DrawBorder = false;
            frame.Show ();

            PackStart (frame, true, true, 0);

            fullscreen_adapter = new FullscreenAdapter ();
            fullscreen_adapter.SuggestUnfullscreen += OnAdapterSuggestUnfullscreen;
            screensaver = new ScreensaverManager ();
        }

        public override void Dispose ()
        {
            base.Dispose ();
            fullscreen_adapter.SuggestUnfullscreen -= OnAdapterSuggestUnfullscreen;
            fullscreen_adapter.Dispose ();
            screensaver.Dispose ();
        }

        private void MoveVideoExternal (bool hidden)
        {
            if (Contents.Parent != video_window) {
                Contents.Visible = !hidden;
                Contents.Reparent (video_window);
            }
        }

        private void MoveVideoInternal ()
        {
            if (Contents.Parent != frame) {
                Contents.Reparent (frame);
                Contents.Show ();
            }
        }

        protected override void OnRealized ()
        {
            base.OnRealized ();
            MoveVideoInternal ();
        }

        protected override void OnUnrealized ()
        {
            MoveVideoExternal (false);
            base.OnUnrealized ();
        }

#region Video Fullscreen Override

        private ViewActions.FullscreenHandler previous_fullscreen_handler;
        private bool primary_window_is_fullscreen;

        private void DisableFullscreenAction ()
        {
            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.ToggleAction action = service.ViewActions["FullScreenAction"] as Gtk.ToggleAction;
            if (action != null) {
                action.Active = false;
            }
        }

        internal void OverrideFullscreen ()
        {
            FullscreenHandler (false);

            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            if (service == null || service.ViewActions == null) {
                return;
            }

            previous_fullscreen_handler = service.ViewActions.Fullscreen;
            primary_window_is_fullscreen = (primary_window.GdkWindow.State & Gdk.WindowState.Fullscreen) != 0;
            service.ViewActions.Fullscreen = FullscreenHandler;
            DisableFullscreenAction ();
        }

        internal void RelinquishFullscreen ()
        {
            FullscreenHandler (false);

            InterfaceActionService service = ServiceManager.Get<InterfaceActionService> ();
            if (service == null || service.ViewActions == null) {
                return;
            }

            service.ViewActions.Fullscreen = previous_fullscreen_handler;
        }

        private void OnFullscreenWindowHidden (object o, EventArgs args)
        {
            MoveVideoInternal ();
            DisableFullscreenAction ();
        }

        private bool is_fullscreen;

        private void FullscreenHandler (bool fullscreen)
        {
            // Note: Since the video window is override-redirect, we
            // need to fullscreen the main window, so the window manager
            // actually knows we are actually doing stuff in fullscreen
            // here. The original primary window fullscreen state is
            // stored, so when we can restore it appropriately

            is_fullscreen = fullscreen;

            if (fullscreen) {
                primary_window.Fullscreen ();

                MoveVideoExternal (true);
                video_window.Show ();
                fullscreen_adapter.Fullscreen (video_window, true);
                screensaver.Inhibit ();
            } else {
                video_window.Hide ();
                screensaver.UnInhibit ();
                fullscreen_adapter.Fullscreen (video_window, false);
                video_window.Hide ();

                if (!primary_window_is_fullscreen) {
                    primary_window.Unfullscreen ();
                }
            }
        }

        private void OnAdapterSuggestUnfullscreen (object o, EventArgs args)
        {
            if (is_fullscreen) {
                Hyena.Log.Debug ("Closing fullscreen at request of the FullscreenAdapter");
                FullscreenHandler (false);
            }
        }

#endregion

#region ISourceContents

        public bool SetSource (ISource src)
        {
            this.source = source as NowPlayingSource;
            return this.source != null;
        }

        public ISource Source {
            get { return source; }
        }

        public void ResetSource ()
        {
            source = null;
        }

        public Widget Widget {
            get { return this; }
        }

#endregion

    }
}
