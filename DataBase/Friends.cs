using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chat_Server.DataBase
{
    [FirestoreData]
    public class Friends
    {
        [FirestoreProperty]
        public int UID { get; set; }
        [FirestoreProperty]
        public int FriendUID { get; set; }
        [FirestoreProperty]
        public bool isFriend { get; set; }
    }
}
