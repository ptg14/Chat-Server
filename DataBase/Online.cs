using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anonymous_chat.DataBase
{
    [FirestoreData]
    internal class Online
    {
        [FirestoreProperty]
        public int UID { get; set; }
        [FirestoreProperty]
        public string? IP { get; set; }
        [FirestoreProperty]
        public bool Searching { get; set; }
    }
}
