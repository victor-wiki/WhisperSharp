using static TorchSharp.torch;

namespace WhisperSharp
{
    public interface SequenceRanker
    {
        /// <summary>
        /// Given a list of groups of samples and their cumulative log probabilities,
        /// return the indices of the samples in each group to select as the final result
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="sum_logprobs"></param>
        /// <returns></returns>
        List<int> rank(List<List<Tensor>> tokens, List<List<float>> sum_logprobs);
    }
}
