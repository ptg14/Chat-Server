using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Tab;

namespace anonymous_chat.Chat
{
    public interface IChatModel
    {
        bool Inbound { get; set; }
        bool Read { get; set; }
        DateTime Time { get; set; }
        string? Author { get; set; }
        string Type { get; }
    }

    public class TextChatModel : IChatModel
    {
        public bool Inbound { get; set; }
        public bool Read { get; set; }
        public DateTime Time { get; set; }
        public string? Author { get; set; }
        public string Type { get; } = "text";

        public string? Body { get; set; }

        public async void SaveText(string File)
        {
            using (StreamWriter writer = new StreamWriter(File, true))
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                await writer.WriteLineAsync(json);
            }
        }

        public static TextChatModel ReadText(string json)
        {
            return JsonConvert.DeserializeObject<TextChatModel>(json);
        }
    }

    public class ImageChatModel : IChatModel
    {
        public bool Inbound { get; set; }
        public bool Read { get; set; }
        public DateTime Time { get; set; }
        public string? Author { get; set; }
        public string Type { get; } = "image";

        public Image? Image { get; set; }
        public string? ImageName { get; set; }

        public async void SaveImage(string File)
        {
            // Convert the Image to a byte array
            byte[] imageBytes;
            using (var ms = new MemoryStream())
            {
                Image.Save(ms, Image.RawFormat);
                imageBytes = ms.ToArray();
            }

            // Convert the byte array to a Base64 string
            string imageBase64 = Convert.ToBase64String(imageBytes);

            // Create a new anonymous object with the same properties as this object, but with the Image replaced by the Base64 string
            var objectToSerialize = new
            {
                Inbound = this.Inbound,
                Read = this.Read,
                Time = this.Time,
                Author = this.Author,
                Type = this.Type,
                Image = imageBase64,
                ImageName = this.ImageName
            };

            using (StreamWriter writer = new StreamWriter(File, true))
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                await writer.WriteLineAsync(json);
            }
        }

        public static ImageChatModel ReadImage(string json)
        {
            var deserializedObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Convert the Base64 string back to a byte array
            byte[] imageBytes = Convert.FromBase64String(deserializedObject["Image"].ToString());

            // Convert the byte array back to an Image
            Image image;
            using (var ms = new MemoryStream(imageBytes))
            {
                image = Image.FromStream(ms);
            }

            // Create an ImageChatModel object
            var imageChatModel = new ImageChatModel
            {
                Inbound = (bool)deserializedObject["Inbound"],
                Read = (bool)deserializedObject["Read"],
                Time = DateTime.Parse(deserializedObject["Time"].ToString()),
                Author = deserializedObject["Author"]?.ToString(),
                Image = image,
                ImageName = deserializedObject["ImageName"]?.ToString()
            };

            return imageChatModel;
        }
    }

    public class AttachmentChatModel : IChatModel
    {
        public bool Inbound { get; set; }
        public bool Read { get; set; }
        public DateTime Time { get; set; }
        public string? Author { get; set; }
        public string Type { get; } = "attachment";

        public byte[]? Attachment { get; set; }
        public string? Filename { get; set; }

        public async void SaveAttachment(string File)
        {
            // Convert the byte array to a Base64 string
            string attachmentBase64 = Attachment != null ? Convert.ToBase64String(Attachment) : null;

            // Create a new anonymous object with the same properties as this object, but with the byte array replaced by the Base64 string
            var objectToSerialize = new
            {
                Inbound = this.Inbound,
                Read = this.Read,
                Time = this.Time,
                Author = this.Author,
                Type = this.Type,
                Attachment = attachmentBase64,
                Filename = this.Filename
            };

            using (StreamWriter writer = new StreamWriter(File, true))
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                await writer.WriteLineAsync(json);
            }
        }

        public static AttachmentChatModel ReadAttachment(string json)
        {
            var deserializedObject = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            // Convert the Base64 string back to a byte array
            byte[] attachmentBytes = Convert.FromBase64String(deserializedObject["Attachment"].ToString());

            // Create an AttachmentChatModel object
            var attachmentChatModel = new AttachmentChatModel
            {
                Inbound = (bool)deserializedObject["Inbound"],
                Read = (bool)deserializedObject["Read"],
                Time = DateTime.Parse(deserializedObject["Time"].ToString()),
                Author = deserializedObject["Author"]?.ToString(),
                Attachment = attachmentBytes,
                Filename = deserializedObject["Filename"]?.ToString()
            };

            return attachmentChatModel;
        }

        public class SimsimiResponse
        {
            public string text { get; set; }
            public string message { get; set; }
            public string id { get; set; }
            public string status { get; set; }
            public string language { get; set; }
        }
    }
}
