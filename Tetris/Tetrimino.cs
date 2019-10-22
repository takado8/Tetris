using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Tetris
{
    class Tetrimino
    {
        public Brush color = Brushes.ForestGreen;
        static char[] shapes = { 'I', 'T', 'O', 'L', 'J', 'S', 'Z' };
        public char shape;
        public List<Box> boxes;
        static int id = 0;
        public int tag;
        static Random r = new Random();

        public Tetrimino()
        {
            // random shape
            shape = shapes[r.Next(7)];
            build_tetrimino();
        }

        void build_tetrimino()
        {
            boxes = new List<Box>();
            int tetr_tag = id++;
            Box box;
            if (shape == 'I')
            {
                for (int i = 0; i < 4; i++)
                {
                    box = new Box(color, tetr_tag, 0, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
            }
            else if (shape == 'T')
            {
                for (int i = 0; i < 3; i++)
                {
                    box = new Box(color, tetr_tag, 0, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
                box = new Box(color, tetr_tag, Box.size, 4 * Box.size);
                boxes.Add(box);
            }
            else if (shape == 'O')
            {
                for (int i = 0; i < 2; i++)
                {
                    box = new Box(color, tetr_tag, 0, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                    box = new Box(color, tetr_tag, Box.size, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
            }
            else if (shape == 'L')
            {
                for (int i = 0; i < 3; i++)
                {
                    box = new Box(color, tetr_tag, 0, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
                box = new Box(color, tetr_tag, Box.size, 3 * Box.size);
                boxes.Add(box);
            }
            else if (shape == 'J')
            {         
                box = new Box(color, tetr_tag, 0, 3 * Box.size);
                boxes.Add(box);
                for (int i = 0; i < 3; i++)
                {
                    box = new Box(color, tetr_tag, Box.size, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }          
            }
            else if (shape == 'S')
            {
                for (int i = 0; i < 2; i++)
                {
                    box = new Box(color, tetr_tag, 0, 4 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
                for (int i = 0; i < 2; i++)
                {
                    box = new Box(color, tetr_tag, Box.size, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
            }
            else if (shape == 'Z')
            {
                for (int i = 0; i < 2; i++)
                {
                    box = new Box(color, tetr_tag, 0, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
                for (int i = 0; i < 2; i++)
                {
                    box = new Box(color, tetr_tag, Box.size, 4 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
            }
        }

        public class Box
        {
            public const uint size = 26;
            public Brush color = Brushes.ForestGreen;
            public Rectangle rect;
            public int id;
            public Box(Brush color, int _id, double top = 0.0, double left = 78.0)
            {
                rect = new Rectangle();
                rect.Tag = _id;
                rect.Stroke = Brushes.Black;
                rect.StrokeThickness = 0.1;
                rect.Height = size;
                rect.Width = size;
                rect.Fill = color;
                rect.SetValue(Canvas.TopProperty, top);
                rect.SetValue(Canvas.LeftProperty, left);
            }
        }


    }
}
