using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace TagCloudVisualization
{
    internal class LayouterTests
    {        
        private static readonly Random randomGenerator = new Random();

        private static Size GetRandomSize()
        {
            var randomWidth = randomGenerator.Next(30, 100);
            var randomHeight = randomGenerator.Next(10, 40);
            return new Size(randomWidth, randomHeight);
        }

        private static IEnumerable<Rectangle> GenerateLayout(Point center, int rectanglesCount)
        {            
            var layouter = new CircularCloudLayouter(center);            
            for (var i = 0; i < rectanglesCount; i++)
            {
                var randomSize = GetRandomSize();
                var nextRectangle = layouter.PutNextRectangle(randomSize);
                yield return nextRectangle;                
            }            
        }

        private static readonly Point cloudCenter = new Point(350, 350);

        [TearDown]
        public static void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
                return;

            var cloudFilename = TestContext.CurrentContext.Test.MethodName + "." + TestContext.CurrentContext.Test.Arguments[0] + ".bmp";
            var cloudDirectory = TestContext.CurrentContext.WorkDirectory;
            TestContext.WriteLine($"Tag cloud visualization saved to file {cloudDirectory}\\{cloudFilename}");
        }

        [TestCase(10, TestName = "10 rectangles")]
        [TestCase(50, TestName = "50 rectangles")]
        [TestCase(70, TestName = "70 rectangles")]
        [TestCase(120, TestName = "120 rectangles")]
        public static void NoIntersectionsTests(int rectanglesCount)
        {
            var layout = new List<Rectangle>();            
            foreach (var nextRectangle in GenerateLayout(cloudCenter, rectanglesCount))
            {
                if (layout.Any(rectangle => rectangle.IntersectsWith(nextRectangle)))
                {
                    SaveLayoutImage($"{nameof(NoIntersectionsTests)}.{rectanglesCount}.bmp", layout);
                    Assert.Fail();
                }
                layout.Add(nextRectangle);
            }            
            Assert.Pass();            
        }

        private static void SaveLayoutImage(string filename, IEnumerable<Rectangle> rectangles)
        {
            var bitmap = new Bitmap(700, 700);
            var graphics = Graphics.FromImage(bitmap);
            graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, 700, 700));
            graphics.DrawRectangles(new Pen(Color.DarkBlue), rectangles.ToArray());
            bitmap.Save(filename);
        }

        [TestCase(10, TestName = "10 rectangles")]
        [TestCase(50, TestName = "50 rectangles")]
        [TestCase(70, TestName = "70 rectangles")]
        [TestCase(120, TestName = "120 rectangles")]
        public static void CloudLooksLikeCircleTest(int rectanglesCount)
        {
            var layout = GenerateLayout(cloudCenter, rectanglesCount).ToList();
            var cloudRadius = (int) GetCloudRadius(layout, cloudCenter);
            var cloudCircleArea = GetCloudCircleArea(layout, cloudCenter);
            var cloudCirclePartArea = cloudCircleArea / 4;
            var cloudPartsRectangles = new[]
            {
                new Rectangle(cloudCenter.X - cloudRadius, cloudCenter.Y - cloudRadius, cloudRadius, cloudRadius),
                new Rectangle(cloudCenter.X, cloudCenter.Y - cloudRadius, cloudRadius, cloudRadius),
                new Rectangle(cloudCenter.X - cloudRadius, cloudCenter.Y, cloudRadius, cloudRadius),
                new Rectangle(cloudCenter.X, cloudCenter.Y, cloudRadius, cloudRadius)
            };

            var cloudPartsAreas = new double[4];
            for (var i = 0; i < 4; i++)
            {
                foreach (var rectangle in layout)                
                    cloudPartsAreas[i] += GetIntersectionArea(rectangle, cloudPartsRectangles[i]);               
            }
            var areasRatios = cloudPartsAreas.Select(area => area / cloudCirclePartArea).ToArray();

            TestContext.WriteLine("Areas ratios: " + string.Join(" ", areasRatios));
            var assertionResult = areasRatios.All(ratio => ratio >= 0.25);
            if (!assertionResult)
                SaveLayoutImage($"{nameof(CloudLooksLikeCircleTest)}.{rectanglesCount}.bmp", layout);
            Assert.True(assertionResult);
        }

        private static double GetIntersectionArea(Rectangle first, Rectangle second)
        {
            var firstCopy = new Rectangle(first.Location, first.Size);
            firstCopy.Intersect(second);
            return firstCopy.Width * firstCopy.Height;
        }

        [TestCase(10, TestName = "10 rectangles")]
        [TestCase(50, TestName = "50 rectangles")]
        [TestCase(70, TestName = "70 rectangles")]
        [TestCase(120, TestName = "120 rectangles")]
        public static void DensityTest(int rectanglesCount)
        {            
            var layout = GenerateLayout(cloudCenter, rectanglesCount).ToList();
            var cloudCircleArea = GetCloudCircleArea(layout, cloudCenter);
            var cloudArea = layout.Sum(rectangle => rectangle.Width * rectangle.Height);
            var areasRatio = cloudArea / cloudCircleArea;

            TestContext.WriteLine("Areas ratio: " + areasRatio);
            var assertionResult = areasRatio >= 0.3;
            if (!assertionResult)
                SaveLayoutImage($"{nameof(DensityTest)}.{rectanglesCount}.bmp", layout);
            Assert.True(assertionResult);
        }

        private static double GetCloudCircleArea(IEnumerable<Rectangle> layout, Point center)
        {
            var cloudRadius = GetCloudRadius(layout, center);
            return Math.PI * cloudRadius * cloudRadius;
        }

        private static double GetCloudRadius(IEnumerable<Rectangle> layout, Point center)
        {
            return layout
                .SelectMany(GetRectanglePoints)
                .Max(point => GetDistance(point, center));
        }

        private static IEnumerable<Point> GetRectanglePoints(Rectangle rectangle)
        {
            return new List<Point>
            {
                new Point(rectangle.Left, rectangle.Top),
                new Point(rectangle.Left, rectangle.Bottom),
                new Point(rectangle.Right, rectangle.Top),
                new Point(rectangle.Right, rectangle.Bottom)
            };
        }

        private static double GetDistance(Point first, Point second)
        {
            var xDelta = first.X - second.X;
            var yDelta = first.Y - second.Y;
            return Math.Sqrt(xDelta * xDelta + yDelta * yDelta);
        }
    }
}
