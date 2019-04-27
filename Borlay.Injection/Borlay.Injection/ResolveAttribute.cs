using System;
using System.Collections.Generic;
using System.Text;

namespace Borlay.Injection
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ResolveAttribute : Attribute
    {
        public bool Singletone { get; set; }

        public bool IncludeBase { get; set; }

        public Type AsType { get; set; }

        public int Priority { get; set; } = 0;

        public ResolveAttribute()
        {
            this.Singletone = false;
            this.IncludeBase = true;
            this.AsType = null;
        }
    }
}
