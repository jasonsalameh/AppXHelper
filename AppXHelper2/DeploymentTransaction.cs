using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Management.Deployment;
using Windows.Foundation;

using DeploymentOperation = Windows.Foundation.IAsyncOperationWithProgress<Windows.Management.Deployment.DeploymentResult, Windows.Management.Deployment.DeploymentProgress>;
using DeploymentProgressEventHandler = Windows.Foundation.AsyncOperationProgressHandler<Windows.Management.Deployment.DeploymentResult, Windows.Management.Deployment.DeploymentProgress>;

namespace AppXHelperUI
{
    public class DeploymentTransaction
    {
        private string _mainPackage;
        private Uri _mainPackageUri;
        private string _moniker;
        private List<string> _depPackages;
        private List<Uri> _depPackagesUri;
        private DeploymentOperation _depOperation;
        private uint _curProgress;
        private uint _prevProgress;
        private int _errorCode;
        private string _errorText;
        private MainWindow _mainWindow;
        private bool _forceFlag;
        private bool _looseFileRegInstall;

        public DeploymentTransaction(string mainPackage, List<string> depPackages, MainWindow mainWindow)
        {
            _mainPackage = mainPackage;
            _moniker = string.Empty;
            _depPackages = depPackages;
            _depOperation = null;
            _curProgress = 0;
            _prevProgress = 0;
            _errorText = string.Empty;
            _errorCode = 0;
            _mainWindow = mainWindow;
            _forceFlag = false;
            _looseFileRegInstall = false;

            convertToUri();
        }

        public DeploymentTransaction(string mainPackage, MainWindow mainWindow, bool lostFileRegInstall)
        {
            _mainPackage = mainPackage;
            _moniker = string.Empty;
            _depPackages = null;
            _depOperation = null;
            _curProgress = 0;
            _prevProgress = 0;
            _errorText = string.Empty;
            _errorCode = 0;
            _mainWindow = mainWindow;
            _forceFlag = false;
            _looseFileRegInstall = lostFileRegInstall;

            convertToUri();
        }

        public DeploymentTransaction(string mainPackage, string moniker, MainWindow mainWindow)
        {
            _mainPackage = mainPackage;
            _moniker = moniker;
            _depPackages = null;
            _depOperation = null;
            _curProgress = 0;
            _prevProgress = 0;
            _errorText = string.Empty;
            _errorCode = 0;
            _mainWindow = mainWindow;
            _forceFlag = false;
            _looseFileRegInstall = false;

            convertToUri();
        }

        public DeploymentTransaction(DeploymentOperation depOperation, MainWindow mainWindow)
        {
            _mainPackage = null;
            _moniker = string.Empty;
            _depPackages = null;
            _depOperation = depOperation;
            _curProgress = 0;
            _prevProgress = 0;
            _errorText = string.Empty;
            _errorCode = 0;
            _mainWindow = mainWindow;
            _forceFlag = false;
            _looseFileRegInstall = false;

            convertToUri();
        }

        public DeploymentTransaction(string mainPackage, int errorCode, string errorText, MainWindow mainWindow)
        {
            _mainPackage = mainPackage;
            _moniker = string.Empty;
            _depPackages = null;
            _depOperation = null;
            _curProgress = 0;
            _prevProgress = 0;
            _errorText = errorText;
            _errorCode = errorCode;
            _mainWindow = mainWindow;
            _forceFlag = false;
            _looseFileRegInstall = false;

            convertToUri();
        }

        private void convertToUri()
        {
            if (_mainPackage != null && _mainPackage != string.Empty)
                _mainPackageUri = new Uri(_mainPackage);
            else
                _mainPackageUri = null;

            if (_depPackages != null && _depPackages.Count > 0)
            {
                _depPackagesUri = new List<Uri>();
                foreach (string dep in _depPackages)
                    _depPackagesUri.Add(new Uri(dep));
            }
            else
                _depPackagesUri = null;
        }

        public Uri MainPackage { get { return _mainPackageUri; } }
        public string Moniker { get { return _moniker; } set { _moniker = value; } }
        public List<Uri> DepPackages { get { return _depPackagesUri; } }
        public DeploymentOperation DepOperation { get { return _depOperation; } set { _depOperation = value; } }
        public uint CurProgress { get { return _curProgress; } set { _curProgress = value; } }
        public uint PrevProgress { get { return _prevProgress; } set { _prevProgress = value; } }
        public int ErrorCode { get { return _errorCode; } set { _errorCode = value; } }
        public string ErrorText { get { return _errorText; } set { _errorText = value; } }
        public MainWindow TheWindow { get { return _mainWindow; } set { _mainWindow = value; } }
        public bool ForceFlag { get { return _forceFlag; } set { _forceFlag = value; } }
        public bool LooseFileReg { get { return _looseFileRegInstall; } set { _looseFileRegInstall = value; } }

        public void deploymentCompleted(DeploymentOperation depOperation)
        {
            _depOperation = depOperation;
            _mainWindow.deploymentCompleted(this);
        }

        public void deploymentProgressUpdate(DeploymentOperation depOperation, DeploymentProgress progressInfo)
        {
            _depOperation = depOperation;
            _mainWindow.deploymentProgressUpdate(this, progressInfo);
        }

        internal void removeCompleted(DeploymentOperation depOperation)
        {
            _depOperation = depOperation;
            _mainWindow.removeCompleted(this);
        }
    }
}
