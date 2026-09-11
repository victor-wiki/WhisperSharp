namespace WhisperSharp.Writer
{
    public class ResultWriter
    {
        private string output_dir;
        protected string output_path;

        public virtual string extension { get; }      
        
        public ResultWriter() { }

        public ResultWriter(string output_dir)
        {
            this.output_dir = output_dir;
        }

        public void call(Dictionary<string, dynamic> result, string audio_path, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            var audio_basename = Path.GetFileNameWithoutExtension(audio_path);

            this.output_path = Path.Combine(this.output_dir, audio_basename + "." + this.extension);

            string content = this.get_result(result);

            File.WriteAllText(this.output_path, content, System.Text.Encoding.UTF8);
        }      

        public virtual string get_result(Dictionary<string, dynamic> result, Dictionary<string, dynamic> options = null, dynamic kwargs = null)
        {
            throw new NotImplementedException();
        }
    }
}
