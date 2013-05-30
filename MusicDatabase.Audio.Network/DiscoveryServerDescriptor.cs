using System.Net;

namespace MusicDatabase.Audio.Network
{
    public class DiscoveryServerDescriptor
    {
        public IPAddress Address { get; private set; }
        public int ThreadCount { get; private set; }

        public DiscoveryServerDescriptor(IPAddress address, int threadCount)
        {
            this.Address = address;
            this.ThreadCount = threadCount;
        }
    }
}
