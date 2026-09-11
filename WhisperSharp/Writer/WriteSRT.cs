using System.Text;

namespace WhisperSharp.Writer
{
    public class WriteSRT: SubtitlesWriter
    {
        public override string extension => "srt";
        protected override bool always_include_hours => false;
        protected override string decimal_marker => ",";

        public WriteSRT() : base(){ }

        public WriteSRT(string output_dir) : base(output_dir) { }

        public override string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            StringBuilder sb = new StringBuilder();

            int i = 0;

            var results = this.iterate_result(result, options, kwargs);

            foreach (var item in results)
            {
                var start = item.Item1;
                var end = item.Item2;
                var text = item.Item3;

                sb.AppendLine($"{i}\n{start} --> {end}\n{text}");

                i++;
            }

            return sb.ToString();
        }
    }
}
