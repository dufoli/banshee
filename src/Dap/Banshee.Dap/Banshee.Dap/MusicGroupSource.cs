//
// VideoGroupSource.cs
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

using Banshee.Collection;
using Banshee.ServiceStack;

namespace Banshee.Dap
{
    public class MusicGroupSource : MediaGroupSource
    {
        public MusicGroupSource (DapSource parent) : base (parent, Catalog.GetString ("Music"))
        {
            Properties.Remove ("Icon.Name");
            Properties.SetStringList ("Icon.Name", Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary.Properties.GetStringList ("Icon.Name"));
            ConditionSql = Banshee.ServiceStack.ServiceManager.SourceManager.MusicLibrary.AttributesCondition;
        }

        public override bool HasEditableTrackProperties {
            get {
                // dap capabilities dictate first
                if (!parent.HasEditableTrackProperties) {
                    return false;
                }

                // we only allow editing in Manual Sync Mode
                // (Auto Sync Mode will require to edit the original tracks)
                foreach (var libsync in parent.Sync.Libraries) {
                    if (libsync.Library.Equals (ServiceManager.SourceManager.MusicLibrary)) {
                        return !libsync.Enabled;
                    }
                }

                return false;
            }
        }
    }
}
