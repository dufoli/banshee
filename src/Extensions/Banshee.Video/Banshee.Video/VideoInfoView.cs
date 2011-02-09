// 
// VideoInfoView.cs
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

using Banshee.Collection.Gui;

using Hyena.Data.Gui;
using Hyena.Gui.Canvas;
using Banshee.ServiceStack;
using Hyena;

namespace Banshee.Video
{
    public class VideoInfoView : TrackFilterListView<VideoInfo>
    {
        public VideoInfoView ()
        {
            var layout = new DataViewLayoutGrid () {
                Fill = true,
                ChildAllocator = () => {
                    DataViewChildImage img = new DataViewChildImage () {
                        ImageSize = 90
                    };
                    return new StackPanel () {
                        Margin = new Thickness (5),
                        Orientation = Orientation.Vertical,
                        Width = 100,
                        Height = 150,
                        Spacing = 5,
                        Children = {
                                img,
                                new TextBlock () {
                                    UseMarkup = true,
                                    TextWrap = TextWrap.WordChar,
                                    TextGenerator = o => {
                                        var track = o as VideoInfo;
                                        if (track != null) {
                                            return string.Format ("<b>{0}</b>\n<small>{1}\n</small>",
                                                    track.Title, track.ReleaseDate.ToShortDateString ());
                                        }
                                        return "";
                                    }
                                }
                            },

                        // Render the prelight just on the cover art, but triggered by being anywhere over the album
                        PrelightRenderer = (cr, theme, size, o) => {
                            Prelight.Gradient (cr, theme, new Rect (img.ContentAllocation.X, img.ContentAllocation.Y, img.ImageSize, img.ImageSize), o);
                        }
                    };
                },
                View = this
            };

            ViewLayout = layout;
        }
    }
}

