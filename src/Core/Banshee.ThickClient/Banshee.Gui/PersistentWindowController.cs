//
// PersistentWindowController.cs
//
// Authors:
//   Gabriel Burt <gburt@novell.com>
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

using Banshee.Configuration;

namespace Banshee.Gui
{
    [Flags]
    public enum WindowPersistOptions
    {
        Size = 1,
        Position = 2,
        All = Size | Position
    }

    public class PersistentWindowController
    {
        private Gtk.Window window;
        private WindowPersistOptions options;
        private uint timer_id = 0;
        private bool pending_changes;

        public PersistentWindowController (Gtk.Window window, WindowConfiguration windowConfig, WindowPersistOptions options)
        {
            this.window = window;
            this.options = options;
            this.window_config = windowConfig;

            window.ConfigureEvent += OnChanged;
            window.WindowStateEvent += OnChanged;
            window.DestroyEvent += OnDestroy;
        }

        private void OnDestroy (object o, EventArgs args)
        {
            window.DestroyEvent -= OnDestroy;
            window.ConfigureEvent -= OnChanged;
            window.StateChanged -= OnChanged;
        }

        [GLib.ConnectBefore]
        private void OnChanged (object o, EventArgs args)
        {
            Save ();
        }

        public void Restore ()
        {
            if ((options & WindowPersistOptions.Size) != 0) {
                int width = window_config.WidthSchema.Get ();
                int height = window_config.HeightSchema.Get ();

                if (width != 0 && height != 0) {
                    window.Resize (width, height);
                }
            }

            if ((options & WindowPersistOptions.Position) != 0) {
                int x = window_config.XPosSchema.Get ();
                int y = window_config.YPosSchema.Get ();

                if (x == 0 && y == 0) {
                    window.SetPosition (Gtk.WindowPosition.Center);
                } else {
                    window.Move (x, y);
                }
            }

            if ((options & WindowPersistOptions.Size) != 0) {
                if (window_config.MaximizedSchema.Get ()) {
                    window.Maximize ();
                } else {
                    window.Unmaximize ();
                }
            }
        }

        private int x, y, width, height;
        private bool maximized;

        public void Save ()
        {
            if (window == null || !window.Visible || !window.IsMapped || window.Window == null) {
                return;
            }

            maximized = (window.Window.State & Gdk.WindowState.Maximized) != 0;
            window.GetPosition (out x, out y);
            window.GetSize (out width, out height);

            if (timer_id == 0) {
                timer_id = GLib.Timeout.Add (250, OnTimeout);
            } else {
                pending_changes = true;
            }
        }

        private bool OnTimeout ()
        {
            if (pending_changes) {
                // Wait another 250ms to see if we've stopped getting updates yet
                pending_changes = false;
                return true;
            } else {
                InnerSave ();
                timer_id = 0;
                return false;
            }
        }

        private void InnerSave ()
        {
            if (maximized) {
                window_config.MaximizedSchema.Set (true);
                return;
            }

            if (x < 0 || y < 0 || width <= 0 || height <= 0) {
                 return;
            }

            window_config.MaximizedSchema.Set (false);
            window_config.XPosSchema.Set (x);
            window_config.YPosSchema.Set (y);
            window_config.WidthSchema.Set (width);
            window_config.HeightSchema.Set (height);
        }

        private WindowConfiguration window_config;
    }
}
