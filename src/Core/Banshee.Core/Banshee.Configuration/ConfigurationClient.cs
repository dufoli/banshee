//
// Configurationclient.cs
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//
// Copyright (C) 2006-2008 Novell, Inc.
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
using Mono.Addins;

using Hyena;
using Banshee.Base;

namespace Banshee.Configuration
{
    public static class ConfigurationClient
    {
        private static IConfigurationClient instance;

        private static void Initialize ()
        {
            lock (typeof (ConfigurationClient)) {
                if (instance != null) {
                    return;
                }

                if (AddinManager.IsInitialized) {
                    foreach (TypeExtensionNode node in AddinManager.GetExtensionNodes (
                        "/Banshee/Platform/ConfigurationClient")) {
                        try {
                            instance = (IConfigurationClient)node.CreateInstance (typeof (IConfigurationClient));
                            if (instance != null) {
                                break;
                            }
                        } catch (Exception e) {
                            Log.Warning ("Configuration client extension failed to load", e.Message);
                        }
                    }

                    if (instance == null) {
                        instance = new XmlConfigurationClient ();
                    }
                } else {
                    instance = new MemoryConfigurationClient ();
                }

                Log.DebugFormat ("Configuration client extension loaded ({0})", instance.GetType ().FullName);
            }
        }

        public static IConfigurationClient Instance {
            get {
                if (instance == null) {
                    Initialize ();
                }
                return instance;
            }
        }
    }
}
