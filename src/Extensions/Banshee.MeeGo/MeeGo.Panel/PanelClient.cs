//
// PanelClient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009-2010 Novell, Inc.
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
using System.Runtime.InteropServices;

namespace MeeGo.Panel
{
    public class PanelClient : GLib.Object
    {
        [DllImport ("libmeego-panel")]
        private static extern IntPtr mpl_panel_client_get_type ();

        public static new GLib.GType GType {
            get { return new GLib.GType (mpl_panel_client_get_type ()); }
        }

        protected PanelClient () : base (IntPtr.Zero)
        {
            CreateNativeObject (new string [0], new GLib.Value [0]);
        }

        public PanelClient (IntPtr raw) : base (raw)
        {
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_unload (IntPtr panel);

        public void Unload ()
        {
            mpl_panel_client_unload (Handle);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_ready (IntPtr panel);

        public void Ready ()
        {
            mpl_panel_client_ready (Handle);
        }

        [DllImport ("libmeego-panel")]
        private static extern bool mpl_panel_client_set_delayed_ready (IntPtr panel, bool delayed);

        public bool SetDelayedReady (bool delayed)
        {
            return mpl_panel_client_set_delayed_ready (Handle, delayed);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_show (IntPtr panel);

        public void Show ()
        {
            mpl_panel_client_show (Handle);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_hide (IntPtr panel);

        public void Hide ()
        {
            mpl_panel_client_hide (Handle);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_set_height_request (IntPtr panel, uint height);

        [DllImport ("libmeego-panel")]
        private static extern uint mpl_panel_client_get_height_request (IntPtr panel);

        public uint HeightRequest {
            get { return mpl_panel_client_get_height_request (Handle); }
            set { mpl_panel_client_set_height_request (Handle, value); }
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_request_focus (IntPtr panel);

        public void RequestFocus ()
        {
            mpl_panel_client_request_focus (Handle);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_request_button_style (IntPtr panel, string style);

        public void RequestButtonStyle (string style)
        {
            mpl_panel_client_request_button_style (Handle, style);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_request_tooltip (IntPtr panel, string tooltip);

        public void RequestTooltip (string tooltip)
        {
            mpl_panel_client_request_tooltip (Handle, tooltip);
        }
        
        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_request_button_state (IntPtr panel, PanelButtonState state);

        public void RequestButtonState (PanelButtonState state)
        {
            mpl_panel_client_request_button_state (Handle, state);
        }

        [DllImport ("libmeego-panel")]
        private static extern void mpl_panel_client_request_modality (IntPtr panel, bool modal);

        public void RequestModality (bool modal)
        {
            mpl_panel_client_request_modality (Handle, modal);
        }

        [DllImport ("libmeego-panel")]
        private static extern uint mpl_panel_client_get_xid (IntPtr panel);

        public uint Xid {
            get { return mpl_panel_client_get_xid (Handle); }
        }

        [DllImport ("libmeego-panel")]
        private static extern bool mpl_panel_client_is_windowless (IntPtr panel);

        public bool IsWindowless {
            get { return mpl_panel_client_is_windowless (Handle); }
        }
        
        [GLib.Signal ("unload")]
        public event EventHandler UnloadEvent {
            add { AddSignalHandler ("unload", value); }
            remove { RemoveSignalHandler ("unload", value); }
        }

        [GLib.Signal ("set-size")]
        public event SetSizeHandler SetSizeEvent {
            add { AddSignalHandler ("set-size", value); }
            remove { RemoveSignalHandler ("set-size", value); }
        }

        [GLib.Signal ("set-position")]
        public event SetPositionHandler SetPositionEvent {
            add { AddSignalHandler ("set-position", value); }
            remove { RemoveSignalHandler ("set-position", value); }
        }

        [GLib.Signal ("show-begin")]
        public event EventHandler ShowBeginEvent {
            add { AddSignalHandler ("show-begin", value); }
            remove { RemoveSignalHandler ("show-begin", value); }
        }

        [GLib.Signal ("show-end")]
        public event EventHandler ShowEndEvent {
            add { AddSignalHandler ("show-end", value); }
            remove { RemoveSignalHandler ("show-end", value); }
        }

        [GLib.Signal ("show")]
        public event EventHandler ShowEvent {
            add { AddSignalHandler ("show", value); }
            remove { RemoveSignalHandler ("show", value); }
        }

        [GLib.Signal ("hide-begin")]
        public event EventHandler HideBeginEvent {
            add { AddSignalHandler ("hide-begin", value); }
            remove { RemoveSignalHandler ("hide-begin", value); }
        }

        [GLib.Signal ("hide-end")]
        public event EventHandler HideEndEvent {
            add { AddSignalHandler ("hide-end", value); }
            remove { RemoveSignalHandler ("hide-end", value); }
        }

        [GLib.Signal ("hide")]
        public event EventHandler HideEvent {
            add { AddSignalHandler ("hide", value); }
            remove { RemoveSignalHandler ("hide", value); }
        }
    }
}
