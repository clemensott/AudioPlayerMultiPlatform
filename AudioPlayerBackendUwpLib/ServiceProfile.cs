using AudioPlayerBackend.AudioLibrary.PlaylistRepo.MediaSource;
using AudioPlayerBackend.AudioLibrary.PlaylistRepo.OwnTcp.Extensions;
using AudioPlayerBackend.Communication.Base;
using System;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage.Streams;
using Windows.Storage;
using System.Runtime.InteropServices.WindowsRuntime;

namespace AudioPlayerBackendUwpLib
{
    public struct ServiceProfile
    {
        private const string filename = "serviceProfile.data";

        public bool BuildStandalone { get; set; }

        public bool BuildServer { get; set; }

        public bool BuildClient { get; set; }

        public bool AutoUpdate { get; set; }

        public int ServerPort { get; set; }

        public int? ClientPort { get; set; }

        public string ServerAddress { get; set; }

        public FileMediaSourceRootInfo[] DefaultUpdateRoots { get; set; }

        public byte[] ToData()
        {
            return new ByteQueue()
                .Enqueue(BuildStandalone)
                .Enqueue(BuildServer)
                .Enqueue(BuildClient)
                .Enqueue(AutoUpdate)
                .Enqueue(ServerPort)
                .Enqueue(ClientPort)
                .Enqueue(ServerAddress)
                .Enqueue(DefaultUpdateRoots);
        }

        public async Task Save()
        {
            byte[] data = ToData();

            StorageFile file = await ApplicationData.Current.LocalFolder
                .CreateFileAsync(filename, CreationCollisionOption.OpenIfExists);
            await FileIO.WriteBytesAsync(file, data);
        }

        public static ServiceProfile FromData(byte[] data)
        {
            ByteQueue queue = data;
            return new ServiceProfile()
            {
                BuildStandalone = queue.DequeueBool(),
                BuildServer = queue.DequeueBool(),
                BuildClient = queue.DequeueBool(),
                AutoUpdate = queue.DequeueBool(),
                ServerPort = queue.DequeueInt(),
                ClientPort = queue.DequeueIntNullable(),
                ServerAddress = queue.DequeueString(),
                DefaultUpdateRoots = queue.DequeueFileMediaSourceRootInfos()?.ToArray(),
            };
        }

        public static async Task<ServiceProfile?> Load()
        {
            try
            {
                IStorageItem item = await ApplicationData.Current.LocalFolder.TryGetItemAsync(filename);
                if (item is StorageFile)
                {
                    IBuffer buffer = await FileIO.ReadBufferAsync((StorageFile)item);
                    ServiceProfile profile = ServiceProfile.FromData(buffer.ToArray());
                    return profile;
                }
            }
            catch (Exception exc)
            {
                System.Diagnostics.Debug.WriteLine("Loading service profile failed:\n" + exc);
            }

            return null;
        }
    }
}
