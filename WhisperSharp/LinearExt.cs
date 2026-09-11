using TorchSharp.Modules;
using WhisperSharp.Extension;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class LinearExt: Linear
    {
        public LinearExt(Parameter weight, Parameter? bias = null):base(weight, bias)
        {

        }

        public LinearExt(long inputSize, long outputSize, bool hasBias = true, Device? device = null, ScalarType? dtype = null)
        :base(inputSize, outputSize, hasBias, device, dtype)
        {

        }


        public Tensor forward(Tensor x)
        {
            return TorchSharp.torch.nn.functional.linear(
                x,
                this.weight.to(x.dtype),
                this.bias.IsNull() ? null : this.bias.to(x.dtype)
            );
        }
    }
}
