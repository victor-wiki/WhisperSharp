using TorchSharp;
using TorchSharp.Modules;
using WhisperSharp.Extension;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class Conv1dExt : Conv1d
    {

        public Conv1dExt(long in_channels, long out_channels, long kernel_size, long stride = 1, long? padding = 0, Padding? padding_type = null, long dilation = 1, long groups = 1, bool bias = true, PaddingModes padding_mode = PaddingModes.Zeros, torch.Device? device = null, ScalarType? dtype = null)
        : base(in_channels, out_channels, kernel_size, stride, padding, padding_type, dilation, groups, bias, padding_mode)
        {
        }

        public override Tensor conv_forward(Tensor x, Tensor weight, Tensor? bias)
        {
            return base.conv_forward(
             x, weight.to(x.dtype),
             !bias.IsNull() ? bias.to(x.dtype) : null
            );
        }
    }


}
