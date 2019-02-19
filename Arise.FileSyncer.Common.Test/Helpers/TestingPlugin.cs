using System.Collections.Generic;
using Arise.FileSyncer.Core.FileSync;
using Arise.FileSyncer.Core.Plugins;

namespace Arise.FileSyncer.Common.Test.Helpers
{
    public class TestingPlugin : Plugin
    {
        public override string Name => "TestPlugin";

        public override string DisplayName => "Test Plugin";

        public override PluginFeatures Features => PluginFeatures.ModifySendData;

        protected override MSD_OUT ModifySendData(MSD_IN data)
        {
            DirectoryTreeDifference delta = new DirectoryTreeDifference(data.LocalState, data.RemoteState, data.Connection.SupportTimestamp);

            List<string> files = new List<string>();
            List<string> redirects = new List<string>();

            foreach (string item in delta.RemoteMissingFiles)
            {
                files.Add(item + ".dat");
                redirects.Add(data.Profile.RootDirectory + item);
            }

            return new MSD_OUT()
            {
                Directories = delta.RemoteMissingDirectories.ToArray(),
                Files = files,
                Redirects = redirects,
            };
        }
    }
}
