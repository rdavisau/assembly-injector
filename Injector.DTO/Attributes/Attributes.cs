using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Injector.DTO.Attributes
{
    public class InjectorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class Watch : InjectorAttribute
    {
        public TimeSpan Interval = TimeSpan.FromSeconds(1);
    }
}
