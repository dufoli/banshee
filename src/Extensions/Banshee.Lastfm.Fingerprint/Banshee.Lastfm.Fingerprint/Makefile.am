ASSEMBLY = Banshee.Lastfm.Fingerprint
TARGET = library
LINK =  \
	$(REF_EXTENSION_LASTFM) \
	-r:../../../../bin/Banshee.Core.dll \
	-r:../../../../bin/Banshee.Services.dll \
	-r:../../../../bin/Banshee.ThickClient.dll \
	-r:../../../../bin/Banshee.Widgets.dll \
	-r:../../../../bin/Hyena.Data.Sqlite.dll \
	-r:../../../../bin/Hyena.dll \
	-r:../../../../bin/Hyena.Gui.dll \
	-r:../../../../bin/Lastfm.dll \
	-r:../../../../bin/Lastfm.Gui.dll \
	-r:../../../../bin/Mono.Media.dll \
	-r:Mono.Security
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)
GMCS_FLAGS = -unsafe

SOURCES =  \
	Banshee.Lastfm.Fingerprint/AudioDecoder.cs \
	Banshee.Lastfm.Fingerprint/FingerprintJob.cs \
	Banshee.Lastfm.Fingerprint/LastfmFingerprintService.cs


RESOURCES =  \
	Banshee.Lastfm.Fingerprint.addin.xml \
	Resources/GlobalUI.xml


include $(top_srcdir)/build/build.mk
EXTRA_DIST += Banshee.Lastfm.Fingerprint.dll.config
module_DATA = Banshee.Lastfm.Fingerprint.dll.config
module_SCRIPTS += Banshee.Lastfm.Fingerprint.dll.config
