using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace Tetris
{
    class Genetics
    {
        public List<AI> population = new List<AI>();
        List<AI> offspring = new List<AI>();

        const int count = 16;
        const double mutation_rate = 0.05;
        const double reproduction_rate = 0.5;

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

        void new_generation()
        {
            population.Sort((y, x) => x.fitness.CompareTo(y.fitness));
            offspring.Add(mutate(crossing_over(population[0], population[1]))); // elitism
            while (offspring.Count < reproduction_rate * count)
            {
                // select random 10%
                List<AI> temp = new List<AI>();
                for (int i = 0; i < 0.1 * count; i++)
                {
                    temp.Add(population[Rand.Next(population.Count)]);
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
            return c;
        }

        AI mutate(AI c)
        {
            if (Rand.NextDouble() < mutation_rate)
            {
                c[Rand.Next(c.genotype.Count)] += Rand.NextDouble(-0.2, 0.2); //  +/-0.2
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
            foreach (var v in population)
            {
                av_fitness += v.fitness;
            }
            av_fitness /= population.Count;
            string dir_name = "generations/" + DateTime.Now.ToString("dd-MM-yyyy HH.mm") + "-" + av_fitness;

            if (!Directory.Exists(dir_name))
            {
                Directory.CreateDirectory(dir_name);
            }
            for (int i = 0; i < population.Count; i++)
            {
                StreamWriter sw = new StreamWriter(dir_name + "/" + i + "-" + population[i].fitness + ".txt", append: true);
                foreach (var value in population[i].genotype)
                {
                    sw.WriteLine(value.ToString());
                }
                sw.Close();
            }
        }
        public void read_population()
        {
            string dir_path = "read population";
            if (Directory.Exists(dir_path))
            {
                int counter = 0;
                Regex rex = new Regex("[0-9]+-(.+).txt");
                foreach (var path in Directory.GetFiles(dir_path))
                {
                    
                    
                    AI temp = new AI(false);
                    var maches = rex.Match(path);
                    temp.fitness = double.Parse(maches.Groups[1].Value);
                    using (StreamReader sr = new StreamReader(path))
                    {
                        string line;
                        while((line = sr.ReadLine()) != null)
                        {
                            temp.Add(double.Parse(line));
                        }
                    }
                    population.Add(temp);
                    if (++counter == count)
                    {
                        break;
                    }
                }
                Console.WriteLine("population count: " + population.Count);
            }
        }
    }
}
