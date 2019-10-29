using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tetris
{
    class Genetics
    {
        static Random r = new Random();
        public List<AI> population = new List<AI>();
        public List<AI> offspring = new List<AI>();
        public const int count = 100;
        const double mutation_rate = 0.05;
        const double reproduction_rate = 0.4;

        public Genetics()
        {

        }

        public void evolve()
        {
            eliminate_weakest();
            new_generation();

            for (int i = 0; i < offspring.Count; i++)
            {
                population.Add(offspring[i]);
            }
            offspring.Clear();
        }

        public void init_random_population()
        {
            for (int i = 0; i < count; i++)
            {
                population.Add(new AI());
            }
        }

        public void new_generation()
        {
            population.Sort((y, x) => x.fitness.CompareTo(y.fitness));
            offspring.Add(mutate(crossing_over(population[0], population[1]))); // elitism
            while(offspring.Count < reproduction_rate * count)
            {
                // select random 10%
                List<AI> temp = new List<AI>();
                for (int i = 0; i < 0.1 * count; i++)
                {
                    temp.Add(population[r.Next(population.Count)]);
                }

                temp.Sort((y, x) => x.fitness.CompareTo(y.fitness));

                // top 2 for crossing over
                var c = crossing_over(temp[0], temp[1]);
                c = mutate(c);
                offspring.Add(c);
            }
        }

        AI crossing_over(AI a, AI b)
        {
            AI c = new AI(false);
            for (int i = 0; i < a.genotype.Count; i++)
            {
                c.Add(a[i] * a.fitness + b[i] * b.fitness);
            }
            c.normalize();
            //c.fitness = (a.fitness + b.fitness) / 2; ////////////////yuyuuyyuyuy
            return c;
        }

        AI mutate(AI c)
        {
            if (r.NextDouble() < mutation_rate)
            {

                c[r.Next(c.genotype.Count)] += r.NextDouble() * 0.4 - 0.2; //  +/-0.2
                c.normalize();
            }
            return c;
        }

        void eliminate_weakest()
        {
            population.Sort((x, y) => x.fitness.CompareTo(y.fitness));
            while (population.Count > (1 - reproduction_rate) * count)
            {
                population.RemoveAt(0);
            }
        }

        public void save_population()
        {
            population.Sort((y, x) => x.fitness.CompareTo(y.fitness));

            double av_fitness = 0;
            foreach(var v in population)
            {
                av_fitness += v.fitness;
            }
            av_fitness /= population.Count;
            string dir_name = "generations/" + DateTime.Now.ToString("dd-MM-yyyy HH.mm") + "-"+av_fitness;

            if (!Directory.Exists(dir_name))
            {
                Directory.CreateDirectory(dir_name);
            }
            for (int i = 0; i < population.Count; i++)
            {
                StreamWriter sw = new StreamWriter(dir_name + "/" + i + "-" + population[i].fitness+".txt", append: true);
                foreach (var value in population[i].genotype)
                {
                    sw.WriteLine(value.ToString());
                }
                sw.Close();

            }
        }
    }
}
