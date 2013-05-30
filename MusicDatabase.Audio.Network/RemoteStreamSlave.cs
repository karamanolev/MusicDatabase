using System;
using System.Linq;

namespace MusicDatabase.Audio.Network
{
    //class RemoteStreamSlave
    //{
    //    private const int BufferSize = 88200;

    //    private byte[] buffer;
    //    private Stream targetStream, networkStream;

    //    public RemoteStreamSlave(Stream targetStream, Stream networkStream)
    //    {
    //        this.targetStream = targetStream;
    //        this.networkStream = networkStream;
    //        this.buffer = new byte[BufferSize];
    //    }

    //    public void ReadToEnd()
    //    {
    //        while (true)
    //        {
    //            NetworkMessageType type = (NetworkMessageType)this.networkStream.ReadByte();
    //            if (type == NetworkMessageType.DataRead)
    //            {
    //                int length = this.networkStream.ReadInt32();
    //                while (length > 0)
    //                {
    //                    int read = 
    //                }
    //            }
    //        }
    //    }
    //}
}
