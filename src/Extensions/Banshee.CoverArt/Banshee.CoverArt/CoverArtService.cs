//
// CoverArtService.cs
//
// Authors:
//   James Willcox <snorp@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
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
using System.Data;
using Gtk;
using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Gui;
using Banshee.Collection.Gui;
using Banshee.Library;
using Banshee.Metadata;
using Banshee.Networking;
using Banshee.Sources;

namespace Banshee.CoverArt
{
    public class CoverArtService : IExtensionService
    {
        private bool disposed;

        private CoverArtJob job;

        public CoverArtService ()
        {
        }

        void IExtensionService.Initialize ()
        {
            if (!ServiceManager.DbConnection.TableExists ("CoverArtDownloads")) {
                ServiceManager.DbConnection.Execute (@"
                    CREATE TABLE CoverArtDownloads (
                        AlbumID     INTEGER UNIQUE,
                        Downloaded  BOOLEAN,
                        LastAttempt INTEGER NOT NULL
                    )");
            }

            if (!ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded += OnSourceAdded;
            }
        }

        private void OnSourceAdded (SourceAddedArgs args)
        {
            if (ServiceStartup ()) {
                ServiceManager.SourceManager.SourceAdded -= OnSourceAdded;
            }
        }

        private bool ServiceStartup ()
        {
            if (ServiceManager.SourceManager.MusicLibrary == null) {
                return false;
            }

            Initialize ();

            return true;
        }

        private void Initialize ()
        {
            ServiceManager.SourceManager.MusicLibrary.TracksAdded += OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged += OnTracksChanged;
        }

        public void Dispose ()
        {
            if (disposed) {
                return;
            }

            ServiceManager.SourceManager.MusicLibrary.TracksAdded -= OnTracksAdded;
            ServiceManager.SourceManager.MusicLibrary.TracksChanged -= OnTracksChanged;

            disposed = true;
        }

        public void FetchCoverArt ()
        {
            bool force = false;
            if (!String.IsNullOrEmpty (Environment.GetEnvironmentVariable ("BANSHEE_FORCE_COVER_ART_FETCH"))) {
                Log.Debug ("Forcing cover art download session");
                force = true;
            }

            FetchCoverArt (force);
        }

        public void FetchCoverArt (bool force)
        {
            if (job == null && ServiceManager.Get<Network> ().Connected) {
                DateTime last_scan = DateTime.MinValue;

                if (!force) {
                    try {
                        last_scan = DatabaseConfigurationClient.Client.Get<DateTime> ("last_cover_art_scan",
                                                                                      DateTime.MinValue);
                    } catch (FormatException) {
                        Log.Warning ("last_cover_art_scan is malformed, resetting to default value");
                        DatabaseConfigurationClient.Client.Set<DateTime> ("last_cover_art_scan",
                                                                          DateTime.MinValue);
                    }
                }
                job = new CoverArtJob (last_scan);
                job.Finished += delegate {
                    if (!job.IsCancelRequested) {
                        DatabaseConfigurationClient.Client.Set<DateTime> ("last_cover_art_scan", DateTime.Now);
                    }
                    job = null;
                };
                job.Start ();
            }
        }

        private void OnTracksAdded (Source sender, TrackEventArgs args)
        {
            FetchCoverArt ();
        }

        private void OnTracksChanged (Source sender, TrackEventArgs args)
        {
            if (args.ChangedFields == null) {
                FetchCoverArt ();
            } else {
                foreach (Hyena.Query.QueryField field in args.ChangedFields) {
                    if (field == Banshee.Query.BansheeQuery.AlbumField ||
                        field == Banshee.Query.BansheeQuery.ArtistField) {
                        FetchCoverArt ();
                        break;
                    }
                }
            }
        }

        string IService.ServiceName {
            get { return "CoverArtService"; }
        }

        public static readonly SchemaEntry<bool> EnabledSchema = new SchemaEntry<bool> (
            "plugins.cover_art", "enabled",
            true,
            "Plugin enabled",
            "Cover art plugin enabled"
        );
    }
}
