using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace AppXHelperUI
{
    public class AppListCollection
    {
        private static ObservableCollection<PackagedAppIdentityInfo> appList = new ObservableCollection<PackagedAppIdentityInfo>();

        public static ObservableCollection<PackagedAppIdentityInfo> AppList
        {
            get { return appList; }
        }
    }
}
