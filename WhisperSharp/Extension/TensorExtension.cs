using NumSharp;
using TorchSharp;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static TorchSharp.torch;
using TensorStringStyle = TorchSharp.TensorStringStyle;

namespace WhisperSharp.Extension
{
    public static class TensorExtension
    {
        public static bool IsNull(this Tensor tensor)
        {
            return tensor is null;
        }

        public static string ToNumpyString(this Tensor tensor)
        {
            return tensor.ToString(TensorStringStyle.Numpy);
        }

        public static Tensor GetStepOnes(this Tensor tensor, int step)
        {
            Tensor indices = torch.arange(0, tensor.shape[0], step, dtype: torch.int32);
            Tensor result = tensor.index_select(0, indices);

            return result;
        }

        public static void UpdateEachRowValue(this Tensor tensor, long[] columnIndices, double value = double.NegativeInfinity)
        {
            var indices = new TensorIndex[]
            {
                TensorIndex.Colon,                  // for first tensor dimension (row)
                TensorIndex.Tensor(columnIndices)   // for second tensor dimension (column)
            };

            tensor.index_put_(value, indices);
        }
    }
}
