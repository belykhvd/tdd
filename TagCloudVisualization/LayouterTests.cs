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
        private static readonly Random RandomGenerator = new Random();

        private static Size GetRandomSize()
        {
            var randomWidth = RandomGenerator.Next(30, 100);
            var randomHeight = RandomGenerator.Next(10, 40);
            return new Size(randomWidth, randomHeight);
        }

        private static IEnumerable<Rectangle> GenerateLayout(int rectanglesCount)
        {                        
            for (var i = 0; i < rectanglesCount; i++)
            {
                var randomSize = GetRandomSize();
                var nextRectangle = Layouter.PutNextRectangle(randomSize);
                yield return nextRectangle;                
            }            
        }

        private static readonly Point CloudCenter = new Point(350, 350);
        private static readonly CircularCloudLayouter Layouter = new CircularCloudLayouter(CloudCenter);
        private static readonly List<Rectangle> Layout = GenerateLayout(120).ToList();
        
        [TearDown]
        public void TearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
                return;

            var testMethodName = TestContext.CurrentContext.Test.MethodName;
            var rectanglesCount = (int) TestContext.CurrentContext.Test.Arguments[0];
            var cloudFilename = $"{testMethodName}.{rectanglesCount}.bmp";
            var cloudDirectory = TestContext.CurrentContext.WorkDirectory;
            
            SaveLayoutImage(cloudFilename, GetLayoutSegment(rectanglesCount));
            TestContext.WriteLine($"Tag cloud visualization saved to file {cloudDirectory}\\{cloudFilename}");
        }

        private static void SaveLayoutImage(string filename, IEnumerable<Rectangle> rectangles)
        {           
            var bitmap = new Bitmap(700, 700);
            var graphics = Graphics.FromImage(bitmap);
            graphics.FillRectangle(Brushes.Black, new Rectangle(0, 0, 700, 700));
            graphics.DrawRectangles(new Pen(Color.DarkBlue), rectangles.ToArray());
            bitmap.Save(filename);
        }

        [TestCase(0, 1, TestName = "Zero side length")]
        [TestCase(-1, -1, TestName = "Negative side length")]
        public void InvalidRectangleSizeTest(int width, int height)
        {
            Assert.Throws<ArgumentException>(() => Layouter.PutNextRectangle(new Size(width, height)));
        }

        [Test]
        public void InvalidCenterPointCoordinates()
        {
            Assert.Throws<ArgumentException>(() => new CircularCloudLayouter(new Point(-1, -1)));
        }
        
        [Test]
        public void NoIntersectionsTests()
        {
            var assertionResult = Enumerable
                .Range(1, Layout.Count - 1)
                .All(i => !Layout[i].IntersectsWith(Layout[i - 1]));
            Assert.True(assertionResult);
        }

        private static readonly object[] CommonTestRectanglesCounts = {10, 50, 70, 120};

        private static IEnumerable<TestCaseData> CommonTestCases()
        {
            foreach (var count in CommonTestRectanglesCounts)
            {
                var testCase = new TestCaseData(count);
                testCase.SetName($"{count} rectangles");
                yield return testCase;
            }                        
        }
        
        [TestCaseSource(nameof(CommonTestCases))]
        public void CloudLooksLikeCircleTest(int rectanglesCount)
        {            
            var cloudRadius = (int) GetCloudRadius(rectanglesCount);
            var cloudCircleArea = GetCloudCircleArea(rectanglesCount);
            var cloudCirclePartArea = cloudCircleArea / 4;

            var cloudPartsRectangles = new[]
            {
                new Rectangle(CloudCenter.X - cloudRadius, CloudCenter.Y - cloudRadius, cloudRadius, cloudRadius),
                new Rectangle(CloudCenter.X, CloudCenter.Y - cloudRadius, cloudRadius, cloudRadius),
                new Rectangle(CloudCenter.X - cloudRadius, CloudCenter.Y, cloudRadius, cloudRadius),
                new Rectangle(CloudCenter.X, CloudCenter.Y, cloudRadius, cloudRadius)
            };
            
            var layoutSegment = GetLayoutSegment(rectanglesCount).ToArray();
            var cloudPartsAreas = Enumerable
                .Range(0, 4)
                .Select(i => layoutSegment.Sum(r => GetIntersectionArea(r, cloudPartsRectangles[i])))
                .ToArray();            
            var areasRatios = cloudPartsAreas.Select(area => area / cloudCirclePartArea).ToArray();

            TestContext.WriteLine("Areas ratios: " + string.Join(" ", areasRatios));

            var assertionResult = areasRatios.All(ratio => ratio >= 0.25);            
            Assert.True(assertionResult);
        }

        [TestCaseSource(nameof(CommonTestCases))]
        public void DensityTest(int rectanglesCount)
        {            
            var cloudCircleArea = GetCloudCircleArea(rectanglesCount);
            var cloudArea = GetLayoutSegment(rectanglesCount)
                .Sum(rectangle => rectangle.Width * rectangle.Height);
            var areasRatio = cloudArea / cloudCircleArea;

            TestContext.WriteLine("Areas ratio: " + areasRatio);

            var assertionResult = areasRatio >= 0.3;            
            Assert.True(assertionResult);
        }

        private static double GetIntersectionArea(Rectangle first, Rectangle second)
        {
            var firstCopy = new Rectangle(first.Location, first.Size);
            firstCopy.Intersect(second);
            return firstCopy.Width * firstCopy.Height;
        }

        private static double GetCloudCircleArea(int rectanglesCount)
        {
            var cloudRadius = GetCloudRadius(rectanglesCount);
            return Math.PI * cloudRadius * cloudRadius;
        }

        private static double GetCloudRadius(int rectanglesCount)
        {            
            return GetLayoutSegment(rectanglesCount)
                .SelectMany(GetRectanglePoints)
                .Max(point => GetDistance(point, CloudCenter));
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

        private static IEnumerable<Rectangle> GetLayoutSegment(int rectanglesCount)
        {
            return Enumerable
                .Range(0, rectanglesCount)
                .Select(i => Layout[i]);
        }
    }
}
