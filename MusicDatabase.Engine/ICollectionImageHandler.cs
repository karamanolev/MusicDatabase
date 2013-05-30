using System;
using System.Linq;
using System.Xml;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public interface ICollectionImageHandler
    {
        long GetImageByteLength(Image image);
        byte[] LoadImage(Image image);
        void StoreImage(Image image, byte[] bytes);
        void StoreImageFromXml(Image image, XmlReader reader);
        void DeleteImage(Image image);
    }
}
