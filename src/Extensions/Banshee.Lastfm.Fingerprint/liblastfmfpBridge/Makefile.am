INCLUDES =

lastfmfpBridgelibdir = $(EXTENSION_DIR)
lastfmfpBridgelib_LTLIBRARIES = liblastfmfpBridge.la

liblastfmfpBridge_la_LDFLAGS = -avoid-version -module
liblastfmfpBridge_la_SOURCES =  \
	gst-lastfmfpbridge.cpp \
	HTTPClient.cpp \
	Sha256.cpp \
	Sha256File.cpp

liblastfmfpBridge_la_LIBADD = \
	$(GLIB_LIBS) \
	$(GSTREAMER_LIBS)

all: $(top_builddir)/bin/liblastfmfpBridge.so

$(top_builddir)/bin/liblastfmfpBridge.so : liblastfmfpBridge.la
	mkdir -p $(top_builddir)/bin
	cp -f liblastfmfp.so $@

CLEANFILES = $(top_builddir)/bin/liblastfmfpBridge.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(liblastfmfpBridge_la_SOURCES)
