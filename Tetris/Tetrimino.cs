﻿using System;
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
        static char[] shapes = { 'I', 'O', 'T', 'L', 'J', 'S', 'Z' };
        //static List<char> available_shapes = new List<char>(shapes);
        public char shape;
        static int index = 7;
        public List<Box> boxes;
        static int id = 0;
        public int position = 0;
        static Random r = new Random();

        public Tetrimino()
        {
            //if (available_shapes.Count == 0)
            //{
            //    for (int i = 0; i < 7; i++)
            //    {
            //        available_shapes.Add(shapes[i]);
            //    }
            //    //for (int i = 0; i < 1; i++)
            //    //{
            //    //    available_shapes.Add(shapes[r.Next(5)]);
            //    //}
            //}
            //// random shape
            //int index = r.Next(available_shapes.Count);
            //shape = available_shapes[index];
            //available_shapes.RemoveAt(index);
            
            if(index == shapes.Length)
            {
                Shuffle();
                index = 0;
            }
            shape = shapes[index++];
            build_tetrimino();
        }

        public void Shuffle()
        {
            int n = shapes.Length;
            while (n > 1)
            {
                n--;
                int k = r.Next(n + 1);
                var value = shapes[k];
                shapes[k] = shapes[n];
                shapes[n] = value;
            }
            if (shapes[0] == 'S' || shapes[0] == 'Z' || shapes[0] == 'O')
            {
                int i = 0;
                do
                {
                    i++;
                } while (shapes[i] == 'S' || shapes[i] == 'Z' || shapes[i] == 'O');
                char temp = shapes[0];
                shapes[0] = shapes[i];
                shapes[i] = temp;
            }
        }

        void build_tetrimino()
        {
            boxes = new List<Box>();
            int tetr_tag = id++;
            Box box;
            Brush color;
            if (shape == 'I')
            {
                color = Brushes.IndianRed;
                for (int i = 0; i < 4; i++)
                {
                    box = new Box(color, tetr_tag, 0, 3 * Box.size + (i * Box.size));
                    boxes.Add(box);
                }
            }
            else if (shape == 'T')
            {
                color = Brushes.SlateGray;
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
                color = Brushes.MediumTurquoise;
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
                color = Brushes.DarkGoldenrod;
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
                color = Brushes.MediumPurple;
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
                color = Brushes.CornflowerBlue;
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
                color = Brushes.ForestGreen;
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
            public const int size = 26;
            public Brush color = Brushes.ForestGreen;
            public Rectangle rect;
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
