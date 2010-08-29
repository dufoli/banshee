// 
// LastfmFingerprintService.cs
// 
// Author:
//   dufoli <${AuthorEmail}>
// 
// Copyright (c) 2010 dufoli
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
using Banshee.Sources;
using Banshee.Gui;
using Gtk;
using Mono.Unix;
using System.Threading;
using Hyena.Jobs;
using Hyena;
using System.Runtime.InteropServices;

namespace Banshee.Lastfm.Fingerprint
{
    public class LastfmFingerprintService : IExtensionService
    {
        private bool disposed;

        string IService.ServiceName {
            get { return "LastfmFingerprintService"; }
        }

        public LastfmFingerprintService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            InterfaceInitialize ();
        }

        public void InterfaceInitialize ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            uia_service.TrackActions.Add (new ActionEntry [] {
                new ActionEntry ("FingerprintAction", null,
                    Catalog.GetString ("Get tag from song fingerprint"), null,
                    Catalog.GetString ("Get tag from lastfm acoustic fingerprint"),
                    OnGetTagFromFingerprint)
            });


            //TODO icon of fingerprint
            //Gtk.Action action = uia_service.TrackActions["FingerprintAction"];
            //action.IconName = "fingerprint";

            uia_service.UIManager.AddUiFromResource ("GlobalUI.xml");

            UpdateActions ();
            uia_service.TrackActions.SelectionChanged += delegate { ThreadAssist.ProxyToMain (UpdateActions); };
            ServiceManager.SourceManager.ActiveSourceChanged += delegate { ThreadAssist.ProxyToMain (UpdateActions); };
        }

        private void OnGetTagFromFingerprint (object sender, EventArgs args)
        {

            ThreadPool.QueueUserWorkItem (delegate {

                Source source = ServiceManager.SourceManager.ActiveSource;

                UserJob job = new UserJob (Catalog.GetString ("Getting sound fingerprint"));
                job.SetResources (Resource.Cpu, Resource.Disk, Resource.Database);
                job.PriorityHints = PriorityHints.SpeedSensitive;
                job.Status = Catalog.GetString ("Scanning...");
                job.IconNames = new string [] { "system-search", "gtk-find" };
                job.Register ();
    
                var selection = ((ITrackModelSource)source).TrackModel.Selection;
                int total = selection.Count;
                int count = 0;

                foreach (int index in selection) {
                    Thread.Sleep (index * 1000);

                    //job.Status = String.Format ("{0} - {1}",
                    job.Progress = (double)++count / (double)total;
                }
                job.Finish ();
            });
        }

        void UpdateActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            Gtk.Action action = uia_service.TrackActions["FingerprintAction"];

            /*Source source = ServiceManager.SourceManager.ActiveSource;
            bool sensitive = false;
            bool visible = false;

            if (source != null) {
                //TODO test MusicLibrarySource ?
                var track_source = source as ITrackModelSource;
                if (track_source != null) {
                    var selection = track_source.TrackModel.Selection;
                    visible = source.HasEditableTrackProperties;
                    sensitive = selection.Count > 0;
                }
            }

            action.Sensitive = sensitive;
            action.Visible = visible;
            */

            action.Sensitive = true;
            action.Visible = true;
        }


        void IDisposable.Dispose ()
        {
            if (disposed) {
                return;
            }

            //TODO dispose resource here

            disposed = true;
        }

/*

    FingerprintExtractor(); // ctor
   ~FingerprintExtractor(); // dtor

   // duration (in seconds!) is optional, but if you want to submit tracks <34 secs
   // it must be provided.
   void initForQuery(int freq, int nchannels, int duration = -1);
   void initForFullSubmit(int freq, int nchannels);

   // return false if it needs more data, otherwise true
   // IMPORTANT: num_samples specify the size of the *short* array pPCM, that is
   //            the number of samples that are in the buffer. This includes
   //            the stereo samples, i.e.
   //            [L][R][L][R][L][R][L][R] would be num_samples=8
   bool process(const short* pPCM, size_t num_samples, bool end_of_stream = false);

   // returns pair<NULL, 0> if the data is not ready
   std::pair<const char*, size_t> getFingerprint();

   //////////////////////////////////////////////////////////////////////////

   // The FingerprintExtractor assumes that the file start from the beginning
   // but since the first SkipMs are ignored, it's possible to feed it with NULL.
   // In order to know how much must be skipped (in milliseconds) call this function.
   // Remark: this is only for "advanced" users!
   size_t getToSkipMs();

   // Return the minimum duration of the file (in ms)
   // Any file with a length smaller than this value will be discarded
   static size_t getMinimumDurationMs();

   // return the version of the fingerprint
   static size_t getVersion();

 * */
        [DllImport ("liblastfmfp.dll")]
        private static extern IntPtr FingerprintExtractor ();


    }
}

