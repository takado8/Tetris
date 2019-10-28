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

namespace Tetris
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer timer = new DispatcherTimer();
        Dictionary<double, List<Tetrimino.Box>> static_boxes = new Dictionary<double, List<Tetrimino.Box>>();
        Tetrimino falling_tetrimino;
        int normal_speed = 30;
        int fast_speed = 30;
        int drop_speed = 3;
        int score = 0;
        int top_score = 0;
        double a = -0.510066;
        double b = 0.760666;
        double c = -0.35633-0.35;
        double d = -0.184483;

        bool speed_key = false;


        public MainWindow()
        {
            InitializeComponent();
        }

        void simulate()
        {
            Console.WriteLine("simulate START");
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
            //Console.WriteLine("simulate checkpoint 1");

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
                    var evaluation = evaluate_move();
                    dict[i].Add(evaluation);
                    //MessageBox.Show("");
                    // return to 0 state
                    while (move_up()) ;
                } while (move_right());
                //move_down();
                //move_down();
                move_left();
                rotate();
            }
            //Console.WriteLine("simulate checkpoint 2");
            // return to zero_state
            for (int i = 0; i < 8; i += 2)
            {
                falling_tetrimino.boxes[i / 2].rect.SetValue(Canvas.TopProperty, zero_state[i]);
                falling_tetrimino.boxes[i / 2].rect.SetValue(Canvas.LeftProperty, zero_state[i + 1]);
            }
            move_down();
            //Console.WriteLine("simulate checkpoint 3");
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
            //Console.WriteLine("simulate checkpoint 4");
            for (int i = 0; i < local_max.Count; i++)
            {
                if (local_max[i] > global_max)
                {
                    global_max = local_max[i];
                    global_max_index = i;
                }
            }
            //Console.WriteLine("simulate checkpoint 5");
            int safe_break = 5;
            do
            {
                rotate();
            } while (falling_tetrimino.position != global_max_index && safe_break-- > 0);
            while (move_left()) ;
            //Console.WriteLine("simulate checkpoint 6");
            for (int i = 0; i < local_max_index[global_max_index]; i++)
            {
                move_right();
            }
            //Console.WriteLine("simulate STOP");
        }

        double evaluate_move()
        {
            var hhb = height_holes_bumpiness();
            var lines = complete_lines();
            var height = hhb[0];
            var holes = hhb[1];
            var bumpiness = hhb[2];
            return a * height + b * lines + c * holes + d * bumpiness;
        }

        double[] height_holes_bumpiness()
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
            for (int i = 0; i < columns_height.Count - 1; i++)
            {
                columns_dif += Math.Abs(columns_height[i] - columns_height[i + 1]);
            }

            result[0] = sum;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            init_dict();
            new_tetrimino();
            timer.Interval = new TimeSpan(0, 0, 0, 0, normal_speed);
            timer.Tick += Timer_Tick;
            timer.IsEnabled = true;
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

        bool flag = false;
        private void Timer_Tick(object sender, EventArgs e)
        {
            try
            {
                if (!flag)
                {
                    simulate();
                    flag = true;
                }
                if (check_drop())
                {        
                    drop();
                    try
                    {
                        simulate();
                    }
                    catch
                    { MessageBox.Show("catch!"); }
                    return;
                }
                //move
                move_down();
            }
            catch
            {
                MessageBox.Show("catch big time");
            }
        }

        void drop()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                static_boxes[Canvas.GetTop(box.rect)].Add(box);
            }
            if (check_loose())
            {
                MessageBox.Show("Gamover!");
                if (score > top_score)
                {
                    top_score = score;
                    label_top.Content = "Top: " + top_score;
                }
                score = 0;
                label_score.Content = "Score: 0";
                canvas.Children.Clear();
                static_boxes.Clear();
                init_dict();
            }
            check_win();
            timer.Interval = new TimeSpan(0, 0, 0, 0, normal_speed);
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
                delta_left = 0.00001;
                delta_top = range * Tetrimino.Box.size + 0.00001;
            }
            else if (dir == 1) //left
            {
                delta_left = -0.00001 - (range - 1) * Tetrimino.Box.size;
                delta_top = 0.00001;
            }
            else if (dir == 2) //right
            {
                delta_top = 0.00001;
                delta_left = range * Tetrimino.Box.size + 0.00001;
            }
            else if (dir == 3) //up
            {
                delta_top = -((range - 1) * Tetrimino.Box.size + 0.00001);
                delta_left = 0.00001;
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
                var ee = (Rectangle)hit;
                if ((int)ee.Tag != (int)box.rect.Tag)
                {
                    return true;
                }
            }
            catch { }
            return false;
        }

        void check_win()
        {
            foreach (var box_list in static_boxes)
            {
                if (box_list.Value.Count == 10)
                {
                    score++;
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
            else if (e.Key == Key.S)
            {
                simulate();
            }
            else if (e.Key == Key.Escape)
            {
                canvas.Children.Clear();
                static_boxes.Clear();
                init_dict();
                new_tetrimino();
            }
            else if (e.Key == Key.H)
            {

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
