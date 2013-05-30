using System.Collections.Generic;

namespace MusicDatabase.Engine
{
    public static class MimeHelper
    {
        private static readonly Dictionary<string, string> extensionToMimeType = new Dictionary<string, string>()
        {
            {".png", "image/png"},
            {".bmp", "image/bmp"},
            {".gif", "image/gif"},
            {".jpg", "image/jpeg"},
            {".jpeg", "image/jpeg"},
            {".tif", "image/tiff"},
            {".tiff", "image/tiff"},
        };
        private static readonly Dictionary<string, string> mimeTypeToExtension;

        static MimeHelper()
        {
            mimeTypeToExtension = new Dictionary<string, string>();
            foreach (KeyValuePair<string, string> item in extensionToMimeType)
            {
                mimeTypeToExtension[item.Value] = item.Key;
            }
        }

        public static string GetExtensionForMimeType(string mimeType)
        {
            string extension;
            if (mimeTypeToExtension.TryGetValue(mimeType.ToLower(), out extension))
            {
                return extension;
            }
            return ".dat";
        }

        public static string GetMimeTypeForExtension(string extension)
        {
            string mimeType;
            if (extensionToMimeType.TryGetValue(extension.ToLower(), out mimeType))
            {
                return mimeType;
            }
            return "application/unknown";
        }
    }
}
