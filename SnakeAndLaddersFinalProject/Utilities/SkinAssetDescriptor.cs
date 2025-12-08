using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SnakeAndLaddersFinalProject.Utilities
{
    public sealed class SkinAssetDescriptor
    {
        public string SkinKey { get; }
        public string TokenKey { get; }
        public string IdleKey { get; }
        public string SadKey { get; }

        public SkinAssetDescriptor(string skinKey, string tokenKey, string idleKey, string sadKey)
        {
            SkinKey = skinKey;
            TokenKey = tokenKey;
            IdleKey = idleKey;
            SadKey = sadKey;
        }
    }
}
