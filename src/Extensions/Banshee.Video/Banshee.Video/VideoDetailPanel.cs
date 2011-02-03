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
            
            left.PackStart (image= new Image ());
            left.PackStart (origine_label = new Label ());
            left.PackStart (studios_label = new Label ());
            left.PackStart (release_date_label = new Label ());
            
            right.PackStart (title_label = new Label ());
            right.PackStart (summary_textview = new TextView ());
            right.PackStart (members_table = new Table (2, 2, false));
            
            right.PackStart (urls_table = new Table (3, 2, false));
            urls_table.Attach (new Label ("Info URL:"), 0, 1, 0, 1,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            urls_table.Attach (new Label ("Homepage URL:"), 0, 1, 1, 2,
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
            urls_table.Attach (new Label ("Trailer URL:"), 0, 1, 2, 3,
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
            

            main_box.PackStart (left, true, true, 5);
            main_box.PackStart (new VSeparator (), false, false, 0);
            main_box.PackStart (right, false, false, 0);

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
            if (pixbuf != null) {
                image.Pixbuf =  pixbuf;
            } else {
                image.Hide ();
            }
            title_label.Text = video_info.Title;
            summary_textview.Buffer.Text = video_info.Summary;
            origine_label.Text = String.Format ("Original title: {0} (1) from {2}", video_info.OriginalTitle, video_info.AlternativeTitle, video_info.Country);
            studios_label.Text = String.Format ("Studios: {0}", video_info.Studios);
            release_date_label.Text = String.Format ("Release date: {0}", video_info.ReleaseDate.ToShortDateString ());
            
            info_url_link.Uri = video_info.InfoUrl;
            homepage_url_link.Uri = video_info.HomepageUrl;
            trailer_url_link.Uri = video_info.TrailerUrl;
            
            List<string> jobs = new List<string> ();
            foreach (var member in members) {
                if (!jobs.Contains (member.Job)) {
                    jobs.Add (member.Job);
                }
            }
            members_table.Resize ((uint)jobs.Count, 2);
            
            for (int i = 0; i < jobs.Count; i++) {
                string job = jobs[i];
                
                members_table.Attach (new Label (String.Format("{0}: ", job)), 0, 1, (uint)i, (uint)(i + 1),
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
                    
                StringBuilder builder = new StringBuilder ();
                foreach (var member in members) {
                    if (member.Job != job) {
                        continue;
                    }
                    builder.AppendFormat ("{0} ({1}),", member.Name, member.Character);
                }
                builder.Remove (builder.Length - 2, 1);
                TextView view = new TextView ();
                view.Buffer.Text = builder.ToString ();
                members_table.Attach (view, 1, 2, (uint)i, (uint)(i + 1),
                    AttachOptions.Expand | AttachOptions.Fill,
                    AttachOptions.Shrink, 0, 0);
                    
            }
            main_box.ShowAll ();
            context_page.SetState (ContextState.Loaded);
        }

        private VideoInfo video_info;
        public VideoInfo VideoInfo {set {video_info = value;}}
        
        private List<CastingMember> members;
        public List<CastingMember> Members {set {members = value;}}
    }
}
