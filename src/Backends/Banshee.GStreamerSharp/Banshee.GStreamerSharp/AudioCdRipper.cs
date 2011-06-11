//
// AudioCdRipper.cs
//
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright (c) 2011 Olivier Dufour
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
using Mono.Unix;

using Hyena;
using Banshee.Base;
using Banshee.Collection;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.MediaProfiles;
using Banshee.Configuration.Schema;

using Gst;
using Banshee.Streaming;
using System.Collections;

namespace Banshee.GStreamerSharp
{
    public class AudioCdRipper : IAudioCdRipper
    {
        private string encoder_pipeline;
        private string output_extension;
        private string output_path;
        private TrackInfo current_track;
        private string device;
        private int paranoia_mode;
        private System.Timers.Timer timer;

        Gst.Pipeline pipeline;
        Element cddasrc;
        Bin encoder;
        Element filesink;

        public event AudioCdRipperProgressHandler Progress;
        public event AudioCdRipperTrackFinishedHandler TrackFinished;
        public event AudioCdRipperErrorHandler Error;

        public void Begin (string device, bool enableErrorCorrection)
        {
            try {
                this.device = device;
                this.paranoia_mode = enableErrorCorrection ? 255 : 0;
                Profile profile = null;
                ProfileConfiguration config = ServiceManager.MediaProfileManager.GetActiveProfileConfiguration ("cd-importing");

                if (config != null) {
                    profile = config.Profile;
                } else {
                    profile = ServiceManager.MediaProfileManager.GetProfileForMimeType ("audio/vorbis")
                        ?? ServiceManager.MediaProfileManager.GetProfileForMimeType ("audio/flac");
                    if (profile != null) {
                        Log.InformationFormat ("Using default/fallback encoding profile: {0}", profile.Name);
                        ProfileConfiguration.SaveActiveProfile (profile, "cd-importing");
                    }
                }

                if (profile != null) {
                    encoder_pipeline = profile.Pipeline.GetProcessById ("gstreamer");
                    output_extension = profile.OutputFileExtension;
                }

                if (String.IsNullOrEmpty (encoder_pipeline)) {
                    throw new ApplicationException ();
                }
                timer = new System.Timers.Timer ();
                timer.Interval = 200;
                timer.AutoReset = true;
                timer.Elapsed += OnTick;
                Hyena.Log.InformationFormat ("Ripping using encoder profile `{0}' with pipeline: {1}", profile.Name, encoder_pipeline);
            } catch (Exception e) {
                throw new ApplicationException (Catalog.GetString ("Could not find an encoder for ripping."), e);
            }
        }

        void OnTick (object o, System.Timers.ElapsedEventArgs args)
        {
            Format format = Format.Time;
            State state;
            long position;

            pipeline.GetState (out state, 0);
            if (state != State.Playing) {
                return;
            }

            if (!cddasrc.QueryPosition (ref format, out position)) {
                return;
            }

            RaiseProgress (current_track, TimeSpan.FromSeconds (position / (long)Clock.Second));
        }

        public void Finish ()
        {
            if (output_path != null) {
                Banshee.IO.File.Delete (new SafeUri (output_path));
            }

            TrackReset ();

            encoder_pipeline = null;
            output_extension = null;

            timer.Stop ();

            if (pipeline != null && pipeline is Element) {
                pipeline.SetState (State.Null);
                pipeline = null;
            }
        }

        public void Cancel ()
        {
            Finish ();
        }

        private void TrackReset ()
        {
            current_track = null;
            output_path = null;
        }

        private TagList MakeTagList (TrackInfo track)
        {
            TagList tags = new TagList ();
            tags.Add (TagMergeMode.Replace, CommonTags.Artist, track.ArtistName);
            tags.Add (TagMergeMode.Replace, CommonTags.Album, track.AlbumTitle);
            tags.Add (TagMergeMode.Replace, CommonTags.Title, track.TrackTitle);
            tags.Add (TagMergeMode.Replace, CommonTags.Genre, track.Genre);

            tags.Add (TagMergeMode.Replace, CommonTags.TrackNumber, (uint)track.TrackNumber);
            tags.Add (TagMergeMode.Replace, CommonTags.TrackCount, (uint)track.TrackCount);
            tags.Add (TagMergeMode.Replace, CommonTags.AlbumDiscNumber, (uint)track.DiscNumber);
            tags.Add (TagMergeMode.Replace, CommonTags.AlbumDiscCount, (uint)track.DiscCount);

            tags.Add (TagMergeMode.Replace, Gst.Tag.Date, track.Year);
            tags.Add (TagMergeMode.Replace, Gst.Tag.Date, track.ReleaseDate);

            tags.Add (TagMergeMode.Replace, CommonTags.Composer, track.Composer);
            tags.Add (TagMergeMode.Replace, CommonTags.Copyright, track.Copyright);
            tags.Add (TagMergeMode.Replace, CommonTags.Comment, track.Comment);

            tags.Add (TagMergeMode.Replace, CommonTags.MusicBrainzTrackId, track.MusicBrainzId);
            tags.Add (TagMergeMode.Replace, CommonTags.MusicBrainzArtistId, track.ArtistMusicBrainzId);
            tags.Add (TagMergeMode.Replace, CommonTags.MusicBrainzAlbumId, track.AlbumMusicBrainzId);
            return tags;
        }

        public void RipTrack (int trackIndex, TrackInfo track, SafeUri outputUri, out bool taggingSupported)
        {
            taggingSupported = false;
            TrackReset ();
            current_track = track;

            using (TagList tags = MakeTagList (track)) {
                output_path = String.Format ("{0}.{1}", outputUri.LocalPath, output_extension);

                // Avoid overwriting an existing file
                int i = 1;
                while (Banshee.IO.File.Exists (new SafeUri (output_path))) {
                    output_path = String.Format ("{0} ({1}).{2}", outputUri.LocalPath, i++, output_extension);
                }

                Log.DebugFormat ("GStreamer ripping track {0} to {1}", trackIndex, output_path);

                if (!ConstructPipeline ()) {
                    return;
                }

                // initialize the pipeline, set the sink output location
                filesink.SetState (State.Null);
                filesink ["location"] = output_path;

                // find an element to do the tagging and set tag data
                foreach (Element element in encoder.GetAllByInterface (typeof (TagSetter))) {
                    TagSetter tag_setter = element as TagSetter;
                    if (tag_setter != null) {
                        tag_setter.AddTag (TagMergeMode.ReplaceAll, Tag.Encoder, new Gst.GLib.Value ("Banshee 2.0.0"));
                        tag_setter.AddTag (TagMergeMode.ReplaceAll, Tag.EncoderVersion, new Gst.GLib.Value ( (2 << 16) | (0 << 8) | 0));
                        // TODO Version not hardcoded

                        if (tags != null) {
                            tag_setter.AddTag (tags, TagMergeMode.Append);
                        }

                        /*if (banshee_is_debugging ()) {
                            bt_tag_list_dump (gst_tag_setter_get_tag_list (tag_setter));
                        }*/

                        // We'll warn the user in the UI if we can't tag the encoded audio files
                        taggingSupported = true;
                    }
                }

                // Begin the rip
                cddasrc ["track"] = trackIndex + 1;
                pipeline.SetState (State.Playing);
                timer.Start ();
            }
        }

        bool ConstructPipeline ()
        {
            Element queue;

            pipeline = new Gst.Pipeline ("pipeline");
            if (pipeline == null) {
                RaiseError (current_track, Catalog.GetString ("Could not create pipeline"));
                return false;
            }

            cddasrc = ElementFactory.MakeFromUri (URIType.Src, "cdda://1", "cddasrc");
            if (cddasrc == null) {
                RaiseError (current_track, Catalog.GetString ("Could not initialize element from cdda URI"));
                return false;
            }

            cddasrc ["device"] = device;

            if (cddasrc.HasProperty ("paranoia-mode")) {
                cddasrc ["paranoia-mode"] = paranoia_mode;
            }

            try {
            encoder = (Bin)Parse.BinFromDescription (encoder_pipeline, true);
            } catch (Exception e) {
                string err = Catalog.GetString ("Could not create encoder pipeline : {0}");
                RaiseError (current_track, String.Format (err, e.Message));
                return false;
            }

            queue = ElementFactory.Make ("queue", "queue");
            if (queue == null) {
                RaiseError (current_track, Catalog.GetString ("Could not create queue plugin"));
                return false;
            }

            queue ["max-size-time"] = 120 * Gst.Clock.Second;

            filesink = ElementFactory.Make ("filesink", "filesink");
            if (filesink == null) {
                RaiseError (current_track, Catalog.GetString ("Could not create filesink plugin"));
                return false;
            }

            pipeline.Add (cddasrc, queue, encoder, filesink);

            if (!Element.Link (cddasrc, queue, encoder, filesink)) {
                RaiseError (current_track, Catalog.GetString ("Could not link pipeline elements"));
            }

            pipeline.Bus.AddWatch (OnBusMessage);

            return true;
        }

        private string ProbeMimeType ()
        {
            Iterator elem_iter = ((Bin)encoder).ElementsRecurse;
            string preferred_mimetype = null;
            IEnumerator en = elem_iter.GetEnumerator ();
            while (en.MoveNext ()) {
                Element element = (Element)en.Current;
                Iterator pad_iter = element.SrcPads;
                IEnumerator enu = pad_iter.GetEnumerator ();
                while (enu.MoveNext ()) {
                    Pad pad = (Pad)enu.Current;
                    Caps caps = pad.Caps;
                    Structure str = (caps != null ? caps [0] : null);

                    if (str != null) {
                        string mimetype = str.Name;
                        int mpeg_layer;

                        Gst.GLib.Value val = str.GetValue ("mpegversion");

                        // Prefer and adjust audio/mpeg, leaving MP3 as audio/mpeg
                        if (mimetype.StartsWith ("audio/mpeg")) {
                            mpeg_layer = (Int32)val.Val;
                            switch (mpeg_layer) {
                                case 2: mimetype = "audio/mp2"; break;
                                case 4: mimetype = "audio/mp4"; break;
                                default: break;
                            }

                            preferred_mimetype = mimetype;

                        // If no preferred type set and it's not RAW, prefer it
                        } else if (preferred_mimetype == null &&
                            !mimetype.StartsWith ("audio/x-raw")) {
                            preferred_mimetype = mimetype;

                        // Always prefer application containers
                        } else if (mimetype.StartsWith ("application/")) {
                            preferred_mimetype = mimetype;
                        }
                    }
                }
            }
            return preferred_mimetype;
        }

        private void RefreshTrackMimeType (string mimetype)
        {
            if (current_track == null)
                return;

            if (mimetype != null) {
                string [] split = mimetype.Split (';', '.', ' ', '\t');
                if (split != null && split.Length > 0) {
                    current_track.MimeType = split[0].Trim ();
                } else {
                    current_track.MimeType = mimetype.Trim ();
                }
            }
        }

        private bool OnBusMessage (Bus bus, Message msg)
        {
            switch (msg.Type) {
                case MessageType.Eos:
                    pipeline.SetState (State.Null);
                    timer.Stop ();
                    OnNativeFinished ();
                    break;

                case MessageType.StateChanged:
                    State old_state, new_state, pending_state;
                    msg.ParseStateChanged (out old_state, out new_state, out pending_state);
                    if (old_state == State.Ready && new_state == State.Paused && pending_state == State.Playing) {
                        string mimetype = ProbeMimeType ();
                        if (mimetype == null)
                            return true;
                        Log.Information ("Ripper : Found Mime Type for encoded content: {0}", mimetype);
                        RefreshTrackMimeType (mimetype);
                    }
                    break;

                case MessageType.Error:
                    Enum error_type;
                    string err_msg, debug;
                    msg.ParseError (out error_type, out err_msg, out debug);
                    RaiseError (current_track, String.Format ("{0} : {1}", err_msg, debug));
                    timer.Stop ();
                    break;
            }

            return true;
        }

        protected virtual void RaiseProgress (TrackInfo track, TimeSpan ellapsedTime)
        {
            AudioCdRipperProgressHandler handler = Progress;
            if (handler != null) {
                handler (this, new AudioCdRipperProgressArgs (track, ellapsedTime, track.Duration));
            }
        }

        protected virtual void RaiseTrackFinished (TrackInfo track, SafeUri outputUri)
        {
            AudioCdRipperTrackFinishedHandler handler = TrackFinished;
            if (handler != null) {
                handler (this, new AudioCdRipperTrackFinishedArgs (track, outputUri));
            }
        }

        protected virtual void RaiseError (TrackInfo track, string message)
        {
            AudioCdRipperErrorHandler handler = Error;
            if (handler != null) {
                handler (this, new AudioCdRipperErrorArgs (track, message));
            }
        }

        private void OnNativeMimeType (IntPtr ripper, IntPtr mimetype)
        {
            if (mimetype != IntPtr.Zero && current_track != null) {
                string type = GLib.Marshaller.Utf8PtrToString (mimetype);
                if (type != null) {
                    string [] split = type.Split (';', '.', ' ', '\t');
                    if (split != null && split.Length > 0) {
                        current_track.MimeType = split[0].Trim ();
                    } else {
                        current_track.MimeType = type.Trim ();
                    }
                }
            }
        }

        private void OnNativeFinished ()
        {
            SafeUri uri = new SafeUri (output_path);
            TrackInfo track = current_track;

            TrackReset ();

            RaiseTrackFinished (track, uri);
        }
    }

}

