//
// AudioCdService.cs
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
using System.Collections.Generic;
using Mono.Unix;

using Hyena;

using Banshee.ServiceStack;
using Banshee.Configuration;
using Banshee.Preferences;
using Banshee.Hardware;
using Banshee.Gui;

namespace Banshee.Discs
{
    public class AbstractDiscService : IExtensionService, IDisposable
    {
        private List<DeviceCommand> unhandled_device_commands;

        public AbstractDiscService ()
        {
        }

        public virtual void Initialize ()
        {
            Sources = new Dictionary<string, AbstractDiscSource> ();

            lock (this) {
                // This says Cdrom, but really it means Cdrom in the general Disc device sense.
                foreach (ICdromDevice device in ServiceManager.HardwareManager.GetAllCdromDevices ()) {
                    MapDiscDevice (device);
                }

                ServiceManager.HardwareManager.DeviceAdded += OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved += OnHardwareDeviceRemoved;
                ServiceManager.HardwareManager.DeviceCommand += OnDeviceCommand;
            }
        }

        public virtual void Dispose ()
        {
            lock (this) {
                ServiceManager.HardwareManager.DeviceAdded -= OnHardwareDeviceAdded;
                ServiceManager.HardwareManager.DeviceRemoved -= OnHardwareDeviceRemoved;
                ServiceManager.HardwareManager.DeviceCommand -= OnDeviceCommand;

                foreach (AbstractDiscSource source in Sources.Values) {
                    source.Dispose ();
                    ServiceManager.SourceManager.RemoveSource (source);
                }

                Sources.Clear ();
                Sources = null;
            }
        }

        protected Dictionary<string, AbstractDiscSource> Sources {
            get; private set;
        }

        protected virtual void MapDiscDevice (ICdromDevice device)
        {
            lock (this) {
                foreach (IVolume volume in device) {
                    if (volume is IDiscVolume) {
                        MapDiscVolume ((IDiscVolume) volume);
                    }
                }
            }
        }

        protected virtual void MapDiscVolume (IDiscVolume volume)
        {
            AbstractDiscSource source = null;
            Log.DebugFormat ("Mapping disc volume", volume.Name);

            lock (this) {
                if (Sources.ContainsKey (volume.Uuid)) {
                    Log.Debug ("Already mapped");
                    return;
                } else if  (volume.HasAudio) {
                    Log.Debug ("Mapping audio cd");
                    source = new AudioCd.AudioCdSource (this as AudioCd.AudioCdService, new AudioCd.AudioCdDiscModel (volume));
                } else if (volume.HasVideo) {
                    Log.Debug ("Mapping dvd");
                    source = new Dvd.DvdSource (this as Dvd.DvdService);
                } else {
                    Log.Debug ("Neither :(");
                    return;
                }
                
                Sources.Add (volume.Uuid, source);
                ServiceManager.SourceManager.AddSource (source);

                // If there are any queued device commands, see if they are to be
                // handled by this new volume (e.g. --device-activate-play=cdda://sr0/)
                try {
                    if (unhandled_device_commands != null) {
                        foreach (DeviceCommand command in unhandled_device_commands) {
                            if (DeviceCommandMatchesSource (source as AudioCd.AudioCdSource, command)) {
                                HandleDeviceCommand (source as AudioCd.AudioCdSource, command.Action);
                                unhandled_device_commands.Remove (command);
                                if (unhandled_device_commands.Count == 0) {
                                    unhandled_device_commands = null;
                                }
                                break;
                            }
                        }
                    }
                } catch (Exception e) {
                    Log.Exception (e);
                }

                Log.DebugFormat ("Mapping disc ({0})", volume.Uuid);
            }
        }

        internal void UnmapDiscVolume (string uuid)
        {
            lock (this) {
                if (Sources.ContainsKey (uuid)) {
                    AudioCd.AudioCdSource source = (AudioCd.AudioCdSource) Sources[uuid];
                    source.StopPlayingDisc ();
                    ServiceManager.SourceManager.RemoveSource (source);
                    Sources.Remove (uuid);
                    Log.DebugFormat ("Unmapping audio CD ({0})", uuid);
                }
            }
        }

        private void OnHardwareDeviceAdded (object o, DeviceAddedArgs args)
        {
            lock (this) {
                if (args.Device is ICdromDevice) {
                    MapDiscDevice ((ICdromDevice)args.Device);
                } else if (args.Device is IDiscVolume) {
                    MapDiscVolume ((IDiscVolume)args.Device);
                }
            }
        }

        private void OnHardwareDeviceRemoved (object o, DeviceRemovedArgs args)
        {
            lock (this) {
                UnmapDiscVolume (args.DeviceUuid);
            }
        }

#region DeviceCommand Handling

        protected virtual bool DeviceCommandMatchesSource (AudioCd.AudioCdSource source, DeviceCommand command)
        {
            if (command.DeviceId.StartsWith ("cdda:")) {
                try {
                    Uri uri = new Uri (command.DeviceId);
                    string match_device_node = String.Format ("{0}{1}", uri.Host,
                        uri.AbsolutePath).TrimEnd ('/', '\\');
                    string device_node = source.DiscModel.Volume.DeviceNode;
                    return device_node.EndsWith (match_device_node);
                } catch {
                }
            }

            return false;
        }

        private void HandleDeviceCommand (AudioCd.AudioCdSource source, DeviceCommandAction action)
        {
            if ((action & DeviceCommandAction.Activate) != 0) {
                ServiceManager.SourceManager.SetActiveSource (source);
            }

            if ((action & DeviceCommandAction.Play) != 0) {
                ServiceManager.PlaybackController.NextSource = source;
                if (!ServiceManager.PlayerEngine.IsPlaying ()) {
                    ServiceManager.PlaybackController.Next ();
                }
            }
        }

        protected virtual void OnDeviceCommand (object o, DeviceCommand command)
        {
            lock (this) {
                // Check to see if we have an already mapped disc volume that should
                // handle this incoming command; if not, queue it for later discs
                foreach (var source in Sources.Values) {
                    if (DeviceCommandMatchesSource ((AudioCd.AudioCdSource) source, command)) {
                        HandleDeviceCommand ((AudioCd.AudioCdSource) source, command.Action);
                        return;
                    }
                }

                if (unhandled_device_commands == null) {
                    unhandled_device_commands = new List<DeviceCommand> ();
                }
                unhandled_device_commands.Add (command);
            }
        }
#endregion
        string IService.ServiceName {
            get { return "DiscService"; }
        }
    }
}
