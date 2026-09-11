using NumSharp;
using TorchSharp;

namespace WhisperSharp
{
    public class MaximumLikelihoodRanker : SequenceRanker
    {
        private float? length_penalty;

        public MaximumLikelihoodRanker(float? length_penalty)
        {
            this.length_penalty = length_penalty;
        }

        public List<int> rank(List<List<torch.Tensor>> tokens, List<List<float>> sum_logprobs)
        {
            Func<List<float>, List<int>, List<int>> scores = (logprobs, lengths) =>
            {
                var result = new List<int>();

                for(var i =0; i<logprobs.Count;i++)
                {
                    var logprob = logprobs[i];
                    var length = lengths[i];

                    double penalty = 0;

                    if (this.length_penalty==null)
                    {
                        penalty = length;
                    }
                    else
                    {
                        penalty = Math.Pow(((5 + length) / 6.0), this.length_penalty.Value);
                    }

                    result.Add((int)(logprob / penalty));
                }

                return result;
            };

            //get the sequence with the highest score
            var lengths = tokens.Select(item=>item.Select(t=> (int)t.shape[0]).ToList()).ToList();

            List<int> list = new List<int>();

            for(var i=0;i< sum_logprobs.Count;i++)
            {
                var p= sum_logprobs[i];
                var l = lengths[i];

                list.Add((int) np.argmax(scores(p, l).ToArray()));
            }

            return list;
        }
    }
}
