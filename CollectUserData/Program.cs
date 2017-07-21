using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CollectUserData
{
    class User
    {
        private string _userName;
        private string _domainName;
        private string _appVersionString;
        private Version _appVersion;

        public User(string userName, string domainName, string appVersion)
        {
            _userName = userName;
            _domainName = domainName;

            try
            {
                _appVersion = new Version(appVersion);
                _appVersionString = _appVersion.ToString();
            }
            catch
            {
                _appVersion = new Version();
                _appVersionString = appVersion;
            }
        }

        public string UserName { get { return _userName; } }
        public string DomainName { get { return _domainName; } }
        public string AppVersionString { get { return _appVersionString; } }
        public Version AppVersion { get { return _appVersion; } }
    }

    class Integer
    {
        public int Value { get; set; }

        public Integer()
        {
            Value = 1;
        }
    }

    class Program
    {
        private static List<Version> SortedBuildList = new List<Version>();

        static void Main(string[] args)
        {
            string USER_LOG = @"\\scratch2\scratch\jasosal\Userlist\";
            string CURR_DIR = AppDomain.CurrentDomain.BaseDirectory;

            List<User> allUsers = new List<User>();

            /*
             * Log is in the format as follows
             * 
             * User: <domain>\<user> AppVersion: <version>
             */

            if (Directory.Exists(USER_LOG))
            {
                foreach (string file in Directory.EnumerateFiles(USER_LOG))
                {
                    User u = getUserInfo(file);

                    if(u != null)
                        allUsers.Add(u);
                }
            }
            else
                Console.Write("Userlog directory doesn't exist: " + USER_LOG);

            Hashtable domainHash = new Hashtable();
            Hashtable versionHash = new Hashtable();

            

            // now we have all users and their app versions.
            foreach (User u in allUsers)
            {
                incrementHash(u.DomainName, domainHash);
                incrementHash(u.AppVersionString, versionHash);
            }

            foreach (string key in versionHash.Keys)
            {
                try
                {
                    SortedBuildList.Add(new Version(key));
                }
                catch
                {
                }
            }

            SortedBuildList.Sort();

            List<string> lines = new List<string>();

            getResults(domainHash, "Domain", lines);
            getResults(versionHash, "Version", lines);

            lines.Add("Total Users: " + allUsers.Count);
            File.WriteAllLines(CURR_DIR + "data.txt", lines);
        }

        private static void getResults(Hashtable t, string keyType, List<string> lines)
        {

            if (keyType == "Version")
            {
                foreach (Version v in SortedBuildList)
                {
                    if (t.ContainsKey(v.ToString()))
                    {
                        lines.Add(keyType + ": " + v.ToString() + " Users: " + (t[v.ToString()] as Integer).Value + "\n");
                        t.Remove(v.ToString());
                    }
                }
            }


            foreach (string key in t.Keys)
                lines.Add(keyType + ": " + key + " Users: " + (t[key] as Integer).Value + "\n");
            
            lines.Add("");
        }

        private static User getUserInfo(string file)
        {
            string text = "";
            try
            {
                text = File.ReadAllText(file);
            }
            catch
            {
                return null;
            }

            string domain = getValue(" .*" + Regex.Escape(@"\"), text);
            string name = getValue(Regex.Escape(@"\") + ".*? ", text);

            // adding in dummy space so function works :)

            
            text += " ";
            string version = getValue(" ([0-9]+" + Regex.Escape(".") + ")+[0-9] ", text);

            if (version == string.Empty)
            {
                //ex: "AppVersion: PDC-6.0"
                version = getValue(" PDC-[0-9].[0-9] ", text);

            }

            return new User(name, domain, version);
        }

        private static void incrementHash(string value, Hashtable t)
        {
            if (t.ContainsKey(value))
            {
                Integer i = t[value] as Integer;
                i.Value++;

                t.Remove(value);
                t.Add(value, i);
            }
            else
                t.Add(value, new Integer());
        }

        private static string getValue(string pattern, string text)
        {
            Regex r = new Regex(pattern);


            string value = r.Match(text).Value;

            try
            {
                value = value.Substring(1, value.Length - 2);
            }
            catch
            {
                return "";
            }

            return value;
        }
    }
}
