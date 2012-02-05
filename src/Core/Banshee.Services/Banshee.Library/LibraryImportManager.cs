//
// LibraryImportManager.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//   Gabriel Burt <gburt@novell.com>
//
// Copyright (C) 2007-2008 Novell, Inc.
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
using System.Collections.Generic;
using System.IO;

using Mono.Unix;

using Hyena;
using Hyena.Data.Sqlite;

using Banshee.Base;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.Collection;
using Banshee.Collection.Database;
using Banshee.Streaming;

namespace Banshee.Library
{
    public class LibraryImportManager : DatabaseImportManager, IRegisterOnDemandService
    {
        public LibraryImportManager () : this (false)
        {
        }

        public LibraryImportManager (bool force_copy) : base (DefaultTrackPrimarySourceChooser)
        {
            ForceCopy = force_copy;
        }

        protected override ErrorSource ErrorSource {
            get { return ServiceManager.SourceManager.MusicLibrary.ErrorSource; }
        }

        protected override int [] PrimarySourceIds {
            get {
                if (base.PrimarySourceIds == null) {
                    List<int> ids = new List<int> ();
                    foreach (var src in ServiceManager.SourceManager.FindSources<LibrarySource> ()) {
                            ids.Add (src.DbId);
                    }
                    base.PrimarySourceIds = ids.ToArray ();
                }

                return base.PrimarySourceIds;
            }
        }

        protected override string BaseDirectory {
            get { return ServiceManager.SourceManager.MusicLibrary.BaseDirectory; }
        }

        protected static PrimarySource DefaultTrackPrimarySourceChooser (DatabaseTrackInfo track)
        {
            LibrarySource src = ServiceManager.SourceManager.GetBestSourceForTrack (track);
            return src ?? ServiceManager.SourceManager.MusicLibrary;
        }

        string IService.ServiceName {
            get { return "LibraryImportManager"; }
        }
    }
}
