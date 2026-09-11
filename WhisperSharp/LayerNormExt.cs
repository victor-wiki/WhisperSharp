using TorchSharp.Modules;
using static TorchSharp.torch;

namespace WhisperSharp
{
    public class LayerNormExt: LayerNorm
    {
        public LayerNormExt(long[] normalized_shape, double eps = 1e-5, bool elementwise_affine = true, bool bias = true, Device? device = null, ScalarType? dtype = null) : 
            base(normalized_shape, eps, elementwise_affine, bias, device, dtype)
        {

        }

        public override Tensor forward(Tensor x)
        {
            return base.forward(x.@float()).type(x.dtype);
        }
    }
}
