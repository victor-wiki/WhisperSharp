using TorchSharp.Modules;
using static TorchSharp.torch;
using nn = TorchSharp.torch.nn;

namespace WhisperSharp
{
    public class ResidualAttentionBlock : nn.Module
    {
        public MultiHeadAttention attn { get; set; }
        public LayerNormExt attn_ln { get; set; }
        public MultiHeadAttention cross_attn { get; set; }
        public LayerNormExt cross_attn_ln { get; set; }
        public LayerNormExt mlp_ln { get; set; }
        public Sequential mlp { get; set; }


        public ResidualAttentionBlock(int n_state, int n_head, bool cross_attention = false):base(nameof(ResidualAttentionBlock))
        {
            var normalized_shape = new long[] { n_state };

            this.attn = new MultiHeadAttention(n_state, n_head);
            this.attn_ln = new LayerNormExt(normalized_shape);

            this.cross_attn = cross_attention ? new MultiHeadAttention(n_state, n_head) : null;
            this.cross_attn_ln = cross_attention ? new LayerNormExt(normalized_shape) : null;

            var n_mlp = n_state * 4;
            this.mlp = nn.Sequential(new Linear(n_state, n_mlp), nn.GELU(), new Linear(n_mlp, n_state));
            this.mlp_ln = new LayerNormExt(normalized_shape);
        }

        public Tensor forward(Tensor x, Tensor xa = null, Tensor mask = null, Dictionary<nn.Module<Tensor, Tensor>, Tensor> kv_cache = null)
        {
            var lx = this.attn_ln.forward(x);
            x = x + this.attn.forward(lx, null, mask, kv_cache).Item1;

            if (this.cross_attn != null)
            {
                lx = this.cross_attn_ln.forward(x);
                x = x + this.cross_attn.forward(lx, xa, null, kv_cache).Item1;
            }

            lx = this.mlp_ln.forward(x);
            x = x + this.mlp.forward(lx);

            return x;
        }
    }
}
