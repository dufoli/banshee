ASSEMBLY = Banshee.MediaPanel
TARGET = library
LINK = $(REF_EXTENSION_MEDIAPANEL) $(LINK_EXTENSION_PLAYQUEUE_DEPS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

@INTLTOOL_DESKTOP_RULE@

SOURCES =  \
	Banshee.MediaPanel/HeaderBox.cs \
	Banshee.MediaPanel/MediaPanel.cs \
	Banshee.MediaPanel/MediaPanelContents.cs \
	Banshee.MediaPanel/MediaPanelService.cs \
	Banshee.MediaPanel/MediaPanelTheme.cs \
	Banshee.MediaPanel/PlaybackBox.cs \
	Banshee.MediaPanel/SearchEntry.cs \
	Banshee.MediaPanel/TrackInfoDisplay.cs

RESOURCES = Banshee.MediaPanel.addin.xml

include $(top_srcdir)/build/build.mk

desktopfiles_in_files = banshee-media-panel.desktop.in
desktopfiles_files = $(desktopfiles_in_files:.desktop.in=.desktop)
desktopfilesdir = $(datadir)/applications
desktopfiles_DATA = $(desktopfiles_files)

clean-local:
	rm -f $(desktopfiles_files)

EXTRA_DIST += \
	$(desktopfiles_in_files)
