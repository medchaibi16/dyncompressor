using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dyncompressor
{
    public interface ICompressionAlgorithm
    {
        byte[] Compress(byte[] input);
        byte[] Decompress(byte[] input);
        string Name { get; }
    }
}
