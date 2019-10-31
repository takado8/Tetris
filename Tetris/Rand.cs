using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    class Rand
    {
        public static Random r = new Random();

        public static int Next(int max)
        {
            return r.Next(max);
        }

        public static double NextDouble(double min, double max)
        {
            return r.NextDouble() * (max - min) + min;
        }

        public static double NextDouble()
        {
            return r.NextDouble();
        }
    }
}
