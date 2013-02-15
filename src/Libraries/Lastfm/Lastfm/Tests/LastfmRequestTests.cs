//
// LastfmRequestTests.cs
//
// Author:
//   Andres G Aragoneses <knocte@gmail.com>
//
// Copyright (C) 2013 Andres G. Aragoneses
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

#if ENABLE_TESTS

using System;
using System.Net;
using NUnit.Framework;

namespace Lastfm.Tests
{
    [TestFixture]
    public class Tests
    {
        class FakeWebRequestCreator : IWebRequestCreate
        {
            public WebRequest Create (Uri uri)
            {
                Uri = uri;
                return (HttpWebRequest)WebRequest.Create (uri);
            }

            internal Uri Uri { get; set; }
        }

        [Test]
        public void EmptyGet ()
        {
            var expected = "http://ws.audioscrobbler.com/2.0/?method=someMethod&api_key=344e9141fffeb02201e1ae455d92ae9f&format=json";
            var creator = new FakeWebRequestCreator ();
            new LastfmRequest ("someMethod", RequestType.Read, ResponseFormat.Json, creator).Send ();
            Assert.AreEqual (expected, creator.Uri.ToString ());
        }

        [Test]
        public void GetWithParams ()
        {
            var expected = "http://ws.audioscrobbler.com/2.0/?method=someMethod&api_key=344e9141fffeb02201e1ae455d92ae9f&x=y&a=b&format=json";
            var creator = new FakeWebRequestCreator ();
            var req = new LastfmRequest ("someMethod", RequestType.Read, ResponseFormat.Json, creator);
            req.AddParameter ("x", "y");
            req.AddParameter ("a", "b");
            req.Send ();
            Assert.AreEqual (expected, creator.Uri.ToString ());
        }

        [Test]
        public void EmptyRawGet ()
        {
            var expected = "http://ws.audioscrobbler.com/2.0/?method=someMethod&api_key=344e9141fffeb02201e1ae455d92ae9f&raw=true";
            var creator = new FakeWebRequestCreator ();
            new LastfmRequest ("someMethod", RequestType.Read, ResponseFormat.Raw, creator).Send ();
            Assert.AreEqual (expected, creator.Uri.ToString ());
        }

        [Test]
        public void EmptyJsonPost ()
        {
            var expected = "http://ws.audioscrobbler.com/2.0/?method=someMethod&api_key=344e9141fffeb02201e1ae455d92ae9f&format=json&sk=&api_sig=33ca04b6d45c54eb1405b3d7cb7735ea";
            var creator = new FakeWebRequestCreator ();
            new LastfmRequest ("someMethod", RequestType.Write, ResponseFormat.Json, creator).Send ();
            Assert.AreEqual (expected, creator.Uri.ToString ());
        }

        [Test]
        public void JsonPostWithParams ()
        {
            var expected = "http://ws.audioscrobbler.com/2.0/?method=someMethod&api_key=344e9141fffeb02201e1ae455d92ae9f&x=y&a=b&format=json&sk=&api_sig=6b369269588df3d3b1ac67834d703c6d";
            var creator = new FakeWebRequestCreator ();
            var req = new LastfmRequest ("someMethod", RequestType.Write, ResponseFormat.Json, creator);
            req.AddParameter ("x", "y");
            req.AddParameter ("a", "b");
            req.Send ();
            Assert.AreEqual (expected, creator.Uri.ToString ());
        }

        [Test]
        public void JsonRawWithParams ()
        {
            var expected = "http://ws.audioscrobbler.com/2.0/?method=someMethod&api_key=344e9141fffeb02201e1ae455d92ae9f&x=y&a=b&raw=true&sk=&api_sig=3b419688648ce7e124c0056aba9a6438";
            var creator = new FakeWebRequestCreator ();
            var req = new LastfmRequest ("someMethod", RequestType.Write, ResponseFormat.Raw, creator);
            req.AddParameter ("x", "y");
            req.AddParameter ("a", "b");
            req.Send ();
            Assert.AreEqual (expected, creator.Uri.ToString ());
        }

    }
}

#endif