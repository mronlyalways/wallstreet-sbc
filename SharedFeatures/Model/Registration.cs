using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedFeatures.Model
{
    [Serializable]
    public class Registration
    {
        public string Email { get; set; }

        public double Budget { get; set; }
    }
}
