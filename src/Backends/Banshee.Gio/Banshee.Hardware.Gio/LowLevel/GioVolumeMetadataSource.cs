//
// GioVolumeMetadataSource.cs
//
// Author:
//   Alan McGovern <amcgovern@novell.com>
//
// Copyright (c) 2010 Moonlight Team
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

#if ENABLE_GIO_HARDWARE
using System;
using System.Linq;

namespace Banshee.Hardware.Gio
{
    public class GioVolumeMetadataSource : GioMetadataSource
    {
        GLib.Volume Volume {
            get; set;
        }

        public GioVolumeMetadataSource (GLib.Volume volume)
        {
            Volume = volume;
        }

        public override string GetPropertyString (string key)
        {
            throw new NotImplementedException ();
        }

        public override double GetPropertyDouble (string key)
        {
            throw new NotImplementedException ();
        }

        public override bool GetPropertyBoolean (string key)
        {
            throw new NotImplementedException ();
        }

        public override int GetPropertyInteger (string key)
        {
            throw new NotImplementedException ();
        }

        public override ulong GetPropertyUInt64 (string key)
        {
            throw new NotImplementedException ();
        }

        public override string[] GetPropertyStringList (string key)
        {
            throw new NotImplementedException ();
        }

        public override bool PropertyExists (string key)
        {
            throw new NotImplementedException ();
        }

        private string[] content_types;
        public string[] MediaContentTypes {
            get {
                if (Volume.MountInstance == null) {
                    content_types = new string[] {};
                } else if (content_types == null) {
                    content_types = Volume.MountInstance.GuessContentTypeSync (false, null);
                }

                return content_types;
            }
        }
    }
}

#endif
