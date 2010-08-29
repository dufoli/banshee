INCLUDES = \
	-I$(top_srcdir) \
	-Wall \
	-ggdb3 \
	-D_FORTIFY_SOURCE=2 \
	$(GLIB_CFLAGS) \
	$(GSTREAMER_CFLAGS)

noinst_PROGRAMS = lastfmfpbrigetest

lastfmfpbrigelibdir = $(EXTENSION_DIR)
lastfmfpbrigelib_LTLIBRARIES = liblastfmfpbrige.la

liblastfmfpbrige_la_LDFLAGS = -avoid-version -module
liblastfmfpbrige_la_SOURCES = \
	gst-lastfmfpbrige.cpp \
	gst-lastfmfpbrige.h

liblastfmfpbrige_la_LIBADD = \
	$(GLIB_LIBS) \
	$(GSTREAMER_LIBS)

lastfmfpbrigetest_SOURCES = \
	lastfmfpbrigetest.cpp
lastfmfpbrigetest_LDADD = \
	liblastfmfpbrige.la \
	$(GSTREAMER_LIBS)

all: $(top_builddir)/bin/liblastfmfpbrige.so

$(top_builddir)/bin/liblastfmfpbrige.so: liblastfmfpbrige.la
	mkdir -p $(top_builddir)/bin
	cp -f liblastfmfpbrige.so $@

CLEANFILES = $(top_builddir)/bin/liblastfmfpbrige.so
MAINTAINERCLEANFILES = Makefile.in
EXTRA_DIST = $(liblastfmfpbrige_la_SOURCES)

