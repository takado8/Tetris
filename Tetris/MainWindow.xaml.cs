using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;

namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Genetics gen = new Genetics();

        int ai_index = 0;
        int tetrimino_limit = 500;
        int tetrimino_count = 1;
        int games_limit = 1;
        int games_count = 0;
        double total_score = 0;
        int generation = 1;

        DispatcherTimer timer = new DispatcherTimer();
        Dictionary<double, List<Tetrimino.Box>> static_boxes = new Dictionary<double, List<Tetrimino.Box>>();
        Tetrimino falling_tetrimino;

        const int normal_speed = 0;
        const int fast_speed = 100;
        const int drop_speed = 100;
        const bool genetic = false;
        const bool auto = true;

        double score = 0;
        int top_score = 0;
        
        bool speed_key = false;

        double a = -0.798752914564018;//-0.510066;  //accumulate height
        double b = 0.522287506868767;//0.760666;     //complete lines
        double c = -0.24921408023878;//-0.35633;   //holes
        double d = -0.164626498034284;//-0.184483;  //bumpiness

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            init_dict();
            new_tetrimino();

            if (genetic)
            {
                string dir_name = "read population";
                if (Directory.Exists(dir_name) && Directory.GetFiles(dir_name).Length > 0)
                {
                    gen.read_population();
                }
                else
                {
                    gen.init_random_population();
                }
                a = gen.population[ai_index][0];
                b = gen.population[ai_index][1];
                c = gen.population[ai_index][2];
                d = gen.population[ai_index][3];
            }
            if (auto) simulate();

            timer.Interval = new TimeSpan(0, 0, 0, 0, normal_speed);
            timer.Tick += Timer_Tick;
            timer.Start();
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (check_drop())
            {
                drop();
                if (auto) simulate();
                return;
            }
            //move
            move_down();
        }

        void simulate()
        {
            List<double> zero_state = new List<double>();
            List<double> moves_evaluation = new List<double>();
            Dictionary<int, List<double>> dict = new Dictionary<int, List<double>>();
            int states = 0;
            foreach (var box in falling_tetrimino.boxes)
            {
                var top = Canvas.GetTop(box.rect);
                var left = Canvas.GetLeft(box.rect);
                zero_state.Add(top);
                zero_state.Add(left);
            }
            switch (falling_tetrimino.shape)
            {
                case 'S':
                case 'Z':
                case 'I': states = 2; break;
                case 'O': states = 1; break;
                default: states = 4; break;
            }
            for (int i = 0; i < states; i++)
            {
                dict.Add(i, new List<double>());
            }

            for (int i = 0; i < states; i++)
            {
                while (move_left()) ;
                do
                {
                    while (!check_drop())
                    {
                        move_down();
                    }
                    // check state
                    canvas.UpdateLayout();
                    var evaluation = evaluate_move();
                    dict[i].Add(evaluation);

                    while (move_up()) ;
                } while (move_right());

                move_left();
                rotate();
            }
            // return to zero_state
            for (int i = 0; i < 8; i += 2)
            {
                falling_tetrimino.boxes[i / 2].rect.SetValue(Canvas.TopProperty, zero_state[i]);
                falling_tetrimino.boxes[i / 2].rect.SetValue(Canvas.LeftProperty, zero_state[i + 1]);
            }
            move_down();

            double global_max = double.MinValue;
            int global_max_index = 0;
            List<double> local_max = new List<double>();
            List<int> local_max_index = new List<int>();
            foreach (var list in dict)
            {
                double loc_max = double.MinValue;
                int loc_max_index = -1;
                for (int i = 0; i < list.Value.Count; i++)
                {
                    if (list.Value[i] > loc_max)
                    {
                        loc_max = list.Value[i];
                        loc_max_index = i;
                    }
                }
                local_max.Add(loc_max);
                local_max_index.Add(loc_max_index);
            }

            for (int i = 0; i < local_max.Count; i++)
            {
                if (local_max[i] > global_max)
                {
                    global_max = local_max[i];
                    global_max_index = i;
                }
            }
            int safe_break = 5;
            do
            {
                rotate();
            } while (falling_tetrimino.position != global_max_index && safe_break-- > 0);
            while (move_left()) ;

            for (int i = 0; i < local_max_index[global_max_index]; i++)
            {
                move_right();
            }
        }

        double evaluate_move()
        {
            var lines = complete_lines();
            var hhb = height_holes_bumpiness(lines);
            var height = hhb[0];
            var holes = hhb[1];
            var bumpiness = hhb[2];
            return a * height + b * lines + c * holes + d * bumpiness;
        }

        double[] height_holes_bumpiness(double compl_lines)
        {
            List<double> columns_height = new List<double>();
            double sum = 0;
            double min = canvas.Height;
            double holes = 0;
            double[] result = new double[3];
            Dictionary<double, List<Tetrimino.Box>> dict = new Dictionary<double, List<Tetrimino.Box>>();

            for (double i = 0; i < canvas.Width; i += Tetrimino.Box.size)
            {
                dict.Add(i, new List<Tetrimino.Box>());
            }
            foreach (var box in falling_tetrimino.boxes)
            {
                dict[Canvas.GetLeft(box.rect)].Add(box);
            }
            foreach (var list in static_boxes)
            {
                foreach (var box in list.Value)
                {
                    dict[Canvas.GetLeft(box.rect)].Add(box);
                }
            }
            foreach (var column in dict)
            {
                foreach (var box in column.Value)
                {
                    var top = Canvas.GetTop(box.rect);
                    if (top < min)
                    {
                        min = top;
                    }
                }
                min = (520 - min) / Tetrimino.Box.size;
                columns_height.Add(min);
                holes += min - column.Value.Count;
                sum += min;
                min = canvas.Height;
            }
            double columns_dif = 0;
            //if (columns_height[0] > max_column_h)
            //{
            //    max_column_h = columns_height[0];
            //}
            for (int i = 0; i < columns_height.Count - 1; i++)
            {
                //if (columns_height[i + 1] > max_column_h)
                //{
                //    max_column_h = columns_height[i + 1];
                //}
                columns_dif += Math.Abs(columns_height[i] - columns_height[i + 1]);
            }
            //total_h += sum;
            //n++;
            result[0] = sum - 10 * compl_lines;
            result[1] = holes;
            result[2] = columns_dif;
            return result;
        }

        double complete_lines()
        {
            double lines = 0;
            Dictionary<double, List<Tetrimino.Box>> dict =
                new Dictionary<double, List<Tetrimino.Box>>();
            foreach (var v in static_boxes)
            {
                dict.Add(v.Key, new List<Tetrimino.Box>());
                foreach (var box in v.Value)
                {
                    dict[v.Key].Add(box);
                }
            }
            foreach (var box in falling_tetrimino.boxes)
            {
                dict[Canvas.GetTop(box.rect)].Add(box);
            }
            foreach (var box_list in dict)
            {
                if (box_list.Value.Count == 10)
                {
                    lines++;
                }
            }
            return lines;
        }

        void init_dict()
        {
            for (int i = 0; i < 20; i++)
            {
                static_boxes.Add(i * Tetrimino.Box.size, new List<Tetrimino.Box>());
            }
        }

        bool check_drop()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                var top = Canvas.GetTop(box.rect);
                if (obstacle_in_way(box, 0))
                {
                    return true;
                }
            }
            return false;
        }

        void drop()
        {
            if (!auto)
            {
                timer.Interval = new TimeSpan(0, 0, 0, 0, normal_speed);
            }
            foreach (var box in falling_tetrimino.boxes)
            {
                static_boxes[Canvas.GetTop(box.rect)].Add(box);
            }
            label_tetr_count.Content = "Tetr: " + (tetrimino_count + 1);

            if (check_loose() || (tetrimino_count++ == tetrimino_limit && genetic)) // next game
            {
                if (!genetic)
                {
                    MessageBox.Show("Gamover!");
                }
                tetrimino_count = 1;

                label_game.Content = "Games: " + ++games_count;
                if (score > top_score)
                {
                    top_score = (int)score;
                    label_top.Content = "Top: " + top_score;
                }
                total_score += score;
                score = 0;
                label_score.Content = "Score: 0";
                canvas.Children.Clear();
                static_boxes.Clear();
                init_dict();
                if (games_count == games_limit && genetic)
                {
                    label_game.Content = "Games: 0";
                    games_count = 0;
                    gen.population[ai_index].fitness = total_score;                   
                    total_score = 0;               
                    ai_index++;
                    if (ai_index == gen.population.Count)
                    {
                        ai_index = 0;
                        generation++;
                        label_gen.Content = "Gen: " + generation;
                        gen.save_population();
                        gen.evolve();
                    }
                    a = gen.population[ai_index][0];
                    b = gen.population[ai_index][1];
                    c = gen.population[ai_index][2];
                    d = gen.population[ai_index][3];
                    label_index.Content = "AI: " + (ai_index + 1);
                }
            }
            check_win();
            new_tetrimino();
        }

        /// <param name="dir">0 down, 1 left, 2 right, 3 up</param>
        bool obstacle_in_way(Tetrimino.Box box, int dir, int range = 1)
        {
            var top = Canvas.GetTop(box.rect);
            var left = Canvas.GetLeft(box.rect);
            double delta_left = 0;
            double delta_top = 0;
            if (dir == 0) //down
            {
                delta_left = 1;
                delta_top = range * Tetrimino.Box.size + 1;
            }
            else if (dir == 1) //left
            {
                delta_left = -1 - (range - 1) * Tetrimino.Box.size;
                delta_top = 1;
            }
            else if (dir == 2) //right
            {
                delta_top = 1;
                delta_left = range * Tetrimino.Box.size + 1;
            }
            else if (dir == 3) //up
            {
                delta_top = -((range - 1) * Tetrimino.Box.size + 1);
                delta_left = 1;
            }
            var new_left = left + delta_left;
            var new_top = top + delta_top;
            if (new_top > canvas.Height ||
                new_top < 0 ||
                new_left > canvas.Width ||
                new_left < 0)
            {
                return true;
            }
            var point = new Point(new_left, new_top);
            var hit = canvas.InputHitTest(point);
            try
            {
                var _hit = (Border)hit;
                if ((int)_hit.Tag != (int)box.rect.Tag)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        void check_win()
        {
            double row_nr = 0;
            foreach (var box_list in static_boxes)
            {
                row_nr++;
                if (box_list.Value.Count == 10)
                {
                    score += 1;// * (row_nr / 10);  // bonus for low rows
                   
                    label_score.Content = "Score: " + score;
                    foreach (var box in box_list.Value)
                    {
                        canvas.Children.Remove(box.rect);
                    }
                    box_list.Value.Clear();

                    double range = box_list.Key - Tetrimino.Box.size;
                    for (double i = range; i > 0; i -= Tetrimino.Box.size)
                    {
                        foreach (var box in static_boxes[i])
                        {
                            box.rect.SetValue(Canvas.TopProperty, i + Tetrimino.Box.size);
                        }
                        static_boxes[(i + Tetrimino.Box.size)].Clear();
                        foreach (var b in static_boxes[i])
                        {
                            static_boxes[(i + Tetrimino.Box.size)].Add(b);
                        }
                    }
                }
            }
        }

        bool check_loose()
        {
            if (static_boxes[0].Count > 0) return true;
            else return false;
        }

        void new_tetrimino()
        {
            falling_tetrimino = new Tetrimino();
            foreach (var box in falling_tetrimino.boxes)
            {
                canvas.Children.Add(box.rect);
            }
        }

        void move_down()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                var current = Canvas.GetTop(box.rect);
                current += Tetrimino.Box.size;
                box.rect.SetValue(Canvas.TopProperty, current);
            }
        }

        bool move_up()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                if (obstacle_in_way(box, 3, 3)) return false;
            }
            foreach (var box in falling_tetrimino.boxes)
            {
                var current = Canvas.GetTop(box.rect);
                current -= Tetrimino.Box.size;
                box.rect.SetValue(Canvas.TopProperty, current);
            }
            return true;
        }

        bool move_right()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                var left = Canvas.GetLeft(box.rect);
                if (obstacle_in_way(box, 2)) return false;
            }

            foreach (var box in falling_tetrimino.boxes)
            {
                var current = Canvas.GetLeft(box.rect);
                current += Tetrimino.Box.size;
                box.rect.SetValue(Canvas.LeftProperty, current);
            }
            return true;
        }

        bool move_left()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                if (obstacle_in_way(box, 1)) return false;
            }
            foreach (var box in falling_tetrimino.boxes)
            {
                var current = Canvas.GetLeft(box.rect);
                current -= Tetrimino.Box.size;
                box.rect.SetValue(Canvas.LeftProperty, current);
            }
            return true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (speed_key) return;
            if (e.Key == Key.Down)
            {
                timer.Interval = new TimeSpan(0, 0, 0, 0, fast_speed);
                speed_key = true;
            }
            else if (e.Key == Key.Space)
            {
                timer.Interval = new TimeSpan(0, 0, 0, 0, drop_speed);
            }
            else if (e.Key == Key.Right)
            {
                move_right();
            }
            else if (e.Key == Key.Left)
            {
                move_left();
            }
            else if (e.Key == Key.Up)
            {
                rotate();
            }
            else if (e.Key == Key.P)
            {
                if (timer.IsEnabled)
                {
                    timer.IsEnabled = false;
                    label_pause.Visibility = Visibility.Visible;
                }
                else
                {
                    timer.IsEnabled = true;
                    label_pause.Visibility = Visibility.Hidden;
                }
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Down)
            {
                timer.Interval = new TimeSpan(0, 0, 0, 0, normal_speed);
                speed_key = false;
            }
        }

        public void rotate()
        {
            if (falling_tetrimino.shape == 'I')
            {
                double top = Canvas.GetTop(falling_tetrimino.boxes[1].rect);
                double left = Canvas.GetLeft(falling_tetrimino.boxes[1].rect);
                var box = falling_tetrimino.boxes[1];
                if (falling_tetrimino.position == 0)
                {
                    if (!obstacle_in_way(box, 0) && !obstacle_in_way(box, 0, 2)
                            && !obstacle_in_way(box, 3))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        for (int i = 2; i < 4; i++)
                        {
                            falling_tetrimino.boxes[i].rect.SetValue(Canvas.TopProperty, top + (i - 1) * Tetrimino.Box.size);
                            falling_tetrimino.boxes[i].rect.SetValue(Canvas.LeftProperty, left);
                        }
                        falling_tetrimino.position = 1;
                    }
                }
                else
                {
                    if (!obstacle_in_way(box, 1) && !obstacle_in_way(box, 2)
                            && !obstacle_in_way(box, 2, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        for (int i = 2; i < 4; i++)
                        {
                            falling_tetrimino.boxes[i].rect.SetValue(Canvas.TopProperty, top);
                            falling_tetrimino.boxes[i].rect.SetValue(Canvas.LeftProperty, left + (i - 1) * Tetrimino.Box.size);
                        }
                        falling_tetrimino.position = 0;
                    }
                }
            }
            else if (falling_tetrimino.shape == 'T')
            {
                var top = Canvas.GetTop(falling_tetrimino.boxes[1].rect);
                var left = Canvas.GetLeft(falling_tetrimino.boxes[1].rect);
                var box = falling_tetrimino.boxes[1];
                if (falling_tetrimino.position == 0)
                {
                    if (!obstacle_in_way(box, 3))
                    {
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.position = 1;
                    }
                }
                else if (falling_tetrimino.position == 1)
                {
                    if (!obstacle_in_way(box, 2))
                    {
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.position = 2;
                    }
                }
                else if (falling_tetrimino.position == 2)
                {
                    if (!obstacle_in_way(box, 0))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.position = 3;
                    }
                }
                else if (falling_tetrimino.position == 3)
                {
                    if (!obstacle_in_way(box, 1))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.position = 0;
                    }
                }
            }
            else if (falling_tetrimino.shape == 'L')
            {
                var top = Canvas.GetTop(falling_tetrimino.boxes[1].rect);
                var left = Canvas.GetLeft(falling_tetrimino.boxes[1].rect);
                var box0 = falling_tetrimino.boxes[0];
                var box1 = falling_tetrimino.boxes[1];
                if (falling_tetrimino.position == 0)
                {
                    if (!obstacle_in_way(box0, 3) && !obstacle_in_way(box1, 3)
                        && !obstacle_in_way(box1, 0))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.position = 1;
                    }
                }
                else if (falling_tetrimino.position == 1)
                {
                    if (!obstacle_in_way(box0, 2) && !obstacle_in_way(box1, 2)
                        && !obstacle_in_way(box1, 1))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.position = 2;
                    }
                }
                else if (falling_tetrimino.position == 2)
                {
                    if (!obstacle_in_way(box0, 0) && !obstacle_in_way(box1, 0)
                        && !obstacle_in_way(box1, 3))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.position = 3;
                    }
                }
                else if (falling_tetrimino.position == 3)
                {
                    if (!obstacle_in_way(box0, 1) && !obstacle_in_way(box1, 1)
                        && !obstacle_in_way(box1, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.position = 0;
                    }
                }
            }
            else if (falling_tetrimino.shape == 'J')
            {
                var top = Canvas.GetTop(falling_tetrimino.boxes[1].rect);
                var left = Canvas.GetLeft(falling_tetrimino.boxes[1].rect);
                var box1 = falling_tetrimino.boxes[1];
                if (falling_tetrimino.position == 0)
                {
                    if (!obstacle_in_way(box1, 0)
                        && !obstacle_in_way(box1, 0, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + 2 * Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.position = 1;
                    }
                }
                else if (falling_tetrimino.position == 1)
                {
                    if (!obstacle_in_way(box1, 1)
                        && !obstacle_in_way(box1, 1, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left - 2 * Tetrimino.Box.size);
                        falling_tetrimino.position = 2;
                    }
                }
                else if (falling_tetrimino.position == 2)
                {
                    if (!obstacle_in_way(box1, 3)
                        && !obstacle_in_way(box1, 3, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top - 2 * Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.position = 3;
                    }
                }
                else if (falling_tetrimino.position == 3)
                {
                    if (!obstacle_in_way(box1, 2)
                        && !obstacle_in_way(box1, 2, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left + 2 * Tetrimino.Box.size);
                        falling_tetrimino.position = 0;
                    }
                }
            }
            else if (falling_tetrimino.shape == 'S')
            {
                var top = Canvas.GetTop(falling_tetrimino.boxes[0].rect);
                var left = Canvas.GetLeft(falling_tetrimino.boxes[0].rect);
                var box1 = falling_tetrimino.boxes[2];
                if (falling_tetrimino.position == 0)
                {
                    if (!obstacle_in_way(box1, 3)
                        && !obstacle_in_way(box1, 3, 2))
                    {
                        falling_tetrimino.boxes[1].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[1].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.position = 1;
                    }
                }
                else if (falling_tetrimino.position == 1)
                {
                    if (!obstacle_in_way(box1, 2)
                        && !obstacle_in_way(box1, 2, 2) && !obstacle_in_way(box1, 0, 2))
                    {
                        falling_tetrimino.boxes[1].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[1].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.position = 0;
                    }
                }
            }
            else if (falling_tetrimino.shape == 'Z')
            {
                var top = Canvas.GetTop(falling_tetrimino.boxes[1].rect);
                var left = Canvas.GetLeft(falling_tetrimino.boxes[1].rect);
                var box2 = falling_tetrimino.boxes[2];
                var box3 = falling_tetrimino.boxes[3];
                if (falling_tetrimino.position == 0)
                {
                    if (!obstacle_in_way(box2, 1)
                        && !obstacle_in_way(box2, 3, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top - Tetrimino.Box.size);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.position = 1;
                    }
                }
                else if (falling_tetrimino.position == 1)
                {
                    if (!obstacle_in_way(box3, 2)
                        && !obstacle_in_way(box3, 2, 2))
                    {
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.TopProperty, top);
                        falling_tetrimino.boxes[0].rect.SetValue(Canvas.LeftProperty, left - Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[2].rect.SetValue(Canvas.LeftProperty, left);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.TopProperty, top + Tetrimino.Box.size);
                        falling_tetrimino.boxes[3].rect.SetValue(Canvas.LeftProperty, left + Tetrimino.Box.size);
                        falling_tetrimino.position = 0;
                    }
                }
            }
        }
    }
}
