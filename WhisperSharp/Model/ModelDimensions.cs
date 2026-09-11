namespace WhisperSharp.Model
{
    public class ModelDimensions
    {
        public int n_mels { get; set; }
        public int n_audio_ctx { get; set; }
        public int n_audio_state { get; set; }
        public int n_audio_head { get; set; }
        public int n_audio_layer { get; set; }
        public int n_vocab { get; set; }
        public int n_text_ctx { get; set; }
        public int n_text_state { get; set; }
        public int n_text_head { get; set; }
        public int n_text_layer { get; set; }
    }
}
