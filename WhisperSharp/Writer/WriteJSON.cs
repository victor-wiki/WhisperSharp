using Newtonsoft.Json;

namespace WhisperSharp.Writer
{
    public class WriteJSON : ResultWriter
    {
        public override string extension => "json";

        public WriteJSON() : base() { }

        public WriteJSON(string output_dir) : base(output_dir) { }

        public override string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            string content = JsonConvert.SerializeObject(result["segments"], Formatting.Indented);

            return content;           
        }
    }
}
