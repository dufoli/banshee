GMCS_FLAGS+="-define:INTERNAL_NOTIFY_SHARP"

if HAVE_INDICATESHARP
GMCS_FLAGS+= "-define:HAVE_INDICATESHARP"
endif

ASSEMBLY = Banshee.SoundMenu
TARGET = library
LINK = $(REF_EXTENSION_SOUNDMENU)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.SoundMenu/SoundMenuProxy.cs \
	Banshee.SoundMenu/SoundMenuService.cs

RESOURCES = Banshee.SoundMenu.addin.xml

if ENABLE_SOUNDMENU
include $(top_srcdir)/build/build.mk
else
EXTRA_DIST = $(SOURCES) $(RESOURCES)
endif
