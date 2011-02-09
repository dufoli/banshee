//
// RecommendationPane.cs
//
// Author:
//   Olivier Dufour <olivier.duff@gmail.com>
//
// Copyright (C) 2011 Olivier Dufour
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Collections.Generic;
using System.Text;

using Gtk;
using Banshee.Base;
using Banshee.Collection.Gui;
using Banshee.ServiceStack;
using Banshee.ContextPane;
using Hyena;

namespace Banshee.Video
{
    public class VideoDetailPanel : HBox
    {
        private ContextPage context_page;
        private HBox main_box;
        private VBox left;
        private VBox right;
        private Image image;
        private Label title_label;
        private TextView summary_textview;
        private Table urls_table;
        private Table members_table;
        private Label origine_label;
        private Label studios_label;
        private Label release_date_label;
        
        private LinkButton info_url_link;
        private LinkButton homepage_url_link;
        private LinkButton trailer_url_link;
        private ArtworkManager artwork_manager;

        public VideoDetailPanel (ContextPage contextPage) : base ()
        {
            this.context_page = contextPage;
            main_box = this;
            main_box.BorderWidth = 5;
            left = new VBox ();
            right = new VBox ();
            left.WidthRequest = 160;

            left.PackStart (image= new Image (), false, true, 5);
            origine_label = new Label ();
            origine_label.Justify = Justification.Left;
            left.PackStart (origine_label, true, true, 5);

            studios_label = new Label ();
            studios_label.Justify = Justification.Left;
            left.PackStart (studios_label, true, true, 5);


            release_date_label = new Label ();
            release_date_label.Justify = Justification.Left;
            left.PackStart (release_date_label, true, true, 5);

            title_label = new Label ();
            title_label.Justify = Justification.Center;
            right.PackStart (title_label, false, false, 5);

            summary_textview = new TextView ();
            summary_textview.Editable = false;
            summary_textview.WrapMode = WrapMode.WordChar;
            right.PackStart (summary_textview, true, true, 1);

            right.PackStart (members_table = new Table (2, 2, false), false, true, 1);

            right.PackStart (urls_table = new Table (3, 2, false));
            Label temp_label = new Label ();
            temp_label.Markup = "<span weight=\"bold\">Info URL:</span>";
            //temp_label.Justify = Justification.Right;
            temp_label.SetAlignment (1f, 0.5f);
            urls_table.Attach (temp_label, 0, 1, 0, 1,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            temp_label = new Label ();
            temp_label.Markup = "<span weight=\"bold\">Homepage URL:</span>";
            //temp_label.Justify = Justification.Right;
            temp_label.SetAlignment (1f, 0.5f);
            urls_table.Attach (temp_label, 0, 1, 1, 2,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            temp_label = new Label ();
            temp_label.Markup = "<span weight=\"bold\">Trailer URL:</span>";
            //temp_label.Justify = Justification.Right;
            temp_label.SetAlignment (1f, 0.5f);
            urls_table.Attach (temp_label, 0, 1, 2, 3,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            urls_table.Attach (info_url_link = new LinkButton (String.Empty), 1, 2, 0, 1,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            urls_table.Attach (homepage_url_link = new LinkButton (String.Empty), 1, 2, 1, 2,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            urls_table.Attach (trailer_url_link = new LinkButton (String.Empty), 1, 2, 2, 3,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            

            main_box.PackStart (left, false, true, 5);
            main_box.PackStart (new VSeparator (), false, false, 0);
            main_box.PackStart (right, true, true, 5);

            artwork_manager = ServiceManager.Get<ArtworkManager> ();
            // TODO
            /*
            ParentId ==> link to main serie
            VideoType 
                            */

        }

        public void Refresh ()
        {
            if (video_info == null) {
                return;
            }
            context_page.SetState (ContextState.Loading);
            Gdk.Pixbuf pixbuf = artwork_manager.LookupPixbuf (video_info.ArtworkId);
            Log.Debug (pixbuf == null? "pixbuf NULL": "pixbuf not null");
            if (pixbuf != null) {
                int width = pixbuf.Width, height = pixbuf.Height;

                if (height >= 110) {
                    width = (int)((width * 110) / height);
                    height = 110;
                }

                if (width >= 110) {
                    height = (int)((height * 110) / width);
                    width = 110;
                }
                Log.Debug (String.Format ("w:{0} h:{1} ", width, height));
                if (width != pixbuf.Width || height != pixbuf.Height) {
                    image.Pixbuf = pixbuf.ScaleSimple (width, height, Gdk.InterpType.Bilinear);
                } else {
                    image.Pixbuf = pixbuf;
                }
            } else {
                image.Hide ();
            }
            title_label.Markup = String.Format ("<span size=\"x-large\" weight=\"bold\">{0}</span>", video_info.Title);
            summary_textview.Buffer.Text = video_info.Summary;
            if (!String.IsNullOrEmpty (video_info.AlternativeTitle) && video_info.AlternativeTitle == video_info.OriginalTitle) {
                origine_label.Markup = String.Format ("<span weight=\"bold\">{0}</span> ({1}) from {2}", String.IsNullOrEmpty (video_info.OriginalTitle) ? video_info.Title : video_info.OriginalTitle, video_info.AlternativeTitle, video_info.Country);
            } else if(!String.IsNullOrEmpty (video_info.Country)) {
                origine_label.Markup = String.Format ("<span weight=\"bold\">{0}</span> from {1}", String.IsNullOrEmpty (video_info.OriginalTitle) ? video_info.Title : video_info.OriginalTitle, video_info.Country);
            } else {
                origine_label.Markup = String.Format ("<span weight=\"bold\">{0}</span>", String.IsNullOrEmpty (video_info.OriginalTitle) ? video_info.Title : video_info.OriginalTitle);
            }

            studios_label.Markup = String.Format ("<span size=\"small\">Studios: {0}</span>", video_info.Studios);
            release_date_label.Markup = String.Format ("<span size=\"small\">Release date: {0}</span>", video_info.ReleaseDate.ToShortDateString ());

            if (!String.IsNullOrEmpty (video_info.InfoUrl)) {
                info_url_link.Uri = video_info.InfoUrl;
            } else {
                //TODO Hide row
            }
            if (!String.IsNullOrEmpty (video_info.HomepageUrl)) {
                homepage_url_link.Uri = video_info.HomepageUrl;
            }
            if (!String.IsNullOrEmpty (video_info.TrailerUrl)) {
                trailer_url_link.Uri = video_info.TrailerUrl;
            }

            if (members != null) {
                RefreshMembers ();
            }

            main_box.ShowAll ();
            context_page.SetState (ContextState.Loaded);
        }

        public void RefreshMembers ()
        {
            List<string> jobs = new List<string> ();
            foreach (var member in members) {
                if (!jobs.Contains (member.Job)) {
                    jobs.Add (member.Job);
                }
            }
            if (jobs.Count == 0) {
                members_table.Hide ();
                return;
            }
            members_table.Resize ((uint)jobs.Count, 2);

            for (int i = 0; i < jobs.Count; i++) {
                string job = jobs[i];

                StringBuilder builder = new StringBuilder ();
                builder.AppendFormat ("{0}: ", job);
                foreach (var member in members) {
                    if (member.Job != job) {
                        continue;
                    }
                    if (!String.IsNullOrEmpty (member.Character)) {
                        builder.AppendFormat ("{0} ({1}), ", member.Name, member.Character);
                    } else {
                        builder.AppendFormat ("{0}, ", member.Name);
                    }
                }
                builder.Remove (builder.Length - 2, 1);
                TextView view = new TextView ();
                view.Editable = false;
                view.WrapMode = WrapMode.WordChar;
                view.Justification = Justification.Left;
                view.Buffer.Text = builder.ToString ();
                members_table.Attach (view, 0, 2, (uint)i, (uint)(i + 1),
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);

            }
        }
        private VideoInfo video_info;
        public VideoInfo VideoInfo {set {video_info = value;}}
        
        private IEnumerable<CastingMember> members;
        public IEnumerable<CastingMember> Members {set {members = value;}}
    }
}
