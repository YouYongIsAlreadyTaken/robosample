using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myrobo
{
    class Utility
    {
        public static double GetBulletSpeed(double power)
        {
            return 20 - 3 * power;
        }
    }
}
