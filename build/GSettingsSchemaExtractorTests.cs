//
// GSettingsExtractorTests.cs
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
using System.Text;

using Banshee.Configuration;

using NUnit.Framework;

namespace GSettingsSchemaExtractor
{
    [TestFixture]
    public class Tests
    {
        internal class StringType {
            public static readonly SchemaEntry<string> DefaultExportFormat = new SchemaEntry<string> (
                "player_window", "default_export_format",
                "m3u",
                "Export Format",
                "The default playlist export format"
            );
        }

        [Test]
        public void SchemaWithString ()
        {
            StringBuilder result = GSettingsSchemaExtractorProgram.Extract (new Type [] { typeof (StringType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<schemalist>
  <schema id=""org.gnome.banshee.player_window"" path=""/apps/banshee/player_window/"" gettext-domain=""banshee"">
    <key name=""default_export_format"" type=""s"">
      <default>'m3u'</default>
      <_summary>Export Format</_summary>
      <_description>The default playlist export format</_description>
    </key>
  </schema>
</schemalist>"
            .Trim ()));
        }


        internal class BooleanType
        {
            public static readonly SchemaEntry<bool> ShowInitialImportDialog = new SchemaEntry<bool>(
                "import", "show_initial_import_dialog",
                true,
                "Show the Initial Import Dialog",
                "Show the Initial Import Dialog when the Banshee library is empty"
            );
        }

        [Test]
        public void SchemaWithBoolean ()
        {
            StringBuilder result = GSettingsSchemaExtractorProgram.Extract (new Type [] { typeof (BooleanType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<schemalist>
  <schema id=""org.gnome.banshee.import"" path=""/apps/banshee/import/"" gettext-domain=""banshee"">
    <key name=""show_initial_import_dialog"" type=""b"">
      <default>true</default>
      <_summary>Show the Initial Import Dialog</_summary>
      <_description>Show the Initial Import Dialog when the Banshee library is empty</_description>
    </key>
  </schema>
</schemalist>"
            .Trim ()));
        }

        internal class IntegerType {
            public static readonly SchemaEntry<int> VolumeSchema = new SchemaEntry<int> (
                "player_engine", "volume",
                80,
                "Volume",
                "Volume of playback relative to mixer output"
            );
        }

        [Test]
        public void SchemaWithInt ()
        {
            StringBuilder result = GSettingsSchemaExtractorProgram.Extract (new Type [] { typeof (IntegerType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<schemalist>
  <schema id=""org.gnome.banshee.player_engine"" path=""/apps/banshee/player_engine/"" gettext-domain=""banshee"">
    <key name=""volume"" type=""i"">
      <default>80</default>
      <_summary>Volume</_summary>
      <_description>Volume of playback relative to mixer output</_description>
    </key>
  </schema>
</schemalist>"
                .Trim ()));
        }

        internal class DoubleType {
            public static readonly SchemaEntry<double> CoverArtSize = new SchemaEntry<double> (
                "player_window", "cover_art_size",
                20.5,
                "Cover art size",
                "Surface size of cover art in the album grid"
            );
        }

        [Test]
        public void SchemaWithDouble ()
        {
            StringBuilder result = GSettingsSchemaExtractorProgram.Extract (new Type [] { typeof (DoubleType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<schemalist>
  <schema id=""org.gnome.banshee.player_window"" path=""/apps/banshee/player_window/"" gettext-domain=""banshee"">
    <key name=""cover_art_size"" type=""d"">
      <default>20.5</default>
      <_summary>Cover art size</_summary>
      <_description>Surface size of cover art in the album grid</_description>
    </key>
  </schema>
</schemalist>"
            .Trim ()));
        }

        internal class ArrayType {
            public static readonly SchemaEntry<string[]> CurrentFiltersSchema = new SchemaEntry<string[]> (
                "sources.fsq", "current_filters",
                new string[] { "album", "artist" },
            null,
            null
            );
        }

        [Test]
        public void SchemaWithArray ()
        {
            StringBuilder result = GSettingsSchemaExtractorProgram.Extract (new Type [] { typeof (ArrayType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<schemalist>
  <schema id=""org.gnome.banshee.sources.fsq"" path=""/apps/banshee/sources/fsq/"" gettext-domain=""banshee"">
    <key name=""current_filters"" type=""as"">
      <default>['album','artist']</default>
      <_summary></_summary>
      <_description></_description>
    </key>
  </schema>
</schemalist>"
                .Trim ()));
        }
    }
}

