namespace WhisperSharp
{
    public class ReadOnlyMemoryByteComparer : IEqualityComparer<ReadOnlyMemory<byte>>
    {
        public bool Equals(ReadOnlyMemory<byte> x, ReadOnlyMemory<byte> y)
            => x.Span.SequenceEqual(y.Span);

        public int GetHashCode(ReadOnlyMemory<byte> obj)
        {
            var span = obj.Span;
            var hash = new HashCode();
            foreach (var b in span)
            {

                hash.Add(b);
            }

            return hash.ToHashCode();
        }
    }
}
