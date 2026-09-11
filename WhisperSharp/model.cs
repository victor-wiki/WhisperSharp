using TorchSharp;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class model
    {
        private static bool prev_state { get; set; }
        public static bool SDPA_AVAILABLE { get; set; } = true;        

        /// <summary>
        /// Returns sinusoids for positional embedding
        /// </summary>
        /// <param name="length"></param>
        /// <param name=""></param>
        /// <param name="max_timescale"></param>
        public static Tensor sinusoids(long length, long channels, int max_timescale = 10000)
        {
            var log_timescale_increment = Math.Log(max_timescale) / (Math.Floor(channels / 2.0) - 1);
            var inv_timescales = torch.exp(-log_timescale_increment * torch.arange(Math.Floor(channels / 2.0)));
            var scaled_time = torch.arange(length).unsqueeze(1) * inv_timescales.unsqueeze(0);

            return torch.cat([torch.sin(scaled_time), torch.cos(scaled_time)], 1);
        }    
    }
}
