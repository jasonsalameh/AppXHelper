using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using IWshRuntimeLibrary;
using System.Threading;
using Microsoft.Win32;

namespace AppXToolScript
{
    class Program
    {
        // Globals
        private static string SYSTEM_DRIVE = Environment.GetEnvironmentVariable("windir", EnvironmentVariableTarget.Machine).ToLower().Replace("\\windows", "");

        // appx helper directory info
        private static string APPX_DIR = SYSTEM_DRIVE + @"\AppXHelper";

        // misc
        private static string LINK_NAME = @"\App Launcher.lnk";
        private static string LINK_NAME_OLD = @"\Start Immersive App Launcher.lnk";
        private static string SHARE = @"\\tkfiltoolbox\tools\appxhelper\";
        
        // tools
        private static string WINBUILDS = @"\\winbuilds\release\winmain\";
        private static string TILE_RENDERER_INSTALLER = @"\\windesign\partner\UEX\TileDesigner\Build\install.cmd";
        private static string TILE_PATH = @"c:\tiles\";

        // unknowns
        private static string APPX_DIR_TOOLS = "";
        private static string MAKE_APPX = "";
        private static string TEST_APP_LAUNCHER = "";
        private static string DEPLOY_APPX = "";

        // max appx helper for PDC-7
        private static string PDC7_CURRENT_HIGHEST = @"\\tkfiltoolbox\tools\appxhelper\3.0.4.0";
        private static int CURRENT_PDC7_BUILD = 7950;

        // max appx helper for PDC-6
        private static string PDC6_CURRENT_HIGHEST = @"\\tkfiltoolbox\tools\appxhelper\PDC-6.1";
        private static int CURRENT_PDC6_BUILD = 7972;
        private static int CURRENT_PDC6_CLR_REV = 16769;

        // max appx helper for PDC-5
        private static string PDC5_CURRENT_HIGHEST = @"\\tkfiltoolbox\tools\appxhelper\PDC-5.2";
        private static int CURRENT_PDC5_BUILD = 7992;
        private static int CURRENT_PDC5_CLR_REV = 16795;

        // max appx helper for PDC-4
        private static string PDC4_CURRENT_HIGHEST = @"\\tkfiltoolbox\tools\appxhelper\3.3.0.7";
        private static int CURRENT_PDC4_BUILD = 8018;
        private static int CURRENT_PDC4_CLR_REV = 16836;

        // SET THIS AS TRUE FOR NOW. 
        private static bool DEPLOY_APPX_NEEDED = true;

        // max version for non CLR hotel build
        private static string NEW_REFACTOR_CURRENT_HIGHEST = @"\\tkfiltoolbox\tools\appxhelper\3.3.0.7";
        private static int CURRENT_NEW_REFACTOR_BUILD = 8030;

        // max version for non CLR hotel build
        private static string PACKAGEMAN_BREAKING_CHANGE_HIGHEST = @"\\tkfiltoolbox\tools\appxhelper\3.4.3.1";
        private static int PACKAGEMAN_BREAKING_CHANGE_BUILD = 8118;

        private static bool TILE_RENDERER_POSSIBLE = false;

        private static string outPut = "";

        static void Main(string[] args)
        {
            try
            {
                chooseToolsOnArch();

                int PID = getPID(args);
                bool isUpdate = PID < 0 ? false: true;

                string newAppXHelper;

                //Console.Write("IsUpdate: " + isUpdate.ToString() + "\n");

                //Console.Write("OSVersion: " + Environment.OSVersion.Version.Build + "\nRevision: " + Environment.Version.Revision);
                //Thread.Sleep(10000);



                // if you have PDC-4
                if (Environment.OSVersion.Version.Build == CURRENT_PDC4_BUILD &&
                    Environment.Version.Revision == CURRENT_PDC4_CLR_REV)
                {
                    newAppXHelper = PDC4_CURRENT_HIGHEST;
                    TILE_RENDERER_POSSIBLE = true;
                }

                // if you have PDC-5
                else if (Environment.OSVersion.Version.Build == CURRENT_PDC5_BUILD &&
                    Environment.Version.Revision == CURRENT_PDC5_CLR_REV)
                {
                    newAppXHelper = PDC5_CURRENT_HIGHEST;
                    DEPLOY_APPX_NEEDED = true;
                }

                // if you have PDC-6
                else if (Environment.OSVersion.Version.Build == CURRENT_PDC6_BUILD &&
                    Environment.Version.Revision == CURRENT_PDC6_CLR_REV)
                {
                    newAppXHelper = PDC6_CURRENT_HIGHEST;
                    DEPLOY_APPX_NEEDED = true;
                }

                // if you have PDC-7
                else if (Environment.OSVersion.Version.Build == CURRENT_PDC7_BUILD)
                {
                    newAppXHelper = PDC7_CURRENT_HIGHEST;
                }

                // if you're not on the newest builds but don't have PDC
                else if (Environment.OSVersion.Version.Build <= CURRENT_NEW_REFACTOR_BUILD)
                {
                    newAppXHelper = NEW_REFACTOR_CURRENT_HIGHEST;
                }

                // if you're not on the newest builds but don't have PDC
                else if (Environment.OSVersion.Version.Build < PACKAGEMAN_BREAKING_CHANGE_BUILD)
                {
                    newAppXHelper = PACKAGEMAN_BREAKING_CHANGE_HIGHEST;                
                }

                // this is so we can designate which version of the tool goes to which builds
                // it will ensure that PDC_CURRENT_HIGHEST ONLY will be installed to builds less
                // than CURRENT_MIN_BUILD
                else //if (Environment.OSVersion.Version.Build >= CURRENT_MIN_BUILD)
                {
                    Console.Write("Fetching information on newest AppXHelper Bits...");
                    newAppXHelper = getNewestAppXHelper();
                    TILE_RENDERER_POSSIBLE = true;
                    Console.Write("Done!\n");
                }

                Console.Write("Fetching information on Tools from winbuilds...");
                string currentWinMain = getCurrentBuildPath();

                Console.Write("Done!\n");

                Console.WriteLine("Build Path: " + currentWinMain);

                if (isUpdate)
                {
                    Console.Write("Waiting for AppXHelper tool to exit...");
                    waitForExit(PID);
                    Console.Write("Done!\n");

                    Console.Write("Copying all AppX tools to local disk...");
                    // copy all the tools over
                    copyToolsToAppXDir(newAppXHelper, currentWinMain, true);

                    if (!Directory.Exists(TILE_PATH))
                    {
                        Console.Write("Copying all Tile Rendering tools to local disk...");
                        copyTileRenderingTools();
                    }

                    Console.Write("Relaunching AppXHelper tool...\n");

                    try
                    {
                        Process.Start(APPX_DIR + @"\appxhelper2.exe");
                        Environment.Exit(0);
                    }

                    catch(Exception e)
                    {
                        Console.Write("ERROR: Could not relaunch AppXHelper\n\n" + e.Message);
                        Thread.Sleep(5000);
                    }
                }
                else
                {
                    Console.Write("Copying all AppX tools to local disk...");
                    
                    // copy all the tools over
                    copyToolsToAppXDir(newAppXHelper, currentWinMain, false);

                    if (TILE_RENDERER_POSSIBLE)
                    {
                        Console.Write("Copying all Tile Rendering tools to local disk...");
                        copyTileRenderingTools();
                    }

                    Console.Write("Creating Shortcut on desktop...");
                    // make the short cut
                    createShortCut();
                    Console.Write("Done!\n");
                }
            }
            catch (Exception e)
            {
                Console.Write("ERROR: AppXHelper Copy Operation NOT successful\n\n" + e.Message);
                Thread.Sleep(5000);
            }
        }

        private static void waitForExit(int PID)
        {
            try
            {
                Process p = Process.GetProcessById(PID);
                p.WaitForExit();
            }
            catch
            {

            }
        }

        private static int getPID(string[] args)
        {
            // if there are any agruments
            if (!(args == null) && !(args.Length == 0))
            {
                string prev = "";
                int pid = -1;
                foreach (string arg in args)
                {
                    if (prev == @"-u")
                        if (int.TryParse(arg, out pid))
                            return pid;

                    prev = arg;
                }
            }

            return -1;
        }

        private static void copyTileRenderingTools()
        {
            bool result = false;
            result  = executeProcess(TILE_RENDERER_INSTALLER, "", true, out outPut);

            printResult(result);
        }

        private static void printResult(bool result)
        {
            if (result)
                Console.Write("Done!\n");
            else
            {
                Console.Write("Failed!\n");
                Thread.Sleep(3000);
            }
        }

        private static void createShortCut()
        {
            // get the environment directory
            string deskDir = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);

            // if the shortcut exists... just kill it
            if (System.IO.File.Exists(deskDir + LINK_NAME))
                System.IO.File.Delete(deskDir + LINK_NAME);

            if (System.IO.File.Exists(deskDir + LINK_NAME_OLD))
                System.IO.File.Delete(deskDir + LINK_NAME_OLD);

            // create the new shortcut shortcut to it
            WshShell shell = new WshShell();
            IWshShortcut link = (IWshShortcut)shell.CreateShortcut(deskDir + LINK_NAME);
            link.TargetPath = APPX_DIR + @"\AppXHelper2.exe";
            link.Description = "Start Immersive App Launcher!";
            link.WorkingDirectory = APPX_DIR;
            link.Save();
        }

        private static void copyToolsToAppXDir(string highestToolVersion, string currentWinmain, bool isUpdate)
        {
            if (!isUpdate)
            {
                // if the directory exists... just delete it
                if (Directory.Exists(APPX_DIR))
                    Directory.Delete(APPX_DIR, true);
            }
            else
            {
                // assume the directory is already there + we'll have a lock on it
                foreach (string file in Directory.EnumerateFiles(APPX_DIR))
                    System.IO.File.Delete(file);

                foreach (string dir in Directory.EnumerateDirectories(APPX_DIR))
                    Directory.Delete(dir, true);
            }
            
            // create the directory structure
            Directory.CreateDirectory(APPX_DIR);
            Directory.CreateDirectory(APPX_DIR_TOOLS);
            
            bool result = false;
            
            // copy the latest appx bits to the users directory
            result = executeProcess("xcopy", @"/Y /S " + highestToolVersion + " " + APPX_DIR, false, out outPut);            
            result &= executeProcess("xcopy", @"/Y /S " + currentWinmain + TEST_APP_LAUNCHER + " " + APPX_DIR_TOOLS, false, out outPut);
            //result &= executeProcess("xcopy", @"/Y /S " + currentWinmain + MAKE_APPX + " " + APPX_DIR_TOOLS, false, out outPut);

            //if(DEPLOY_APPX_NEEDED)
            //    result &= executeProcess("xcopy", @"/Y /S " + currentWinmain + DEPLOY_APPX + "* " + APPX_DIR_TOOLS, false, out outPut);
            

            printResult(result);
        }

        // create correct tool local links
        public static void chooseToolsOnArch()
        {
            // we need to figure out which architecture we're on to launch the correct testAppLauncher
            if (Environment.Is64BitOperatingSystem)
            {
                TEST_APP_LAUNCHER = @"\amd64fre\bin\idw\TestAppLauncher.exe";
                MAKE_APPX = @"\amd64fre\bin\AppxTools\MakeAppx\makeappx.exe";
                DEPLOY_APPX = @"\amd64fre\bin\AppxTools\DeployAppx\";
                APPX_DIR_TOOLS = APPX_DIR + @"\amd64";
            }
            else
            {
                TEST_APP_LAUNCHER = @"\x86fre\bin\idw\TestAppLauncher.exe";
                MAKE_APPX = @"\x86fre\bin\AppxTools\MakeAppx\makeappx.exe";
                DEPLOY_APPX = @"\x86fre\bin\AppxTools\DeployAppx\";
                APPX_DIR_TOOLS = APPX_DIR + @"\x86";
            }
        }

        private static string getBuildString()
        {
            RegistryKey key = Registry.LocalMachine.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion");

            return key.GetValue("BuildLab").ToString();
        }

        private static string getCurrentBuildPath()
        {
            string buildString = getBuildString();

            //build string is setup like this <build>.<branch>.<build date>
            List<string> buildSplit = buildString.Split('.').ToList();

            // path is setup like this \\winbuilds\release\<build>\<branch>.0.<build date>\...
            string currentBuildPath = WINBUILDS + buildSplit[1] + @"\" + buildSplit[0] + ".0." + buildSplit[2];

            if (!Directory.Exists(currentBuildPath))
                return getCurrentWinmain();

            return currentBuildPath;
        }

        private static string getCurrentWinmain()
        {
            if (!Directory.Exists(WINBUILDS))
                throw new FileNotFoundException("Could not find the winbuilds directory");

            string currentWinmainPath = "";
            string alternateWinmainPath = "";

            bool buildFound = false;


            // get the build of the PC the tool is being run on so we can fetch the exact (or close version)
            // version of the tools from that winmain directory
            Version currentBuild = new Version(Environment.OSVersion.Version.Build.ToString() + ".0.0");
            Version highestMatchBuild = new Version(int.MaxValue.ToString() + ".0.0");

            Version highestBuild = new Version("0.0.0");

            // grab all the directory strings from the winbuilds share so we don't go there too many times
            foreach (string dir in Directory.EnumerateDirectories(WINBUILDS))
            {
                try
                {
                    // get the version info on the currnt directory
                    Version v = new Version(getPath(dir).Substring(0, 13));

                    // if this directory is equal to or higher than the current build we're on add it
                    if (currentBuild.CompareTo(v) <= 0 &&
                        highestMatchBuild.CompareTo(v) > 0 &&
                        System.IO.File.Exists(dir + TEST_APP_LAUNCHER))
                    {
                        highestMatchBuild = v;
                        currentWinmainPath = dir;
                        buildFound = true;
                    }

                    // this will find the highest winmain directory in the set
                    if (highestBuild.CompareTo(v) <= 0 &&
                        System.IO.File.Exists(dir + TEST_APP_LAUNCHER))
                    {
                        highestBuild = v;
                        alternateWinmainPath = dir;
                    }
                }
                catch
                {
                    continue;
                }
            }

            // if we didn't find a winmain directory, return the highest winmain path we did find
            if (!buildFound)
                return alternateWinmainPath;

            return currentWinmainPath;
        }

        private static string getNewestAppXHelper()
        {
            if(!Directory.Exists(SHARE))
                throw new FileNotFoundException("Could not find the appxhelper directory");

            Version highestVersion = new Version();
            string highestVersionPath = "";

            foreach (string dir in Directory.EnumerateDirectories(SHARE))
            {
                try
                {
                    Version v = new Version(getPath(dir));

                    if (v.CompareTo(highestVersion) > 0)
                    {
                        highestVersion = v;
                        highestVersionPath = dir;
                    }
                }
                catch
                {
                    continue;
                }

            }

            if(highestVersionPath == "")
                throw new FileNotFoundException("Could not find the appxhelper directory");

            return highestVersionPath;
        }

        private static string getPath(string dir)
        {
            string[] split = dir.Split('\\');

            return split[split.Length - 1];

        }

        // elevate
        // true - elevate
        // false - don't elevate
        // null - shellexec
        public static bool executeProcess(string fileToExecute, string arguments, bool? elevate, out string operationInformation)
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
            
            if (elevate == true)
            {
                p.StartInfo.Verb = "runas";
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.RedirectStandardError = false;
                p.StartInfo.RedirectStandardOutput = false;
            }
            else if(elevate == false)
            {
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.UseShellExecute = false;
            }
            else if (elevate == null)
            {
                p.StartInfo.UseShellExecute = true;
                p.StartInfo.RedirectStandardError = false;
                p.StartInfo.RedirectStandardOutput = false;
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

            if (elevate == false)
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
                    return false;
                }
                try
                {
                    errOutput = p.StandardError.ReadToEnd();
                }
                catch
                {
                    //operationInformation = ex.ToString();
                    return false;
                }

                if (stdOutput != "" || errOutput != "")
                {
                    operationInformation = (stdOutput == "" ? errOutput : (stdOutput + " " + errOutput));
                }
                else
                    operationInformation = ("Command " + fileToExecute + " complete.");
            }

            // if we had success return true 
            return (p.ExitCode == 0 || p.ExitCode == 1);
        }

    }
}
