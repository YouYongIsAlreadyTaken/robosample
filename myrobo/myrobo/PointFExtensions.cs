using System;
using System.Drawing;

namespace myrobo
{
    public static class PointFExtensions
    {
        public static float Distance(this PointF p1, PointF p2)
        {
            float a = p1.X - p2.X;
            float b = p1.Y - p2.Y;
            float distance = (float)Math.Sqrt(a * a + b * b);
            return distance;
        }
    }
}