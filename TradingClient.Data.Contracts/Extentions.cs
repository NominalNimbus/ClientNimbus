using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace TradingClient.Data.Contracts
{
    public static class Extentions
    {
        public static IEnumerable<T> EmptyIfNull<T>(this IEnumerable<T> source) =>
            source ?? Enumerable.Empty<T>();

        public static bool EqualsValue(this string s1, string s2)
        {
            if (string.IsNullOrEmpty(s1) && string.IsNullOrEmpty(s2))
                return true;

            if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2))
                return false;

            return s1.Equals(s2, StringComparison.InvariantCultureIgnoreCase);
        }

        public static bool IsUserObjectNameValid(string name) =>
            string.IsNullOrWhiteSpace(name) ? false : Regex.IsMatch(name, @"^[0-9a-zA-Z\(\)\-\.\ ]+$");

        /// <summary>
        /// Gets all combinations (cartesian product) for a set of collections
        /// </summary>
        /// <typeparam name="T">Type of elements in generic collections</typeparam>
        /// <param name="sequences">Set of collections</param>
        /// <returns>Set of combinations for given set of input collections</returns>
        public static IEnumerable<IEnumerable<T>> CartesianProduct<T>(IEnumerable<IEnumerable<T>> sequences)
        {
            IEnumerable<IEnumerable<T>> emptyProduct = new[] { Enumerable.Empty<T>() };
            return sequences.Aggregate(emptyProduct, (acc, seq) =>
                from accseq in acc
                from item in seq
                select accseq.Concat(new[] { item }));
        }

        public static string GetTimeFrameString(TimeFrame tf, int interval)
        {
            return tf == TimeFrame.Month ? tf.ToString()
                : tf.ToString().Substring(0, 1) + interval.ToString();
        }

        public static int GetMaxInterval(TimeFrame timeframe)
        {
            switch (timeframe)
            {
                case TimeFrame.Tick: return 999;
                case TimeFrame.Minute: return 59;
                case TimeFrame.Hour: return 23;
                case TimeFrame.Day: return 30;
                default: return 1;
            }
        }

        public static List<Bar> ParseBarData(string file)
        {
            if (file == null || !File.Exists(file))
                return null;

            string[] formats = new[] { "yyyy-MM-dd", "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd HH:mm:ss.fff" };
            var result = new List<Bar>();
            using (TextReader reader = new StreamReader(file))
            {
                string line = null;
                string[] parts = null;
                while (!String.IsNullOrWhiteSpace(line = reader.ReadLine()))
                {
                    if (!Char.IsDigit(line[0]))
                        continue;

                    parts = line.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length > 4)
                    {
                        result.Add(new Bar
                        {
                            Timestamp = DateTime.ParseExact(parts[0], formats,
                                System.Globalization.CultureInfo.InvariantCulture,
                                System.Globalization.DateTimeStyles.None),
                            OpenBid = Decimal.Parse(parts[1]),
                            HighBid = Decimal.Parse(parts[2]),
                            LowBid = Decimal.Parse(parts[3]),
                            CloseBid = Decimal.Parse(parts[4]),
                            VolumeBid = parts.Length > 5 ? Int64.Parse(parts[5]) : 0L
                        });
                    }
                }
            }

            return result;
        }

        public static void Serialize<T>(T data, string file)
        {
            try
            {
                XmlSerializerNamespaces ns = new XmlSerializerNamespaces();  //optional: strip namespace stuff
                ns.Add("", "");
                using (System.Xml.XmlTextWriter textWriter = new System.Xml.XmlTextWriter(file, Encoding.UTF8))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    textWriter.Formatting = System.Xml.Formatting.Indented;
                    xmlSerializer.Serialize(textWriter, data, ns);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to serialize data of type " + typeof(T).ToString() + ": " + e.Message);
                throw e;
            }
        }

        public static T Deserialize<T>(string file)
        {
            if (!File.Exists(file))
                return default(T);

            try
            {
                using (TextReader textReader = new StreamReader(file, Encoding.UTF8))
                {
                    XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
                    return (T)xmlSerializer.Deserialize(textReader);
                }
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to deserialize data of type " + typeof(T).ToString() + ": " + e.Message);
                return default(T);  //or throw;
            }
        }

        public static void CsvSerialize<T>(IEnumerable<T> data, string file)
        {
            try
            {
                var csvSerializer = new CsvSerializer<T>();
                csvSerializer.Serialize(file, data);
            }
            catch (Exception e)
            {
                Trace.TraceError("Failed to serialize data of type " + typeof(T).ToString() + ": " + e.Message);
                throw e;
            }
        }

        public static string GetFileVersion(string file)
            => File.Exists(file) ? FileVersionInfo.GetVersionInfo(file).FileVersion : String.Empty;

        public static void CopyDirContents(string source, string destination, byte levels = 1)
        {
            var sourceDir = new DirectoryInfo(source);
            var destinationDir = new DirectoryInfo(destination);

            if (levels < 1)
                levels = 1;

            for (byte i = 0; i < levels; i++)
            {
                if (sourceDir == null || destinationDir == null)
                    break;

                foreach (var f in sourceDir.GetFiles())
                {
                    var destFile = Path.Combine(destinationDir.FullName, f.Name);
                    if (!File.Exists(destFile))
                        File.Copy(f.FullName, destFile);
                }

                sourceDir = sourceDir.Parent;
                destinationDir = destinationDir.Parent;
            }
        }

        public static byte[] ZipFolder(string path, string fileExtensionsToSkip = null)
        {
            var extToSkip = String.IsNullOrWhiteSpace(fileExtensionsToSkip) ? null
                : fileExtensionsToSkip.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList();

            var files = GetFilesFromDirectory(path, extToSkip);
            if (files == null || files.Count == 0)
                return null;

            using (var stream = new MemoryStream())
            {
                using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
                {
                    foreach (var file in files)
                        zip.CreateEntryFromFile(file, GetRelativePath(file, path));
                }
                return stream.ToArray();
            }
        }

        public static void UnzipFile(string from, string to)
        {
            if (from != null && File.Exists(from))
                ZipFile.ExtractToDirectory(from, to);
        }

        public static void UnzipData(byte[] data, string to)
        {
            char separator = Path.DirectorySeparatorChar;
            if (data == null || data.Length == 0
                || String.IsNullOrWhiteSpace(to) || to[to.Length - 1] == separator)
            {
                return;
            }

            if (to.Contains(separator) && !Directory.Exists(to.Remove(to.LastIndexOf(separator))))
                Directory.CreateDirectory(to.Remove(to.LastIndexOf(separator)));

            using (var zip = new GZipStream(new MemoryStream(data), CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var stream = new FileStream(to, FileMode.Create, FileAccess.Write))
                {
                    var count = 0;
                    do
                    {
                        count = zip.Read(buffer, 0, size);
                        if (count > 0)
                            stream.Write(buffer, 0, count);
                    }
                    while (count > 0);
                }
            }
        }

        public static byte[] Compress(byte[] raw)
        {
            using (var memory = new MemoryStream())
            {
                using (var gzip = new GZipStream(memory, CompressionMode.Compress, true))
                    gzip.Write(raw, 0, raw.Length);

                return memory.ToArray();
            }
        }

        public static byte[] Decompress(byte[] raw)
        {
            using (var stream = new GZipStream(new MemoryStream(raw), CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    var count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                            memory.Write(buffer, 0, count);
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private static List<string> GetFilesFromDirectory(string path, List<string> fileExtensionsToSkip)
        {
            if (!Directory.Exists(path))
                return null;

            var ret = new List<string>();
            var dirInfo = new DirectoryInfo(path);
            foreach (var file in dirInfo.GetFiles())
            {
                if (fileExtensionsToSkip == null || !fileExtensionsToSkip.Contains(file.Extension))
                    ret.Add(file.FullName);
            }

            foreach (var dir in dirInfo.GetDirectories())
            {
                var subFiles = GetFilesFromDirectory(dir.FullName, fileExtensionsToSkip);
                ret.AddRange(subFiles);
            }

            return ret;
        }

        private static string GetRelativePath(string fullPath, string rootFullPath)
        {
            var relative = fullPath.Substring(rootFullPath.Length);
            return relative.Length > 0 && relative[0] == Path.DirectorySeparatorChar 
                ? relative.Substring(1) : relative;
        }
    }
}