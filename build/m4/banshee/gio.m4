AC_DEFUN([BANSHEE_CHECK_GIO_SHARP],
[
	GIOSHARP_REQUIRED=2.99
	GUDEVSHARP_REQUIRED=3.0
	
	AC_ARG_ENABLE(gio, AC_HELP_STRING([--disable-gio], [Disable GIO for IO operations]), ,enable_gio="yes")
	AC_ARG_ENABLE(gio_hardware, AC_HELP_STRING([--disable-gio-hardware], [Disable GIO Hardware backend]), ,enable_gio_hardware="yes")
	
	if test "x$enable_gio" = "xyes"; then

		has_gio_sharp=no
		PKG_CHECK_MODULES(GIOSHARP,
			gio-sharp-3.0 >= $GIOSHARP_REQUIRED,
			has_gio_sharp=yes, has_gio_sharp=no)
		if test "x$has_gio_sharp" = "xno"; then
			AC_MSG_ERROR([gio-sharp-3.0 was not found or is not up to date. Please install gio-sharp-3.0 of at least version $GIOSHARP_REQUIRED, or disable GIO support by passing --disable-gio])
		fi

		if test "x$enable_gio_hardware" = "xyes"; then

			has_gudev_sharp=no
			PKG_CHECK_MODULES(GUDEV_SHARP,
				gudev-sharp-3.0 >= $GUDEVSHARP_REQUIRED,
				has_gudev_sharp=yes, has_gudev_sharp=no)

			if test "x$has_gudev_sharp" = "xno"; then
				AC_MSG_ERROR([gudev-sharp-3.0 was not found or is not up to date. Please install gudev-sharp-3.0 of at least version $GUDEVSHARP_REQUIRED, or disable GIO Hardware support by passing --disable-gio-hardware])
			fi

			if test "x$enable_gio_hardware" = "xno"; then
				GUDEV_SHARP_LIBS=''
			fi
		fi

		AM_CONDITIONAL(ENABLE_GIO, true)
		AM_CONDITIONAL(ENABLE_GIO_HARDWARE, test "x$enable_gio_hardware" = "xyes")
	else
		enable_gio_hardware="no"
		AM_CONDITIONAL(ENABLE_GIO, false)
		AM_CONDITIONAL(ENABLE_GIO_HARDWARE, false)
	fi
])

