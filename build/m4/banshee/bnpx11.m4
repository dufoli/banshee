dnl Stolen with gratitude from Totem's configure.in

AC_DEFUN([BANSHEE_CHECK_NOW_PLAYING_X11],
[
	GRAPHICS_SUBSYSTEM="Unknown"
	have_xvidmode=no
	GTK_TARGETS=$(pkg-config --variable=targets gtk+-3.0)
	for GTK_TARGET in $GTK_TARGETS; do
		if test x$GTK_TARGET = xquartz; then
			GRAPHICS_SUBSYSTEM="Quartz"
		elif test x$GTK_TARGET = xx11; then
			GRAPHICS_SUBSYSTEM="X11"

			PKG_CHECK_MODULES(BNPX_GTK, gtk+-3.0 >= 3.0 gdk-3.0 >= 3.0)

			AC_PATH_X

			if test x"$x_includes" != x"NONE" && test -n "$x_includes" ; then
				X_INCLUDES=-I`echo $x_includes | sed -e "s/:/ -I/g"`
			fi
			if test x"$x_libraries" != x"NONE" && test -n "$x_libraries" ; then
				X_LIBRARIES=-L`echo $x_libraries | sed -e "s/:/ -L/g"`
			fi
			BNPX_CFLAGS="$X_INCLUDES $CFLAGS"
			BNPX_LIBS="$X_LIBRARIES $LIBS"
	
			PKG_CHECK_MODULES(XVIDMODE, xrandr >= 1.1.1 xxf86vm >= 1.0.1,
				have_xvidmode=yes, have_xvidmode=no)

			if test x$have_xvidmode = xyes; then
				AC_DEFINE(HAVE_XVIDMODE,, [Define this if you have the XVidMode and XRandR extension installed])
			fi

			dnl Explicit link against libX11 to avoid problems with crappy linkers
			BNPX_LIBS="$X_LIBRARIES -lX11"
			AC_SUBST(BNPX_LIBS)
			AC_SUBST(BNPX_CFLAGS)
		fi
	done
	AM_CONDITIONAL(HAVE_XVIDMODE, [test x$have_xvidmode = xyes])
	AC_SUBST(GRAPHICS_SUBSYSTEM)
])

