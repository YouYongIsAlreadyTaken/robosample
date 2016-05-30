using System;
using Robocode;

namespace myrobo
{
    public class CalculatedParams
    {
        public double AbsoluteBearing { get; set; }
        public double LaterVelocity { get; set; }

        public CalculatedParams(AdvancedRobot robot, ScannedRobotEvent e)
        {
            AbsoluteBearing = e.BearingRadians + robot.HeadingRadians;
            LaterVelocity = e.Velocity * Math.Sin(e.HeadingRadians - AbsoluteBearing);
        }
    }
}
