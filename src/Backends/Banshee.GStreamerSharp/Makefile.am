ASSEMBLY = Banshee.GStreamerSharp
TARGET = library
LINK = $(REF_BACKEND_GSTREAMER) $(GST_SHARP_LIBS)
INSTALL_DIR = $(BACKENDS_INSTALL_DIR)

SOURCES =  \
	Banshee.GStreamerSharp/AudioCdRipper.cs \
	Banshee.GStreamerSharp/BpmDetector.cs \
	Banshee.GStreamerSharp/CddaManager.cs \
	Banshee.GStreamerSharp/DvdManager.cs \
	Banshee.GStreamerSharp/GstExtensions.cs \
	Banshee.GStreamerSharp/PlayerEngine.cs \
	Banshee.GStreamerSharp/Transcoder.cs \
	Banshee.GStreamerSharp/VideoManager.cs \
	Banshee.GStreamerSharp/Visualization.cs

RESOURCES = Banshee.GStreamerSharp.addin.xml

include $(top_srcdir)/build/build.mk
EXTRA_DIST += Banshee.GStreamerSharp.dll.config
module_SCRIPTS += Banshee.GStreamerSharp.dll.config

