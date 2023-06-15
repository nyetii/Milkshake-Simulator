using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Milkshake.Attributes;
using Milkshake.Models;

namespace Kimi.Services.Models
{
    [Instance]
    public class MilkshakeInstance : Instance
    {
        public ICollection<Servers> Servers { get; set; } = new List<Servers>();
    }
}
