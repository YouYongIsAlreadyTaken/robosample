using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myrobo.Handlers;
using Robocode;
using Robocode.Util;

namespace myrobo.Robots
{
    public class MaxRisk : AdvancedRobot
    {

        static double expectedHeading;

        public override void Run()
        {
            // Set colors
            SetColors(Color.Blue, Color.Blue, Color.Black);

            IsAdjustGunForRobotTurn = true;
            IsAdjustRadarForGunTurn = true;
            do
            {
                TurnRadarRightRadians(double.MaxValue);
            } while (true);
        }

        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            double absoluteBearing = HeadingRadians + e.BearingRadians;
            PointF myLocation = new PointF((float)X, (float)Y);
            PointF predictedLocation = projectMotion(myLocation, absoluteBearing, e.Distance);
            double predictedHeading;
            double enemyTurnRate = -Utils.NormalRelativeAngle(expectedHeading - (predictedHeading = expectedHeading = e.HeadingRadians));
            double bulletPower = Math.Min(e.Distance < 250 ? 500 / e.Distance : 200 / e.Distance, 3);

            int time = 0;
            while ((time++) * (20 - (3 * bulletPower)) < myLocation.Distance(predictedLocation) - 18)
            {
                predictedHeading += (enemyTurnRate / 3);
                predictedLocation = projectMotion(predictedLocation, predictedHeading, e.Velocity);

                if (!new RectangleF(18, 18, 764, 564).Contains(predictedLocation))
                {
                    break;
                }
            }

            double maxValue = Double.MinValue;
            double angle = 0;
            do
            {
                double value = Math.Abs(Math.Cos(Utils.NormalRelativeAngle(absoluteBearing - angle))) * e.Distance / 150;
                PointF testedLocation = projectMotion(myLocation, angle, 8);

                value -= testedLocation.Distance(predictedLocation);
                value -= testedLocation.Distance(new PointF(400, 300)) / 3;

                if (!new RectangleF(30, 30, 740, 540).Contains(testedLocation))
                {
                    value -= 10000;
                }

                if (value > maxValue)
                {
                    maxValue = value;
                    double turnAngle = angle - HeadingRadians;
                    SetAhead(Math.Cos(turnAngle) > 0 ? 100 : -100);
                    SetTurnRightRadians(Math.Tan(turnAngle));
                }
            } while ((angle += 0.01) < Math.PI * 2);

            MaxVelocity = (Math.Abs(TurnRemaining) < 30 ? 8 : 8 - Math.Abs(TurnRemaining / 30));

            double gunTurn = Utils.NormalRelativeAngle(Math.Atan2(predictedLocation.X - myLocation.X, predictedLocation.Y - myLocation.Y) - GunHeadingRadians);
            SetTurnGunRightRadians(gunTurn);

            if ((Math.Abs(gunTurn) < Math.PI / 9) && (GunHeat == 0.0))
            {
                SetFire(bulletPower);
            }

            SetTurnRadarRightRadians(2 * Utils.NormalRelativeAngle(absoluteBearing - RadarHeadingRadians));
        }

        static PointF projectMotion(PointF location, double heading, double distance)
        {
            return new PointF((float)(location.X + distance * Math.Sin(heading)), (float)(location.Y + distance * Math.Cos(heading)));
        }
    }
}
