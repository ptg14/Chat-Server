using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace anonymous_chat.DataBase
{
    [FirestoreData]
    internal class UserData
    {
        [FirestoreProperty]
        public int UID { get; set; }
        [FirestoreProperty]
        public string? Email { get; set; }
        [FirestoreProperty]
        public string? UserName { get; set; }
        [FirestoreProperty]
        public string? Password { get; set; }
        [FirestoreProperty]
        public string? ResetToken { get; set; }
    }
}
