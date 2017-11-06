using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

namespace TagCloudVisualization
{
    internal class CircularCloudLayouter
    {
        private readonly List<Rectangle> layout = new List<Rectangle>();
        private double helixParameter;

        public readonly Point CenterCoordinates;
        
        public CircularCloudLayouter(Point center)
        {
            if (center.X < 0 || center.Y < 0)
                throw new ArgumentException("Coordinates of cloud center must be non-negative");

            CenterCoordinates = center;            
        }

        public Rectangle PutNextRectangle(Size rectangleSize)
        {
            if (rectangleSize.Width <= 0 || rectangleSize.Height <= 0)
                throw new ArgumentException("Invalid size of rectangle: sides lengths must be positive");

            Func<double, Point> helixEquasion = t =>
                new Point(CenterCoordinates.X + (int) (t * Math.Cos(t)),
                    CenterCoordinates.Y + (int) (t * Math.Sin(t)));

            Rectangle rectangle;

            while (true)
            {
                var rectangleCenter = helixEquasion(helixParameter);
                var rectangleLocation = GetCoordinatesByCenter(rectangleCenter, rectangleSize);
                rectangle = new Rectangle(rectangleLocation, rectangleSize);

                helixParameter += 0.5;

                if (!layout.Any(r => r.IntersectsWith(rectangle)))
                    break;                
            }

            layout.Add(rectangle);
            return rectangle;            
        }

        private static Point GetCoordinatesByCenter(Point rectangleCenter, Size rectangleSize)
            => new Point(rectangleCenter.X - rectangleSize.Width / 2, rectangleCenter.Y - rectangleSize.Height / 2);
    }
}
