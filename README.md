# Tetris
Tetris environment and AI for playing it.

## AI algorithm:

In each turn every possible move is simulated and four heuristics are used for gamefield evaluation:
1. Height - summed height of the columns.
2. Complete lines - number of completed lines.
3. Holes - number of 'holes' - empty inaccessible spaces.
4. Bumpiness - summed difference of adjacent columns height.

each value has a multiplier (successively a, b, c, d), so the complete equation looks like this:

    field_value = Height * a + Complete lines * b + Holes * c + Bumpiness * d

multipliers values are searched for with genetic algorithm.

## Genetic algorithm

Genral purpose of genetic algorithms is to maximize (or minimize) a function - here called fitness function, through simplified evolutionary mechanisms observed in natural environment, such as natural selection and genetic variation.

In this tetris environment, a goal is to maximize number of cleaned lines, using field evaluation formula from above.
On start, set of vectors is initialized, each vector [a,b,c,d] has 4 floats (called genes) in range <-1;1>. Those vectors are called genotypes, and
all together makes a population.
Now every subject (genotype) plays tetris game using their gene values substituted into the field evaluation formula.
fitness function is simple - +1 point for 1 cleaned line untill gameover.
After the trial, time for elimination - the weaker half of the population is removed.
Now those with better fitness will reproduce. 10% of population is randomly selected, than best two will reproduce.
Process repeats untill the population will be restored.
Population is now tested again, and whole cycle repeats.

After several hours the following set of number has been found:

    a = -0.798752914564018
    b = 0.522287506868767
    c = -0.24921408023878
    d = -0.164626498034284

Created AI manage to clean over 2000 lines in one game.

![img1](/imgs/tetris1.jpg)
