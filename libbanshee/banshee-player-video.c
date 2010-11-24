//
// banshee-player-video.c
//
// Author:
//   Aaron Bockover <abockover@novell.com>
//   Julien Moutte <julien@fluendo.com>
//
// Copyright (C) 2005-2008 Novell, Inc.
// Copyright (C) 2010 Fluendo S.A.
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

#include "banshee-player-video.h"

// ---------------------------------------------------------------------------
// Private Functions
// ---------------------------------------------------------------------------

#if defined(GDK_WINDOWING_X11) || defined(GDK_WINDOWING_WIN32)

static gboolean
bp_video_find_xoverlay (BansheePlayer *player)
{
    GstElement *video_sink = NULL;
    GstElement *xoverlay;
    GstXOverlay *previous_xoverlay;
    gboolean    found_xoverlay;

    g_object_get (player->playbin, "video-sink", &video_sink, NULL);

    g_mutex_lock (player->video_mutex);
    previous_xoverlay = player->xoverlay;

    if (video_sink == NULL) {
        player->xoverlay = NULL;
        if (previous_xoverlay != NULL) {
            gst_object_unref (previous_xoverlay);
        }
        g_mutex_unlock (player->video_mutex);
        return FALSE;
    }
    
    xoverlay = GST_IS_BIN (video_sink)
        ? gst_bin_get_by_interface (GST_BIN (video_sink), GST_TYPE_X_OVERLAY)
        : video_sink;
    
    player->xoverlay = GST_IS_X_OVERLAY (xoverlay) ? GST_X_OVERLAY (xoverlay) : NULL;
    
    if (previous_xoverlay != NULL) {
        gst_object_unref (previous_xoverlay);
    }
        
#if !defined(GDK_WINDOWING_WIN32) // We can't rely on aspect ratio from dshowvideosink
    if (player->xoverlay != NULL && g_object_class_find_property (
        G_OBJECT_GET_CLASS (player->xoverlay), "force-aspect-ratio")) {
        g_object_set (G_OBJECT (player->xoverlay), "force-aspect-ratio", TRUE, NULL);
    }
#endif
    
    if (player->xoverlay != NULL && g_object_class_find_property (
        G_OBJECT_GET_CLASS (player->xoverlay), "handle-events")) {
        g_object_set (G_OBJECT (player->xoverlay), "handle-events", FALSE, NULL);
    }

    gst_object_unref (video_sink);
    found_xoverlay = (player->xoverlay != NULL) ? TRUE : FALSE;

    g_mutex_unlock (player->video_mutex);
    return found_xoverlay;
}

#endif /* GDK_WINDOWING_X11 || GDK_WINDOWING_WIN32 */

P_INVOKE int
bp_get_subtitle_count (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), 0);

    int n_text;
    g_object_get (G_OBJECT (player->playbin), "n-text", &n_text, NULL);
    return n_text;
}

P_INVOKE void
bp_set_subtitle (BansheePlayer *player, int index)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    int n_text = bp_get_subtitle_count (player);

    if (n_text == 0 || index < 0 || index >= n_text)
        return;

    g_object_set (G_OBJECT (player->playbin), "current-text", index, NULL);
}

static void
bp_video_sink_element_added (GstBin *videosink, GstElement *element, BansheePlayer *player)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    #if defined(GDK_WINDOWING_X11) || defined(GDK_WINDOWING_WIN32)
    bp_video_find_xoverlay (player);
    #endif
}

static void
bp_video_bus_element_sync_message (GstBus *bus, GstMessage *message, BansheePlayer *player)
{
    gboolean found_xoverlay;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

    #if defined(GDK_WINDOWING_X11) || defined(GDK_WINDOWING_WIN32)

    if (message->structure == NULL || !gst_structure_has_name (message->structure, "prepare-xwindow-id")) {
        return;
    }

    found_xoverlay = bp_video_find_xoverlay (player);

    if (found_xoverlay) {
        gst_x_overlay_set_xwindow_id (player->xoverlay, player->video_window_xid);
    }

    #endif
}

// ---------------------------------------------------------------------------
// Internal Functions
// ---------------------------------------------------------------------------

static void
cb_caps_set (GObject *obj, GParamSpec *pspec, BansheePlayer *p)
{
    GstStructure * s = NULL;
    GstCaps * caps = gst_pad_get_negotiated_caps (GST_PAD (obj));

    if (G_UNLIKELY (!caps)) {
        return;
    }

    /* Get video decoder caps */
    s = gst_caps_get_structure (caps, 0);
    if (s) {
        const GValue *par;

        /* We need at least width/height and framerate */
        if (!(gst_structure_get_fraction (s, "framerate", &p->fps_n, &p->fps_d) &&
            gst_structure_get_int (s, "width", &p->width) && gst_structure_get_int (s, "height", &p->height))) {
            return;
        }

        /* Get the PAR if available */
        par = gst_structure_get_value (s, "pixel-aspect-ratio");
        if (par) {
            p->par_n = gst_value_get_fraction_numerator (par);
            p->par_d = gst_value_get_fraction_denominator (par);
        }
        else { /* Square pixels */
            p->par_n = 1;
            p->par_d = 1;
        }

        /* Notify PlayerEngine if a callback was set */
        if (p->video_geometry_notify_cb != NULL) {
            p->video_geometry_notify_cb (p, p->width, p->height, p->fps_n, p->fps_d, p->par_n, p->par_d);
        }
    }

    gst_caps_unref (caps);
}

void
_bp_parse_stream_info (BansheePlayer *player)
{
    gint audios_streams, video_streams, text_streams;
    GstPad *vpad = NULL;

    g_object_get (G_OBJECT (player->playbin), "n-audio", &audios_streams,
        "n-video", &video_streams, "n-text", &text_streams, NULL);

    if (video_streams) {
        gint i;
        /* Try to obtain a video pad */
        for (i = 0; i < video_streams && vpad == NULL; i++) {
            g_signal_emit_by_name (player->playbin, "get-video-pad", i, &vpad);
        }
    }

    if (G_LIKELY (vpad)) {
        GstCaps *caps = gst_pad_get_negotiated_caps (vpad);
        if (G_LIKELY (caps)) {
            cb_caps_set (G_OBJECT (vpad), NULL, player);
            gst_caps_unref (caps);
        }
        g_signal_connect (vpad, "notify::caps", G_CALLBACK (cb_caps_set), player);
        gst_object_unref (vpad);
    }
}

void
_bp_video_pipeline_setup (BansheePlayer *player, GstBus *bus)
{
    GstElement *videosink;
    
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (player->video_pipeline_setup_cb != NULL) {
        videosink = player->video_pipeline_setup_cb (player, bus);
        if (videosink != NULL && GST_IS_ELEMENT (videosink)) {
            g_object_set (G_OBJECT (player->playbin), "video-sink", videosink, NULL);
            player->video_display_context_type = BP_VIDEO_DISPLAY_CONTEXT_CUSTOM;
            return;
        }
    }
    
    #if defined(GDK_WINDOWING_X11) || defined(GDK_WINDOWING_WIN32)

    player->video_display_context_type = BP_VIDEO_DISPLAY_CONTEXT_GDK_WINDOW;
    
    videosink = gst_element_factory_make ("gconfvideosink", "videosink");
    if (videosink == NULL) {
        videosink = gst_element_factory_make ("autovideosink", "videosink");
        if (videosink == NULL) {
            player->video_display_context_type = BP_VIDEO_DISPLAY_CONTEXT_UNSUPPORTED;
            videosink = gst_element_factory_make ("fakesink", "videosink");
            if (videosink != NULL) {
                g_object_set (G_OBJECT (videosink), "sync", TRUE, NULL);
            }
        }
    }
    
    g_object_set (G_OBJECT (player->playbin), "video-sink", videosink, NULL);
    
    gst_bus_set_sync_handler (bus, gst_bus_sync_signal_handler, player);
    g_signal_connect (bus, "sync-message::element", G_CALLBACK (bp_video_bus_element_sync_message), player);
        
    if (GST_IS_BIN (videosink)) {
        g_signal_connect (videosink, "element-added", G_CALLBACK (bp_video_sink_element_added), player);
    }
    
    #else
    
    player->video_display_context_type = BP_VIDEO_DISPLAY_CONTEXT_UNSUPPORTED;

    #ifndef WIN32

    videosink = gst_element_factory_make ("fakesink", "videosink");
    if (videosink != NULL) {
        g_object_set (G_OBJECT (videosink), "sync", TRUE, NULL);
    }
    
    g_object_set (G_OBJECT (player->playbin), "video-sink", videosink, NULL);

    #endif
    
    #endif

    if (player->video_prepare_window_cb != NULL) {
        player->video_prepare_window_cb (player);
    }
}

P_INVOKE void
bp_set_video_pipeline_setup_callback (BansheePlayer *player, BansheePlayerVideoPipelineSetupCallback cb)
{
    SET_CALLBACK (video_pipeline_setup_cb);
}

P_INVOKE void
bp_set_video_geometry_notify_callback (BansheePlayer *player, BansheePlayerVideoGeometryNotifyCallback cb)
{
    SET_CALLBACK (video_geometry_notify_cb);
}

P_INVOKE void
bp_set_video_prepare_window_callback (BansheePlayer *player, BansheePlayerVideoPrepareWindowCallback cb)
{
    SET_CALLBACK (video_prepare_window_cb);
}

// ---------------------------------------------------------------------------
// Public Functions
// ---------------------------------------------------------------------------

#if defined(GDK_WINDOWING_X11) || defined(GDK_WINDOWING_WIN32)

P_INVOKE BpVideoDisplayContextType
bp_video_get_display_context_type (BansheePlayer *player)
{
    return player->video_display_context_type;
}

P_INVOKE void
bp_video_set_display_context (BansheePlayer *player, gpointer context)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (bp_video_get_display_context_type (player) == BP_VIDEO_DISPLAY_CONTEXT_GDK_WINDOW) {
        player->video_window = (GdkWindow *)context;
    }
}

P_INVOKE gpointer
bp_video_get_display_context (BansheePlayer *player)
{
    g_return_val_if_fail (IS_BANSHEE_PLAYER (player), NULL);
   
    if (bp_video_get_display_context_type (player) == BP_VIDEO_DISPLAY_CONTEXT_GDK_WINDOW) {
        return player->video_window;
    }
    
    return NULL;
}

P_INVOKE void
bp_video_window_expose (BansheePlayer *player, GdkWindow *window, gboolean direct)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));
    
    if (direct && player->xoverlay != NULL && GST_IS_X_OVERLAY (player->xoverlay)) {
        gst_x_overlay_expose (player->xoverlay);
        return;
    }

    if (player->xoverlay == NULL && !bp_video_find_xoverlay (player)) {
        return;
    }
    
    gst_object_ref (player->xoverlay);

    gst_x_overlay_set_xwindow_id (player->xoverlay, player->video_window_xid);
    gst_x_overlay_expose (player->xoverlay);

    gst_object_unref (player->xoverlay);
}

// MUST be called from the GTK main thread; calling it in OnRealized will do the right thing.
P_INVOKE void
bp_video_window_realize (BansheePlayer *player, GdkWindow *window)
{
    g_return_if_fail (IS_BANSHEE_PLAYER (player));

// Code commented out - this requires including gtk/gtk.h for GTK_CHECK_VERSION, which requires too many
// buildsystem changes for the benefit of a single debug message in the failure case.
//
//#if GTK_CHECK_VERSION(2,18,0)
//    //Explicitly create the native window.  GDK_WINDOW_XWINDOW will call this
//    //function anyway, but this way we can raise a more useful message should it fail.
//    if (!gdk_window_ensure_native (window)) {
//        banshee_log (BANSHEE_LOG_TYPE_ERROR, "player-video", "Couldn't create native window needed for GstXOverlay!");
//    }
//#endif

#if defined(GDK_WINDOWING_X11)
    player->video_window_xid = GDK_WINDOW_XID (window);
#elif defined (GDK_WINDOWING_WIN32)
    player->video_window_xid = GDK_WINDOW_HWND (window);
#endif
}

#else /* GDK_WINDOWING_X11 || GDK_WINDOWING_WIN32 */

P_INVOKE BpVideoDisplayContextType
bp_video_get_display_context_type (BansheePlayer *player)
{
    return player->video_display_context_type;
}

P_INVOKE void
bp_video_set_display_context (BansheePlayer *player, gpointer context)
{
}

P_INVOKE gpointer
bp_video_get_display_context (BansheePlayer *player)
{
    return NULL;
}

P_INVOKE void
bp_video_window_expose (BansheePlayer *player, GdkWindow *window, gboolean direct)
{
}

#endif /* GDK_WINDOWING_X11 || GDK_WINDOWING_WIN32 */
