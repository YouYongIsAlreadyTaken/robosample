using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;
using Robocode.Util;

namespace myrobo.Handlers
{
    public class RamMinRisk : IHandleScanedRobot
    {
        static double expectedHeading;

        public Confidence Evaluate(AdvancedRobot robot, ScannedRobotEvent e, BattleEvents battleEvents)
        {
            Confidence confidence = Confidence.DonotBlameMeIfILoose;
            if (e.Distance < 200)
            {
                if (robot.Energy > e.Energy)
                {
                    confidence = Confidence.DesignedForThis;
                }
                else
                {
                    confidence = Confidence.CanHandleIt;
                }

            }
            else
            {
                if (robot.Energy > e.Energy)
                {
                    confidence = Confidence.TryMe;
                }
                else
                {
                    confidence = Confidence.DonotBlameMeIfILoose;
                }
            }

            return confidence;
        }

        public Operations HandleScanedRobot(AdvancedRobot robot, ScannedRobotEvent e, ScannedRobotEvent previousScaned,
            Operations operations, BattleEvents battleEvents)
        {
            var newOperations = operations.Clone();

            double absoluteBearing = robot.HeadingRadians + e.BearingRadians;
            PointF myLocation = new PointF((float)robot.X, (float)robot.Y);
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
                    double turnAngle = angle - robot.HeadingRadians;
                    newOperations.Ahead = (Math.Cos(turnAngle) > 0 ? 100 : -100);
                    newOperations.TurnRightRadians = (Math.Tan(turnAngle));
                }
            } while ((angle += 0.01) < Math.PI * 2);

            robot.MaxVelocity = (Math.Abs(robot.TurnRemaining) < 30 ? 8 : 8 - Math.Abs(robot.TurnRemaining / 30));

            double gunTurn = Utils.NormalRelativeAngle(Math.Atan2(predictedLocation.X - myLocation.X, predictedLocation.Y - myLocation.Y) - robot.GunHeadingRadians);
            newOperations.TurnGunRightRadians = (gunTurn);

            if ((Math.Abs(gunTurn) < Math.PI / 9) && (robot.GunHeat == 0.0))
            {
                newOperations.BulletPower = (bulletPower);
            }

            newOperations.TurnRadarRightRadians = (2 * Utils.NormalRelativeAngle(absoluteBearing - robot.RadarHeadingRadians));

            return newOperations;
        }

        public void OnBulletHit(BulletHitEvent evnt)
        {
            
        }

        static PointF projectMotion(PointF location, double heading, double distance)
        {
            return new PointF((float)(location.X + distance * Math.Sin(heading)), (float)(location.Y + distance * Math.Cos(heading)));
        }
    }
}
