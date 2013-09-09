//
// GConfSchemaExtractorTests.cs
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

namespace GConfSchemaExtractor
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
            StringBuilder result = GConfSchemaExtractorProgram.Extract (new Type [] { typeof (StringType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<?xml version=""1.0""?>
<gconfschemafile>
  <schemalist>
    <schema>
      <key>/schemas/apps/banshee/player_window/default_export_format</key>
      <applyto>/apps/banshee/player_window/default_export_format</applyto>
      <owner>banshee</owner>
      <type>string</type>
      <default>m3u</default>
      <locale name=""C"">
        <short>Export Format</short>
        <long>The default playlist export format</long>
      </locale>
    </schema>
  </schemalist>
</gconfschemafile>"
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
            StringBuilder result = GConfSchemaExtractorProgram.Extract (new Type [] { typeof (BooleanType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<?xml version=""1.0""?>
<gconfschemafile>
  <schemalist>
    <schema>
      <key>/schemas/apps/banshee/import/show_initial_import_dialog</key>
      <applyto>/apps/banshee/import/show_initial_import_dialog</applyto>
      <owner>banshee</owner>
      <type>bool</type>
      <default>true</default>
      <locale name=""C"">
        <short>Show the Initial Import Dialog</short>
        <long>Show the Initial Import Dialog when the Banshee library is empty</long>
      </locale>
    </schema>
  </schemalist>
</gconfschemafile>"
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
            StringBuilder result = GConfSchemaExtractorProgram.Extract (new Type [] { typeof (IntegerType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<?xml version=""1.0""?>
<gconfschemafile>
  <schemalist>
    <schema>
      <key>/schemas/apps/banshee/player_engine/volume</key>
      <applyto>/apps/banshee/player_engine/volume</applyto>
      <owner>banshee</owner>
      <type>int</type>
      <default>80</default>
      <locale name=""C"">
        <short>Volume</short>
        <long>Volume of playback relative to mixer output</long>
      </locale>
    </schema>
  </schemalist>
</gconfschemafile>"
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
            StringBuilder result = GConfSchemaExtractorProgram.Extract (new Type [] { typeof (DoubleType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<?xml version=""1.0""?>
<gconfschemafile>
  <schemalist>
    <schema>
      <key>/schemas/apps/banshee/player_window/cover_art_size</key>
      <applyto>/apps/banshee/player_window/cover_art_size</applyto>
      <owner>banshee</owner>
      <type>float</type>
      <default>20.5</default>
      <locale name=""C"">
        <short>Cover art size</short>
        <long>Surface size of cover art in the album grid</long>
      </locale>
    </schema>
  </schemalist>
</gconfschemafile>"
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
            StringBuilder result = GConfSchemaExtractorProgram.Extract (new Type [] { typeof (ArrayType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<?xml version=""1.0""?>
<gconfschemafile>
  <schemalist>
    <schema>
      <key>/schemas/apps/banshee/sources/fsq/current_filters</key>
      <applyto>/apps/banshee/sources/fsq/current_filters</applyto>
      <owner>banshee</owner>
      <type>list</type>
      <list_type>string</list_type>
      <default>[album,artist]</default>
      <locale name=""C"">
        <short></short>
        <long></long>
      </locale>
    </schema>
  </schemalist>
</gconfschemafile>"
                .Trim ()));
        }

        [Test]
        public void SchemaWithMoreThanOneKey ()
        {
            StringBuilder result = GConfSchemaExtractorProgram.Extract (
                new Type [] { typeof (IntegerType), typeof (DoubleType), typeof (StringType) });

            Assert.That (result, Is.Not.Null);
            Assert.That (result.ToString ().Trim (), Is.EqualTo (@"
<?xml version=""1.0""?>
<gconfschemafile>
  <schemalist>
    <schema>
      <key>/schemas/apps/banshee/player_engine/volume</key>
      <applyto>/apps/banshee/player_engine/volume</applyto>
      <owner>banshee</owner>
      <type>int</type>
      <default>80</default>
      <locale name=""C"">
        <short>Volume</short>
        <long>Volume of playback relative to mixer output</long>
      </locale>
    </schema>
    <schema>
      <key>/schemas/apps/banshee/player_window/cover_art_size</key>
      <applyto>/apps/banshee/player_window/cover_art_size</applyto>
      <owner>banshee</owner>
      <type>float</type>
      <default>20.5</default>
      <locale name=""C"">
        <short>Cover art size</short>
        <long>Surface size of cover art in the album grid</long>
      </locale>
    </schema>
    <schema>
      <key>/schemas/apps/banshee/player_window/default_export_format</key>
      <applyto>/apps/banshee/player_window/default_export_format</applyto>
      <owner>banshee</owner>
      <type>string</type>
      <default>m3u</default>
      <locale name=""C"">
        <short>Export Format</short>
        <long>The default playlist export format</long>
      </locale>
    </schema>
  </schemalist>
</gconfschemafile>
"
                .Trim ()));
        }
    }
}

