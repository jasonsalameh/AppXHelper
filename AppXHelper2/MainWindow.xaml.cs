using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Media.Animation;
using System.Threading;
using Windows.Management.Deployment;
using System.Security.Principal;
using System.Windows.Threading;
using System.IO.Packaging;
using System.Collections;


using DeploymentOperation = Windows.Foundation.IAsyncOperationWithProgress<Windows.Management.Deployment.DeploymentResult, Windows.Management.Deployment.DeploymentProgress>;
using DeploymentProgressEventHandler = Windows.Foundation.AsyncOperationProgressHandler<Windows.Management.Deployment.DeploymentResult, Windows.Management.Deployment.DeploymentProgress>;

namespace AppXHelperUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region Globals
        // Globals
        public static string SYSTEM_DRIVE = Environment.GetEnvironmentVariable("windir", EnvironmentVariableTarget.Machine).ToLower().Replace("\\windows", "");
        
        // Single instance store
        public static string APPLICATIONS_FOLDER_OLD = SYSTEM_DRIVE + @"\Program Files\Applications";
        public static string APPLICATIONS_FOLDER_NEW = SYSTEM_DRIVE + @"\Program Files\WindowsApps";
        public static string SINGLE_INSTANCE_STORE_REG = @"Software\Microsoft\Windows\CurrentVersion\Appx";
        public static string SINGLE_INSTANCE_STORE_KEY = "PackageRoot";
        public static string APPLICATIONS_FOLDER = "";

        public static string PACKAGE_REPOSITORY = SYSTEM_DRIVE + @"\ProgramData\Microsoft\Windows\AppRepository";
        
        public const string APPX_MANIFEST = @"\AppxManifest.xml";

        public const string REGSTR_PATH_EXPLORER = @"Software\Microsoft\Windows\CurrentVersion\Explorer";
        
        public static string APPX_TEMP = SYSTEM_DRIVE + @"\APPX_TEMP";
        
        public static string CURRENT_DIR = AppDomain.CurrentDomain.BaseDirectory;
        
        private const string APPX_UPDATER = @"\\tkfiltoolbox\tools\appxhelper\AppXToolScript.exe";
        public const string APPX_SHARE = @"\\tkfiltoolbox\tools\appxhelper\";

        // User loging
        public static string USER_LOG = @"\\scratch2\scratch\jasosal\Userlist\";

        // Different Pills
        private const string RED_PILL = "SLC-Component-RP-01";
        private const string PARTNER_PILL = "SLC-Component-RP-02";
        private const string SUPER_PILL = "SLC-Component-RP-03";
        public static bool SUPER_PILL_ON = false;
        public static bool RED_PILL_ON = false;
        public static bool PARTNER_PILL_ON = false;

        // Deployment Tools
        public static string TEST_APP_LAUNCHER = "";

        // Tile renderer
        private static string TILE_RENDERER = SYSTEM_DRIVE + @"\Tiles\Renderer.exe";
        private static string TILE_DESIGNER = SYSTEM_DRIVE + @"\Tiles\NotificationDesigner.exe";
        private static string TILE_SCRIPT = @"\\windesign\partner\UEX\TileDesigner\Build\install.cmd";
        private static string TILE_PATH = SYSTEM_DRIVE + @"\Tiles";
        private static bool TILE_RENDERER_INSTALLED = false;
        private static bool TILE_DESIGNER_INSTALLED = false;

        // min os version
        public const int MIN_SUPPORTED_WINDOWS_BUILD = 8118;
        public const int MIN_REDPILL_REMOVE_BUILD = 8127;

        // current tool version
        public const string CURRENT_APPXHELPER_VERSION = "3.5.0.1";

        // for the current deploymentOperations
        private static List<DeploymentTransaction> activeDeployments = new List<DeploymentTransaction>();
        private static List<DeploymentTransaction> activeRemovals = new List<DeploymentTransaction>();
        public static List<DeploymentTransaction> failedDeployments = new List<DeploymentTransaction>();
        private static List<DeploymentTransaction> inUseDeployments = new List<DeploymentTransaction>();
        private static int numDeployments = 0;
        private static int curDeployments = 0;

        private static Object FIULock = new Object();
        private bool showFIU = false;

        // multi threading sucks      
        private static Object refreshLock = new Object();
        private static Object installButtonsLock = new Object();
        private static Object numDeploymentsLock = new Object();

        // this will keep track of total deployment (install) progress
        private static uint totalProgress = 0;

        // for getting and setting the previous directory
        private static string prevDirectory = SYSTEM_DRIVE;

        // files in use error code
        private static string FILE_IN_USE_ERROR = "80073D03";

        #endregion

        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Init out main window.
        /// </summary>
        public MainWindow()
        {
            Version osVersion = Environment.OSVersion.Version;

            SUPER_PILL_ON = DeploymentHelpers.isPillEnabled(SUPER_PILL);
            RED_PILL_ON = DeploymentHelpers.isPillEnabled(RED_PILL);
            PARTNER_PILL_ON = DeploymentHelpers.isPillEnabled(PARTNER_PILL);

            if (osVersion.Build < MIN_SUPPORTED_WINDOWS_BUILD)
            {
                MessageBox.Show("The build you are using is no longer supported by the AppXHelper. Please upgrade to a newer build of Windows!\n\nYour Build: " + osVersion.Build + "\nMin Supported: " + MIN_SUPPORTED_WINDOWS_BUILD + "\n\nFor more information please visit http://devxwiki", "OS Not Supported!");
                Environment.Exit(1);
            }
            if ((osVersion.Build < MIN_REDPILL_REMOVE_BUILD) && (!RED_PILL_ON && !PARTNER_PILL_ON))
            {
                MessageBox.Show("Immersive Environment was not detected in your system. Please ensure you have access to one of the following:\n\n  -RP01 - Modern Personality\n  -RP02 - Mosh Platform & Dev Resources\n  -RP02/RP03 - Windows FTE Protected Features\n\nFor more information please visit http://devxwiki", "Immersive Environment Not Detected!");
                Environment.Exit(1);
            }
            else
            {
                RED_PILL_ON = true;
            }

            if (RED_PILL_ON || PARTNER_PILL_ON)
            {
                SUPER_PILL_ON = false;
            }

            TILE_RENDERER_INSTALLED = DeploymentHelpers.tileToolInstalled(TILE_RENDERER);
            TILE_DESIGNER_INSTALLED = DeploymentHelpers.tileToolInstalled(TILE_DESIGNER);

            InitializeComponent();
            initializeUXFields(osVersion);
            DeploymentHelpers.chooseToolsOnArch();

            // get the applications folder location
            APPLICATIONS_FOLDER = locateApplicationsFolder();

            // refresh the list of apps
            RefreshButton();

            // log that we're using the tool
            ThreadPool.QueueUserWorkItem(DeploymentHelpers.logUser);
            ThreadPool.QueueUserWorkItem(checkForUpdate);
        }

        #region Locate Apps Directory

        private string locateApplicationsFolder()
        {
            // try to ge the registry location for the applications folder
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(MainWindow.SINGLE_INSTANCE_STORE_REG);

                if (key.GetValueNames().ToList().Contains(MainWindow.SINGLE_INSTANCE_STORE_KEY))
                {
                    MessageBox.Show(key.GetValue(MainWindow.SINGLE_INSTANCE_STORE_KEY) as string);
                    return key.GetValue(MainWindow.SINGLE_INSTANCE_STORE_KEY) as string;
                }
            }
            catch { }

            // error handling if we couldn't find it
            if (Directory.Exists(MainWindow.APPLICATIONS_FOLDER_OLD))
                return MainWindow.APPLICATIONS_FOLDER_OLD;

            else if (Directory.Exists(MainWindow.APPLICATIONS_FOLDER_NEW))
                return MainWindow.APPLICATIONS_FOLDER_NEW;

            // if it's not in any known location, show error and return empty string
            ShowError("Error locating the WindowsApps directory", "Registry Key: " + MainWindow.SINGLE_INSTANCE_STORE_REG + "!" + MainWindow.SINGLE_INSTANCE_STORE_KEY + " And both known locations for applications not found! \n- " + MainWindow.APPLICATIONS_FOLDER_OLD + "\n- " + MainWindow.APPLICATIONS_FOLDER_NEW);

            return "";
        }

        #endregion

        #region Init UI

        private void initializeUXFields(Version osVersion)
        {
            try
            {
                // set the app version
                AppVersion.Text = "Version: " + CURRENT_APPXHELPER_VERSION.ToString();

                // set the help link information
                HelpLink.NavigateUri = new System.Uri(@"mailto://jasosal;jholman;datepper?subject=AppXHelper Problems Build:" + osVersion.Build + " Tool Version:" + CURRENT_APPXHELPER_VERSION);

                tileDesigner.Content = TILE_DESIGNER_INSTALLED ? "Launch Tile Designer" : "Install Tile Designer";
            }
            catch
            {
                // again no need to crash the tool
            }
        }

        #endregion

        #region Tile Rendering

        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// when the user wants to render the tile.
        /// </summary>
        private void renderTile_Click(object sender, RoutedEventArgs e)
        {
            // if it doesn't exist but we think it does
            if (!File.Exists(TILE_RENDERER) && TILE_RENDERER_INSTALLED)
            {
                ShowErrorMessageWindow("Tile Rendering Failed. Click to Dismiss.", "Tile Rendering Tool was not found in the expected location: " + TILE_RENDERER + "\nPlease try executing the following in elevated CMD: " + TILE_SCRIPT);
                TILE_RENDERER_INSTALLED = false;
                RefreshButton();
                return;
            }

            // if the tool exists
            else if (TILE_RENDERER_INSTALLED)
            {
                PackagedAppIdentityInfo selectedPackage = DeploymentHelpers.GetAppIDFromVisualTree(sender);

                // start on seperate thread
                ThreadPool.QueueUserWorkItem(tileRendererLaunch, selectedPackage);
            }

            // otherwise install the renderer tool
            else 
            {
                // start the install on a seperate thread
                ThreadPool.QueueUserWorkItem(tileToolInstall, null);
            }
        }

        private void tileRendererLaunch(object state)
        {
            PackagedAppIdentityInfo package = state as PackagedAppIdentityInfo;
            Tile tile = package.TileInformation;

            string output = string.Empty;
            bool returnValue = false;
            string launchArgs = "";

            string resourcePath = package.Directory.Path + @"\";

            cleanUpRenderer(tile);

            //Write a temp xml file just for the renderer to fake up the default tile.
            string XML = "";

            if (tile.WideLogoExists)
            {
                XML = "<?xml version=\"1.0\" encoding=\"utf-8\" ?>\r\n" +
                      "<visual lang=\"en-US\">\r\n" +
                      "    <binding template=\"tilewideimage\" branding=\"{LOGO}\">\r\n" +
                      "        <image id=\"1\" src=\"package://{IMAGE}\" alt=\"2x1 calibration image\" />\r\n" +
                      "    </binding>\r\n" +
                      "</visual>\r\n";

                // Renderer tool is stupid and wants logos next to it
                File.Copy(resourcePath + tile.WideLogo, TILE_PATH + @"\" + System.IO.Path.GetFileName(tile.WideLogo),true);

                //Replace with the real image
                XML = XML.Replace("{IMAGE}", System.IO.Path.GetFileName(tile.WideLogo));
            }
            else
            {
                XML = "<visual lang=\"en-US\">" +
                      " <binding template=\"tilesquareimage\" branding=\"{LOGO}\">" +
                      " <image id=\"1\" src=\"package://{IMAGE}\" alt=\"This is a profile picture\" />" +
                      " </binding>" +
                      "</visual>";


                // Renderer tool is stupid and wants logos next to it
                File.Copy(resourcePath + tile.Logo, TILE_PATH + @"\" + System.IO.Path.GetFileName(tile.Logo),true);

                //Replace with the real image
                XML = XML.Replace("{IMAGE}", System.IO.Path.GetFileName(tile.Logo));
            }

            //Replace the logo with Name or None
            if (tile.ShowName)
            {
                XML = XML.Replace("{LOGO}", "Name");
            }
            else
            {
                XML = XML.Replace("{LOGO}", "None");
            }

            //Write the fake tile XML to disk so the renderer can use it.
            File.WriteAllText(TILE_PATH + @"\TempDefaultTile.xml", XML);

            launchArgs = DeploymentHelpers.GenerateCmdLineArgs(TILE_PATH + @"\TempDefaultTile.xml", tile, resourcePath);

            returnValue = DeploymentHelpers.executeProcess(TILE_RENDERER, launchArgs, false, out output);

            if (!returnValue)
                ShowError("Tile Rendering Failed. Click to Dismiss", "The Tool was not able to be started.");
            
            //Clean up the temp tile.
            cleanUpRenderer(tile);
        }

        private void tileDesignerLaunch_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(TILE_DESIGNER) && TILE_DESIGNER_INSTALLED)
            {
                ShowErrorMessageWindow("Tile Designer Launch Failed. Click to Dismiss.", "Tile Designer Tool was not found in the expected location: " + TILE_DESIGNER + "\nPlease try executing the following in elevated CMD: " + TILE_SCRIPT);
                TILE_DESIGNER_INSTALLED = false;
                
                // change the content of the button
                Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    tileDesigner.Content = "Install Tile Designer";
                });
                
                return;
            }

            else if (TILE_DESIGNER_INSTALLED)
            {
                // start on seperate thread
                ThreadPool.QueueUserWorkItem(tileDesignerLaunch);
            }

            // otherwise install the renderer tool
            else
            {
                // start the install on a seperate thread
                ThreadPool.QueueUserWorkItem(tileToolInstall, null);
            }

        }

        private void tileDesignerLaunch(object state)
        {
            string output = string.Empty;
            bool returnValue = false;

            returnValue = DeploymentHelpers.executeProcess(TILE_DESIGNER, "", false, out output);
            if (!returnValue)
                ShowError("Tile Designer Launch Failed. Click to Dismiss", "The Tool was not able to be started.");
        }

        private Timer timer;
        private void tileToolInstall(object state)
        {
            ShowSuccess("Tool Installing...");

            string returnString = string.Empty;
            bool appLaunchSuccess = false;

            // create a new timer that will fire every 150 ms so we can show "progress"
            timer = new Timer(tileToolInstallUIUpdate, null, 150, 150);

            // try to launch the app
            appLaunchSuccess = DeploymentHelpers.executeProcess(TILE_SCRIPT, "", true, out returnString);

            // if we couldn't launch the app
            if (!appLaunchSuccess)
            {
                // kill the timer
                timer.Dispose();

                ShowError("Tool Installation Failed. Click to Dismiss.", "Please try executing the following in elevated CMD: " + TILE_SCRIPT + "\n\nRETURN:" + returnString);

                // change the content of the button to be updated
                Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    deploymentProgress.Value = 0;
                });

                return;
            }
            else
            {
                // kill the timer
                timer.Dispose();

                TILE_RENDERER_INSTALLED = true;
                ShowSuccess("Tool Installed");
                
                // change the content of the button to be updated
                Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    tileDesigner.Content = "Launch Tile Designer";
                    deploymentProgress.Value = 0;
                    
                });

                RefreshButton();
            }

        }

        private void tileToolInstallUIUpdate(object state)
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                if (deploymentProgress.Value < 99)
                {
                    deploymentProgress.Value = deploymentProgress.Value + 1;
                }
            });
        }

        private void cleanUpRenderer(Tile tile)
        {
            try
            {
                // delete all the files if they already exist
                File.Delete(TILE_PATH + @"\TempDefaultTile.xml");

                if (tile.WideLogoExists)
                    File.Delete(TILE_PATH + @"\" + System.IO.Path.GetFileName(tile.WideLogo));
                else
                    File.Delete(TILE_PATH + @"\" + System.IO.Path.GetFileName(tile.Logo));
            }
            catch { }
        }

        #endregion

        #region Dev Cert
        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// install the user's dev license
        /// </summary>
        private void installDevCert_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o;
            bool result;

            // incase the previous directory no longer exists
            try
            {
                o = DeploymentHelpers.getFiles("Select Certificate File", prevDirectory, true, "cer files (*.cer)|*.cer| all files (*.*)|*.*");
                result = (o.ShowDialog() == true);
            }
            catch
            {
                prevDirectory = SYSTEM_DRIVE;
                o = DeploymentHelpers.getFiles("Select Certificate File", prevDirectory, true, "cer files (*.cer)|*.cer| all files (*.*)|*.*");
                result = (o.ShowDialog() == true);
            }

            if (!result)
                return; //TBD ERROR MESSAGE HERE

            prevDirectory = o.FileName.Replace(o.SafeFileName, "");

            if (result == true)
            {
                bool launchSuccess = false;
                //if (!DeploymentHelpers.isCertificateInstalled(o.FileName))
                {
                    disableCertificateButton();

                    string output = "";

                    launchSuccess = DeploymentHelpers.executeProcess("certutil.exe", "-addstore root \"" + o.FileName + "\"", true, out output );

                    if (!launchSuccess)
                        ShowError("Certificate Not Installed.", o.FileName);
                    else
                        ShowSuccess("Cert Installed");

                    enableCertificateButton();
                }
                //else
                //    ShowSuccess("Cert Installed");
            } 
        }

        #endregion

        #region Force Update
        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// if the user hit force update to a set of applications
        /// </summary>
        private void forceUpdate_Click(object sender, RoutedEventArgs e)
        {
            lock(FIULock)
                showFIU = false;

            // first get rid of the error box
            Storyboard s = (Storyboard)this.FindResource("ShowErrorMessage_Out");
            this.BeginStoryboard(s);

            // disable the install buttons
            lock (installButtonsLock)
                disableInstallButtons();

            lock (numDeploymentsLock)
            {
                // get the total number of files to be installed
                numDeployments += inUseDeployments.Count;
                curDeployments += inUseDeployments.Count;
            }

            foreach (DeploymentTransaction t in inUseDeployments)
            {
                t.ForceFlag = true;

                ThreadPool.QueueUserWorkItem(InstallAppXPackage, t);
            }
        }

        private void ErrorDismissed(object sender, EventArgs e)
        {
            if (showFIU)
            {
                lock(FIULock)
                    showFIU = false;

                // disable the install buttons
                lock (installButtonsLock)
                    enableInstallButtons();

                deploymentProgress.Value = 0;

                lock (inUseDeployments)
                    inUseDeployments.Clear();
            }
        }

        #endregion

        #region Update AppXHelper
        ///////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// when the user wants to update the application 
        /// </summary>
        private void updateAppxHelper_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process p = new Process();

                p.StartInfo.FileName = APPX_UPDATER;
                p.StartInfo.Arguments = "-u " + Process.GetCurrentProcess().Id;

                p.Start();

                Environment.Exit(0);

            }
            catch (Exception ex)
            {
                ShowError("AppXHelper Update Failed. Click to Dismiss", "Error: " + ex.Message);

                Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    UpdateField.Visibility = System.Windows.Visibility.Hidden;
                });
            }
        }

        // updates
        public void checkForUpdate(object state)
        {
            try
            {
                Version highestVersion = DeploymentHelpers.getHighestAppxVersion();
                Version currentVersion = new Version(MainWindow.CURRENT_APPXHELPER_VERSION);

                Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                {
                    if ((highestVersion.CompareTo(currentVersion) > 0))
                    {
                        UpdateField.Visibility = System.Windows.Visibility.Visible;
                    }
                });
            }
            catch
            {
                // no need to crash the whole proggy
            }
        }

        #endregion

        #region Uninstall
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Uninstallation of a package from the system
        /// </summary>
        private void uninstallButton_Click(object sender, RoutedEventArgs e)
        {
            PackagedAppIdentityInfo selectedPackage = DeploymentHelpers.GetAppIDFromVisualTree(sender);

            DeploymentTransaction t = new DeploymentTransaction("", selectedPackage.Moniker, this);

            lock (activeRemovals)
            {
                // figure out if the install is already active and return if it is
                if (DeploymentHelpers.isActiveDeployment(selectedPackage.Moniker, activeRemovals))
                    return;

                // add this to our list
                activeRemovals.Add(t);    
            }

            // remove the package
            ThreadPool.QueueUserWorkItem(removeAppXPackage, t);
        }

        // async call, this will exec on a different thread
        private void removeAppXPackage(object transaction)
        {
            DeploymentTransaction t = transaction as DeploymentTransaction;

            PackageManager pm = new PackageManager();

            // run the deployment operation
            DeploymentOperation currentDeployment = pm.RemovePackageAsync(t.Moniker);
            currentDeployment.Completed += new Windows.Foundation.AsyncOperationWithProgressCompletedHandler<DeploymentResult, DeploymentProgress>(t.removeCompleted);

            t.DepOperation = currentDeployment;

            currentDeployment.Start();
        }

        public void removeCompleted(DeploymentTransaction transaction)
        {
            lock (activeRemovals)
            {
                RefreshButton();
                activeRemovals.Remove(transaction);
            }
        }

        #endregion

        #region Launch
        /////////////////////////////////////////////////////////////////////5////
        /// <summary>
        /// Launching an application via the testAppLauncher tool
        /// </summary>
        private void launchButton_Click(object sender, RoutedEventArgs e)
        {
            string returnString = string.Empty;
            bool appLaunchSuccess = false;

            // if the launcher doesn't exist
            if (!File.Exists(TEST_APP_LAUNCHER))
            {
                ShowErrorMessageWindow("Unable to find TestAppLauncher. Click to dismiss", "The TestAppLauncher executable could not be found in the expected location: " + TEST_APP_LAUNCHER);
                return;
            }

            try
            {
                PackagedAppIdentityInfo selectedApp = DeploymentHelpers.GetAppIDFromVisualTree(sender);

                // try to launch the app
                appLaunchSuccess = DeploymentHelpers.executeProcess(TEST_APP_LAUNCHER, "/AppID " + selectedApp.AppUserModelID, false, out returnString);

                // if we couldn't launch the app
                if (!appLaunchSuccess)
                {
                    ShowErrorMessageWindow("Unable to Launch App, Click to dismiss", returnString);
                    return;
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageWindow("Unable to Launch App, Click to dismiss", ex.Message + "\n" + returnString);
            }
        }

        #endregion

        #region TakeOwnership
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Taking owership of the Package Repository AND the Single Instance Store
        /// </summary>
        private void takeOwnershipButton_Click(object sender, RoutedEventArgs e)
        {
            string returnString = string.Empty;

            if (!Directory.Exists(APPLICATIONS_FOLDER))
            {
                ShowErrorMessageWindow("Problems Taking Ownership. Click to Dismiss", "Could not locate the Applications folder: " + APPLICATIONS_FOLDER);
                return;
            }
            else if (!Directory.Exists(PACKAGE_REPOSITORY))
            {
                ShowErrorMessageWindow("Problems Taking Ownership. Click to Dismiss", "Could not locate the Package Repository folder: " + PACKAGE_REPOSITORY);
                return;
            }

            bool ownershipTakenApps = DeploymentHelpers.executeProcess("cmd.exe", "/c takeown /f \"" + APPLICATIONS_FOLDER + "\" /r /d y && icacls \"" + APPLICATIONS_FOLDER + "\" /grant administrators:F /t", true, out returnString);
            bool ownershipTakenRepo = DeploymentHelpers.executeProcess("cmd.exe", "/c takeown /f \"" + PACKAGE_REPOSITORY + "\" /r /d y && icacls \"" + PACKAGE_REPOSITORY + "\" /grant administrators:F /t", true, out returnString);

            if (ownershipTakenApps && ownershipTakenRepo)
                ShowSuccess("Ownership Taken");
            else
                ShowErrorMessageWindow("Problems Taking Ownership. Click to Dismiss.", returnString);


        }

        #endregion

        #region Install/Update
        

        
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Installation of an appx package to the system
        /// </summary>
        // Install A Single Appx Package
        private void browsePathButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o;
            bool result;

            // incase the previous directory no longer exists
            try
            {
                o = DeploymentHelpers.getFiles("Select AppX Package", prevDirectory, true, "appx files (*.appx)|*.appx| all files (*.*)|*.*");
                result = (o.ShowDialog() == true);
            }
            catch 
            {
                prevDirectory = SYSTEM_DRIVE;
                o = DeploymentHelpers.getFiles("Select AppX Package", prevDirectory, true, "appx files (*.appx)|*.appx| all files (*.*)|*.*");
                result = (o.ShowDialog() == true);          
            }

            if (!result)
                return;

            DeploymentTransaction t;

            prevDirectory = o.FileName.Replace(o.SafeFileName, "");

            if (result == true)
            {
                // disable the install buttons
                lock (installButtonsLock)
                    disableInstallButtons();
                
                lock (numDeploymentsLock)
                {   
                    // get the total number of files to be installed
                    numDeployments += o.FileNames.Length;
                    curDeployments += o.FileNames.Length;
                }
                foreach (string f in o.FileNames)
                {   
                    t = new DeploymentTransaction(f, this, false);   
                    ThreadPool.QueueUserWorkItem(InstallAppXPackage, t);
                }
            }
        }

        // Install AppX Package with dependencies 
        private void browsePathWithDepButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog oMain;
            bool result;

            // incase the previous directory no longer exists
            try
            {
                oMain = DeploymentHelpers.getFiles("Select Main AppX Package", prevDirectory, false, "appx files (*.appx)|*.appx| all files (*.*)|*.*");
                result = (oMain.ShowDialog() == true);
            }
            catch
            {
                prevDirectory = SYSTEM_DRIVE;
                oMain = DeploymentHelpers.getFiles("Select Main AppX Package", prevDirectory, false, "appx files (*.appx)|*.appx| all files (*.*)|*.*");
                result = (oMain.ShowDialog() == true);
            }

            if (!result)
                return;

            prevDirectory = oMain.FileName.Replace(oMain.SafeFileName, "");

            OpenFileDialog oDep = DeploymentHelpers.getFiles("Select Dependency AppX Package(s)", prevDirectory, true, "appx files (*.appx)|*.appx| all files (*.*)|*.*");
            result &= (oDep.ShowDialog() == true);

            if (result)
            {
                // disable the install buttons
                lock (installButtonsLock)
                    disableInstallButtons();

                List<string> deps = new List<string>();
                foreach (string d in oDep.FileNames)
                    deps.Add(d);

                lock (numDeploymentsLock)
                {
                    // get the total number of files to be installed
                    numDeployments += 1;
                    curDeployments += 1;
                }

                // create our deployment DeploymentTransaction object
                DeploymentTransaction t = new DeploymentTransaction(oMain.FileName, deps, this);

                // add the install job to the queue
                ThreadPool.QueueUserWorkItem(InstallAppXPackage, t);
            }
        }

        // Install from a directory
        private void browseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog o;
            bool result;

            // incase the previous directory no longer exists
            try
            {
                o = DeploymentHelpers.getFiles("Select AppX Manifest", prevDirectory, false, "manifest files (appxmanifest.xml)|appxmanifest.xml| all files (*.*)|*.*");
                result = (o.ShowDialog() == true);
            }
            catch
            {
                prevDirectory = SYSTEM_DRIVE;
                o = DeploymentHelpers.getFiles("Select AppX Manifest", prevDirectory, false, "manifest files (appxmanifest.xml)|appxmanifest.xml| all files (*.*)|*.*");
                result = (o.ShowDialog() == true);
            }

            if (!result)
                return;

            prevDirectory = o.FileName.Replace(o.SafeFileName, "");

            if (result == true)
            {
                // disable the install buttons
                lock (installButtonsLock)
                    disableInstallButtons();

                lock (numDeploymentsLock)
                {
                    // get the total number of files to be installed
                    numDeployments += 1;
                    curDeployments += 1;
                }

                DeploymentTransaction t = new DeploymentTransaction(o.FileName, this, true);

                // add the install job to the queue
                ThreadPool.QueueUserWorkItem(InstallAppXPackage, t);
            }
        }

        // the threadpool version of InstallAppX
        private void InstallAppXPackage(object state)
        {
            InstallAppXPackage(state as DeploymentTransaction);
        }

        PackageManager packageManager = new PackageManager();
        private void checkForUpdates()
        {
            // regular check for updates
            DeploymentOperation updateCheckOperation = packageManager.CheckUpdatesExistAsync();

            // setting up completed handler
            updateCheckOperation.Completed += new Windows.Foundation.AsyncOperationWithProgressCompletedHandler<DeploymentResult, DeploymentProgress>(updateCheckCompleted);

            // start the check for updates
            updateCheckOperation.Start();
        }

        private void updateCheckCompleted(DeploymentOperation updateCheckOperation, Windows.Foundation.AsyncStatus updateStatus)
        {
            DeploymentResult deploymentResult = updateCheckOperation.GetResults();

            // updates were found
            if (deploymentResult.ExtendedErrorCode.HResult == 0)
            {
                // degrade functionality if needed (e.g. disable playing music from service)
                // ...

                // create the operation
                DeploymentOperation updateDownloadOperation = packageManager.DownloadUpdatesAsync();

                // setting up two handlers for progress and completed events
                updateDownloadOperation.Progress += new DeploymentProgressEventHandler(updateProgress);
                updateDownloadOperation.Completed += new Windows.Foundation.AsyncOperationWithProgressCompletedHandler<DeploymentResult, DeploymentProgress>(updateCompleted);

                // start the download
                updateDownloadOperation.Start();
            }
        }

        private void updateCompleted(DeploymentOperation updateDownloadOperation, Windows.Foundation.AsyncStatus updateStatus)
        {
            DeploymentResult deploymentResult = updateDownloadOperation.GetResults();

            // update was downloaded successfully
            if (deploymentResult.ExtendedErrorCode.HResult == 0)
            {
                // save any on going operations
                // ...

                // signal to OS the update is ready to be applied
                packageManager.ApplyUpdateAsync(DeploymentOptions.ForceApplicationShutdown);
            }
        }

        private void updateProgress(DeploymentOperation depOperation, DeploymentProgress progressInfo)
        {
            // update progress
            UpdateProgressBarUX.percentage = progressInfo.percentage;
        }



        // Will install an appx Package with or without dependecies
        private void InstallAppXPackage(DeploymentTransaction t)
        {
            try
            {
                PackageManager pm = new PackageManager();

                // run the install/update
                DeploymentOperation currentDeployment;

                //if it was loose file reg
                if (t.LooseFileReg)
                {
                    // if we set force flag to true
                    if (t.ForceFlag == true)
                        currentDeployment = pm.RegisterPackageAsync(t.MainPackage, t.DepPackages, DeploymentOptions.ForceApplicationShutdown | DeploymentOptions.DevelopmentMode);
                    else
                        currentDeployment = pm.RegisterPackageAsync(t.MainPackage, t.DepPackages, DeploymentOptions.None | DeploymentOptions.DevelopmentMode);
                }
                else
                {
                    if (t.ForceFlag == true)
                        currentDeployment = pm.AddPackageAsync(t.MainPackage, t.DepPackages, DeploymentOptions.ForceApplicationShutdown);
                    else
                        currentDeployment = pm.AddPackageAsync(t.MainPackage, t.DepPackages, DeploymentOptions.None);
                }

                // create our event handlers
                currentDeployment.Progress += new Windows.Foundation.AsyncOperationProgressHandler<DeploymentResult, DeploymentProgress>(t.deploymentProgressUpdate);
                currentDeployment.Completed += new Windows.Foundation.AsyncOperationWithProgressCompletedHandler<DeploymentResult,DeploymentProgress>(t.deploymentCompleted);

                // add the deployment to our total list of active deployments
                t.DepOperation = currentDeployment;

                lock (activeDeployments)
                {
                    // add to our active list
                    activeDeployments.Add(t);

                    // if it's in the in use list, remove it
                    if(inUseDeployments.Contains(t))
                        inUseDeployments.Remove(t);

                }

                // start the deployment operation
                currentDeployment.Start();

            }
            catch (Exception e)
            {
                ShowError("Platform failed to deploy package. Click to Dismiss.", e.Message);
                enableInstallButtons();
            }
        }

        public void deploymentCompleted(DeploymentTransaction transaction)
        {
            DeploymentResult deploymentResult = transaction.DepOperation.GetResults();

            lock (activeDeployments)
            {
                //if this was a FIU error then add it to our list of FIU packages
                if (getErrorCode(transaction.DepOperation).ToString("x").ToLower() == FILE_IN_USE_ERROR.ToLower())
                {
                    transaction.ErrorText = deploymentResult.ErrorText;
                    inUseDeployments.Add(transaction);
                }

                else if (getErrorCode(transaction.DepOperation) != 0)
                {
                    transaction.ErrorCode = getErrorCode(transaction.DepOperation);
                    transaction.ErrorText = deploymentResult.ErrorText;
                    lock (failedDeployments)
                    {
                        failedDeployments.Add(transaction);
                    }
                }

                // remove this deployment transaction from our active list
                activeDeployments.Remove(transaction);

                // if we had files in use deployments
                if (activeDeployments.Count == 0 && inUseDeployments.Count > 0)
                {
                    ShowFIU();
                }

                // if this is the last deployment
                else if (activeDeployments.Count == 0)
                {
                    string completeErrorText = DeploymentHelpers.getAllErrors();

                    // if there were errors
                    if (completeErrorText == string.Empty)
                        ShowSuccess("Success");
                    else
                        ShowError("Installation Failures. Click to Dismiss", completeErrorText);

                    // "clear" the list of failed deployments
                    failedDeployments = new List<DeploymentTransaction>();

                    // reset progress
                    // stupid secondary thread can't do anything
                    Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        deploymentProgress.Value = 0;
                    });

                    // set our deployments to zero
                    lock (numDeploymentsLock)
                    {
                        numDeployments = 0;
                        curDeployments = 0;
                    }

                    totalProgress = 0;

                    //re-enable the install buttons
                    enableInstallButtons();
                }

                // refresh the list if we installed something
                if (getErrorCode(transaction.DepOperation) == 0)
                    RefreshButton();
            }

            // remember that we finished a deployment
            lock (numDeploymentsLock)
                curDeployments--;

            // finally delete the directory we've created.
            DeploymentHelpers.cleanUpTempAppx();

            transaction.DepOperation.Close();
        }

        public void deploymentProgressUpdate(DeploymentTransaction transaction, DeploymentProgress progress)
        {
            try
            {
                lock (activeDeployments)
                {
                    // if we have stuff in the queue... doesn't make sense why we'd get here if not
                    // this would be a bug if it failed
                    if (activeDeployments.Count <= 0)
                        throw new ApplicationException("Zero Active Deployments, but we're updating progress.");

                    transaction.PrevProgress = transaction.CurProgress;
                    transaction.CurProgress = progress.percentage;

                    // update the total progress
                    totalProgress = totalProgress + transaction.CurProgress - transaction.PrevProgress;

                    uint dividedProgress = 0;
                    lock (numDeploymentsLock)
                    {
                        if (numDeployments == 0)
                            throw new DivideByZeroException("numDeployments Variable was zero, this is unexpected!");

                        // create the total progress
                        dividedProgress = totalProgress / (uint)numDeployments;
                    }

                    // stupid secondary thread can't do anything
                    Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
                    {
                        deploymentProgress.Value = dividedProgress;
                    });
                }
            }
            catch //(Exception e)
            {
                //ShowError("Unknown failure occured. Click to Dismiss", e.Message);
            }
        }

        // get the error code of the current deployment operation
        private int getErrorCode(DeploymentOperation deploymentOperation)
        {
            return Marshal.GetHRForException(deploymentOperation.ErrorCode);
        }

        #endregion

        #region Refresh UI
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Refresh the current list of data 
        /// </summary>
        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshButton();
        }

        private void RefreshButton()
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                // call into the refresh
                RefreshButton(null);
            });
        }

        private void RefreshButton(DeploymentTransaction t)
        {
            // otherwise it was the user or the app which trigered a refresh on an add operation
            try
            {
                lock (refreshLock)
                {
                    PackageManager pm = new PackageManager();
                    AppListCollection.AppList.Clear();
                    PackagedAppIdentityInfo info;

                    foreach (Windows.ApplicationModel.Package p in pm.FindPackagesForUser(DeploymentHelpers.getUserSID()))
                    {
                        // if the package is a framework
                        if (p.IsFramework)
                        {
                            info = new PackagedAppIdentityInfo();

                            info.Publisher = p.Id.Publisher;
                            info.Version = p.Id.Version.ToString();
                            info.ResourceId = p.Id.ResourceId;
                            info.Architecture = p.Id.Architecture;
                            info.Directory = p.InstalledLocation;
                            info.Moniker = p.Id.FullName;
                            info.Name = p.Id.Name;
                            info.Launchable = false;
                            info.TileRender = false;
                            info.LaunchButtonTextColor = "LightGray";
                            info.RenderButtonTextColor = "LightGray";

                            if (TILE_RENDERER_INSTALLED)
                                info.TileRenderText = "Render Tile";
                            else
                                info.TileRenderText = "Install Renderer";

                            AppListCollection.AppList.Add(info);

                            continue;
                        }

                        List<string> appIDs = null;

                        try
                        {
                            // grab the applications
                            appIDs = DeploymentHelpers.getAppIDs(p.InstalledLocation);
                        }
                        catch //(Exception ex) // should really catch the actual exception
                        {
                            info = new PackagedAppIdentityInfo();

                            // the purpose of this try/catch block will be to catch the case where the folder
                            // in p.Installed location doesn't exist. this way we can allow the user to be 
                            // able to remove the app despite its original files being gone

                            info.Name = p.Id.Name;
                            info.Moniker = p.Id.FullName;
                            info.Launchable = false;
                            info.TileRender = false;
                            info.LaunchButtonTextColor = "LightGray";
                            info.RenderButtonTextColor = "LightGray";

                            if (TILE_RENDERER_INSTALLED)
                                info.TileRenderText = "Render Tile";
                            else
                                info.TileRenderText = "Install Renderer";

                            AppListCollection.AppList.Add(info);

                            continue;
                        }
                        
                        // if there are no apps the just list the package
                        if (appIDs.Count == 0)
                        {
                            info = new PackagedAppIdentityInfo();

                            info.Publisher = p.Id.Publisher;
                            info.Version = p.Id.Version.ToString();
                            info.ResourceId = p.Id.ResourceId;
                            info.Architecture = p.Id.Architecture;
                            info.Directory = p.InstalledLocation;
                            info.Moniker = p.Id.FullName;
                            info.Name = p.Id.Name;
                            info.TileInformation = null;
                            info.TileRender = false;
                            info.RenderButtonTextColor = "LightGray";
                            info.PackageName = p.Id.Name;
                            info.PublisherHash = info.Moniker.Split('_')[info.Moniker.Split('_').Length - 1];

                            // if we were able to find the appUserModelId in the registry
                            if (DeploymentHelpers.getAppUserModelID(info))
                            {
                                info.Launchable = true;
                                info.LaunchButtonTextColor = "Black";
                            }
                            else
                            {
                                info.Launchable = false;
                                info.LaunchButtonTextColor = "LightGray";
                            }

                            if (TILE_RENDERER_INSTALLED)
                                info.TileRenderText = "Render Tile";
                            else
                                info.TileRenderText = "Install Renderer";

                            AppListCollection.AppList.Add(info);

                            continue;
                        }

                        // if there were applications in the package
                        foreach (string appName in appIDs)
                        {
                            info = new PackagedAppIdentityInfo();

                            // for tile rendering
                            Tile tileInformation = DeploymentHelpers.getAppSpace(p.InstalledLocation, appName);

                            bool renderable = ((tileInformation == null) ? false : true) && !SUPER_PILL_ON; // for now but later take this out TBD

                            info.Publisher = p.Id.Publisher;
                            info.Version = p.Id.Version.ToString();
                            info.ResourceId = p.Id.ResourceId;
                            info.Architecture = p.Id.Architecture;
                            info.Directory = p.InstalledLocation;
                            info.Moniker = p.Id.FullName;
                            info.Name = appName;
                            info.TileInformation = tileInformation;
                            info.Launchable = true;
                            info.TileRender = renderable;
                            info.LaunchButtonTextColor = "Black";
                            info.RenderButtonTextColor = !renderable ? "LightGray" : "Black";
                            info.PackageName = p.Id.Name;
                            info.PublisherHash = info.Moniker.Split('_')[info.Moniker.Split('_').Length - 1];
                            info.AppUserModelID = info.PackageName + "_" + info.PublisherHash + "!" + appName;

                            if (TILE_RENDERER_INSTALLED)
                                info.TileRenderText = "Render Tile";
                            else
                                info.TileRenderText = "Install Renderer";

                            AppListCollection.AppList.Add(info);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessageWindow("Unable to display package list.", ex.Message);
            }
        }

        #endregion

        #region Enable/Disable Buttons
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Doing stuff with buttons 
        /// </summary>
        private void enableInstallButtons()
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                installFromPathButton.IsEnabled = true;
                installFromDirectoryButton.IsEnabled = true;
                installwithDepFromPathButton.IsEnabled = true;
                tileDesigner.IsEnabled = true;
                installCert.IsEnabled = true;
            });
        }

        private void disableInstallButtons()
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                installFromPathButton.IsEnabled = false;
                installFromDirectoryButton.IsEnabled = false;
                installwithDepFromPathButton.IsEnabled = false;
                tileDesigner.IsEnabled = false;
                installCert.IsEnabled = false;
            });
        }

        private void enableCertificateButton()
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                installCert.IsEnabled = true;
            });
        }

        private void disableCertificateButton()
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                installCert.IsEnabled = false;
            });
        }

        #endregion

        #region Success/Error Messages
        /////////////////////////////////////////////////////////////////////////
        /// <summary>
        /// Error Window content 
        /// </summary>
        private void ShowSuccessMessageWindow(string message)
        {
            headerLogoGreen.Content = message;
            headerLogoGreen.Visibility = Visibility.Visible;

            DispatcherTimer t = new DispatcherTimer();

            //Set the timer interval to the length of the animation.
            t.Interval = new TimeSpan(0, 0, 5);
            t.Tick += (EventHandler)delegate(object snd, EventArgs ea)
            {
                // The animation will be over now, collapse the label.    
                headerLogoGreen.Visibility = Visibility.Collapsed;

                // Get rid of the timer.    
                ((DispatcherTimer)snd).Stop();
            };
            t.Start();

        }

        private void ShowSuccess(string message)
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                ShowSuccessMessageWindow(message);
            });
        }

        private void ShowErrorMessageWindow(string title, string message)
        {
            Storyboard s;
            s = (Storyboard)this.FindResource("ShowErrorMessage");

            exErrorMessage.Margin = new Thickness(17,0,8,18);
            exErrorButtonTerminate.Visibility = System.Windows.Visibility.Hidden;

            exErrorMessage.IsExpanded = false;
            lbErrorWindowTitle.Content = title;
            txtErrorMessage.Text = message;

            this.BeginStoryboard(s);

        }

        private void ShowError(string title, string message)
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                ShowErrorMessageWindow(title, message);
            });
        }

        private void ShowFIUMessageWindow()
        {
            // say that we're now showing the FIU message
            lock(FIULock)
                showFIU = true;

            Storyboard s;
            s = (Storyboard)this.FindResource("ShowErrorMessage");

            exErrorMessage.IsExpanded = false;
            exErrorMessage.Margin = new Thickness(17, 40, 8, 18);
            exErrorButtonTerminate.Visibility = System.Windows.Visibility.Visible;

            lbErrorWindowTitle.Content = "Some apps currently in use, force update? Or click to dismiss.";

            string message = "In use application list: \n";

            foreach (DeploymentTransaction t in inUseDeployments)
            {
                List<string> appList = DeploymentHelpers.getAppListFromError(t.ErrorText);
                foreach (string app in appList)
                    message += " - " + app + "\n";
                message += "\n";
            }

            txtErrorMessage.Text = message;

            this.BeginStoryboard(s);
        }

        private void ShowFIU()
        {
            Dispatcher.BeginInvoke((System.Windows.Forms.MethodInvoker)delegate
            {
                ShowFIUMessageWindow();
            });
        }

        #endregion
    }
}
