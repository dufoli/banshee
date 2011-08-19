//
// DiscModel.cs
//
// Author:
//   Alex Launi <alex.launi@gmail.com>
//
// Copyright 2010 Alex Launi
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

using Banshee.Collection;
using Banshee.Hardware;

namespace Banshee.Discs
{
    public class DiscModel : MemoryTrackListModel
    {
        public DiscModel (IDiscVolume volume)
        {
           Volume = volume;
        }

        public IDiscVolume Volume { get; protected set;}

        public virtual string Title {
            get {
                return Volume.Name;
            }
        }

        public virtual void LoadModelFromDisc ()
        {
        }

        private ICdromDevice Drive {
            get { return Volume == null ? null : (Volume.Parent as ICdromDevice); }
        }

        public bool LockDoor ()
        {
            ICdromDevice drive = Drive;
            return drive != null ? drive.LockDoor () : false;
        }

        public bool UnlockDoor ()
        {
            ICdromDevice drive = Drive;
            return drive != null ? drive.UnlockDoor () : false;
        }

        public bool IsDoorLocked {
            get {
                ICdromDevice drive = Drive;
                return drive != null ? drive.IsDoorLocked : false;
            }
        }
    }
}

