using System.Text;

namespace WhisperSharp.Writer
{
    /// <summary>
    /// Write a transcript to a file in TSV (tab-separated values) format containing lines like:
    /// <start time in integer milliseconds>\t<end time in integer milliseconds>\t<transcript text>
    /// 
    /// Using integer milliseconds as start and end times means there's no chance of interference from
    /// an environment setting a language encoding that causes the decimal in a floating point number
    /// to appear as a comma; also is faster and more efficient to parse & store, e.g., in C++.
    /// </summary>
    public class WriteTSV: ResultWriter
    {
        public override string extension => "tsv";

        public WriteTSV() : base() { }

        public WriteTSV(string output_dir) : base(output_dir) { }

        public override string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine($"start\tend\ttext");     

            foreach (var segment in result["segments"])
            {
                sb.AppendLine($"{Math.Round(1000 * segment["start"])}\t");
                sb.AppendLine($"\t{Math.Round(1000 * segment["end"])}");
                sb.AppendLine($"\t\t{segment["text"].Trim().Replace("\t", " ")}");
            }

            return sb.ToString();
        }
    }
}
