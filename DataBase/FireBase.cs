using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Cloud.Firestore;

namespace anonymous_chat.DataBase
{
    public static class FireBase
    {
        static string fireConfig = @"{
        ""type"": ""service_account"",
          ""project_id"": ""fir-e49a0"",
          ""private_key_id"": ""0883d290df58d0bd49eee60db4efab8f9e5da5c7"",
          ""private_key"": ""-----BEGIN PRIVATE KEY-----\nMIIEvgIBADANBgkqhkiG9w0BAQEFAASCBKgwggSkAgEAAoIBAQCoy6quYZD7rrvB\neRKRqJJRqolYivxjsDluY5LMCG3bv/eM+r3YUuymyJw3JAmJItD+p3GOGItcoa8R\n4awrPft31m9qx5mCbMJsXNerMWjziBgmifX/OHxnWrkCrgpnip+f6phf0S9cdIWR\n0nPPTIqirN4XeN1zGfObzLWxzUZ7E1xMr8OwvGY52RpFMpKnAkaXk7AEvn7JAF9C\ngz6FCmVWiqHKgAoC9McBxPjK/h6BJPj0V7su7ydFv6n1I7k8k+irILZjj+n0TCOI\n2UNWJpE/u9Ig/Wu4Kp02zg9SXgY0D4j6xPc6qjgHB3Jp1KQvxnH0V3t37sIavqNs\n2cj2VJb3AgMBAAECggEAFJxt9oYmYH0pIwpIKK4XU86Jn9y0ze1uTdrtyWk8FOm/\nbiEN/nS9ve3gGWGWon60W2wR0yKPHdwGVx4h2lPiuXLfPwApcAIcyYOauCY75QTY\nwhf1iFka47nxvnCHB7anIC1SttyBtn3p83DpwEXd5dhK9D3HeK8SDpO0GgZxVUTI\noLQpYWkc/YjNDWsELG4pYh57DSkSjKW1Bg2cR+8aisMsu7XSn/C3mXcwHg14YGwT\nMCkja8/MOfmLrTivrJkMhamJ/+654TBRuvmid65VtsDiFT/OHXvV/Xl8Raex55tx\nR/FVWbexAF+fqAeswnszhLN75MFugy9p19PgHbsPoQKBgQDYow+LLb/A7Z6GEC4T\nlZz7Bj+yWOrhAC0vVcBKZ3MHyHvKwc/gRUBP8LtZVj8blpimhqC1uusmKm0FHX3r\nWUEiLfGU2OuTDdkOaUNTLEfkF131cLo3WcL/7FNL7Z9s+VkSU/9o44/gUkefn8Ju\nKQAs8cu3ss8ClEBEVvUkpzC5YQKBgQDHdz+ThVq3KNKTn9BhuAVY3dYnHBdxxnMR\nWZ6Fs19FkRL3gemXI3AuYSGg863+toSXxn4j9Q5qdQOblrZcxzqeeo8YiYwMjuL0\nQp57Nsi8D+xVs9U+RsQEtGYzqG+5rG0U/VlXJ+Z61Dk1YKtiyp5Yl0iM+r6ukSH3\np3TnDYn3VwKBgQCjKNRZH4CvPofJMsBxMZqEmSU6HqvwACEH6I6luxowv3c0sxns\nMFwKmSWRWFq5XYfky7qYhIn8ObHYS2j960AaDP5I+8MZAzH6H598MfqHOG4kn90w\nOOObupLVucb73SSPoEHZ7qH68h1NhJZ8P1cJgqbK2Nn+eEea8N861szkoQKBgE+r\nEXmaieEeLP1Jl7FwDrKv6Bk23yZSXSNQ6D/+pM7kYu74tJPpU6Uypnvi2FwkClGU\ntk0mU9uxYP+D8tKbwI4L5ZunVIiviXaGJUaRWxvCQGL6sPFu0lwOscNwQy+l7uuz\nRTvKdAQbP75SHiIXS6eWxSOKAx82WJrSB59BiRmBAoGBAIgzvZSIYseA/a8xG9n1\nc8pHNTP/h0O5yg4ich9NBAyfU8sQyNlLq9tF34SFp+rDaVaX7jGVmZ/6Geksitsy\nGqQvq0FT/Cm72Lu5INjWulFo9Lq4HeTjJhiNpd3s7bcid3Og7Xht6riILTYDGbT0\noQzXMdTg9+6OxE9VUBeO0KUT\n-----END PRIVATE KEY-----\n"",
          ""client_email"": ""firebase-adminsdk-3kxcy@fir-e49a0.iam.gserviceaccount.com"",
          ""client_id"": ""114529438711579192535"",
          ""auth_uri"": ""https://accounts.google.com/o/oauth2/auth"",
          ""token_uri"": ""https://oauth2.googleapis.com/token"",
          ""auth_provider_x509_cert_url"": ""https://www.googleapis.com/oauth2/v1/certs"",
          ""client_x509_cert_url"": ""https://www.googleapis.com/robot/v1/metadata/x509/firebase-adminsdk-3kxcy%40fir-e49a0.iam.gserviceaccount.com"",
          ""universe_domain"": ""googleapis.com""
        }";

        static string firePath = "";
        public static FirestoreDb dataBase { get; private set; }

        public static bool setEnironmentVariables()
        {
            try
            {
                firePath = Path.Combine(Path.GetTempPath(), Path.GetFileNameWithoutExtension(Path.GetRandomFileName())) + ".json";
                File.WriteAllText(firePath, fireConfig);
                File.SetAttributes(firePath, FileAttributes.Hidden);
                Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", firePath);
                dataBase = FirestoreDb.Create("fir-e49a0");
                File.Delete(firePath);
                return true;
            }
            catch (Exception)
            {
                MessageBox.Show("Không thể kết nối đến cơ sở dữ liệu", "ERROR", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }
    }
}
