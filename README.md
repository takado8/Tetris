# Tetris
Tetris environment and AI for playing it.

## AI algorithm:

In each turn every possible move is simulated and evaluated using four heuristics:
1. Height - summed height of the columns.
2. Complete_lines - number of completed lines.
3. Holes - number of 'holes' - empty inaccessible spaces.
4. Bumpiness - summed difference of adjacent columns height.

each value has a multiplier (successively a, b, c, d), so the complete equation is as follows:
```python
    field_value = Height * a + Complete_lines * b + Holes * c + Bumpiness * d
```
multipliers values are searched for with genetic algorithm.

## Genetic algorithm

General purpose of genetic algorithms is to maximize (or minimize) a function - here called fitness function, through simplified evolutionary mechanisms observed in natural environment, such as natural selection and genetic variation.

In this tetris environment, a goal is to maximize number of cleaned lines, using field evaluation formula from above.
On start, set of vectors is initialized, each vector [a,b,c,d] has 4 floats (called genes) in range <-1;1>. Those vectors are called genotypes, and
all together makes a population.

Natural selection is based on competition - those with best fitness to environment are more likely to survive and reproduce.
Each subject (genotype) will play one tetris game, using the values of its genes substituted in the field evaluation formula. fitness function is simple: +1 point for 1 cleaned line untill gameover.
After the trial the weaker half of the population is removed.
Now those with better fitness will reproduce. 10% of population is randomly selected, than best two will reproduce.
Process repeats untill the population will be restored.
Population is now tested again and whole cycle repeats.

Genetic variability is provided by the crossing-over process and mutation.
In this project, crossing-over process refers to creating a new genotype from existing two, by summing corresponding genes weighted towards more fitted parrent and normalizing derived vector.
```python
    AI crossing_over(AI a, AI b)
    {
        AI c = new AI();
        for (int i = 0; i < a.genotype.Count; i++)
        {
            c.Add(a[i] * a.fitness + b[i] * b.fitness);
        }
        c.normalize();
        return c;
    }
```
A mutation is a random addition to a gene value in the range <-0.2; 0.2>, which appears rather rarely (5% in this project).
```python
    AI mutate(AI c)
    {
        if (Rand.NextDouble() < mutation_rate)
        {
            c[Rand.Next(c.genotype.Count)] += Rand.NextDouble(-0.2, 0.2);
            c.normalize();
        }
        return c;
    }
```

## Results

After several hours the following set of genes has been found:
```python
    a = -0.798752914564018
    b = 0.522287506868767
    c = -0.24921408023878
    d = -0.164626498034284
```
Created AI manage to clean over 2000 lines in one game.

![img1](/imgs/tetris1.jpg)
