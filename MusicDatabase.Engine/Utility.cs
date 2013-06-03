using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Xml;
using MongoDB.Bson;
using MusicDatabase.Engine.Entities;

namespace MusicDatabase.Engine
{
    public static class Utility
    {
        public const string DateTimeFormatString = "yyyy-MM-dd HH:mm:ss";
        public const string AllFilesFilter = "All Files (*.*)|*.*";
        public const string DialogImageFilter = "JPEG Files (*.jpeg; *.jpg)|*.jpeg;*.jpg|PNG Files (*.png)|*.png|TIFF Files (*.tif;*.tiff)|*.tif;*.tiff|" + AllFilesFilter;
        private static readonly object ErrorLogSyncRoot = new object();

        public static string ApplicationDataPath
        {
            get
            {
                return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MusicDatabase");
            }
        }

        public static string ErrorLogPath
        {
            get
            {
                return Path.Combine(ApplicationDataPath, "Error.log");
            }
        }

        public static string SettingsDatabasePath
        {
            get
            {
                return Path.Combine(ApplicationDataPath, "Settings.s3db");
            }
        }

        public static string RemoveDiacritics(string stIn)
        {
            string stFormD = stIn.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            for (int ich = 0; ich < stFormD.Length; ich++)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(stFormD[ich]);
                if (uc != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(stFormD[ich]);
                }
            }
            return (sb.ToString().Normalize(NormalizationForm.FormC));
        }

        public static int LastIndexWhere<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            int index = -1, currentIndex = 0;
            foreach (T item in source)
            {
                if (predicate(item))
                {
                    index = currentIndex;
                }
                ++currentIndex;
            }
            return index;
        }

        public static string Join(this IEnumerable<IReleaseArtist> source)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var releaseArtist in source)
            {
                sb.Append(releaseArtist.Artist.Name);
                if (!string.IsNullOrEmpty(releaseArtist.JoinString))
                {
                    if (releaseArtist.JoinString == ",")
                    {
                        sb.Append(", ");
                    }
                    else
                    {
                        sb.Append(" " + releaseArtist.JoinString + " ");
                    }
                }
            }
            return sb.ToString();
        }

        private static readonly string[] bytesToStringSuffixes = new string[] { "B", "KB", "MB", "GB", "TB", "PB" };
        public static string BytesToString(long bytes)
        {
            double result = bytes;
            int suffix = 0;
            while (suffix < bytesToStringSuffixes.Length && result >= 1000)
            {
                result /= 1000;
                ++suffix;
            }
            if (result >= 100)
            {
                return result.ToString("0") + " " + bytesToStringSuffixes[suffix];
            }
            else
            {
                return result.ToString("0.0") + " " + bytesToStringSuffixes[suffix];
            }
        }

        private static readonly char[] NonBreakingCharacters = new char[] { '\'' };
        public static string CapitalizeWords(string value)
        {
            StringBuilder sb = new StringBuilder(value.Length);
            bool cap = true;
            foreach (char c in value)
            {
                if (char.IsLetterOrDigit(c) || NonBreakingCharacters.Contains(c))
                {
                    sb.Append(cap ? char.ToUpper(c) : char.ToLower(c));
                    cap = false;
                }
                else
                {
                    sb.Append(c);
                    cap = true;
                }
            }
            return sb.ToString();
        }

        public static bool ContainsCustomAttribute<T>(this MemberInfo memberInfo)
        {
            return memberInfo.GetCustomAttributes(typeof(T), true).Length > 0;
        }

        public static int GetCombinedHashCode(params object[] objects)
        {
            int combinedHash = 271;
            for (int i = 0; i < objects.Length; ++i)
            {
                combinedHash *= 31;
                if (objects[i] != null)
                {
                    combinedHash += objects[i].GetHashCode();
                }
                else
                {
                    combinedHash += 271;
                }
            }
            return combinedHash;
        }

        public static void AddRange<T>(this IList<T> target, IEnumerable<T> items)
        {
            foreach (T item in items)
            {
                target.Add(item);
            }
        }

        public static bool TryDeleteFile(string fileName)
        {
            try
            {
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDeleteDirectoryRecursive(string directoryName)
        {
            try
            {
                if (Directory.Exists(directoryName))
                {
                    Directory.Delete(directoryName, true);
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool TryDeleteEmptyFoldersToTheRoot(string originalPath)
        {
            try
            {
                while (Directory.Exists(originalPath))
                {
                    if (Directory.GetFiles(originalPath).Length == 0 && Directory.GetDirectories(originalPath).Length == 0)
                    {
                        Directory.Delete(originalPath);
                        originalPath = Path.GetDirectoryName(originalPath);
                    }
                    else
                    {
                        break;
                    }
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void WriteToErrorLog(string error)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ErrorLogPath));
            lock (ErrorLogSyncRoot)
            {
                File.AppendAllText(ErrorLogPath, DateTime.Now.ToString(Utility.DateTimeFormatString) + ": " + error + Environment.NewLine + Environment.NewLine);
            }
        }

        public static void AssertElementStart(this XmlReader reader, string name)
        {
            Assert.IsTrue(reader.NodeType == XmlNodeType.Element && reader.Name == name);
        }

        public static void AssertElementEnd(this XmlReader reader, string name)
        {
            Assert.IsTrue(reader.NodeType == XmlNodeType.EndElement && reader.Name == name);
        }

        public static bool IsElementStart(this XmlReader reader, string name)
        {
            return reader.NodeType == XmlNodeType.Element && reader.Name == name;
        }

        public static bool IsElementEnd(this XmlReader reader, string name)
        {
            return reader.NodeType == XmlNodeType.EndElement && reader.Name == name;
        }

        public static string GetAttributeOrNull(this XmlReader reader, string attributeName)
        {
            string value = reader.GetAttribute(attributeName);
            if (string.IsNullOrEmpty(value))
            {
                return null;
            }
            return value;
        }

        public static int GetAttributeInt32(this XmlReader reader, string attributeName, int defaultValue)
        {
            string value = reader.GetAttributeOrNull(attributeName);
            if (value == null)
            {
                return defaultValue;
            }
            int intValue;
            if (int.TryParse(value, out intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        public static double GetAttributeDouble(this XmlReader reader, string attributeName, double defaultValue)
        {
            string value = reader.GetAttributeOrNull(attributeName);
            if (value == null)
            {
                return defaultValue;
            }
            double intValue;
            if (double.TryParse(value, out intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        public static long GetAttributeInt64(this XmlReader reader, string attributeName, long defaultValue)
        {
            string value = reader.GetAttributeOrNull(attributeName);
            if (value == null)
            {
                return defaultValue;
            }
            long intValue;
            if (long.TryParse(value, out intValue))
            {
                return intValue;
            }
            return defaultValue;
        }

        public static T ParseEnum<T>(string value)
        {
            return (T)Enum.Parse(typeof(T), value);
        }

        public static string Join(this IEnumerable<string> source, string join)
        {
            return string.Join(join, source);
        }

        public static bool TryEmptyDirectory(string path)
        {
            foreach (string file in Directory.GetFiles(path))
            {
                if (!TryDeleteFile(file))
                {
                    return false;
                }
            }

            foreach (string directory in Directory.GetDirectories(path))
            {
                if (!TryDeleteDirectoryRecursive(directory))
                {
                    return false;
                }
            }

            return true;
        }

        public static string PascalCaseToString(string pascalCase)
        {
            if (pascalCase.Length == 0)
            {
                return "";
            }
            StringBuilder result = new StringBuilder(pascalCase.Length);
            result.Append(pascalCase[0]);
            for (int i = 1; i < pascalCase.Length; ++i)
            {
                if (char.IsUpper(pascalCase[i]))
                {
                    result.Append(' ');
                }
                result.Append(pascalCase[i]);
            }
            return result.ToString();
        }

        public static void ForEach<T>(this IEnumerable<T> source, Action<T> action)
        {
            foreach (T item in source)
            {
                action(item);
            }
        }

        public static double Clamp(double value, double min, double max)
        {
            return Math.Min(max, Math.Max(min, value));
        }

        public static string ComputeSHA1(byte[] data)
        {
            using (SHA1CryptoServiceProvider sha1 = new SHA1CryptoServiceProvider())
            {
                return Convert.ToBase64String(sha1.ComputeHash(data));
            }
        }

        public static bool EqualsThreshold(this double value, double other, double epsilon = 0.00001)
        {
            return Math.Abs(value - other) <= epsilon;
        }

        public static TagLib.File TryCreateTagLibFile(string path, int retries = 3, int intervalMillis = 500)
        {
            Exception exception = null;

            for (int i = 0; i < retries; ++i)
            {
                try
                {
                    return TagLib.File.Create(path);
                }
                catch(Exception ex)
                {
                    exception = ex;
                    Thread.Sleep(intervalMillis);
                }
            }

            throw exception;
        }
    }
}
