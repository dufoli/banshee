ASSEMBLY = Banshee.Video
TARGET = library
LINK = $(LINK_BANSHEE_THICKCLIENT_DEPS)
INSTALL_DIR = $(EXTENSIONS_INSTALL_DIR)

SOURCES =  \
	Banshee.Video/CastingMember.cs \
	Banshee.Video/MovieGroupSource.cs \
	Banshee.Video/TvShowGroupSource.cs \
	Banshee.Video/VideoInfo.cs \
	Banshee.Video/VideoInfoModel.cs \
	Banshee.Video/VideoInfoView.cs \
	Banshee.Video/VideoLibrarySource.cs \
	Banshee.Video/VideoService.cs \
	Banshee.Video/VideoSourceContents.cs \
    Banshee.Video.Metadata/ImdbMetadataJob.cs \
    Banshee.Video.Metadata/ImdbMetadataProvider.cs \
    Banshee.Video.Metadata/TmdbMetadataJob.cs \
    Banshee.Video.Metadata/TmdbMetadataProvider.cs \
    Banshee.Video.Metadata/TvdbMetadataJob.cs \
    Banshee.Video.Metadata/TvdbMetadataProvider.cs
	
RESOURCES = Banshee.Video.addin.xml

include $(top_srcdir)/build/build.mk
