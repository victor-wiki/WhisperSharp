using System.IO.Compression;

namespace WhisperSharp.Helper
{
    public class ZipHelper
    {
        public static byte[] DecompressBytes(byte[] compressed)
        {
            using (var input = new MemoryStream(compressed))
            {
                var gzip = new GZipStream(input, CompressionMode.Decompress);
                var output = new MemoryStream();
                gzip.CopyTo(output);

                return output.ToArray();
            }          
        }

        public static byte[] Compress(byte[] bytes)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                GZipStream zipStream = new GZipStream(outStream, CompressionMode.Compress, true);

                zipStream.Write(bytes, 0, bytes.Length);

                return outStream.ToArray();
            }
        }
    }
}
