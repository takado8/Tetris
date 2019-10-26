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
        int normal_speed = 300;
        int fast_speed = 40;
        int score = 0;
        int top_score = 0;

        public MainWindow()
        {
            InitializeComponent();
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
        int motion_step_set = 13;
        int motion_step = 0;

        private void Timer_Tick(object sender, EventArgs e)
        {
            //if (motion_step++ == 0)
            //{

            foreach (var box in falling_tetrimino.boxes)
            {
                var top = Canvas.GetTop(box.rect);
                var point = new Point(top + Tetrimino.Box.size,
                       Canvas.GetLeft(box.rect));
                if (obstacle_in_way(box, 0) || (top >= canvas.Height - Tetrimino.Box.size))
                {
                    drop();
                    return;
                }
            }



            // }

            //if (motion_step == motion_step_set)
            //{
            //    motion_step = 0;
            //}

            foreach (var box in falling_tetrimino.boxes)
            {
                var current = Canvas.GetTop(box.rect);
                current += Tetrimino.Box.size; // motion_step_set;
                box.rect.SetValue(Canvas.TopProperty, current);
            }
        }

        void drop()
        {
            foreach (var box in falling_tetrimino.boxes)
            {
                static_boxes[Canvas.GetTop(box.rect)].Add(box);
            }
            check_win();
            if (check_loose())
            {
                MessageBox.Show("Gamover!");
                if (score > top_score)
                {
                    top_score = score;
                    label_top.Content = "Top: " + top_score;
                }
                score = 0;
                canvas.Children.Clear();
                static_boxes.Clear();
                init_dict();
            }
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
                delta_top = -(range * Tetrimino.Box.size + 0.00001);
                delta_left = 0.00001;
            }
            var new_left = left + delta_left;
            var new_top = top + delta_top;
            if (new_top > canvas.Height ||
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
                        foreach (var b in static_boxes[(double)i])
                        {
                            static_boxes[(double)(i + Tetrimino.Box.size)].Add(b);
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
        bool speed_key = false;
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (speed_key) return;
            if (e.Key == Key.Down) // speed
            {
                timer.Interval = new TimeSpan(0, 0, 0, 0, fast_speed);
                speed_key = true;
            }
            else if (e.Key == Key.Right)
            {
                foreach (var box in falling_tetrimino.boxes)
                {
                    var left = Canvas.GetLeft(box.rect);
                    if (obstacle_in_way(box, 2) ||
                            left >= canvas.Width - Tetrimino.Box.size) return;
                }
                foreach (var box in falling_tetrimino.boxes)
                {
                    var current = Canvas.GetLeft(box.rect);
                    current += Tetrimino.Box.size;
                    box.rect.SetValue(Canvas.LeftProperty, current);
                }
            }
            else if (e.Key == Key.Left)
            {
                foreach (var box in falling_tetrimino.boxes)
                {
                    if (obstacle_in_way(box, 1) || Canvas.GetLeft(box.rect) <= 0) return;
                }
                foreach (var box in falling_tetrimino.boxes)
                {
                    var current = Canvas.GetLeft(box.rect);
                    current -= Tetrimino.Box.size;
                    box.rect.SetValue(Canvas.LeftProperty, current);
                }
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
                timer.Interval = new TimeSpan(0, 0, 0, 0, 300);
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
