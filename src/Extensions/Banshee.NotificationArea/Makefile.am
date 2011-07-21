GMCS_FLAGS+="-define:INTERNAL_NOTIFY_SHARP"
NOTIFY_SHARP_LIBS = 

if HAVE_GDK_2_22
GMCS_FLAGS+= "-define:HAVE_GDK_2_22"
endif

ASSEMBLY = Banshee.NotificationArea
TARGET = library
LINK = $(REF_EXTENSION_NOTIFICATIONAREA)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.NotificationArea/GtkNotificationAreaBox.cs \
	Banshee.NotificationArea/INotificationAreaBox.cs \
	Banshee.NotificationArea/NotificationAreaService.cs \
	Banshee.NotificationArea/TrackInfoPopup.cs \
	Notifications/Notification.cs \
	Notifications/Notifications.cs

RESOURCES =  \
	Banshee.NotificationArea.addin.xml \
	Resources/NotificationAreaMenu.xml

if PLATFORM_DARWIN
EXTRA_DIST = $(SOURCES) $(RESOURCES)
else
include $(top_srcdir)/build/build.mk
module_SCRIPTS += Banshee.NotificationArea.dll.config
EXTRA_DIST += Banshee.NotificationArea.dll.config $(SOURCES) $(RESOURCES)
endif
