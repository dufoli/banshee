/*
 * last.fm Fingerprint - Accoustic fingerprint lib to get puid of song
 * inspired from mirage : http://hop.at/mirage
 * 
 * Copyright (C) 2010 Olivier Dufour <olivier(dot)duff(at)gmail(dot)com
 * Modified version of Dominik Schnitzer <dominik@schnitzer.at>
 * 
 * This program is free software; you can redistribute it and/or
 * modify it under the terms of the GNU General Public License
 * as published by the Free Software Foundation; either version 2
 * of the License, or (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor,
 * Boston, MA  02110-1301, USA.
 */

//#include <math.h>
#include <string.h>
#include <time.h>

#include <glib.h>
#include <gst/gst.h>

#include "gst-lastfmfpbridge.h"
#include "FingerprintExtractor.h"

struct LastfmfpAudio {

    // Cancel decoding mutex
    GMutex *decoding_mutex;

    // Gstreamer
    GstElement *pipeline;
    GstElement *audio;

    gint rate;
    gint filerate;
    gint seconds;
    gint winsize;
    gint samples;

	//input
    float *data_in;
    size_t input_frames;
    
    fingerprint::FingerprintExtractor extractor;
    int fpid

    gboolean quit;
    gboolean invalidate;
};

static const int NUM_FRAMES_CLIENT = 32; // ~= 10 secs.

#define SRC_BUFFERLENGTH 4096

static void
Lastfmfp_cb_newpad(GstElement *decodebin, GstPad *pad, gboolean last, LastfmfpAudio *ma)
{
    GstCaps *caps;
    GstStructure *str;
    GstPad *audiopad;

    // only link once
    audiopad = gst_element_get_pad(ma->audio, "sink");
    if (GST_PAD_IS_LINKED(audiopad)) {
        g_object_unref(audiopad);
        return;
    }

    // check media type
    caps = gst_pad_get_caps(pad);
    str = gst_caps_get_structure(caps, 0);

    if (!g_strrstr(gst_structure_get_name(str), "audio")) {
        gst_caps_unref(caps);
        gst_object_unref(audiopad);
        return;
    }
    gst_caps_unref(caps);

    // link
    gst_pad_link(pad, audiopad);
    gst_object_unref(audiopad);
}

static void
Lastfmfp_cb_have_data(GstElement *element, GstBuffer *buffer, GstPad *pad, LastfmfpAudio *ma)
{
    gint buffersamples;
    gint bufferpos;
    gint i;
    gint j;
    gint fill;

    // if data continues to flow/EOS is not yet processed
    if (ma->quit)
        return;

    // exit on empty buffer
    if (buffer->size <= 0)
        return;

    ma->data_in = (short*)GST_BUFFER_DATA(buffer);
    ma->input_frames = (size_t)(GST_BUFFER_SIZE(buffer)/sizeof(short));

    //extractor.process(const short* pPCM, size_t num_samples, bool end_of_stream = false);
    if (extractor.process(ma->data_in, ma->input_frames, false))//TODO check parametters
    {
        //we have the fingerprint
        pair<const char*, size_t> fpData = fextr.getFingerprint();
        
        // send the fingerprint data, and get the fingerprint ID
        HTTPClient client;
        string c = client.postRawObj( serverName, urlParams, 
                                    fpData.first, fpData.second, 
                                    HTTP_POST_DATA_NAME, false );
        int fpid;
        istringstream iss(c);
        iss >> fpid;
        
		//stop the gstreamer loop to free all and return fpid
        GstBus *bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
        GstMessage* eosmsg = gst_message_new_eos(GST_OBJECT(ma->pipeline));
        gst_bus_post(bus, eosmsg);
        g_print("libmirageaudio: EOS Message sent\n");
        gst_object_unref(bus);
        ma->quit = TRUE;
        return;

    }
    
    
    return;
}

LastfmfpAudio*
Lastfmfp_initialize(gint rate, gint seconds, gint winsize)
{
    LastfmfpAudio *ma;
    gint i;

    
    ma = g_new0(LastfmfpAudio, 1);
    ma->rate = rate;
    ma->seconds = seconds;
    ma->extractor = new FingerprintExtractor ();

    
    //TODO not sure if rate is good
    //ma->extractor.initForQuery(int freq, int nchannels, int duration = -1);
    ma->extractor.initForQuery(rate, winsize, seconds);
    
    // cancel decoding mutex
    ma->decoding_mutex = g_mutex_new();

    return ma;
}

void
Lastfmfp_initgstreamer(LastfmfpAudio *ma, const gchar *file)
{
    GstPad *audiopad;
    GstCaps *filter_float;
    GstCaps *filter_resample;
    GstElement *cfilt_float;
    GstElement *cfilt_resample;
    GstElement *dec;
    GstElement *src;
    GstElement *sink;
    GstElement *audioresample;
    GstElement *audioconvert;

    // Gstreamer decoder setup
    ma->pipeline = gst_pipeline_new("pipeline");

    // decoder
    src = gst_element_factory_make("filesrc", "source");
    g_object_set(G_OBJECT(src), "location", file, NULL);
    dec = gst_element_factory_make("decodebin", "decoder");
    g_signal_connect(dec, "new-decoded-pad", G_CALLBACK(Lastfmfp_cb_newpad), ma);
    gst_bin_add_many(GST_BIN(ma->pipeline), src, dec, NULL);
    gst_element_link(src, dec);

    // audio conversion
    ma->audio = gst_bin_new("audio");

    audioconvert = gst_element_factory_make("audioconvert", "conv");
    filter_float = gst_caps_new_simple("audio/x-raw-float",
         "width", G_TYPE_INT, 32,
         NULL);
    cfilt_float = gst_element_factory_make("capsfilter", "cfilt_float");
    g_object_set(G_OBJECT(cfilt_float), "caps", filter_float, NULL);
    gst_caps_unref(filter_float);

    audioresample = gst_element_factory_make("audioresample", "resample");

    filter_resample =  gst_caps_new_simple("audio/x-raw-float",
          "channels", G_TYPE_INT, 1,
          NULL);
    cfilt_resample = gst_element_factory_make("capsfilter", "cfilt_resample");
    g_object_set(G_OBJECT(cfilt_resample), "caps", filter_resample, NULL);
    gst_caps_unref(filter_resample);

    sink = gst_element_factory_make("fakesink", "sink");
    g_object_set(G_OBJECT(sink), "signal-handoffs", TRUE, NULL);
    g_signal_connect(sink, "handoff", G_CALLBACK(Lastfmfp_cb_have_data), ma);
    

    gst_bin_add_many(GST_BIN(ma->audio),
            audioconvert, audioresample,
            cfilt_resample, cfilt_float,
            sink, NULL);
    gst_element_link_many(audioconvert, cfilt_float,
           audioresample, cfilt_resample,
           sink, NULL);

    audiopad = gst_element_get_pad(audioconvert, "sink");
    gst_element_add_pad(ma->audio,
            gst_ghost_pad_new("sink", audiopad));
    gst_object_unref(audiopad);

    gst_bin_add(GST_BIN(ma->pipeline), ma->audio);

    // Get sampling rate of audio file
    GstClockTime max_wait = 1 * GST_SECOND;
    if (gst_element_set_state(ma->pipeline, GST_STATE_READY) == GST_STATE_CHANGE_ASYNC) {
        gst_element_get_state(ma->pipeline, NULL, NULL, max_wait);
    }
    if (gst_element_set_state(ma->pipeline, GST_STATE_PAUSED) == GST_STATE_CHANGE_ASYNC) {
        gst_element_get_state(ma->pipeline, NULL, NULL, max_wait);
    }

    GstPad *pad = gst_element_get_pad(sink, "sink");
    GstCaps *caps = gst_pad_get_negotiated_caps(pad);
    if (GST_IS_CAPS(caps)) {
        GstStructure *str = gst_caps_get_structure(caps, 0);
        gst_structure_get_int(str, "rate", &ma->filerate);

    } else {
        ma->filerate = -1;
    }
    gst_caps_unref(caps);
    gst_object_unref(pad);
}

int
Lastfmfp_decode(LastfmfpAudio *ma, const gchar *file, int* size, int* ret)
{
    GstBus *bus;

    ma->quit = FALSE;

    g_mutex_lock(ma->decoding_mutex);
    ma->invalidate = FALSE;
    g_mutex_unlock(ma->decoding_mutex);

    // Gstreamer setup
    Lastfmfp_initgstreamer(ma, file);
    if (ma->filerate < 0) {
        *size = 0;
        *ret = -1;

        // Gstreamer cleanup
        gst_element_set_state(ma->pipeline, GST_STATE_NULL);
        gst_object_unref(GST_OBJECT(ma->pipeline));

        return NULL;
    }

    // decode...
    gst_element_set_state(ma->pipeline, GST_STATE_PLAYING);
    g_print("libLastfmfp: decoding %s\n", file);


    bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
    gboolean decoding = TRUE;
    *ret = 0;
    while (decoding) {
        GstMessage* message = gst_bus_timed_pop_filtered(bus, GST_MSECOND*100,
                GST_MESSAGE_ERROR | GST_MESSAGE_EOS);

        if (message == NULL)
            continue;

        switch (GST_MESSAGE_TYPE(message)) {
            case GST_MESSAGE_ERROR: {
                GError *err;
                gchar *debug;

                gst_message_parse_error(message, &err, &debug);
                g_print("libLastfmfp: error: %s\n", err->message);
                g_error_free(err);
                g_free(debug);
                decoding = FALSE;
                *ret = -1;

                break;
            }
            case GST_MESSAGE_EOS: {
                g_print("libLastfmfp: EOS Message received\n");
                decoding = FALSE;
                break;
            }
            default:
                break;
        }
        gst_message_unref(message);
    }
    gst_object_unref(bus);


    g_mutex_lock(ma->decoding_mutex);

    // Gstreamer cleanup
    gst_element_set_state(ma->pipeline, GST_STATE_NULL);
    gst_object_unref(GST_OBJECT(ma->pipeline));

    if (ma->invalidate) {
        *size = 0;
        *ret = -2;
    } else {
        *size = ma->winsize/2 + 1;
    }

    g_mutex_unlock(ma->decoding_mutex);

    return ma->fpid;
}

LastfmfpAudio*
Lastfmfp_destroy(LastfmfpAudio *ma)
{
    g_print("libLastfmfp: destroy.\n");

    g_mutex_free(ma->decoding_mutex);

    // common
    free(ma);

    return NULL;
}

void
Lastfmfp_initgst()
{
    gst_init(NULL, NULL);
}

void
Lastfmfp_canceldecode(LastfmfpAudio *ma)
{
    if (GST_IS_ELEMENT(ma->pipeline)) {

        GstState state;
        gst_element_get_state(ma->pipeline, &state, NULL, 100*GST_MSECOND);

        if (state != GST_STATE_NULL) {
            g_mutex_lock(ma->decoding_mutex);

            GstBus *bus = gst_pipeline_get_bus(GST_PIPELINE(ma->pipeline));
            GstMessage* eosmsg = gst_message_new_eos(GST_OBJECT(ma->pipeline));
            gst_bus_post(bus, eosmsg);
            g_print("libLastfmfp: EOS Message sent\n");
            gst_object_unref(bus);

            ma->invalidate = TRUE;

            g_mutex_unlock(ma->decoding_mutex);
        }
    }
}
