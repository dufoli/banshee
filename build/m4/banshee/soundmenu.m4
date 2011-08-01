AC_DEFUN([BANSHEE_CHECK_SOUNDMENU],
[
	AC_ARG_ENABLE([soundmenu],
		AS_HELP_STRING([--enable-soundmenu], [Enable sound menu support]),
		enable_soundmenu=$enableval, enable_soundmenu=no
	)

	AM_CONDITIONAL(ENABLE_SOUNDMENU, test "x$enable_soundmenu" = "xyes")
])

