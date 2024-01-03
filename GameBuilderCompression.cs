using System.IO;
using System.IO.Compression;

namespace GameBuilderEditor
{
    /// <summary>
    /// Helper class for compressing files into an archive.
    /// </summary>
    public static class GameBuilderCompression
    {
        /// <summary>
        /// compresses files (not folders) into the specified archive path
        /// </summary>
        /// <returns></returns>
        public static void ZipFiles(string[] files, string outputPath, CompressionLevel compressionLevel)
        {
            if (files == null || files.Length == 0)
            {
                return;
            }

            using var archive = ZipFile.Open(outputPath, ZipArchiveMode.Create);
            foreach (var file in files)
            {
                if (File.Exists(file))
                {
                    archive.CreateEntryFromFile(file, Path.GetFileName(file), compressionLevel);
                }
            }
        }
    }
}