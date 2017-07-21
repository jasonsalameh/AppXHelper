using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Windows.Management.Deployment;
using Windows.Storage;
using Windows.System;

namespace AppXHelperUI
{
    public class PackagedAppIdentityInfo
    {
        public string Name { get; set; }
        public ProcessorArchitecture Architecture { get; set; }
        public string Version { get; set; }
        public string Moniker { get; set; }
        public string Publisher { get; set; }
        public string ResourceId { get; set; }
        public StorageFolder Directory { get; set; }
        public bool Launchable { get; set; }
        public string LaunchButtonTextColor { get; set; }
        public bool TileRender { get; set; }
        public string TileRenderText { get; set; }
        public string TilePath { get; set; }
        public string RenderButtonTextColor { get; set; }
        public string PackageName { get; set; }
        public string PublisherHash { get; set; }
        public Tile TileInformation { get; set; }
        public string AppUserModelID { get; set; }
    }
}
