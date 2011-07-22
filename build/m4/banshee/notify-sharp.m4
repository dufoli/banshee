AC_DEFUN([BANSHEE_CHECK_NOTIFY_SHARP],
[
	AC_ARG_WITH([system-notify-sharp],
		AC_HELP_STRING([--with-system-notify-sharp], [Use the notify-sharp library installed on the system]),
		use_system_notifysharp="yes", use_system_notifysharp="no")

	if test "x$use_system_notifysharp" = "xyes"; then
		PKG_CHECK_MODULES(NOTIFY_SHARP, notify-sharp)
		AC_SUBST(NOTIFY_SHARP_LIBS)
		AM_CONDITIONAL(EXTERNAL_NOTIFY_SHARP, true)
	else
		AM_CONDITIONAL(EXTERNAL_NOTIFY_SHARP, false)
		AC_MSG_NOTICE([Using internal copy of notify-sharp])
	fi
])

