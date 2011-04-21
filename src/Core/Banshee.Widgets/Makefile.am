ASSEMBLY = Banshee.Widgets
TARGET = library
#FIXME: not hardcode cairo-sharp here!
ASSEMBLY_BUILD_FLAGS = -unsafe -r:/usr/lib/mono/gtk-sharp-3.0/cairo-sharp.dll
LINK = $(REF_BANSHEE_WIDGETS)
SOURCES =  \
	Banshee.Widgets/CustomActionProxy.cs \
	Banshee.Widgets/DateButton.cs \
	Banshee.Widgets/DictionaryComboBox.cs \
	Banshee.Widgets/DiscUsageDisplay.cs \
	Banshee.Widgets/HoverImageButton.cs \
	Banshee.Widgets/LinearProgress.cs \
	Banshee.Widgets/LinkLabel.cs \
	Banshee.Widgets/MenuTile.cs \
	Banshee.Widgets/MessagePane.cs \
	Banshee.Widgets/PropertyTable.cs \
	Banshee.Widgets/RatingActionProxy.cs \
	Banshee.Widgets/SearchEntry.cs \
	Banshee.Widgets/SeekSlider.cs \
	Banshee.Widgets/StreamPositionLabel.cs \
	Banshee.Widgets/Tile.cs \
	Banshee.Widgets/TileView.cs \
	Banshee.Widgets/VolumeButton.cs  

RESOURCES =

include $(top_srcdir)/build/build.mk

module_SCRIPTS += Banshee.Widgets.dll.config
EXTRA_DIST += Banshee.Widgets.dll.config

