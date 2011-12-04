AC_DEFUN([BANSHEE_CHECK_LIBBANSHEE],
[
	AC_ISC_POSIX
	AC_PROG_CC

	AC_HEADER_STDC

	AM_PATH_GLIB_2_0

	LIBBANSHEE_LIBS=""
	LIBBANSHEE_CFLAGS=""

	PKG_CHECK_MODULES(GTK, gtk+-3.0 >= 3.0)
	SHAMROCK_CONCAT_MODULE(LIBBANSHEE, GTK)

	GTK_TARGETS=$(pkg-config --variable=targets gtk+-3.0)
	for GTK_TARGET in $GTK_TARGETS; do
		if test x$GTK_TARGET = xx11; then
			GRAPHICS_SUBSYSTEM_X11="yes"
		elif test x$GTK_TARGET = xquartz; then
			GRAPHICS_SUBSYSTEM_QUARTZ="yes"
		fi
	done

	AC_ARG_ENABLE(clutter, AS_HELP_STRING([--enable-clutter],
		[Enable support for clutter video sink]), , enable_clutter="no")

	if test "x$enable_clutter" = "xyes"; then
		PKG_CHECK_MODULES(CLUTTER,
			clutter-1.0 >= 1.0.1,
			enable_clutter=yes)
		SHAMROCK_CONCAT_MODULE(LIBBANSHEE, CLUTTER)
		AC_DEFINE(HAVE_CLUTTER, 1,
			[Define if the video sink should be Clutter])
	fi

	AM_CONDITIONAL(HAVE_X11, test "x$GRAPHICS_SUBSYSTEM_X11" = "xyes")
	AM_CONDITIONAL(HAVE_QUARTZ, test "x$GRAPHICS_SUBSYSTEM_QUARTZ" = "xyes")
	AM_CONDITIONAL(HAVE_CLUTTER, test "x$enable_clutter" = "xyes")

	AC_SUBST(GRAPHICS_SUBSYSTEM)
	AC_SUBST(LIBBANSHEE_CFLAGS)
	AC_SUBST(LIBBANSHEE_LIBS)
])

