using TorchSharp.Modules;
using static TorchSharp.torch;
using nn = TorchSharp.torch.nn;

namespace WhisperSharp
{
    public class AudioEncoder : nn.Module
    {
        private LayerNormExt ln_post;
        private Tensor positional_embedding;

        public Conv1dExt conv1 { get; set; }
        public Conv1dExt conv2 { get; set; }
        public ModuleList<ResidualAttentionBlock> blocks { get; set; }
       

        public AudioEncoder(int n_mels, int n_ctx, int n_state, int n_head, int n_layer):base(nameof(AudioEncoder))
        {
            this.conv1 = new Conv1dExt(n_mels, n_state, kernel_size: 3, padding: 1);
            this.conv2 = new Conv1dExt(n_state, n_state, kernel_size: 3, stride: 2, padding: 1);

            this.positional_embedding = model.sinusoids(n_ctx, n_state);

            this.register_buffer("positional_embedding", positional_embedding);

            List<ResidualAttentionBlock> list = new List<ResidualAttentionBlock>();

            for (int i = 0; i < n_layer; i++)
            {
                list.Add(new ResidualAttentionBlock(n_state, n_head));
            }

            this.blocks = nn.ModuleList<ResidualAttentionBlock>(list.ToArray());
            this.ln_post = new LayerNormExt(new long[] { n_state });
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">torch.Tensor, shape = (batch_size, n_mels, n_ctx)
        /// the mel spectrogram of the audio
        /// </param>
        /// <returns></returns>
        public Tensor forward(Tensor x)
        {
            x = nn.functional.gelu(this.conv1.conv_forward(x, this.conv1.weight, this.conv1.bias));
            x = nn.functional.gelu(this.conv2.conv_forward(x, this.conv2.weight, this.conv2.bias));
            x = x.permute(0, 2, 1);

            x = (x + this.positional_embedding).to(x.dtype);

            foreach(var block in this.blocks)
            {
                x = block.forward(x);
            }

            x = this.ln_post.forward(x);

            return x;
        }
    }
}
