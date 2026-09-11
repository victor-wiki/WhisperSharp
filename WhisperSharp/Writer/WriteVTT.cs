using System.Text;

namespace WhisperSharp.Writer
{
    public class WriteVTT : SubtitlesWriter
    {
        public override string extension => "vtt";
        protected override bool always_include_hours => false;
        protected override string decimal_marker => ".";

        public WriteVTT() : base() { }

        public WriteVTT(string output_dir) : base(output_dir) { }

        public override string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("WEBVTT");

            foreach (var item in this.iterate_result(result, options, kwargs))
            {
                var start = item.Item1;
                var end = item.Item2;
                var text = item.Item3;

                sb.AppendLine($"{start} --> {end}\n{text}");
            }

            return sb.ToString();
        }
    }
}
