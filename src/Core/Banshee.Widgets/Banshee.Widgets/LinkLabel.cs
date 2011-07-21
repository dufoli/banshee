/***************************************************************************
 *  LinkLabel.cs
 *
 *  Copyright (C) 2006 Novell, Inc.
 *  Written by Aaron Bockover <aaron@abock.org>
 ****************************************************************************/

/*  THIS FILE IS LICENSED UNDER THE MIT LICENSE AS OUTLINED IMMEDIATELY BELOW:
 *
 *  Permission is hereby granted, free of charge, to any person obtaining a
 *  copy of this software and associated documentation files (the "Software"),
 *  to deal in the Software without restriction, including without limitation
 *  the rights to use, copy, modify, merge, publish, distribute, sublicense,
 *  and/or sell copies of the Software, and to permit persons to whom the
 *  Software is furnished to do so, subject to the following conditions:
 *
 *  The above copyright notice and this permission notice shall be included in
 *  all copies or substantial portions of the Software.
 *
 *  THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 *  IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 *  FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 *  AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 *  LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
 *  FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 *  DEALINGS IN THE SOFTWARE.
 */

using System;
using Gtk;

using Hyena.Gui;

namespace Banshee.Widgets
{
    public class LinkLabel : EventBox
    {
        public delegate bool UriOpenHandler(string uri);

        private static UriOpenHandler default_open_handler;
        private static Gdk.Cursor hand_cursor = new Gdk.Cursor(Gdk.CursorType.Hand1);

        private Label label;
        private Uri uri;
        private bool act_as_link;
        private bool is_pressed;
        private bool is_hovering;
        private bool selectable;
        private UriOpenHandler open_handler;
        private Gdk.RGBA link_color;

        private bool interior_focus;
        private int focus_width;
        private int focus_padding;
        private int padding;

        public event EventHandler Clicked;

        public LinkLabel() : this(null, null)
        {
            Open = DefaultOpen;
        }

        public LinkLabel(string text, Uri uri)
        {
            CanFocus = true;
            AppPaintable = true;

            this.uri = uri;

            label = new Label(text);
            label.Show();

            link_color = label.StyleContext.GetBackgroundColor (StateFlags.Selected);
            ActAsLink = true;

            Add(label);
        }

        protected override void OnStyleUpdated ()
        {
            base.OnStyleUpdated ();

            CheckButton check = new CheckButton ();

            interior_focus = GtkUtilities.StyleGetProperty<bool> (check, "interior-focus", false);
            focus_width = GtkUtilities.StyleGetProperty<int> (check, "focus-line-width", -1);
            focus_padding = GtkUtilities.StyleGetProperty<int> (check, "focus-padding", -1);
            padding = interior_focus ? focus_width + focus_padding : 0;
        }

        protected virtual void OnClicked()
        {
            if(uri != null && Open != null) {
                Open(uri.AbsoluteUri);
            }

            EventHandler handler = Clicked;
            if(handler != null) {
                handler(this, new EventArgs());
            }
        }

        protected override bool OnDrawn (Cairo.Context cr)
        {
            if(!IsDrawable) {
                return false;
            }

            if(CairoHelper.ShouldDrawWindow (cr, Window) && HasFocus) {
                int layout_width = 0, layout_height = 0;
                label.Layout.GetPixelSize(out layout_width, out layout_height);
                StyleContext.Save ();
                StyleContext.AddClass ("check");
                StyleContext.RenderFocus (cr, 0, 0, layout_width + 2 * padding, layout_height + 2 * padding);
                StyleContext.Restore ();
            }

            if(Child != null) {
                PropagateDraw(Child, cr);
            }

            return false;
        }

        protected override void OnGetPreferredHeight (out int minimum_height, out int natural_height)
        {
            if (label == null) {
                base.OnGetPreferredHeight (out minimum_height, out natural_height);
                return;
            }

            minimum_height = natural_height = 0;

            int label_min_height, label_natural_height;
            label.GetPreferredHeight (out label_min_height, out label_natural_height);
            minimum_height += label_min_height;
            natural_height += label_natural_height;

            natural_height += ((int)BorderWidth + padding) * 2;
            minimum_height += ((int)BorderWidth + padding) * 2;
        }

        protected override void OnGetPreferredWidth (out int minimum_width, out int natural_width)
        {
            if (label == null) {
                base.OnGetPreferredWidth (out minimum_width, out natural_width);
                return;
            }

            minimum_width = natural_width = 0;

            int label_min_width, label_natural_width;
            label.GetPreferredWidth (out label_min_width, out label_natural_width);
            minimum_width = Math.Max (minimum_width, label_min_width);
            natural_width = Math.Max (natural_width, label_natural_width);

            minimum_width += ((int)BorderWidth + padding) * 2;
            natural_width += ((int)BorderWidth + padding) * 2;
        }

        protected override void OnSizeAllocated (Gdk.Rectangle allocation)
        {
            base.OnSizeAllocated (allocation);

            Gdk.Rectangle child_allocation = new Gdk.Rectangle ();

            if (label == null || !label.Visible) {
                return;
            }

            int total_padding = (int)BorderWidth + padding;

            child_allocation.X = total_padding;
            child_allocation.Y = total_padding;
            child_allocation.Width = (int)Math.Max (1, Allocation.Width - 2 * total_padding);
            child_allocation.Height = (int)Math.Max (1, Allocation.Height - 2 * total_padding);

            label.SizeAllocate (child_allocation);
        }

        protected override bool OnButtonPressEvent(Gdk.EventButton evnt)
        {
            if(evnt.Button == 1) {
                HasFocus = true;
                is_pressed = true;
            }

            return false;
        }

        protected override bool OnButtonReleaseEvent(Gdk.EventButton evnt)
        {
            if(evnt.Button == 1 && is_pressed && is_hovering) {
                OnClicked();
                is_pressed = false;
            }

            return false;
        }

        protected override bool OnKeyReleaseEvent(Gdk.EventKey evnt)
        {
            if(evnt.Key != Gdk.Key.KP_Enter && evnt.Key != Gdk.Key.Return
                && evnt.Key != Gdk.Key.space) {
                return  false;
            }

            OnClicked();
            return false;
        }

        protected override bool OnEnterNotifyEvent(Gdk.EventCrossing evnt)
        {
            is_hovering = true;
            Window.Cursor = hand_cursor;
            return false;
        }

        protected override bool OnLeaveNotifyEvent(Gdk.EventCrossing evnt)
        {
            is_hovering = false;
            Window.Cursor = null;
            return false;
        }

        public Pango.EllipsizeMode Ellipsize {
            get { return label.Ellipsize; }
            set { label.Ellipsize = value; }
        }

        public string Text {
            get { return label.Text; }
            set { label.Text = value; }
        }

        public string Markup {
            set { label.Markup = value; }
        }

        public Label Label {
            get { return label; }
        }

        public float Xalign {
            get { return label.Xalign; }
            set { label.Xalign = value; }
        }

        public float Yalign {
            get { return label.Yalign; }
            set { label.Yalign = value; }
        }

        public bool Selectable {
            get { return selectable; }
            set {
                if((value && !ActAsLink) || !value) {
                    label.Selectable = value;
                }

                selectable = value;
            }
        }

        public Uri Uri {
            get { return uri; }
            set { uri = value; }
        }

        public string UriString {
            get { return uri == null ? null : uri.AbsoluteUri; }
            set { uri = value == null ? null : new Uri(value); }
        }

        public UriOpenHandler Open {
            get { return open_handler; }
            set { open_handler = value; }
        }

        public bool ActAsLink {
            get { return act_as_link; }
            set {
                if(act_as_link == value) {
                    return;
                }

                act_as_link = value;

                if(act_as_link) {
                    label.Selectable = false;
                    label.OverrideColor (StateFlags.Normal, link_color);
                } else {
                    label.Selectable = selectable;
                    label.OverrideColor (StateFlags.Normal, label.StyleContext.GetColor (StateFlags.Normal));
                }

                label.QueueDraw();
            }
        }

        public static UriOpenHandler DefaultOpen {
            get { return default_open_handler; }
            set { default_open_handler = value; }
        }
    }
}
