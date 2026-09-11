using System.Text;

namespace WhisperSharp.Writer
{
    public class WriteTXT:ResultWriter
    {
        public override string extension => "txt";

        public WriteTXT() : base() { }

        public WriteTXT(string output_dir):base(output_dir) { }

        public override string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var segment in result["segments"])
            {
                 sb.AppendLine(segment["text"].Trim());
            }

            return sb.ToString();
        }
    }
}
