using TorchSharp;
using WhisperSharp.Extension;
using static TorchSharp.torch;
using nn = TorchSharp.torch.nn;

namespace WhisperSharp
{
    public class MultiHeadAttention : nn.Module<Tensor, Tensor>
    {
        private int n_head;
        private LinearExt query;
        private LinearExt @out;
        public LinearExt key { get; set; }
        public LinearExt value { get; set; }

        public static bool use_sdpa { get; set; } = true;

        public MultiHeadAttention(string name) : base(name) { }

        public MultiHeadAttention(IntPtr handle, IntPtr boxedHandle) : base(handle, boxedHandle) { }
        public MultiHeadAttention(HType handle, IntPtr? boxedHandle) : base(handle, boxedHandle) { }

        public MultiHeadAttention(int n_state, int n_head) : base(nameof(MultiHeadAttention))
        {
            this.n_head = n_head;
            this.query = new LinearExt(n_state, n_state);
            this.key = new LinearExt(n_state, n_state, false);
            this.value = new LinearExt(n_state, n_state);
            this.@out = new LinearExt(n_state, n_state);
        }

        public (Tensor, Tensor) forward(Tensor x, Tensor xa = null, Tensor mask = null, Dictionary<nn.Module<Tensor, Tensor>, Tensor> kv_cache = null)
        {
            Tensor q = this.query.forward(x);

            Tensor k = null, v = null;

            if (kv_cache == null || xa.IsNull() || !kv_cache.ContainsKey(this.key))
            {
                //hooks, if installed (i.e. kv_cache is not null), will prepend the cached kv tensors;
                // otherwise, perform key/value projections for this- or cross-attention as usual.
                //k = this.key.forward(xa.IsNull() ? x : xa);
                //v = this.value.forward(xa.IsNull() ? x : xa);

                k = this.key.call(xa.IsNull() ? x : xa);
                v = this.value.call(xa.IsNull() ? x : xa);
            }
            else
            {
                // for cross-attention, calculate keys and values once and reuse in subsequent calls.
                k = kv_cache[this.key];
                v = kv_cache[this.value];
            }

            var (wv, qk) = this.qkv_attention(q, k, v, mask);

            var result = (this.@out.forward(wv), qk);

            if (this.post_hooks.Count > 0)
            {
                foreach (var hook in this.post_hooks.Values)
                {
                    hook(this, result.Item1, result.Item2);
                }
            }

            return result;
        }

        public (Tensor, Tensor) qkv_attention(Tensor q, Tensor k, Tensor v, Tensor mask = null)
        {
            long n_batch = q.shape[0];
            long n_ctx = q.shape[1];
            long n_state = q.shape[2];

            var scale = Math.Pow(Math.Floor(n_state * 1.0 / this.n_head), -0.25);
            q = q.view(Enumerable.Concat(q.shape.Take(2), [this.n_head, -1]).ToArray()).permute(0, 2, 1, 3);
            k = k.view(Enumerable.Concat(k.shape.Take(2), [this.n_head, -1]).ToArray()).permute(0, 2, 1, 3);
            v = v.view(Enumerable.Concat(v.shape.Take(2), [this.n_head, -1]).ToArray()).permute(0, 2, 1, 3);

            Tensor _out = null;
            Tensor qk = null;

            if (model.SDPA_AVAILABLE && MultiHeadAttention.use_sdpa)
            {
                if (k.shape[0] == 1 && q.shape[0] != 1)
                {
                    // Cross-attention K/V have batch 1 and broadcast against the
                    // beam-expanded query; the fused SDPA kernels reject the batch
                    // mismatch and fall back to math, so expand K/V to a stride-0
                    // view (no copy) to keep them on the fast path.

                    k = k.expand(Enumerable.Concat(q.shape.Take(1), k.shape.Skip(1)).ToArray());
                    v = v.expand(Enumerable.Concat(v.shape.Take(1), v.shape.Skip(1)).ToArray());
                }

                var a = nn.functional.scaled_dot_product_attention(q, k, v, is_causal: (mask is not null && n_ctx > 1));
                _out = a.permute(0, 2, 1, 3).flatten(2);
            }
            else
            {
                qk = torch.matmul((q * scale), (k * scale).transpose(-1, -2));

                if (!mask.IsNull())
                {
                    qk = qk + mask[..(int)n_ctx, ..(int)n_ctx];
                }

                qk = qk.@float();

                var w = nn.functional.softmax(qk, -1).to(q.dtype);

                _out = torch.matmul(w, v).permute(0, 2, 1, 3).flatten(2);
                qk = qk.detach();
            }

            return (_out, qk);
        }

        public override Tensor forward(Tensor input)
        {
            return null;
        }
    }
}
