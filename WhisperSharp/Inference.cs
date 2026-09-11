using static TorchSharp.torch;

namespace WhisperSharp
{
    public interface Inference
    {
        /// <summary>
        /// Perform a forward pass on the decoder and return per-token logits
        /// </summary>
        /// <param name="tokens"></param>
        /// <param name="audio_features"></param>
        /// <returns></returns>
        Tensor logits(Tensor tokens, Tensor audio_features);

        /// <summary>
        /// Update the key-value cache according to the updated beams
        /// </summary>
        /// <param name="source_indices"></param>
        void rearrange_kv_cache(List<int> source_indices);

        /// <summary>
        /// Clean up any resources or hooks after decoding is finished
        /// </summary>
        void cleanup_caching();
    }
}
