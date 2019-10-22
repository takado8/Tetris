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
        List<Tetrimino> static_tetriminos = new List<Tetrimino>();
        Tetrimino falling_tetrimino;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            new_tetrimino();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 300);
            timer.Tick += Timer_Tick;
            timer.IsEnabled = true;
        }
        int motion_step_set = 13;
        int motion_step = 0;

        private void Timer_Tick(object sender, EventArgs e)
        {
            //if (motion_step++ == 0)
            //{
            var bottom = Canvas.GetTop(falling_tetrimino.boxes[3].rect);
            if (bottom >= canvas.Height - Tetrimino.Box.size) // bottom of map
            {
                static_tetriminos.Add(falling_tetrimino);
                new_tetrimino();
                //return;
            }
            foreach (var box in falling_tetrimino.boxes)
            {
                var point = new Point(Canvas.GetTop(box.rect) + Tetrimino.Box.size,
                       Canvas.GetLeft(box.rect));
                if (obstacle_in_way(box, 0))
                {
                    static_tetriminos.Add(falling_tetrimino);
                    new_tetrimino();
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

        bool obstacle_in_way(Tetrimino.Box box, int dir)
        {
            var top = Canvas.GetTop(box.rect);
            var left = Canvas.GetLeft(box.rect);
            double delta_left = 0;
            double delta_top = 0;
            if (dir == 0) //down
            {
                delta_left = 0.00001;
                delta_top = Tetrimino.Box.size + 0.00001;
            }
            else if (dir == 1) //left
            {
                delta_left = -0.00001;
                delta_top = 0.00001;
            }
            else if (dir == 2) //right
            {
                delta_top = 0.00001;
                delta_left = Tetrimino.Box.size + 0.00001;
            }
            var point = new Point(left + delta_left, top + delta_top);
            var hit = canvas.InputHitTest(point);
            try
            {
                var ee = (Rectangle)hit;
                if ((int)ee.Tag != (int)box.rect.Tag)// && Canvas.GetTop(ee) - 26 == top)
                {
                    //MessageBox.Show(hit.ToString() + "  tag1: " + ee.Tag + "  tag2: " + box.rect.Tag);
                    return true;
                }
            }
            catch { }
            return false;
        }

        void check_win()
        {
            Dictionary<double, List<Tetrimino.Box>> dict = new Dictionary<double, List<Tetrimino.Box>>();
            for (int i = 0; i < 20; i++)
            {
                dict.Add(i * Tetrimino.Box.size, new List<Tetrimino.Box>());
            }
            foreach (var tetrimino in static_tetriminos)
            {
                foreach (var box in tetrimino.boxes)
                {
                    dict[Canvas.GetTop(box.rect)].Add(box);
                }
            }
            foreach(var boxes in dict)
            {
                if(boxes.Value.Count == 10)
                {
                    var threshold = Canvas.GetTop(boxes.Value[0].rect);
                    foreach(var box in boxes.Value)
                    {
                        canvas.Children.Remove(box.rect);
                    }
                    foreach(var tetrimino in static_tetriminos)
                    {
                        foreach(var box in tetrimino.boxes)
                        {
                            var top = Canvas.GetTop(box.rect) + Tetrimino.Box.size;
                            if (top < canvas.Height && top < threshold)
                            {
                                box.rect.SetValue(Canvas.TopProperty, top);
                            }
                        }
                    }
                }
            }
        }
        void new_tetrimino()
        {
            if(static_tetriminos.Count > 0)
            {
                check_win();
            }
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
                timer.Interval = new TimeSpan(0, 0, 0, 0, 60);
                speed_key = true;
            }
            else if (e.Key == Key.Right)
            {
                int max_right_index;
                switch (falling_tetrimino.shape)
                {
                    case 'T':
                    case 'L': max_right_index = 2; break;
                    case 'S': max_right_index = 1; break;
                    default: max_right_index = 3; break;
                }
                var max_right = falling_tetrimino.boxes[max_right_index];
                var left = Canvas.GetLeft(max_right.rect);
                if (left < canvas.Width - Tetrimino.Box.size)
                {
                    foreach (var box in falling_tetrimino.boxes)
                    {
                        if (obstacle_in_way(box, 2)) return;
                    }
                    foreach (var box in falling_tetrimino.boxes)
                    {
                        var current = Canvas.GetLeft(box.rect);
                        current += Tetrimino.Box.size;
                        box.rect.SetValue(Canvas.LeftProperty, current);
                    }
                }
            }
            else if (e.Key == Key.Left)
            {
                int max_left_index = 0;
                if (falling_tetrimino.shape == 'S')
                {
                    max_left_index = 2;
                }
                else
                {
                    max_left_index = 0;
                }
                if (Canvas.GetLeft(falling_tetrimino.boxes[max_left_index].rect) > 0)
                {
                    foreach (var box in falling_tetrimino.boxes)
                    {
                        if (obstacle_in_way(box, 1)) return;
                    }
                    foreach (var box in falling_tetrimino.boxes)
                    {
                        var current = Canvas.GetLeft(box.rect);
                        current -= Tetrimino.Box.size;
                        box.rect.SetValue(Canvas.LeftProperty, current);
                    }
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
    }
}
