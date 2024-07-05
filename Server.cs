using anonymous_chat.Chat;
using anonymous_chat.DataBase;
using Chat_Server.DataBase;
using Google.Cloud.Firestore;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Chat_Server
{
    public class Call
    {
        public TcpClient A { get; set; }
        public TcpClient B { get; set; }

        public Call(TcpClient personA, TcpClient personB)
        {
            A = personA;
            B = personB;
        }
    }
    public partial class Server : Form
    {
        public Server()
        {
            InitializeComponent();

            if (!FireBase.setEnironmentVariables())
            {
                rTB_log.ForeColor = Color.Red;
                rTB_log.Text = "Không thể kết nối đến cơ sở dữ liệu";
                return;
            }
        }

        private TcpListener? serverSocket;
        private Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();
        private bool isListening;
        private static FirestoreDb db = FireBase.dataBase;
        private Dictionary<int, int> random = new Dictionary<int, int>();
        private TcpListener? videoReceiver;
        private Dictionary<int, TcpClient> callList = new Dictionary<int, TcpClient>();
        private List<Call> calls = new List<Call>();
        private TcpListener? audioReceiver;
        private Dictionary<int, TcpClient> audioCallList = new Dictionary<int, TcpClient>();
        private List<Call> audioCalls = new List<Call>();

        private void BT_listen_Click(object sender, EventArgs e)
        {
            if (!isListening)
            {
                serverSocket = new TcpListener(IPAddress.Any, 8888);
                videoReceiver = new TcpListener(IPAddress.Any, 9001);
                audioReceiver = new TcpListener(IPAddress.Any, 9000);
                serverSocket.Start();
                videoReceiver.Start();
                audioReceiver.Start();
                isListening = true;
                rTB_log.Text += "Server is listening." + Environment.NewLine;

                Thread acceptThread = new Thread(AcceptClients);
                acceptThread.IsBackground = true;
                acceptThread.Start();

                Thread videoThread = new Thread(AcceptVideoCall);
                videoThread.IsBackground = true;
                videoThread.Start();

                Thread audioThread = new Thread(AcceptAudioCall);
                audioThread.IsBackground = true;
                audioThread.Start();

            }
            else
            {
                MessageBox.Show("Server is already listening.", "ERROR");
            }
        }

        private async void OfflineChat(int UID)
        {
            try
            {
                // Create a query against the collection "Messages" where 'toUID' equals the specified UID
                Query query = db.Collection("Messages").WhereEqualTo("toUID", UID);
                // Execute the query
                QuerySnapshot snapshot = await query.GetSnapshotAsync();

                foreach (DocumentSnapshot document in snapshot.Documents)
                {
                    storeMessage message = document.ConvertTo<storeMessage>();
                    if (message.Type == "TEXT")
                    {
                        Send(message.toUID, message.Content);
                    }
                    else if (message.Type == "FILE")
                    {
                        SendFile(message.toUID, message.Content);
                    }
                    await document.Reference.DeleteAsync();
                }
            }
            catch (Exception ex)
            {
                rTB_log.Invoke((MethodInvoker)delegate {
                    rTB_log.Text += "Error retrieving messages: " + ex.Message + Environment.NewLine;
                });
            }
        }

        private void AcceptClients()
        {
            while (true)
            {
                try
                {
                    TcpClient clientSocket = serverSocket.AcceptTcpClient();
                    NetworkStream clientStream = clientSocket.GetStream();
                    byte[] message = new byte[4096];
                    int bytesRead = clientStream.Read(message, 0, 4096);
                    ASCIIEncoding encoder = new ASCIIEncoding();
                    string receivedMessage = encoder.GetString(message, 0, bytesRead);
                    string[] parts = receivedMessage.Split('=');
                    if (parts[0] == "UIDCONNECT")
                    {
                        int uid = int.Parse(parts[1]);
                        clients[uid] = clientSocket;
                        rTB_log.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += uid + " has connected." + Environment.NewLine;
                            BroadcastMessage(receivedMessage);
                            LBox_user.Items.Add(uid);
                        });

                        Thread offline = new Thread(() => OfflineChat(uid));
                        offline.IsBackground = true;
                        offline.Start();

                        Thread clientThread = new Thread(() => HandleClientComm(clientSocket));
                        clientThread.IsBackground = true;
                        clientThread.Start();
                    }
                }
                catch (Exception ex)
                {
                    // Handle the exception here
                    rTB_log.Invoke((MethodInvoker)delegate {
                        rTB_log.Text += "Error: " + ex.Message + Environment.NewLine;
                    });
                }
            }
        }

        private void AcceptVideoCall()
        {
            while (true)
            {
                try
                {
                    TcpClient tcpClient = videoReceiver.AcceptTcpClient();
                    NetworkStream clientStream = tcpClient.GetStream();
                    byte[] message = new byte[4096];
                    int bytesRead = clientStream.Read(message, 0, 4096);
                    ASCIIEncoding encoder = new ASCIIEncoding();
                    string receivedMessage = encoder.GetString(message, 0, bytesRead);
                    string[] parts = receivedMessage.Split('=');
                    if (parts[0] == "UIDCALL")
                    {
                        int uid = int.Parse(parts[1]);
                        callList[uid] = tcpClient;
                    }
                }
                catch (Exception ex)
                {
                    rTB_log.Invoke((MethodInvoker)delegate
                    {
                        rTB_log.Text += "Error: " + ex.Message + Environment.NewLine;
                    });
                }
            }
        }

        private void AcceptAudioCall()
        {
            while (true)
            {
                try
                {
                    TcpClient tcpClient = audioReceiver.AcceptTcpClient();
                    NetworkStream clientStream = tcpClient.GetStream();
                    byte[] message = new byte[4096];
                    int bytesRead = clientStream.Read(message, 0, 4096);
                    ASCIIEncoding encoder = new ASCIIEncoding();
                    string receivedMessage = encoder.GetString(message, 0, bytesRead);
                    string[] parts = receivedMessage.Split('=');
                    if (parts[0] == "UIDCALL")
                    {
                        int uid = int.Parse(parts[1]);
                        audioCallList[uid] = tcpClient;
                    }
                }
                catch (Exception ex)
                {
                    rTB_log.Invoke((MethodInvoker)delegate
                    {
                        rTB_log.Text += "Error: " + ex.Message + Environment.NewLine;
                    });
                }
            }
        }

        private TcpClient? FindReceiverForCaller(List<Call> calls, TcpClient caller)
        {
            foreach (Call call in calls)
            {
                if (call.A == caller)
                {
                    return call.B;
                }
                else if (call.B == caller)
                {
                    return call.A;
                }
            }
            return null;
        }

        private async void HandleVideo(object client)
        {
            TcpClient callerClient = (TcpClient)client;
            NetworkStream callerStream = callerClient.GetStream();

            TcpClient receiverClient = FindReceiverForCaller(calls, callerClient);
            if (receiverClient == null)
            {
                rTB_log.Invoke((MethodInvoker)delegate
                {
                    rTB_log.Text += "Error: not found receive call"+ Environment.NewLine;
                });
                return;
            }
            NetworkStream receiverStream = receiverClient.GetStream();

            try
            {
                while (true)
                {
                    // Read the size of the next frame
                    byte[] sizeBytes = new byte[4];
                    if (await callerStream.ReadAsync(sizeBytes, 0, sizeBytes.Length) != sizeBytes.Length)
                        break; // Connection closed or error

                    int frameSize = BitConverter.ToInt32(sizeBytes, 0);
                    byte[] frameBytes = new byte[frameSize];

                    // Read the frame itself
                    int bytesRead = 0;
                    while (bytesRead < frameSize)
                    {
                        int read = await callerStream.ReadAsync(frameBytes, bytesRead, frameSize - bytesRead);
                        if (read == 0)
                            break; // Connection closed or error
                        bytesRead += read;
                    }

                    if (bytesRead == frameSize)
                    {
                        // Forward the frame size and frame to the receiver
                        await receiverStream.WriteAsync(sizeBytes, 0, sizeBytes.Length);
                        await receiverStream.WriteAsync(frameBytes, 0, frameBytes.Length);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private void HandleAudio(object client)
        {
            TcpClient callerClient = (TcpClient)client;
            NetworkStream callerStream = callerClient.GetStream();

            TcpClient receiverClient = FindReceiverForCaller(audioCalls, callerClient);
            if (receiverClient == null)
            {
                rTB_log.Invoke((MethodInvoker)delegate
                {
                    rTB_log.Text += "Error: not found receive call" + Environment.NewLine;
                });
                return;
            }
            NetworkStream receiverStream = receiverClient.GetStream();

            try
            {
                while (true)
                {
                    // Read the size of the next frame
                    byte[] sizeBytes = new byte[4];
                    if (callerStream.Read(sizeBytes, 0, sizeBytes.Length) != sizeBytes.Length)
                        break; // Connection closed or error

                    int frameSize = BitConverter.ToInt32(sizeBytes, 0);
                    byte[] frameBytes = new byte[frameSize];

                    // Read the frame itself
                    int bytesRead = 0;
                    while (bytesRead < frameSize)
                    {
                        int read = callerStream.Read(frameBytes, bytesRead, frameSize - bytesRead);
                        if (read == 0)
                            break; // Connection closed or error
                        bytesRead += read;
                    }

                    if (bytesRead == frameSize)
                    {
                        // Forward the frame size and frame to the receiver
                        receiverStream.Write(sizeBytes, 0, sizeBytes.Length);
                        receiverStream.Write(frameBytes, 0, frameBytes.Length);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            NetworkStream clientStream = tcpClient.GetStream();
            int key = clients.First(c => c.Value == tcpClient).Key;

            byte[] message = new byte[100 * 1024 * 1024];
            int bytesRead;

            while (true)
            {
                bytesRead = 0;

                try
                {
                    bytesRead = await clientStream.ReadAsync(message, 0, message.Length);
                }
                catch
                {
                    // A socket error has occurred
                    break;
                }

                if (bytesRead == 0)
                {
                    // The client has disconnected from the server
                    rTB_log.Text += key + " has disconnected." + Environment.NewLine;
                    break;
                }

                try
                {
                    // Message has successfully been received
                    string receivedMessage = Encoding.UTF8.GetString(message, 0, bytesRead);
                    Debug.WriteLine(receivedMessage);
                    //MessageBox.Show(receivedMessage);
                    string[] parts = receivedMessage.Split('=');

                    if (parts[0] == "FILE")
                    {
                        // Extract file name and size from the header
                        string[] fileInfo = parts[1].Split(':');
                        string fileName = fileInfo[0];
                        long fileSize = long.Parse(fileInfo[1]);
                        string[] mode = fileName.Split("~");
                        string dowhat = mode[0];
                        //MessageBox.Show(dowhat);
                        string content = mode[1];
                        int senderID = 0;
                        int receiverID = 0;
                        string folderPath;
                        string filePath;

                        if (dowhat == "SETAVATAR")
                        {
                            string[] nameAndEx = content.Split(".");
                            folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, nameAndEx[0]);
                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }
                            filePath = Path.Combine(folderPath, fileName);
                        }
                        else
                        {
                            string[] send = dowhat.Split("-");
                            senderID = int.Parse(send[0]);
                            receiverID = int.Parse(send[1]);
                            string folderPathroot = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, receiverID.ToString());
                            folderPath = Path.Combine(folderPathroot, senderID.ToString());
                            if (!Directory.Exists(folderPath))
                            {
                                Directory.CreateDirectory(folderPath);
                            }
                            filePath = Path.Combine(folderPath, fileName);
                        }

                        // Prepare to receive the file
                        using (var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
                        {
                            long totalBytesReceived = 0;
                            byte[] fileBuffer = new byte[100 * 1024 * 1024];

                            while (totalBytesReceived < fileSize)
                            {
                                int bytesReadfile = await clientStream.ReadAsync(fileBuffer, 0, fileBuffer.Length);
                                fileStream.Write(fileBuffer, 0, bytesReadfile);
                                totalBytesReceived += bytesReadfile;
                            }
                        }

                        if (receiverID < 10000)
                        {
                            this.Invoke((MethodInvoker)delegate
                            {
                                if (dowhat == "SETAVATAR")
                                {
                                    string[] nameAndEx = content.Split(".");
                                    rTB_log.Text += nameAndEx[0] + " has set avatar." + Environment.NewLine;
                                }
                                else
                                {
                                    if (receiverID == 999)
                                    {
                                        if (random.ContainsKey(senderID))
                                        {
                                            receiverID = random[senderID];
                                        }
                                        else if (random.ContainsValue(senderID))
                                        {
                                            int receiveKey = random.First(x => x.Value == senderID).Key;
                                            receiverID = receiveKey;
                                        }
                                    }
                                    if (clients.ContainsKey(receiverID))
                                    {
                                        SendFile(receiverID, filePath);
                                        rTB_log.Text += senderID + " send file to " + receiverID + Environment.NewLine;
                                    }
                                    else
                                    {
                                        saveCloud(senderID, receiverID, "FILE", filePath);
                                        rTB_log.Text += senderID + " send file to " + receiverID + " offline" + Environment.NewLine;
                                    }
                                }
                            });
                        }
                        else
                        {
                            // Get all members of the group
                            DocumentSnapshot snapshot = await db.Collection("Group").Document(receiverID.ToString()).GetSnapshotAsync();
                            GroupChat group = snapshot.ConvertTo<GroupChat>();
                            foreach (int memberID in group.MemberUID)
                            {
                                if (memberID != senderID)
                                {
                                    if (clients.ContainsKey(memberID))
                                    {
                                        SendFile(memberID, filePath);
                                    }
                                    else
                                    {
                                        saveCloud(senderID, memberID, "FILE", filePath);
                                    }
                                }
                            }

                            // Group chat
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " send file to group " + receiverID + Environment.NewLine;
                            });
                        }
                    }
                    else if (parts[0] == "DISCONNECT")
                    {
                        int senderID = int.Parse(parts[1]);
                        clients.Remove(senderID);
                        BroadcastMessage(receivedMessage);
                        rTB_log.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " has disconnected." + Environment.NewLine;
                            LBox_user.Items.Remove(senderID);
                        });

                        // Delete the user from the Online collection
                        DocumentReference docRef = db.Collection("Online").Document(senderID.ToString());
                        await docRef.DeleteAsync();

                        if (random.ContainsKey(senderID))
                        {
                            Send(random[senderID], "RANDOMCANCELLED=" + senderID);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " cancel random chat with " + random[senderID] + Environment.NewLine;
                            });
                            DocumentReference randomdocRef = db.Collection("Online").Document(random[senderID].ToString());

                            await randomdocRef.UpdateAsync("isSearch", false);
                            random.Remove(senderID);
                        }
                        else if (random.ContainsValue(senderID))
                        {
                            int receiveKey = random.First(x => x.Value == senderID).Key;
                            Send(receiveKey, "RANDOMCANCELLED=" + senderID);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " cancel random chat with " + receiveKey + Environment.NewLine;
                            });
                            DocumentReference randomdocRef = db.Collection("Online").Document(receiveKey.ToString());

                            await randomdocRef.UpdateAsync("isSearch", false);
                            random.Remove(key);
                        }
                        break;
                    }
                    else if (parts[0] == "GETAVATAR")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        string fileName = "SETAVATAR~" + receiverID + ".png";
                        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, receiverID.ToString());
                        string filePath = Path.Combine(folderPath, fileName);
                        if (File.Exists(filePath))
                        {
                            SendFile(senderID, filePath);
                        }
                    }
                    else if (parts[0] == "RANDOMREQUEST")
                    {
                        int senderID = int.Parse(parts[1]);

                        if (random.ContainsKey(senderID))
                        {
                            Send(random[senderID], "RANDOMCANCELLED=" + senderID);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " cancel random chat with " + random[senderID] + Environment.NewLine;
                            });
                            DocumentReference randomdocRef = db.Collection("Online").Document(random[senderID].ToString());

                            await randomdocRef.UpdateAsync("isSearch", false);
                            random.Remove(senderID);
                        }
                        else if (random.ContainsValue(senderID))
                        {
                            int receiveKey = random.First(x => x.Value == senderID).Key;
                            Send(receiveKey, "RANDOMCANCELLED=" + senderID);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " cancel random chat with " + receiveKey + Environment.NewLine;
                            });
                            DocumentReference randomdocRef = db.Collection("Online").Document(receiveKey.ToString());

                            await randomdocRef.UpdateAsync("isSearch", false);
                            random.Remove(key);
                        }

                        int? randomUserId = await FindRandomSearchableUserExcluding();
                        if (randomUserId.HasValue)
                        {
                            DocumentReference docRef = db.Collection("Online").Document(randomUserId.ToString());

                            await docRef.UpdateAsync("isSearch", false);

                            random[senderID] = randomUserId.Value;
                            Send(senderID, "RANDOMACCEPTED=" + randomUserId.Value);
                            Send(randomUserId.Value, "RANDOMACCEPTED=" + senderID);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " random chat with " + randomUserId.Value + Environment.NewLine;
                            });
                        }
                        else
                        {
                            DocumentReference docRef = db.Collection("Online").Document(parts[1]);

                            await docRef.UpdateAsync("isSearch", true);

                            Send(senderID, "RANDOMREJECTED="+ senderID);
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " find no one to random chat" + Environment.NewLine;
                            });
                        }
                    }
                    else if (parts[0] == "FINDLOGO")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int findID = int.Parse(ids[1]);
                        string folderPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, findID.ToString());
                        string newFileName = "FINDLOGO~" + findID + ".png";
                        string newFilePath = Path.Combine(folderPath, newFileName);
                        if (File.Exists(newFilePath))
                        {
                            SendFile(senderID, newFilePath);
                        }
                        else
                        {
                            string fileName = "SETAVATAR~" + findID + ".png";
                            string filePath = Path.Combine(folderPath, fileName);

                            if (File.Exists(filePath))
                            {
                                File.Copy(filePath, newFilePath, overwrite: true);
                                ResizeImage(newFilePath, newFilePath, 25, 25);
                                SendFile(senderID, newFilePath);
                            }
                        }
                    }
                    else if (parts[0] == "FRIENDREQUEST")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        // Create a new Friends object
                        Friends newFriend = new Friends
                        {
                            UID = senderID,
                            FriendUID = receiverID,
                            isFriend = false // Set to false initially
                        };

                        // Add the new Friends object to the Friends collection
                        CollectionReference friendsRef = db.Collection("Friends");
                        string documentName = $"{senderID}&{receiverID}";
                        DocumentReference docRef = friendsRef.Document(documentName);
                        await docRef.SetAsync(newFriend);

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " send friend request to " + receiverID + Environment.NewLine;
                        });

                        if (clients.ContainsKey(receiverID))
                        {
                            Send(receiverID, receivedMessage);
                        }
                        else
                        {
                            saveCloud(senderID, receiverID, "TEXT", receivedMessage);
                        }
                    }
                    else if (parts[0] == "FRIENDACCEPTED")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        Friends newFriend = new Friends
                        {
                            UID = senderID,
                            FriendUID = receiverID,
                            isFriend = true
                        };

                        // Add the new Friends object to the Friends collection
                        CollectionReference friendsRef = db.Collection("Friends");
                        string documentName = $"{senderID}&{receiverID}";
                        DocumentReference docRef = friendsRef.Document(documentName);
                        await docRef.SetAsync(newFriend);

                        Query query = friendsRef.WhereEqualTo("UID", receiverID).WhereEqualTo("FriendUID", senderID);
                        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
                        foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                        {
                            await documentSnapshot.Reference.UpdateAsync("isFriend", true);
                        }

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " accepted friend request from " + receiverID + Environment.NewLine;
                        });

                        if (clients.ContainsKey(receiverID))
                        {
                            Send(receiverID, receivedMessage);
                        }
                        else
                        {
                            saveCloud(senderID, receiverID, "TEXT", receivedMessage);
                        }
                    }
                    else if (parts[0] == "FRIENDREJECTED")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        CollectionReference friendsRef = db.Collection("Friends");
                        Query query = friendsRef.WhereEqualTo("UID", receiverID).WhereEqualTo("FriendUID", senderID);
                        QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
                        foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                        {
                            await documentSnapshot.Reference.DeleteAsync();
                        }

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " rejected friend request from " + receiverID + Environment.NewLine;
                        });

                        if (clients.ContainsKey(receiverID))
                        {
                            Send(receiverID, receivedMessage);
                        }
                        else
                        {
                            saveCloud(senderID, receiverID, "TEXT", receivedMessage);
                        }
                    }
                    else if (parts[0] == "GROUPREQUEST")
                    {
                        string[] nua = parts[1].Split("#");
                        string[] dau = nua[0].Split(">");
                        string[] cuoi = nua[1].Split("&");
                        int senderID = int.Parse(dau[0]);
                        int receiverID = int.Parse(dau[1]);
                        int groupID = int.Parse(cuoi[0]);
                        string groupName = cuoi[1];

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " invite " + receiverID + "to join group " + groupName + "(" + groupID + ")" + Environment.NewLine;
                        });

                        if (clients.ContainsKey(receiverID))
                        {
                            Send(receiverID, receivedMessage);
                        }
                        else
                        {
                            saveCloud(senderID, receiverID, "TEXT", receivedMessage);
                        }
                    }
                    else if (parts[0] == "CALLREQUEST")
                    {
                        string[] ids = parts[1].Split(">");

                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " want to call " + receiverID + Environment.NewLine;
                        });

                        Send(receiverID, receivedMessage);
                    }
                    else if (parts[0] == "CALLACCEPTED")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        Send(receiverID, receivedMessage);

                        TcpClient senderClient = callList[senderID];
                        TcpClient receiverClient = callList[receiverID];
                        TcpClient audioSender = audioCallList[senderID];
                        TcpClient audioReceiver = audioCallList[receiverID];

                        Call call = new Call(senderClient, receiverClient);
                        calls.Add(call);
                        Call audiocall = new Call(audioSender, audioReceiver);  
                        audioCalls.Add(audiocall);

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " accepted call from " + receiverID + Environment.NewLine;
                        });

                        Thread receiveThreadA = new Thread(() => HandleVideo(receiverClient));
                        receiveThreadA.IsBackground = true;
                        receiveThreadA.Start();

                        Thread receiveThreadB = new Thread(() => HandleVideo(senderClient));
                        receiveThreadB.IsBackground = true;
                        receiveThreadB.Start();

                        Thread receiveThreadC = new Thread(() => HandleAudio(audioReceiver));
                        receiveThreadC.IsBackground = true;
                        receiveThreadC.Start();

                        Thread receiveThreadD = new Thread(() => HandleAudio(audioSender));
                        receiveThreadD.IsBackground = true;
                        receiveThreadD.Start();
                    }
                    else if (parts[0] == "CALLREJECTED")
                    {
                        string[] ids = parts[1].Split(">");
                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " rejected call from " + receiverID + Environment.NewLine;
                        });

                        Send(receiverID, receivedMessage);
                    }
                    else if (parts[0] == "ENDCALL")
                    {
                        int senderID = int.Parse(parts[1]);

                        TcpClient senderClient = callList[senderID];

                        TcpClient receiverClient = FindReceiverForCaller(calls, senderClient);

                        int receiverID = callList.First(c => c.Value == receiverClient).Key;

                        this.Invoke((MethodInvoker)delegate
                        {
                            rTB_log.Text += senderID + " end call with " + receiverID + Environment.NewLine;
                        });

                        Send(receiverID, receivedMessage);
                    }
                    else if (parts[0] == "CAMOFF" || parts[0] == "MICOFF")
                    {
                        int senderID = int.Parse(parts[1]);

                        TcpClient senderClient = callList[senderID];

                        TcpClient receiverClient = FindReceiverForCaller(calls, senderClient);

                        int receiverID = callList.First(c => c.Value == receiverClient).Key;

                        Send(receiverID, receivedMessage);
                    }
                    else
                    {
                        string[] ids = parts[0].Split(">");

                        int senderID = int.Parse(ids[0]);
                        int receiverID = int.Parse(ids[1]);
                        string json = parts[1];

                        if (receiverID < 10000)
                        {
                            if (receiverID == 999)
                            {
                                if (random.ContainsKey(senderID))
                                {
                                    receiverID = random[senderID];
                                }
                                else if (random.ContainsValue(senderID))
                                {
                                    int receiveKey = random.First(x => x.Value == senderID).Key;
                                    receiverID = receiveKey;
                                }
                            }
                            this.Invoke((MethodInvoker)delegate
                            {
                                if (clients.ContainsKey(receiverID))
                                {
                                    Send(receiverID, receivedMessage);
                                    rTB_log.Text += senderID + " send message to " + receiverID + Environment.NewLine;
                                }
                                else
                                {
                                    saveCloud(senderID, receiverID, "TEXT", receivedMessage);
                                    rTB_log.Text += senderID + " send message to " + receiverID + " offline" + Environment.NewLine;
                                }
                            });
                        }
                        else
                        {
                            // Get all members of the group
                            DocumentSnapshot snapshot = await db.Collection("Group").Document(receiverID.ToString()).GetSnapshotAsync();
                            GroupChat group = snapshot.ConvertTo<GroupChat>();
                            foreach (int memberID in group.MemberUID)
                            {
                                if (memberID != senderID)
                                {
                                    if (clients.ContainsKey(memberID))
                                    {
                                        Send(memberID, receivedMessage);
                                    }
                                    else
                                    {
                                        saveCloud(senderID, memberID, "TEXT", receivedMessage);
                                    }
                                }
                            }

                            // Group chat
                            this.Invoke((MethodInvoker)delegate
                            {
                                rTB_log.Text += senderID + " send message to group " + receiverID + Environment.NewLine;
                            });
                        }
                    }

                }
                catch (Exception ex)
                {
                    rTB_log.Invoke((MethodInvoker)delegate
                    {
                        rTB_log.Text += "Error: " + ex.Message + Environment.NewLine;
                    });
                }
            }

            tcpClient.Close();
            clients.Remove(key);
        }

        public async Task<int?> FindRandomSearchableUserExcluding()
        {
            try
            {
                // Query the Online collection for documents where isSearch is true and UID is not the excludedUID
                Query query = db.Collection("Online").WhereEqualTo("isSearch", true);
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                if (querySnapshot.Count == 0)
                {
                    // No users found
                    return null;
                }

                // Convert the documents to a list of UIDs, excluding the given UID
                List<int> searchableUserIds = querySnapshot.Documents.Select(doc => int.Parse(doc.Id)).ToList();

                // Select a random user from the list
                if (searchableUserIds.Count > 1)
                {
                    Random random = new Random();
                    int randomIndex = random.Next(searchableUserIds.Count);
                    int randomUserId = searchableUserIds[randomIndex];
                    return randomUserId;
                }

                return null;
            }
            catch (Exception ex)
            {
                // Log or handle the exception
                rTB_log.Invoke((MethodInvoker)delegate
                {
                    rTB_log.Text += "Error finding random searchable user: " + ex.Message + Environment.NewLine;
                });
                return null;
            }
        }


        public async void SendFile(int UID, string filePath)
        {
            try
            {
                NetworkStream stream = clients[UID].GetStream();

                FileInfo fileInfo = new FileInfo(filePath);
                string header = $"FILE={fileInfo.Name}:{fileInfo.Length}";
                //rTB_log.Text += "header file: " + header + Environment.NewLine;
                byte[] headerBytes = Encoding.ASCII.GetBytes(header);
                await stream.WriteAsync(headerBytes, 0, headerBytes.Length);

                // Now send the file
                byte[] fileBuffer = new byte[100 * 1024 * 1024];
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead;
                    while ((bytesRead = await fileStream.ReadAsync(fileBuffer, 0, fileBuffer.Length)) > 0)
                    {
                        await stream.WriteAsync(fileBuffer, 0, bytesRead);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending file: {ex.Message}", "ERROR");
            }
        }

        private void Send(int UID, string message)
        {
            NetworkStream toClientStream = clients[UID].GetStream();
            byte[] buffer = Encoding.UTF8.GetBytes(message);
            toClientStream.Write(buffer, 0, buffer.Length);
        }

        private async void saveCloud(int UID, int toUID, string Type, string Content)
        {
            storeMessage message = new storeMessage
            {
                UID = UID,
                toUID = toUID,
                Content = Content,
                Type = Type
            };

            CollectionReference messagesRef = db.Collection("Messages");
            string documentName = $"{UID}>{toUID}-{DateTime.Now}";
            DocumentReference docRef = messagesRef.Document(documentName);
            await docRef.SetAsync(message);
        }

        public void BroadcastMessage(string message)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(message);

            foreach (var clientPair in clients)
            {
                try
                {
                    NetworkStream toClientStream = clientPair.Value.GetStream();
                    toClientStream.Write(buffer, 0, buffer.Length);
                }
                catch (Exception ex)
                {
                    // Log or handle the exception
                    rTB_log.Invoke((MethodInvoker)delegate
                    {
                        rTB_log.Text += $"Error broadcasting to {clientPair.Key}: {ex.Message}" + Environment.NewLine;
                    });
                }
            }
        }

        public void ResizeImage(string originalFile, string newFile, int width, int height)
        {
            using (var ms = new MemoryStream(File.ReadAllBytes(originalFile))) // Read the original file into a MemoryStream.
            {
                using (Bitmap originalBitmap = new Bitmap(ms)) // Create a Bitmap from the MemoryStream.
                {
                    using (Bitmap newImage = new Bitmap(width, height))
                    {
                        using (Graphics graphics = Graphics.FromImage(newImage))
                        {
                            graphics.CompositingQuality = CompositingQuality.HighQuality;
                            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                            graphics.SmoothingMode = SmoothingMode.HighQuality;
                            graphics.DrawImage(originalBitmap, 0, 0, width, height);
                        }

                        newImage.Save(newFile, ImageFormat.Png);
                    }
                }
            }
        }
    }
}
