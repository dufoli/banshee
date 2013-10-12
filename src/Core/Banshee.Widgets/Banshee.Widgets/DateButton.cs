
/***************************************************************************
 *  DateButton.cs
 *
 *  Copyright (C) 2005 Novell
 *  Written by Aaron Bockover (aaron@aaronbock.net)
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

namespace Banshee.Widgets
{
    public class DateButton : ToggleButton
    {
        private Window popup;
        private DateTime date;
        private Calendar cal;
        private string displayFormat;

        private const uint CURRENT_TIME = 0;
        private Gdk.Device pointer, keyboard;

        public DateButton (string text) : base (text)
        {
            Toggled += OnToggled;
            popup = null;
            date = DateTime.Now;
            displayFormat = "yyyy/MM/dd";
        }

        private void ShowCalendar ()
        {
            popup = new Window (WindowType.Popup);
            popup.Screen = this.Screen;

            Frame frame = new Frame ();
            frame.ShadowType = ShadowType.Out;
            frame.Show ();

            popup.Add (frame);

            VBox box = new VBox (false, 0);
            box.Show ();
            frame.Add (box);

            cal = new Calendar ();
            cal.DisplayOptions = CalendarDisplayOptions.ShowHeading
                | CalendarDisplayOptions.ShowDayNames
                | CalendarDisplayOptions.ShowWeekNumbers;

            cal.KeyPressEvent += OnCalendarKeyPressed;
            popup.ButtonPressEvent += OnButtonPressed;

            cal.Show ();

            Alignment calAlignment = new Alignment (0.0f, 0.0f, 1.0f, 1.0f);
            calAlignment.Show ();
            calAlignment.SetPadding (4, 4, 4, 4);
            calAlignment.Add (cal);

            box.PackStart (calAlignment, false, false, 0);

            int minimum_height, natural_height;
            GetPreferredHeight (out minimum_height, out natural_height);
            int x = 0, y = 0;
            Window.GetOrigin (out x, out y);
            popup.Move (x + Allocation.X, y + Allocation.Y + natural_height + 3);
            popup.Show ();
            popup.GrabFocus ();

            Grab.Add (popup);

            Gdk.Device event_device = Gtk.Global.CurrentEventDevice;
            if (event_device.Source == Gdk.InputSource.Mouse) {
                pointer = event_device;
                keyboard = event_device.AssociatedDevice;
            } else {
                keyboard = event_device;
                pointer = event_device.AssociatedDevice;
            }

            Gdk.GrabStatus grabbed = pointer.Grab (popup.Window, Gdk.GrabOwnership.Application, true,
                Gdk.EventMask.ButtonPressMask
                | Gdk.EventMask.ButtonReleaseMask
                | Gdk.EventMask.PointerMotionMask, null, CURRENT_TIME);

            if (grabbed == Gdk.GrabStatus.Success) {
                grabbed = keyboard.Grab (popup.Window, Gdk.GrabOwnership.Application, true,
                    Gdk.EventMask.KeyPressMask | Gdk.EventMask.KeyReleaseMask, null, CURRENT_TIME);

                if (grabbed != Gdk.GrabStatus.Success) {
                    Grab.Remove (popup);
                    popup.Destroy ();
                    popup = null;
                }
            } else {
                Grab.Remove (popup);
                popup.Destroy ();
                popup = null;
            }

            cal.DaySelectedDoubleClick += OnCalendarDaySelected;
            cal.ButtonPressEvent += OnCalendarButtonPressed;

            cal.Date = date;
        }

        public void HideCalendar (bool update)
        {
            if (popup != null) {
                Grab.Remove (popup);
                pointer.Ungrab (CURRENT_TIME);
                keyboard.Ungrab (CURRENT_TIME);

                popup.Destroy ();
                popup = null;
            }

            if (update) {
                date = cal.GetDate ();
                Label = date.ToString (displayFormat);
            }

            Active = false;
        }

        private void OnToggled (object o, EventArgs args)
        {
            if (Active) {
                ShowCalendar ();
            } else {
                HideCalendar (false);
            }
        }

        private void OnButtonPressed (object o, ButtonPressEventArgs args)
        {
            if (popup != null)
                HideCalendar (false);
        }

        private void OnCalendarDaySelected (object o, EventArgs args)
        {
            HideCalendar (true);
        }

        private void OnCalendarButtonPressed (object o, ButtonPressEventArgs args)
        {
            args.RetVal = true;
        }

        private void OnCalendarKeyPressed (object o, KeyPressEventArgs args)
        {
            switch (args.Event.Key) {
            case Gdk.Key.Escape:
                HideCalendar (false);
                break;
            case Gdk.Key.KP_Enter:
            case Gdk.Key.ISO_Enter:
            case Gdk.Key.Key_3270_Enter:
            case Gdk.Key.Return:
            case Gdk.Key.space:
            case Gdk.Key.KP_Space:
                HideCalendar (true);
                break;
            default:
                break;
            }
        }

        public string DisplayFormat {
            set { displayFormat = value; }
        }

        public DateTime Date {
            get { return date; }
        }
    }
}
