// QueueableSourceComboBox.cs
//
// Authors:
//   Alexander Kojevnikov <alexander@kojevnikov.com>
//
// Copyright (C) 2009 Alexander Kojevnikov
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

using Gtk;

using Banshee.Library;
using Banshee.ServiceStack;
using Banshee.Sources;
using Banshee.Sources.Gui;
using Banshee.Collection;

namespace Banshee.PlayQueue
{
    public class QueueableSourceComboBox : ComboBox
    {
        private readonly TreeModelFilter filter;

        public QueueableSourceComboBox (string source_name)
        {
            // FIXME: Would probably be nice to use this, but variable
            // width reporting in SourceRowRenderer does not work as
            // I would expect, so currently it's forced to 200px wide
            // which causes quite a problem with a UI like Muinshee
            // and the MeeGo Media Panel
            //
            // SourceRowRenderer renderer = new SourceRowRenderer ();
            // renderer.ParentWidget = this;

            var renderer = new CellRendererText ();
            PackStart (renderer, true);
            SetCellDataFunc (renderer, new CellLayoutDataFunc (
                (layout, cell, model, iter) => renderer.Text = ((Source)model.GetValue (iter, 0)).Name
            ));

            var store = new SourceModel ();
            filter = new TreeModelFilter (store, null);
            filter.VisibleFunc = (model, iter) => IsQueueable (((SourceModel)model).GetSource (iter));
            Model = filter;

            store.Refresh ();

            SetActiveSource (source_name);

            HasTooltip = true;
            QueryTooltip += HandleQueryTooltip;
        }

        private void HandleQueryTooltip (object o, QueryTooltipArgs args)
        {
            var source = Source;
            if (source != null && Child.Allocation.Width < Child.Requisition.Width) {
                args.Tooltip.Text = source.Name;
                args.RetVal = true;
            }

            // Work around ref counting SIGSEGV, see http://bugzilla.gnome.org/show_bug.cgi?id=478519#c9
            if (args.Tooltip != null) {
                args.Tooltip.Dispose ();
            }
        }

        private void SetActiveSource (string name)
        {
            TreeIter first;
            if (filter.GetIterFirst (out first)) {
                TreeIter iter = FindSource (name, first);
                if (!iter.Equals (TreeIter.Zero)) {
                    SetActiveIter (iter);
                }
            }
        }

        private bool IsQueueable (Source source)
        {
            LibrarySource lib_source = source as LibrarySource;
            return lib_source != null && (
                (lib_source.MediaTypes & (TrackMediaAttributes.Music | TrackMediaAttributes.VideoStream)) != 0 ||
                (lib_source.Parent != null && lib_source.Parent is LibrarySource &&
                (((LibrarySource)lib_source.Parent).MediaTypes & (TrackMediaAttributes.Music | TrackMediaAttributes.VideoStream)) != 0));
        }

        private TreeIter FindSource (string name, TreeIter iter)
        {
            do {
                var source = filter.GetValue (iter, 0) as ISource;
                if (source != null && source.Name == name) {
                    return iter;
                }

                TreeIter citer;
                if (filter.IterChildren (out citer, iter)) {
                    var yiter = FindSource (name, citer);
                    if (!yiter.Equals (TreeIter.Zero)) {
                        return yiter;
                    }
                }
            } while (filter.IterNext (ref iter));

            return TreeIter.Zero;
        }

        public DatabaseSource Source {
            get {
                TreeIter iter;
                if (GetActiveIter (out iter)) {
                    return filter.GetValue(iter, 0) as DatabaseSource;
                }
                return null;
            }
        }
    }
}
