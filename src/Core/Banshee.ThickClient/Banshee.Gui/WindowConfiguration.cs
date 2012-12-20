//
// WindowConfiguration.cs
//
// Author:
//   Andres G. Aragoneses <knocte@gmail.com>
//
// Copyright 2012 Andres G. Aragoneses
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
using Banshee.Configuration;

namespace Banshee.Gui
{
    public class WindowConfiguration
    {
        public SchemaEntry<int> WidthSchema { get; private set; }
        public SchemaEntry<int> HeightSchema { get; private set; }
        public SchemaEntry<int> XPosSchema { get; private set; }
        public SchemaEntry<int> YPosSchema { get; private set; }
        public SchemaEntry<bool> MaximizedSchema { get; private set; }

        public WindowConfiguration (SchemaEntry<int> widthSchema,
                                    SchemaEntry<int> heightSchema,
                                    SchemaEntry<int> xPosSchema,
                                    SchemaEntry<int> yPosSchema,
                                    SchemaEntry<bool> maximizedSchema)
        {
            WidthSchema = widthSchema;
            HeightSchema = heightSchema;
            XPosSchema = xPosSchema;
            YPosSchema = yPosSchema;
            MaximizedSchema = maximizedSchema;
        }

        public static SchemaEntry<int> NewWidthSchema (string configNamespace, int defaultWidth)
        {
            return new SchemaEntry <int> (
                configNamespace, "width",
                defaultWidth,
                "Window Width",
                "Width of the main interface window."
            );
        }

        public static SchemaEntry<int> NewHeightSchema (string configNamespace, int defaultHeight)
        {
            return new SchemaEntry <int> (
                configNamespace, "height",
                defaultHeight,
                "Window Height",
                "Height of the main interface window."
            );
        }

        public static SchemaEntry<int> NewXPosSchema (string configNamespace)
        {
            return new SchemaEntry <int> (
                configNamespace, "x_pos",
                0,
                "Window Position X",
                "Pixel position of Main Player Window on the X Axis"
            );
        }

        public static SchemaEntry<int> NewYPosSchema (string configNamespace)
        {
            return new SchemaEntry <int> (
                configNamespace, "y_pos",
                0,
                "Window Position Y",
                "Pixel position of Main Player Window on the Y Axis"
            );
        }

        public static SchemaEntry<bool> NewMaximizedSchema (string configNamespace)
        {
            return new SchemaEntry <bool> (
                configNamespace, "maximized",
                false,
                "Window Maximized",
                "True if main window is to be maximized, false if it is not."
            );
        }
    }
}

