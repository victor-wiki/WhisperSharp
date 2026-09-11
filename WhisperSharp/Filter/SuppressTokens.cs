using NumSharp;
using TorchSharp;
using WhisperSharp.Extension;

namespace WhisperSharp.Filter
{
    public class SuppressTokens : LogitFilter
    {
        private List<int> suppress_tokens;

        public SuppressTokens(List<int> suppress_tokens)
        {
            this.suppress_tokens = suppress_tokens;
        }

        public void apply(torch.Tensor logits, torch.Tensor tokens)
        {
            logits.UpdateEachRowValue(this.suppress_tokens.Select(item=>(long)item).ToArray(), -np.inf);
        }
    }
}
