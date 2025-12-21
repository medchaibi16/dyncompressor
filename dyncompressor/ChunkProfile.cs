using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace dyncompressor
{
    public class ChunkProfile
    {
        public bool IsImage { get; set; }
        public bool IsText { get; set; }
        public double Entropy { get; set; }
        public bool HasRepeats { get; set; }
    }

}
