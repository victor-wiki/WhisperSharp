using NumSharp;
using TorchSharp;
using WhisperSharp.Extension;

namespace WhisperSharp.Filter
{
    public class SuppressBlank : LogitFilter
    {
        private Tokenizer tokenizer;
        private int sample_begin;

        public SuppressBlank(Tokenizer tokenizer, int sample_begin)
        {
            this.tokenizer = tokenizer;
            this.sample_begin = sample_begin;
        }

        public void apply(torch.Tensor logits, torch.Tensor tokens)
        {
            if (tokens.shape[1] == this.sample_begin)
            {
                var array = Enumerable.Concat(this.tokenizer.encode(" ").Select(item=>(long)item).ToArray(), new long[] { this.tokenizer.eot.Value }).ToArray();

                logits.UpdateEachRowValue(array, -np.inf);
            }
        }
    }
}
