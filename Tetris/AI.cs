using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    class AI
    {
        public double fitness;
        public List<double> genotype = new List<double>();

        public AI(bool rand_init = true)
        {
            if (rand_init)
            {
                random_init();
            }
        }

        public void random_init()
        {
            for (int i = 0; i < 4; i++)
            {
                genotype.Add(Rand.NextDouble(-1,1)); // random < -1;1 )
            }
            normalize();
        }

        public void normalize()
        {
            double module = 0;
            foreach (var value in genotype)
            {
                module += Math.Pow(value, 2);
            }
            module = Math.Sqrt(module);
            for (int i = 0; i < genotype.Count; i++)
            {
                genotype[i] /= module;
            }
        }

        public double this[int index]
        {
            get => genotype[index];
            set => genotype[index] = value;
        }

        public void Add(double element)
        {
            genotype.Add(element);
        }


    }
}
