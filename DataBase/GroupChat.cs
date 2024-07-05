using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anonymous_chat.DataBase
{
    [FirestoreData]
    public class GroupChat
    {
        [FirestoreProperty]
        public int GroupUID { get; set; }
        [FirestoreProperty]
        public string? GroupName { get; set; }
        [FirestoreProperty]
        public string? Password { get; set; }
        [FirestoreProperty]
        public int AdminUID { get; set; }
        [FirestoreProperty]
        public List<int>? MemberUID { get; set; }
        [FirestoreProperty]
        public List<int>? BanUID { get; set; }
    }
}
