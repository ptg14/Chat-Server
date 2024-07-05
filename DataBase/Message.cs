using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Server.DataBase
{
    [FirestoreData]
    public class storeMessage
    {
        [FirestoreProperty]
        public int UID { get; set; }
        [FirestoreProperty]
        public int toUID { get; set; }
        [FirestoreProperty]
        public string? Content { get; set; }
        [FirestoreProperty]
        public string? Type { get; set; }
    }
}
