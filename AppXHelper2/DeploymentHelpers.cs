using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Controls;
using System.Diagnostics;
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Xml;
using System.Windows.Threading;
using Windows.ApplicationModel;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.Storage.Search;
using System.Text.RegularExpressions;
using Windows.System;
using System.Security.Cryptography.X509Certificates;

namespace AppXHelperUI
{
    public class Tile
    {
        // Visual Elements
        public string DisplayName { get; set; }
        public string Description { get; set; }
        public string Logo { get; set; }
        public string SmallLogo { get; set; }
        public string BackgroundColor { get; set; }
        public string ForegroundColor { get; set; }
        
        // Default Tile
        public string ShortName { get; set; }
        public string WideLogo { get; set; }
        public bool ShowName { get; set; }
        public bool WideLogoExists { get; set; }
    }
    class DeploymentHelpers
    {

        #region RedPill 

        ///////////////////////////////////////////////////////
        /// <summary>
        /// to check if RED / PARTNER PILL is enabled.
        /// </summary>
        [DllImport("slc.dll")]
        public static extern Int32 SLGetWindowsInformationDWORD(
            [MarshalAs(UnmanagedType.LPWStr)] string pwszValueName,
            ref Int32 pdwValue);

        public static bool isPillEnabled(string pillString)
        {
            Int32 dwEnabled = 0;
            Int32 result = SLGetWindowsInformationDWORD(pillString, ref dwEnabled);
            bool returnValue = (dwEnabled == 1) ? true : false;
            if (returnValue)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(MainWindow.REGSTR_PATH_EXPLORER);
                List<string> allKeys = key.GetValueNames().ToList();

                if (allKeys.Contains("RPEnabled") && int.Parse(key.GetValue("RPEnabled").ToString()) == 1)
                    return true;
            }
            return false;
        }

        #endregion

        public static void cleanUpTempAppx()
        {
            try
            {
                if (Directory.Exists(MainWindow.APPX_TEMP))
                {
                    Directory.Delete(MainWindow.APPX_TEMP, true);
                }
            }
            catch
            {
                // if this can't delete... no need to throw an error
            }
        }

        public static PackagedAppIdentityInfo GetAppIDFromVisualTree(object sender)
        {
            // Button -> Grid -> ContentPresenter -> Border -> ListBoxItem
            // Recursively enumerate until we get to the ListBox that should contain a PackagedAppIdentityInfo object
            DependencyObject currentObject = VisualTreeHelper.GetParent(sender as DependencyObject);


            if (currentObject == null)
                return null;
            else if (!(currentObject is ListBoxItem))
            {
                return GetAppIDFromVisualTree(currentObject);
            }
            else
            {
                return ((currentObject as ListBoxItem).Content as PackagedAppIdentityInfo);
            }
        }

        public static OpenFileDialog getFiles(string title, string startingDir, bool multi, string filter)
        {
            OpenFileDialog o = new OpenFileDialog();
            o.InitialDirectory = startingDir;
            o.Multiselect = multi;
            o.Filter = filter;
            o.Title = title;

            return o;
        }

        public static string getUserSID()
        {
            System.Security.Principal.WindowsIdentity id = System.Security.Principal.WindowsIdentity.GetCurrent();
            return id.User.ToString();
        }

        private static XmlTextReader openManifest(StorageFolder installLocation)
        {
            string pkgManifest = installLocation.Path + MainWindow.APPX_MANIFEST;

            // if there is badness and the manifest file doesn't exist
            if (!File.Exists(pkgManifest))
            {
                return null;
            }

            return new XmlTextReader(pkgManifest);
        }

        public static bool isCertificateInstalled(string certificatePath)
        {
            X509Store store = new X509Store(StoreLocation.LocalMachine);
            store.Open(OpenFlags.ReadOnly);

            //Looks like we can create this from a byte array as well
            X509Certificate2 certificate = new X509Certificate2(certificatePath);

            X509Certificate2Collection certCollection = store.Certificates;
            foreach (X509Certificate2 cert in certCollection)
            {
                if (cert.GetSerialNumberString() == certificate.GetSerialNumberString())
                {
                    return true;
                }
            }

            store.Close();

            return false;
        }

        # region Generate Tile

        public static Tile getAppSpace(StorageFolder installLocation, string appID)
        {
            if (appID == null || appID == string.Empty || installLocation == null)
                return null;

            XmlTextReader reader = openManifest(installLocation);

            if (reader == null)
                return null;

            while (reader.Read())
            {
                reader.ReadToFollowing("Application");
                reader.MoveToAttribute("Id");
                if (reader.Value == appID)
                {
                    Tile t = new Tile();

                    // first read from the VisualElements section of the manifest
                    if (reader.ReadToFollowing("VisualElements"))
                    {
                        t.DisplayName = "";
                        t.Description = "";
                        t.Logo = "";
                        t.SmallLogo = "";
                        t.BackgroundColor = "";
                        t.ForegroundColor = "";
                        t.ShortName = "";
                        t.WideLogo = "";
                        t.ShowName = true;
                        t.WideLogoExists = false;

                        if (reader.MoveToAttribute("DisplayName"))
                            t.DisplayName = reader.Value;
                        if (reader.MoveToAttribute("Logo"))
                            t.Logo = reader.Value;
                        if (reader.MoveToAttribute("SmallLogo"))
                            t.SmallLogo = reader.Value;
                        if (reader.MoveToAttribute("Description"))
                            t.Description = reader.Value;
                        if (reader.MoveToAttribute("BackgroundColor"))
                            t.BackgroundColor = reader.Value;
                        if (reader.MoveToAttribute("ForegroundColor"))
                            t.ForegroundColor = reader.Value;

                        // then read from the Default Tile section
                        if (reader.ReadToFollowing("DefaultTile"))
                        {
                            if (reader.MoveToAttribute("ShortName"))
                                t.ShortName = reader.Value;
                            if (reader.MoveToAttribute("WideLogo"))
                            {
                                t.WideLogo = reader.Value;
                                t.WideLogoExists = true;
                            }
                            if (reader.MoveToAttribute("ShowName"))
                            {
                                bool value = false;
                                if (!Boolean.TryParse(reader.Value.ToLower(), out value))
                                    t.ShowName = value;
                            }
                        }

                        // finally return the data
                        reader.Close();
                        return t;
                    }
                }
            }
            reader.Close();
            return null;
        }

        /// <summary>
        /// This creates the cmd line args to be passed to the renderer.
        /// </summary>
        /// <param name="notificationXML"></param>
        /// <param name="defaultTile"></param>
        /// <returns></returns>
        public static string GenerateCmdLineArgs(string notificationXML, Tile tile, string resourcePath)
        {
            //Tile Type
            string args = " /T IMMERSIVE";

            if (tile.WideLogoExists)
            {
                args += " /S MEDIUM";
            }
            else
            {
                args += " /S SMALL";
            }


            //Tile Name
            if (tile.DisplayName.Length != 0)
            {
                args += " /DN \"" + tile.DisplayName + "\"";
            }

            //The logo if unspecified falls back to the manifest logo options.
            if (tile.Logo.Length != 0)
            {
                args += " /L \"" + resourcePath + tile.Logo + "\"";
            }

            //Background and Foreground Colors
            if (tile.BackgroundColor.Length != 0)
            {
                args += " /BC " + tile.BackgroundColor;
            }

            if (tile.ForegroundColor.Length != 0)
            {
                args += " /FC " + tile.ForegroundColor;
            }


            args += " /TN \"" + notificationXML + "\"";


            return args;
        }

        # endregion

        public static bool tileToolInstalled(string toolPath)
        {
            return (File.Exists(toolPath));
        }

        public static List<string> getAppIDs(StorageFolder installLocation)
        {
            List<string> appIDs = new List<string>();

            if (installLocation == null)
                return appIDs;

            XmlTextReader reader = openManifest(installLocation);
            if (reader == null)
                return appIDs;

            while (reader.Read())
            {
                reader.ReadToFollowing("Application");
                reader.MoveToAttribute("Id");
                if (reader.Value != string.Empty)
                    appIDs.Add(reader.Value);
            }

            reader.Close();
            return appIDs;

        }

        public static List<string> getAppListFromError(string error)
        {
            List<string> tempList = new List<string>();
            List<string> finalList = new List<string>();

            // get the list of PRAIDs
            tempList = error
                .Replace("Unable to install because the following applications need to be closed", "")
                .Replace(". Failed with HRESULT 0x80004004.", "")
                .Trim()
                .Split(' ')
                .ToList();

            string pattern = "!(?<AppName>.*)";

            foreach (string praid in tempList)
            {
                // get the app from the praid
                MatchCollection matches = Regex.Matches(praid, pattern);

                foreach (Match m in matches)
                {
                    foreach (Capture c in m.Groups["AppName"].Captures)
                        finalList.Add(c.Value);
                    break;
                }
            }

            return finalList;
        }

        public static PackagedAppIdentityInfo getNewPackageInfo(Package package)
        {
            PackagedAppIdentityInfo info = new PackagedAppIdentityInfo();
            info.Publisher = package.Id.Publisher;
            info.Version = package.Id.Version.ToString();
            info.ResourceId = package.Id.ResourceId;
            info.Architecture = package.Id.Architecture;
            info.Directory = package.InstalledLocation;
            info.Moniker = package.Id.FullName;

            return info;
        }

        // finds the AppUserModelId for the package specified and returns true if 
        // it was able to find it, otherwise returns false.
        public static bool getAppUserModelID(PackagedAppIdentityInfo info)
        {
            // structure here is the following
            // Computer\HCR\ActivatableClasses\Package\<pkg_fullname>\Server\<appName>\AppUserModelId
            try
            {
                // open up the registry key where all inbox apps are stored and specifically our app
                RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"ActivatableClasses\Package\" + info.Moniker + @"\Server");
                
                // enumerate the list of keys within the this key 
                string[] allKeys = key.GetSubKeyNames();

                // open up the appname key
                key = Registry.ClassesRoot.OpenSubKey(@"ActivatableClasses\Package\" + info.Moniker + @"\Server\" + allKeys[0]);

                // get all the values within that subkey
                List<string> allValues = key.GetValueNames().ToList();

                // look up the AppUserModelId
                if (allValues.Contains("AppUserModelId"))
                {
                    info.AppUserModelID = key.GetValue("AppUserModelId").ToString();
                    return true;
                }

            }
            catch //(Exception ex)
            {
                // no need to crash
                //return false;
            }

            return false;
        }

        public static bool isActiveDeployment(string moniker, List<DeploymentTransaction> list)
        {
            foreach (DeploymentTransaction dt in list)
            {
                if (dt.Moniker == moniker)
                    return true;
            }
            return false;
        }

        // get all the errors from our failed deployments
        public static string getAllErrors()
        {
            string completeErrorText = string.Empty;
            lock (MainWindow.failedDeployments)
            {
                foreach (DeploymentTransaction t in MainWindow.failedDeployments)
                {
                    string packagePath = "Package: " + t.MainPackage;
                    string errorCode = "HRESULT: 0x" + t.ErrorCode.ToString("X");
                    string errorText = errorText = "Error Text: " + t.ErrorText;
                    completeErrorText += packagePath + "\n" + errorCode + "\n" + errorText + "\n\n";
                }
            }
            return completeErrorText;
        }

        public static void logUser(object state)
        {
            try
            {
                // get the domain and app version
                string text = "User: " + Environment.UserDomainName + @"\" + String.Format("{0:X8}", Environment.UserName.GetHashCode()) + "  \n\nAppVersion: " + MainWindow.CURRENT_APPXHELPER_VERSION;

                // hash the user domain and name to create a text file name
                string file = String.Format("{0:X8}",Environment.UserDomainName.GetHashCode()) + String.Format("{0:X8}",Environment.UserName.GetHashCode()) + ".txt";

                if (Directory.Exists(MainWindow.USER_LOG))
                {
                    if (File.Exists(MainWindow.USER_LOG + file))
                        File.Delete(MainWindow.USER_LOG + file);

                    File.WriteAllText(MainWindow.USER_LOG + file, text);
                }
            }
            catch
            {
                // in case there's an error... no need to crash the whole tool
            }
        }

        // create correct tool local links
        public static void chooseToolsOnArch()
        {
            MainWindow.TEST_APP_LAUNCHER = AppDomain.CurrentDomain.BaseDirectory + @"\x86\TestAppLauncher.exe";
            
            // if we're in 64 bit
            if (Environment.Is64BitOperatingSystem)
                MainWindow.TEST_APP_LAUNCHER = AppDomain.CurrentDomain.BaseDirectory + @"\amd64\TestAppLauncher.exe";
        }

        private static string getPath(string dir)
        {
            string[] split = dir.Split('\\');

            return split[split.Length - 1];

        }

        public static Version getHighestAppxVersion()
        {
            Version highestVersion = new Version();

            if (!Directory.Exists(MainWindow.APPX_SHARE))
                throw new FileNotFoundException("Could not find the appxhelper directory");

            highestVersion = new Version();

            foreach (string dir in Directory.EnumerateDirectories(MainWindow.APPX_SHARE))
            {
                try
                {
                    Version v = new Version(getPath(dir));

                    if (v.CompareTo(highestVersion) > 0)
                        highestVersion = v;
                }
                catch
                {
                    continue;
                }
            }
            
            return highestVersion;
        }

        //
        // ShellExecute
        // True - we need to elevate
        // False - we don't elevate
        // null - we shellexecute AND don't elevate
        public static bool executeProcess(string fileToExecute, string arguments, bool shellExecute, out string operationInformation)
        {
            Process p = new Process();
            operationInformation = String.Empty;

            if (fileToExecute == null || fileToExecute == "")
            {
                return false;
            }

            p.StartInfo.FileName = fileToExecute;
            p.StartInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;

            if (arguments != null && arguments != "")
                p.StartInfo.Arguments = arguments;

            if (shellExecute)
            {
                p.StartInfo.Verb = "runas";
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.RedirectStandardError = false;
                p.StartInfo.RedirectStandardOutput = false;
            }
            else
            {
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
            }

            //Seperate try/catch blocks for debugging
            try
            {
                p.Start();
            }
            catch (Exception ex)
            {
                operationInformation = ex.ToString();
                return false;
            }

            try
            {
                p.WaitForExit();
            }
            catch //(Exception ex)
            {
                //operationInformation = ex.ToString();
                //return false;
            }

            if (shellExecute == false)
            {

                string stdOutput = "";
                string errOutput = "";

                try
                {
                    stdOutput = p.StandardOutput.ReadToEnd();
                }
                catch
                {
                    //operationInformation = ex.ToString();
                    //return false;
                }
                try
                {
                    errOutput = p.StandardError.ReadToEnd();
                }
                catch
                {
                    //operationInformation = ex.ToString();
                    //return false;
                }

                if (stdOutput != "" || errOutput != "")
                {
                    operationInformation = (stdOutput == "" ? errOutput : (stdOutput + " " + errOutput));
                }
                else
                    operationInformation = ("Command " + fileToExecute + " complete.");
            }

            // if we had success return true 
            return (p.ExitCode == 0);
        }
    }
}
