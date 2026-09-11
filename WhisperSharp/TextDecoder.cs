using TorchSharp;
using TorchSharp.Modules;
using static TorchSharp.torch;
using nn = TorchSharp.torch.nn;

namespace WhisperSharp
{
    public class TextDecoder : nn.Module
    {
        private Embedding token_embedding;
        private Parameter positional_embedding;        
        private LayerNormExt ln;
        private Tensor mask;
        public ModuleList<ResidualAttentionBlock> blocks { get; set; }

        public TextDecoder(int n_vocab, int n_ctx, int n_state, int n_head, int n_layer):base(nameof(TextDecoder))
        {
            this.token_embedding = nn.Embedding(n_vocab, n_state);
            this.positional_embedding = nn.Parameter(torch.empty(n_ctx, n_state));

            List<ResidualAttentionBlock> list = new List<ResidualAttentionBlock>();

            for (int i = 0; i < n_layer; i++)
            {
                list.Add(new ResidualAttentionBlock(n_state, n_head, true));
            }

            this.blocks = nn.ModuleList<ResidualAttentionBlock>(list.ToArray());

            this.ln = new LayerNormExt(new long[] { n_state });

            this.mask = torch.empty(n_ctx, n_ctx).fill_(float.NegativeInfinity).triu_(1);
            this.register_buffer("mask", this.mask, persistent: false);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="x">torch.LongTensor, shape = (batch_size, <= n_ctx) the text tokens</param>
        /// <param name="xa">torch.Tensor, shape = (batch_size, n_audio_ctx, n_audio_state)
        /// torch.Tensor, shape = (batch_size, n_audio_ctx, n_audio_state)
        /// </param>
        /// <param name="kv_cache"></param>
        /// <returns></returns>
        public Tensor forward(Tensor x, Tensor xa, Dictionary<nn.Module<Tensor, Tensor>, Tensor> kv_cache = null)
        {
            var offset =(int)( kv_cache != null && kv_cache.Count>0 ? kv_cache.FirstOrDefault().Value.shape[1] : 0);         

            x = this.token_embedding.forward(x) + this.positional_embedding[offset..(int)(offset + x.shape.LastOrDefault())];
            x = x.to(xa.dtype);

            foreach (var block in this.blocks)
            {
                x = block.forward(x, xa, mask: this.mask, kv_cache: kv_cache);
            }

            x = this.ln.forward(x);

            var logits =torch.matmul(x, torch.transpose(this.token_embedding.weight.to(x.dtype), 0, 1)).@float();

            return logits;
        }
    }
}
