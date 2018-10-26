using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Framefield.Tooll.Utils
{
    internal class SpecialOperators
    {
        public static readonly Guid ANNOTATION = Guid.Parse("{e65cc223-c1cf-4b68-9d79-d6356e6546a4}");

        public List<Guid> RelevantOps = new List<Guid>() {
            ANNOTATION,
        };
    }
}