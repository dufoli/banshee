AC_DEFUN([BANSHEE_CHECK_GSTREAMER_SHARP],
[
	AC_ARG_ENABLE(gst_sharp, AC_HELP_STRING([--enable-gst-sharp], [Enable Gst# backend]), , enable_gst_sharp="no")

	if test "x$enable_gst_sharp" = "xyes"; then
		PKG_CHECK_MODULES(GST_SHARP, gstreamer-sharp-1.0)
		AC_SUBST(GST_SHARP_LIBS)

		PLAYBACK_BACKEND="Banshee.GStreamerSharp"
		AC_SUBST(PLAYBACK_BACKEND)

		dnl Clutter support is not available in Gst# backend (and was opt-in in the unmanaged one)
		AM_CONDITIONAL(HAVE_CLUTTER, false)
	fi
])

