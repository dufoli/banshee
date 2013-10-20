//
// MediaPanelService.cs
//
// Authors:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright 2009-2010 Novell, Inc.
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
using Gtk;
using Mono.Unix;

using Hyena;

using Banshee.Base;
using Banshee.Collection;
using Banshee.Sources;
using Banshee.ServiceStack;
using Banshee.MediaEngine;
using Banshee.Gui;

namespace Banshee.MediaPanel
{
    public class MediaPanelService : IExtensionService
    {
        private GtkElementsService elements_service;
        private InterfaceActionService interface_action_service;
        private SourceManager source_manager;
        private PlayerEngineService player;
        private Menu view_menu;
        private MenuItem menu_item;
        private MediaPanel panel;

        void IExtensionService.Initialize ()
        {
            elements_service = ServiceManager.Get<GtkElementsService> ();
            interface_action_service = ServiceManager.Get<InterfaceActionService> ();
            source_manager = ServiceManager.SourceManager;
            player = ServiceManager.PlayerEngine;

            if (!ServiceStartup ()) {
                ServiceManager.ServiceStarted += OnServiceStarted;
            }
        }

        private void OnServiceStarted (ServiceStartedArgs args)
        {
            if (args.Service is Banshee.Gui.InterfaceActionService) {
                interface_action_service = (InterfaceActionService)args.Service;
            } else if (args.Service is GtkElementsService) {
                elements_service = (GtkElementsService)args.Service;
            } else if (args.Service is SourceManager) {
                source_manager = ServiceManager.SourceManager;
            } else if (args.Service is PlayerEngineService) {
                player = ServiceManager.PlayerEngine;
            }

            ServiceStartup ();
        }

        private bool ServiceStartup ()
        {
            if (elements_service == null || interface_action_service == null ||
                source_manager == null || player == null) {
                return false;
            }

            Initialize ();

            ServiceManager.ServiceStarted -= OnServiceStarted;

            return true;
        }

        private void Initialize ()
        {
            view_menu = (interface_action_service.UIManager.GetWidget ("/MainMenu/ViewMenu") as MenuItem).Submenu as Menu;
            menu_item = new MenuItem (Catalog.GetString ("Media _Panel"));
            menu_item.Activated += delegate { PresentMediaPanel (); };
            view_menu.Insert (menu_item, 2);
            menu_item.Show ();

            // If Banshee is running from the MediaPanel client entry assembly,
            // the MediaPanel instance will have already been created.
            panel = MediaPanel.Instance;

            if (panel != null) {
                panel.BuildContents ();
                PresentMediaPanel ();
            }
        }

        public void PresentPrimaryInterface ()
        {
            elements_service.PrimaryWindow.Present ();
            if (panel != null) {
                panel.Hide ();
            }
        }

        public void PresentMediaPanel ()
        {
            if (panel == null) {
                panel = new MediaPanel ();
                panel.BuildContents ();
            }
            elements_service.PrimaryWindow.Hide ();
            panel.Show ();
        }

        public void Dispose ()
        {
            if (view_menu != null && menu_item != null) {
                view_menu.Remove (menu_item);
            }

            if (panel != null) {
                panel.Dispose ();
                panel = null;
            }

            interface_action_service = null;
            elements_service = null;
        }

        string IService.ServiceName {
            get { return "MediaPanelService"; }
        }
    }
}
