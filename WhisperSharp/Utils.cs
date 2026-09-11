using System.Text;
using WhisperSharp.Helper;
using WhisperSharp.Writer;

namespace WhisperSharp
{
    public class Utils
    {
        public static string make_safe(string str)
        {
            if (Encoding.Default == Encoding.UTF8)
            {
                //utf-8 can encode any Unicode code point, so no need to do the round-trip encoding
                return str;
            }
            else
            {
                // replaces any character not representable using the system default encoding with an '?',
                // avoiding UnicodeEncodeError (https://github.com/openai/whisper/discussions/729).
                return System.Text.Encoding.Default.GetString(System.Text.Encoding.Default.GetBytes(str));
            }

            return null;
        }

        public static int exact_div(int x, int y)
        {
            return (int)Math.Floor(x * 1.0 / y);
        }

        public static bool str2bool(string str)
        {
            if (str == "True" || str == "true")
            {
                return true;
            }

            return false;
        }

        public static int? optional_int(string str)
        {
            return str == null ? null : int.Parse(str);
        }

        public static float? optional_float(string str)
        {
            return str == null ? null : float.Parse(str);
        }

        public static float compression_ratio(string text)
        {
            var text_bytes = System.Text.Encoding.UTF8.GetBytes(text);

            return text_bytes.Length * 1.0f / ZipHelper.Compress(text_bytes).Length;
        }

        public static string format_timestamp(float seconds, bool always_include_hours = false, string decimal_marker = ".")
        {
            var milliseconds = (int)Math.Round(seconds * 1000.0);

            var hours = (int)Math.Floor(milliseconds * 1.0 / 3600000);
            milliseconds -= hours * 3_600_000;

            var minutes = (int)Math.Floor(milliseconds * 1.0 / 60000);
            milliseconds -= minutes * 60_000;

            seconds = (int)Math.Floor(milliseconds * 1.0 / 1000);
            milliseconds -= (int)seconds * 1_000;

            var hours_marker = (always_include_hours || hours > 0) ? $"{hours:02d}" : "";

            return $"{hours_marker}{GetPadString(minutes, 2)}:{GetPadString((int)seconds, 2)}{decimal_marker}{GetPadString(milliseconds, 3)}";
        }

        public static string GetPadString(int number, int totalWidth)
        {
            return number.ToString().PadLeft(totalWidth, '0');
        }

        public static dynamic get_writer(string output_format, string output_dir = null)
        {
            var writers = new Dictionary<string, Type> {
                {"txt", typeof(WriteTXT) },
                {"vtt", typeof(WriteVTT)},
                {"srt", typeof(WriteSRT)},
                {"tsv", typeof(WriteTSV)},
                {"json", typeof(WriteJSON)}
            };

            Func<Type, ResultWriter> getWriterInstance = (type) =>
            {
                if(output_dir != null)
                {
                    return (ResultWriter)Activator.CreateInstance(type, output_dir);
                }
                else
                {
                    return (ResultWriter)Activator.CreateInstance(type);
                }
            };

            if (output_format == "all")
            {
                var all_writers = writers.Values;

                Action<Dictionary<string, dynamic>, string, Dictionary<string, dynamic>, Dictionary<string, dynamic>> write_all = (result, file, options, kwargs) =>
                {
                    foreach (var writerType in all_writers)
                    {
                        ResultWriter writer = getWriterInstance(writerType);

                        writer.call(result, file, options, kwargs);
                    }
                };

                return write_all;
            }

            return getWriterInstance(writers[output_format]);
        }
    }
}
