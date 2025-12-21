using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dyncompressor
{
    public class CompressedChunk
    {
        public string MethodName { get; set; }
        public byte[] CompressedData { get; set; }
    }

}
