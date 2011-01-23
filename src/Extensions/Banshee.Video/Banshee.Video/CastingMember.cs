// 
// CastingMember.cs
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

using Banshee.Collection;

using Hyena.Data;
using Hyena.Data.Sqlite;
using Banshee.Database;
using Banshee.ServiceStack;

namespace Banshee.Video
{
    public class CastingMember : CacheableItem
    {
        private static BansheeModelProvider<CastingMember> provider = new BansheeModelProvider<CastingMember> (
            ServiceManager.DbConnection, "CastingMembers"
        );

        public static BansheeModelProvider<CastingMember> Provider {
            get { return provider; }
        }

        [DatabaseColumn("CastingMemberID", Constraints = DatabaseColumnConstraints.PrimaryKey)]
        private int dbid;
        public int DbId {
            get { return dbid; }
        }

        //Foreign key to video db table
        [DatabaseColumn("VideoID")]
        private int video_id;
        public int VideoID {
            get { return video_id; }
            set { video_id = value; }
        }

        private string name;
        private string character;
        private string job;

        [DatabaseColumn]
        public string Name {
            get { return name; }
            set { name = value; }
        }

        //role
        [DatabaseColumn]
        public string Character {
            get { return character; }
            set { character = value; }
        }

        //director, actor, author, special guest...
        [DatabaseColumn]
        public string Job {
            get { return job; }
            set { job = value; }
        }

        public void Save ()
        {
            Provider.Save (this);
        }
    }
}


