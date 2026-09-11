namespace WhisperSharp.Model
{
    public class WordTiming
    {
        public string word { get; set; }
        public List<int> tokens { get; set; }
        public float start { get; set; }
        public float end { get; set; }
        public float probability { get; set; }
    }
}
