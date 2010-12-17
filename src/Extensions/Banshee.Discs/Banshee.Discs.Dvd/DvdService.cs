//
// DvdService.cs
//
// Author:
//   Alex Launi <alex.launi@canonical.com>
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

using Banshee.Hardware;
using Banshee.Gui;
using Banshee.ServiceStack;
using Mono.Unix;

namespace Banshee.Discs.Dvd
{
    public class DvdService : DiscService, IService
    {
        private uint global_interface_id;
        
        public DvdService ()
        {
        }

        // FIXME Implement DVD device command matching
        protected override bool DeviceCommandMatchesSource (DiscSource source, DeviceCommand command)
        {
            return false;
        }

        public override void Initialize()
        {
            lock (this) {
                //InstallPreferences ();
                base.Initialize ();
                SetupActions ();
            }

        }

        public override void Dispose ()
        {
            lock (this) {
                //UninstallPreferences ();
                base.Dispose ();
                DisposeActions ();
            }
        }

        #region UI Actions

        private void SetupActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }

            uia_service.GlobalActions.AddImportant (new Gtk.ActionEntry [] {
                new Gtk.ActionEntry ("GoToMenuAction", null,
                    Catalog.GetString ("Go to Menu"), null,
                    Catalog.GetString ("Naviguate to menu"),
                    (object o, EventArgs args) => { ServiceManager.PlayerEngine.NavigateToMenu (); })
            });

            global_interface_id = uia_service.UIManager.AddUiFromResource ("GlobalUIDvd.xml");
        }

        private void DisposeActions ()
        {
            InterfaceActionService uia_service = ServiceManager.Get<InterfaceActionService> ();
            if (uia_service == null) {
                return;
            }

            uia_service.GlobalActions.Remove ("GoToMenuAction");
            uia_service.UIManager.RemoveUi (global_interface_id);
        }

        #endregion

        string IService.ServiceName {
            get { return "DvdService"; }
        }
    }
}

