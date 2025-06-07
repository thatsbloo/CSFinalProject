using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        public bool addUser(string username, string hashedPassword, string salt)
        {
            if (users.ContainsKey(username))
                return false;

            users[username] = new User
            {
                hashedPassword = hashedPassword,
                salt = salt
            };

            saveUsers();
            return true;
        }
    }
}
