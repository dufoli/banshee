INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(GLIB_CFLAGS) \
	$(GSTREAMER_CFLAGS)

lastfmfpbrigelibdir = $(EXTENSION_DIR)
lastfmfpbrigelib_LTLIBRARIES = liblastfmfpbrige.la

liblastfmfpbrige_la_LDFLAGS = -avoid-version -module
liblastfmfpbrige_la_SOURCES = gst-lastfmfpbridge.cpp

liblastfmfpbrige_la_LIBADD = \
	$(GLIB_LIBS) \
	$(GSTREAMER_LIBS)

all: $(top_builddir)/bin/liblastfmfpbrige.so

$(top_builddir)/bin/liblastfmfp.so : liblastfmfp.la
	mkdir -p $(top_builddir)/bin
	cp -f liblastfmfp.so $@

CLEANFILES = $(top_builddir)/bin/liblastfmfpbrige.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(liblastfmfpbrige_la_SOURCES)

