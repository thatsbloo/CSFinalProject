using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace YoavProject
{
    internal class JsonHandler
    {
        private readonly string filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Users.json");

        private class User
        {
            public string hashedPassword {  get; set; }
            public string salt { get; set; }
        }

        private Dictionary<string, User> users;
        private List<string> loggedusers = new List<string>();

        public JsonHandler()
        {
            if (File.Exists(filePath))
            {
                try
                {
                    string json = File.ReadAllText(filePath);
                    users = JsonConvert.DeserializeObject<Dictionary<string, User>>(json);
                    Console.WriteLine("Loaded " + users.Count + " users.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Failed to load users: " + ex.Message);
                    users = new Dictionary<string, User>();
                }
            }
            else
            {
                users = new Dictionary<string, User>();
                Console.WriteLine("No user file found. Starting with empty list.");
            }
        }

        private void saveUsers()
        {
            string json = JsonConvert.SerializeObject(users, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }

        public bool userExists(string username)
        {
            return users.ContainsKey(username);
        }

        public bool tryGetUserInfo(string username, out string hashedPassword, out string salt)
        {
            if (users.TryGetValue(username, out var entry))
            {
                hashedPassword = entry.hashedPassword;
                salt = entry.salt;
                return true;
            }
            hashedPassword = null;
            salt = null;
            return false;
        }

        public bool verifyLogin(string username, string password)
        {
            if (users.TryGetValue(username, out var entry))
            {
                string hash = getHashString(password + entry.salt);
                if (hash == entry.hashedPassword && !loggedusers.Contains(username))
                {
                    loggedusers.Add(username);
                    return true;
                }
            }
            return false;
        }

        public bool userLoggedIn(string username)
        {
            return loggedusers.Contains(username);
        }

        public void disconnectUser(string username)
        {
            if (loggedusers.Contains(username))
                loggedusers.Remove(username);
        }

        public bool addUser(string username, string password)
        {
            if (users.ContainsKey(username))
                return false;

            string salt = generateSalt();
            string hashedPassword = getHashString(password + salt);

            users[username] = new User
            {
                hashedPassword = hashedPassword,
                salt = salt
            };

            saveUsers();
            return true;
        }

        private static string generateSalt(int length = 16)
        {
            var rng = new RNGCryptoServiceProvider();
            byte[] saltBytes = new byte[length];
            rng.GetBytes(saltBytes);
            return Convert.ToBase64String(saltBytes);
        }
        private static byte[] getHash(string inputString)
        {
            using (HashAlgorithm algorithm = SHA256.Create())
                return algorithm.ComputeHash(Encoding.UTF8.GetBytes(inputString));
        }

        private static string getHashString(string inputString)
        {
            StringBuilder sb = new StringBuilder();
            foreach (byte b in getHash(inputString))
                sb.Append(b.ToString("X2"));

            return sb.ToString();
        }
    }
}
