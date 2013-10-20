//
// MediaPanelClient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2010 Novell, Inc.
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

// The MediaPanel client is just a wrapper around the Nereid client.
// This is done to allow the user to explicitly start the MediaPanel UI, for
// example through a shortcut or an icon. Otherwise the MediaPanel UI would
// show itself depending on whether the extension is enabled or not.
//

using System;
using System.IO;
using System.Reflection;

using Hyena;

namespace Banshee.MediaPanel.Client
{
    public class MediaPanelClient : Nereid.Client
    {
        public new static void Main (string [] args)
        {
            // Normally Mono.Addins would load the MediaPanel extension from the
            // Extensions directory, so we need to load this reference manually
            var assembly_file = Paths.Combine (Path.GetDirectoryName (
                Assembly.GetEntryAssembly ().Location), "Extensions", "Banshee.MediaPanel.dll");
            if (!File.Exists (assembly_file)) {
                // Also look into the current folder, so that we can run uninstalled
                assembly_file = Paths.Combine (Path.GetDirectoryName (
                    Assembly.GetEntryAssembly ().Location), "Banshee.MediaPanel.dll");
            }
            Assembly.LoadFile (assembly_file);
            Startup<MediaPanelClient> (args);
        }

        protected override void InitializeGtk ()
        {
            base.InitializeGtk ();
            new Banshee.MediaPanel.MediaPanel ();
        }
    }
}
