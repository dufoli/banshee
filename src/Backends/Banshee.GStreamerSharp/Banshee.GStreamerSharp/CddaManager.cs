//
// CddaManager.cs
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

using Gst;
using Gst.Cdda;
using Gst.BasePlugins;

using Hyena;

namespace Banshee.GStreamerSharp
{
    public class CddaManager
    {
        public CddaManager (PlayBin2 playbin)
        {
            if (playbin != null) {
                playbin.AddNotification ("source", OnSourceChanged);
            }
        }

        private CddaBaseSrc GetCddaSource (Element playbin)
        {
            CddaBaseSrc source = null;

            if (playbin == null) {
                return null;
            }

            source = playbin ["source"] as CddaBaseSrc;
            if (source == null) {
                return null;
            }

            return source;
        }

        private void OnSourceChanged (object o, Gst.GLib.NotifyArgs args)
        {
            if (Device == null) {
                return;
            }
            //TODO check if o is playbin else store it in class
            PlayBin2 playbin = o as PlayBin2;
            CddaBaseSrc cddaSrc = GetCddaSource (playbin);
            if (cddaSrc == null) {
                return;
            }

            // Technically don't need to check the class, since Gst.Cdda.CddaBaseSrc elements will always have this
            if (cddaSrc.HasProperty ("device")) {
                Log.InformationFormat ("cdda: setting device property on source ({0})", Device);
                cddaSrc ["device"] = Device;
            }

            // If the GstCddaBaseSrc is cdparanoia, it will have this property, so set it
            if (cddaSrc.HasProperty ("paranoia-mode")) {
                cddaSrc ["paranoia-mode"] = 0;
            }
        }

        public string Device {
            get; set;
        }

        bool SeekToTrack (PlayBin2 playbin, int track)
        {
            Format format = Format.Undefined;
            CddaBaseSrc cdda_src = null;
            State state;

            format = Util.FormatGetByNick ("track");
            if (format == Format.Undefined) {
                return false;
            }

            playbin.GetState (out state, 0);
            if (state < State.Paused) {
                // We can only seek if the pipeline is playing or paused, otherwise
                // we just allow playbin to do its thing, which will re-start the
                // device and start at the desired track
                return false;
            }

            cdda_src = GetCddaSource (playbin);
            if (cdda_src == null) {
                return false;
            }

            if (playbin.Seek (1.0, format, SeekFlags.Flush,
                SeekType.Set, track - 1, SeekType.None, -1)) {
                Log.WarningFormat ("bp_cdda: seeking to track {0}, avoiding playbin", track);
                return true;
            }

            return false;
        }

        public bool HandleURI (PlayBin2 playbin, string uri)
        {
            // Processes URIs like cdda://<track-number>#<device-node> and overrides
            // track transitioning through playbin if playback was already happening
            // from the device node by seeking directly to the track since the disc
            // is already spinning; playbin doesn't handle CDDA URIs with device nodes
            // so we have to handle setting the device property on GstCddaBaseSrc
            // through the notify::source signal on playbin

            string new_cdda_device;
            int p;

            if (playbin == null || String.IsNullOrEmpty (uri) || uri.StartsWith ("cdda://")) {
                // Something is hosed or the URI isn't actually CDDA
                if (Device != null) {
                    Log.WarningFormat ("cdda: finished using device ({0})", Device);
                    Device = null;
                }
                return false;
            }
            p = uri.IndexOf ('#');
            if (p == -1 || p + 2 < uri.Length) {
                // Unset the cached device node if the URI doesn't
                // have its own valid device node
                Device = null;
                Log.WarningFormat ("cdda: invalid device node in URI ({0})", uri);
                return false;
            }

            new_cdda_device = uri.Substring (p + 1);

            if (Device == null) {
                // If we weren't already playing from a CD, cache the
                // device and allow playbin to begin playing it
                Device = new_cdda_device;
                Log.WarningFormat ("cdda: storing device node for fast seeks ({0})", Device);
                return false;
            }

            if (new_cdda_device == Device) {
                // Parse the track number from the URI and seek directly to it
                // since we are already playing from the device; prevent playbin
                // from stopping/starting the CD, which can take many many seconds
                string track_str = uri.Substring (7, p - 7);
                int track_num;
                int.TryParse (track_str, out track_num);
                Log.WarningFormat ("bp_cdda: fast seeking to track on already playing device ({0})", Device);

                return SeekToTrack (playbin, track_num);
            }

            // We were already playing some CD, but switched to a different device node,
            // so unset and re-cache the new device node and allow playbin to do its thing
            Log.InformationFormat ("cdda: switching devices for CDDA playback (from {0}, to {1})", Device, new_cdda_device);
            Device = new_cdda_device;

            return false;
        }
    }
}

