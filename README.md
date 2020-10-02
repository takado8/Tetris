# Tetris
AI for playing tetris.

Every possible move is simulated and four heuristics are used for gamefield evaluation:
1. Height - summed height of the columns.
2. Complete lines - number of completed lines.
3. Holes - number of 'holes' - empty inaccessible space.
4. Bumpiness - summed difference of adjacent columns height.

each value has a multiplier (successively a, b, c, d), so the complete equation looks like this:

field_value = Height * a + Complete lines * b + Holes * c + Bumpiness + d

multipliers values are searched for with genetic algorithm.

After few hours the following set of number has been found:

    *a = -0.798752914564018
    *b = 0.522287506868767
    *c = -0.24921408023878
    *d = -0.164626498034284

Created AI manage to clean over 2000 lines.

![img1](/imgs/tetris1.jpg)
