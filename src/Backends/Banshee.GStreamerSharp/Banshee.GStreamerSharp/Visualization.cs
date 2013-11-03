//
// Visualization.cs
//
// Authors:
//   olivier dufour <olivier.duff@gmail.com>
//   Andrés G. Aragoneses <knocte@gmail.com>
//   Stephan Sundermann <stephansundermann@gmail.com>
//
// Copyright (C) 2011 olivier dufour.
// Copyright (C) 2013 Andrés G. Aragoneses
// Copyright (C) 2013 Stephan Sundermann
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
using System.Runtime.InteropServices;

using Gst;
using Gst.Base;

using Hyena;

using Banshee.MediaEngine;

namespace Banshee.GStreamerSharp
{
    public class Visualization
    {
        private readonly int SLICE_SIZE  = 735;
        Element vis_resampler;
        Adapter vis_buffer;
        bool active;
        bool vis_thawing;
        Gst.FFT.FFTF32 vis_fft;
        GstFFTF32Complex[] vis_fft_buffer;
        float[] vis_fft_sample_buffer;
        uint wanted_size;

        [StructLayout(LayoutKind.Sequential)]
        struct GstFFTF32Complex {
          public float r;
          public float i;
        };

        enum FFTWindow
        {
          Rectangular,
          Hamming,
          Hann,
          Bartlett,
          Blackman
        }

        public Visualization (Bin audiobin, Pad teepad)
        {
            // The basic pipeline we're constructing is:
            // .audiotee ! queue ! audioresample ! audioconvert ! fakesink

            Element converter, resampler;
            Element audiosinkqueue;
            Pad pad;

            vis_buffer = null;
            vis_fft = new Gst.FFT.FFTF32 (SLICE_SIZE * 2, false);
            vis_fft_buffer = new GstFFTF32Complex [SLICE_SIZE + 1];
            vis_fft_sample_buffer = new float [SLICE_SIZE];
            
            // Core elements, if something fails here, it's the end of the world
            audiosinkqueue = ElementFactory.Make ("queue", "vis-queue");

            pad = audiosinkqueue.GetStaticPad ("sink");
            pad.AddProbe (PadProbeType.EventDownstream, EventProbe);

            resampler = ElementFactory.Make ("audioresample", "vis-resample");
            converter = ElementFactory.Make ("audioconvert", "vis-convert");
            Element fakesink = ElementFactory.Make ("fakesink", "vis-sink");

            // channels * slice size * float size = size of chunks we want
            wanted_size = (uint)(2 * SLICE_SIZE * sizeof(float));

            if (audiosinkqueue == null || resampler == null || converter == null || fakesink == null) {
                Log.Debug ("Could not construct visualization pipeline, a fundamental element could not be created");
                return;
            }

            //http://gstreamer.freedesktop.org/data/doc/gstreamer/head/gstreamer-plugins/html/gstreamer-plugins-queue.html#GstQueueLeaky
            const int GST_QUEUE_LEAK_DOWNSTREAM = 2;

            // Keep around the 5 most recent seconds of audio so that when resuming
            // visualization we have something to show right away.
            audiosinkqueue ["leaky"] = GST_QUEUE_LEAK_DOWNSTREAM;
            audiosinkqueue ["max-size-buffers"] = 0;
            audiosinkqueue ["max-size-bytes"] = 0;
            audiosinkqueue ["max-size-time"] = ((long)Constants.SECOND) * 5L;

            fakesink.Connect ("handoff", PCMHandoff);

            // This enables the handoff signal.
            fakesink ["signal-handoffs"] = true;
            // Synchronize so we see vis at the same time as we hear it.
            fakesink ["sync"] = true;
            // Drop buffers if they come in too late.  This is mainly used when
            // thawing the vis pipeline.
            fakesink ["max-lateness"] = (long)(Constants.SECOND / 120);
            // Deliver buffers one frame early.  This allows for rendering
            // time.  (TODO: It would be great to calculate this on-the-fly so
            // we match the rendering time.
            fakesink ["ts-offset"] = -(long)(Constants.SECOND / 60);
            // Don't go to PAUSED when we freeze the pipeline.
            fakesink ["async"] = false;

            //FIXME BEFORE PUSHING NEW GST# BACKEND: this line below is commented to make playback work :(
            //audiobin.Add (audiosinkqueue);
            audiobin.Add (resampler, converter, fakesink);

            pad = audiosinkqueue.GetStaticPad ("sink");
            teepad.Link (pad);
            
            Element.Link (audiosinkqueue, resampler, converter);
            
            converter.LinkFiltered (fakesink, caps);
            
            vis_buffer = new Adapter ();
            vis_resampler = resampler;
            vis_thawing = false;
            active = false;
        
            // Disable the pipeline till we hear otherwise from managed land.
            Blocked = true;
        }

        public bool Active
        {
            set {
                Blocked = !value;
                active = value;
            }
        }

        private Caps caps = Caps.FromString (
            "audio/x-raw, " +
            "rate = (int) 44100, " +
            "channels = (int) 2, " +
            "endianness = (int) BYTE_ORDER, " +
            "width = (int) 32");

        ulong? block_probe = null;
        private bool Blocked
        {
            set {
                if (vis_resampler == null)
                    return;

                Pad queue_sink = vis_resampler.GetStaticPad ("src");
                if (value) {
                    if (!block_probe.HasValue) {
                        block_probe = queue_sink.AddProbe (PadProbeType.Block, (o, i) => PadProbeReturn.Ok);
                    }
                } else {
                    if (block_probe.HasValue) {
                        queue_sink.RemoveProbe (block_probe.Value);
                        block_probe = null;
                    }

                    // Set thawing mode (discards buffers that are too old from the queue).
                    vis_thawing = true;
                }
            }
        }

        private event VisualizationDataHandler OnDataAvailable = null;
        public event VisualizationDataHandler DataAvailable {
            add {
                if (value == null) {
                    return;
                } else if (OnDataAvailable == null) {
                    Active = true;
                }

                OnDataAvailable += value;
            }

            remove {
                if (value == null) {
                    return;
                }

                OnDataAvailable -= value;

                if (OnDataAvailable == null) {
                    Active = false;
                }
            }
        }

        private void PCMHandoff (object o, GLib.SignalArgs args)
        {
            throw new NotImplementedException ();
            /*
            Gst.Buffer data;

            if (OnDataAvailable == null) {
                return;
            }

            var handoff_args = args as FakeSink.HandoffArgs;

            if (vis_thawing) {
                // Flush our buffers out.
                vis_buffer.Clear ();
                System.Array.Clear (vis_fft_sample_buffer, 0, vis_fft_sample_buffer.Length);
        
                vis_thawing = false;
            }
        
            Structure structure = args.Buffer.Caps [0];
            int channels = (int)structure.GetValue ("channels");
        
            wanted_size = (uint)(channels * SLICE_SIZE * sizeof (float));

            //TODO see if buffer need a copy or not
            //but copy is no available in gst# ;(
            vis_buffer.Push (args.Buffer);
            int i, j;
            while ((data = vis_buffer.Peek (wanted_size)) != null) {
                float[] buff = new float[data.Size];
                Marshal.Copy (data.Data, buff, 0, (int) data.Size);
                float[] deinterlaced = new float [channels * SLICE_SIZE];
                float[] specbuf = new float [SLICE_SIZE * 2];

                System.Array.Copy (specbuf, vis_fft_sample_buffer, SLICE_SIZE);

                for (i = 0; i < SLICE_SIZE; i++) {
                    float avg = 0.0f;
        
                    for (j = 0; j < channels; j++) {
                        float sample = buff[i * channels + j];
        
                        deinterlaced[j * SLICE_SIZE + i] = sample;
                        avg += sample;
                    }

                    avg /= channels;
                    specbuf[i + SLICE_SIZE] = avg;
                }

                System.Array.Copy (vis_fft_sample_buffer, 0, specbuf, SLICE_SIZE, SLICE_SIZE);

                gst_fft_f32_window (vis_fft, specbuf, FFTWindow.Hamming);
                gst_fft_f32_fft (vis_fft, specbuf, vis_fft_buffer);

                for (i = 0; i < SLICE_SIZE; i++) {
                    float val;

                    GstFFTF32Complex cplx = vis_fft_buffer[i];

                    val = cplx.r * cplx.r + cplx.i * cplx.i;
                    val /= SLICE_SIZE * SLICE_SIZE;
                    val = (float)(10.0f * System.Math.Log10 ((double)val));

                    val = (val + 60.0f) / 60.0f;
                    if (val < 0.0f)
                        val = 0.0f;

                    specbuf[i] = val;
                }

                float [] flat = new float[channels * SLICE_SIZE];
                System.Array.Copy (deinterlaced, flat, flat.Length);

                float [][] cbd = new float[channels][];
                for (int k = 0; k < channels; k++) {
                    float [] channel = new float[SLICE_SIZE];
                    System.Array.Copy (flat, k * SLICE_SIZE, channel, 0, SLICE_SIZE);
                    cbd [k] = channel;
                }

                float [] spec = new float [SLICE_SIZE];
                System.Array.Copy (specbuf, spec, SLICE_SIZE);

                try {
                    OnDataAvailable (cbd, new float[][] { spec });
                } catch (System.Exception e) {
                    Log.Exception ("Uncaught exception during visualization data post.", e);
                }

                vis_buffer.Flush ((uint)wanted_size);
            }
            */
        }

        PadProbeReturn EventProbe (Pad pad, PadProbeInfo info)
        {
            var padEvent = info.Event;
            switch (padEvent.Type) {
                case EventType.FlushStart:
                case EventType.FlushStop:
                case EventType.Seek:
                case EventType.Segment:
                case EventType.CustomDownstream:
                    vis_thawing = true;
                break;
            }
        
            if (active)
                return PadProbeReturn.Pass;

            switch (padEvent.Type) {
                case EventType.Eos:
                case EventType.CustomDownstreamOob:
                    Blocked = false;
                    break;

                case EventType.Segment:
                case EventType.CustomDownstream:
                    Blocked = true;
                    break;
            }

            return PadProbeReturn.Pass;
        }
    }
}
