// 
// VideoInfoModel.cs
// 
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
// 
// Copyright 2011 
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Banshee.Collection.Database;
using Banshee.Database;
using Banshee.I18n;
using Hyena.Query;
namespace Banshee.Video
{
    //TODO DatabaseVideoInfo, VideoInfo
    public class VideoInfoModel : DatabaseFilterListModel<VideoInfo, VideoInfo>
    {
        public VideoInfoModel (Banshee.Sources.DatabaseSource source, DatabaseTrackListModel trackModel, BansheeDbConnection connection, string uuid)
            : base ("Videos", Catalog.GetString ("Videos"),
                    source, trackModel, connection, VideoInfo.Provider, new VideoInfo (), uuid)
        {
            QueryFields = new QueryFieldSet (VideoService.VideoTitleField, VideoService.VideoOriginalTitleField, VideoService.VideoAlternativeTitleField );

            //int video_library_dbid = (source as VideoLibrarySource ?? source.Parent as VideoLibrarySource).DbId;
            ReloadFragmentFormat = @"
                    FROM Videos WHERE EXISTS
                        (SELECT * FROM CoreTracks, CoreCache{0}
                            WHERE Videos.ParentId = 0
                            AND   CoreTracks.ExternalID = Videos.VideoID
                            AND   CoreCache.ModelID = {1} AND CoreCache.ItemId = {2} {3}
                        )
                        OR EXISTS
                        (SELECT * FROM Videos AS episode, CoreTracks, CoreCache{0}
                            WHERE episode.ParentId != 0
                            AND   episode.ParentId = Videos.VideoID
                            AND   CoreTracks.ExternalID = episode.VideoID
                            AND   CoreCache.ModelID = {1} AND CoreCache.ItemId = {2} {3}
                        )
                    ORDER BY Videos.Title";
        }

        public override string FilterColumn {
            get { return "CoreTracks.ExternalId"; }
        }

        protected override string ItemToFilterValue (object item)
        {
            return (item is VideoInfo) ? (item as VideoInfo).DbId.ToString () : null;
        }

        public override void UpdateSelectAllItem (long count)
        {
            select_all_item.Title = String.Format (Catalog.GetString ("All Videos ({0})"), count);
        }
    }
}

