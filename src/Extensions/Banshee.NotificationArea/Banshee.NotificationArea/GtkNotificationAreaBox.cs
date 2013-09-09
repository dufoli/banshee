//
// GtkNotificationAreaBox.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2008 Novell, Inc.
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

using Banshee.Gui;
using Banshee.ServiceStack;
using Banshee.MediaEngine;

namespace Banshee.NotificationArea
{
    public class GtkNotificationAreaBox : StatusIcon, INotificationAreaBox
    {
        public event EventHandler Disconnected;
        public event EventHandler Activated;
        public event PopupMenuHandler PopupMenuEvent;

        private TrackInfoPopup popup;
        private bool can_show_popup = false;
        private bool cursor_over_trayicon = false;
        private bool hide_delay_started = false;
        
        public Widget Widget {
            get { return null; }
        }

        public GtkNotificationAreaBox (BaseClientWindow window)
        {
            Visible = false;
            IconName = (IconThemeUtils.HasIcon ("banshee-panel")) ?
                "banshee-panel" : Banshee.ServiceStack.Application.IconName;

            HasTooltip = true;
            base.Activate += delegate {OnActivated ();};
            base.PopupMenu += delegate {OnPopupMenuEvent ();};
            popup = new TrackInfoPopup ();
        }

        public void PositionMenu (Menu menu, out int x, out int y, out bool push_in)
        {
            StatusIcon.PositionMenu (menu, out x, out y, out push_in, Handle);
        }

        private void HidePopup ()
        {
            if (popup == null) {
                return;
            }

            popup.Hide ();
            popup.EnterNotifyEvent -= OnPopupEnterNotify;
            popup.LeaveNotifyEvent -= OnPopupLeaveNotify;
            popup.Destroy ();
            popup.Dispose ();
            popup = null;
        }

        private void ShowPopup ()
        {
            if (popup != null) {
                return;
            }

            popup = new TrackInfoPopup ();
            popup.EnterNotifyEvent += OnPopupEnterNotify;
            popup.LeaveNotifyEvent += OnPopupLeaveNotify;

            PositionPopup ();

            popup.Show ();
        }

        private void OnPopupEnterNotify (object o, EnterNotifyEventArgs args)
        {
            hide_delay_started = false;
        }

        private void OnPopupLeaveNotify (object o, LeaveNotifyEventArgs args)
        {
            Gdk.Rectangle rect;
            if (!popup.Intersect (new Gdk.Rectangle ((int)args.Event.X, (int)args.Event.Y, 1, 1), out rect)) {
                OnLeaveNotifyEvent (o, args);
            }
        }

        private void PositionPopup ()
        {
            int x, y;
            Gdk.Screen screen;
            Gdk.Rectangle area;
            Orientation orientation;

            GetGeometry (out screen, out area, out orientation);

            Gtk.Requisition popup_min_size, popup_natural_size;
            popup.GetPreferredSize (out popup_min_size, out popup_natural_size);

            bool on_bottom = area.Bottom + popup_natural_size.Height >= screen.Height;

            y = on_bottom
                ? area.Top - popup_natural_size.Height - 5
                : area.Bottom + 5;

            int monitor = screen.GetMonitorAtPoint (area.Left, y);
            var monitor_rect = screen.GetMonitorGeometry(monitor);

            x = area.Left - (popup_natural_size.Width / 2) + (area.Width / 2);

            if (x + popup_natural_size.Width >= monitor_rect.Right - 5) {
                x = monitor_rect.Right - popup_natural_size.Width - 5;
            } else if (x < monitor_rect.Left + 5) {
                x = monitor_rect.Left + 5;
            }

            popup.Move (x, y);
        }

        private void OnEnterNotifyEvent (object o, EnterNotifyEventArgs args)
        {
            hide_delay_started = false;
            cursor_over_trayicon = true;
            if (can_show_popup) {
                // only show the popup when the cursor is still over the
                // tray icon after 500ms
                GLib.Timeout.Add (500, delegate {
                    if (cursor_over_trayicon && can_show_popup) {
                        ShowPopup ();
                    }
                    return false;
                });
            }
        }

        private void OnLeaveNotifyEvent (object o, LeaveNotifyEventArgs args)
        {
            // Give the user half a second to move the mouse cursor to the popup.
            if (!hide_delay_started) {
                hide_delay_started = true;
                cursor_over_trayicon = false;
                GLib.Timeout.Add (500, delegate {
                    if (hide_delay_started) {
                        hide_delay_started = false;
                        HidePopup ();
                    }
                    return false;
                });
            }
        }

        public void OnPlayerEvent (PlayerEventArgs args)
        {
            switch (args.Event) {
                case PlayerEvent.StartOfStream:
                    can_show_popup = false;
                    break;

                case PlayerEvent.EndOfStream:
                    // only hide the popup when we don't play again after 250ms
                    GLib.Timeout.Add (250, delegate {
                        if (ServiceManager.PlayerEngine.CurrentState != PlayerState.Playing) {
                            can_show_popup = false;
                            HidePopup ();
                         }
                         return false;
                    });
                    break;
            }
        }

        protected override bool OnScrollEvent (Gdk.EventScroll evnt)
        {
            switch (evnt.Direction) {
                case Gdk.ScrollDirection.Up:
                    if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                        ServiceManager.PlayerEngine.Volume += (ushort)PlayerEngine.VolumeDelta;
                    } else if((evnt.State & Gdk.ModifierType.ShiftMask) != 0) {
                        ServiceManager.PlayerEngine.Position += PlayerEngine.SkipDelta;
                    } else {
                        ServiceManager.PlaybackController.Next ();
                    }
                    break;

                case Gdk.ScrollDirection.Down:
                    if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                        if (ServiceManager.PlayerEngine.Volume < (ushort)PlayerEngine.VolumeDelta) {
                            ServiceManager.PlayerEngine.Volume = 0;
                        } else {
                            ServiceManager.PlayerEngine.Volume -= (ushort)PlayerEngine.VolumeDelta;
                        }
                    } else if((evnt.State & Gdk.ModifierType.ShiftMask) != 0) {
                        ServiceManager.PlayerEngine.Position -= PlayerEngine.SkipDelta;
                    } else {
                        ServiceManager.PlaybackController.Previous ();
                    }
                    break;
            }
            return true;
        }

        protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
        {
            if (evnt.Type != Gdk.EventType.ButtonPress) {
                return false;
            }

            switch (evnt.Button) {
                case 1:
                    if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                        ServiceManager.PlaybackController.Next ();
                    } else {
                        OnActivated ();
                    }
                    break;
                case 2:
                    ServiceManager.PlayerEngine.TogglePlaying ();
                    break;
                case 3:
                    if ((evnt.State & Gdk.ModifierType.ControlMask) != 0) {
                        ServiceManager.PlaybackController.Next ();
                    } else {
                        OnPopupMenuEvent ();
                    }
                    break;
            }
            return true;
        }


        protected override bool OnQueryTooltip (int x, int y, bool keyboard_mode, Tooltip tooltip)
        {
            tooltip.Custom = popup;
            return true;
        }

        public void Show ()
        {
            Visible = true;
        }

        public void Hide ()
        {
            Visible = false;
        }

        protected virtual void OnActivated ()
        {
            EventHandler handler = Activated;
            if (handler != null) {
                handler (this, EventArgs.Empty);
            }
        }

        protected virtual void OnPopupMenuEvent ()
        {
            PopupMenuHandler handler = PopupMenuEvent;
            if (handler != null) {
                handler (this, new PopupMenuArgs ());
            }
        }
    }
}
