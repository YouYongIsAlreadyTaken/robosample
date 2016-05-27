using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;
using Robocode.Util;

namespace myrobo.Handlers
{
    class Ram : IHandleScanedRobot
    {
        private Random rnd = new Random(DateTime.Now.Millisecond);

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

        public void OnBulletHit(BulletHitEvent evnt)
        {
            
        }

        public Operations HandleScanedRobot(AdvancedRobot robot, ScannedRobotEvent e, ScannedRobotEvent previousScaned, Operations operations, BattleEvents battleEvents)
        {
            var calculatedParams = new CalculatedParams(robot, e);
            var newOperations = operations.Clone();
            if (previousScaned != null && previousScaned.Energy > e.Energy)
            {
                OnEnemyFired(e, newOperations);
            }

            double turn = calculatedParams.AbsoluteBearing + Math.PI / 2;
            turn -= Math.Max(0.5, (1 / e.Distance) * 100) * newOperations.Direction;
            newOperations.TurnRightRadians = Utils.NormalRelativeAngle(turn - robot.HeadingRadians);

            //This line makes us slow down when we need to turn sharply.
            newOperations.MaxVelocity = 400 / robot.TurnRemaining;

            newOperations.Ahead = 100 * newOperations.Direction;
            if (newOperations.BulletPower.HasValue)
            {
                newOperations.TurnGunRightRadians =
                    Utils.NormalRelativeAngle(
                        GetCircularTargeting(robot, e, previousScaned != null ? previousScaned.HeadingRadians : 0,
                            newOperations.BulletPower.Value, calculatedParams) - robot.GunHeadingRadians);
            }
            newOperations.TurnRadarRightRadians = Utils.NormalRelativeAngle(calculatedParams.AbsoluteBearing - robot.RadarHeadingRadians) * 2;



            return newOperations;
        }
        private void OnEnemyFired(ScannedRobotEvent e, Operations op)
        {
            if (rnd.NextDouble() > 200 / e.Distance)
            {
                op.Direction = -op.Direction;
            }
        }

        private double GetCircularTargeting(AdvancedRobot robot, ScannedRobotEvent e, double lastHeadingRadians, double power, CalculatedParams calculated)
        {
            //Finding the heading and heading change.
            double enemyHeading = e.HeadingRadians;
            double enemyHeadingChange = enemyHeading - lastHeadingRadians;


            /*This method of targeting is know as circular targeting; you assume your enemy will
             *keep moving with the same speed and turn rate that he is using at fire time.The 
             *base code comes from the wiki.
            */
            double deltaTime = 0;
            double predictedX = robot.X + e.Distance * Math.Sin(calculated.AbsoluteBearing);
            double predictedY = robot.Y + e.Distance * Math.Cos(calculated.AbsoluteBearing);
            double speed = Utility.GetBulletSpeed(power);
            while ((++deltaTime) * speed < Math.Sqrt(Math.Pow(robot.X - predictedX, 2) + Math.Pow(robot.Y - predictedY, 2)))
            {

                //Add the movement we think our enemy will make to our enemy's current X and Y
                predictedX += Math.Sin(enemyHeading) * e.Velocity;
                predictedY += Math.Cos(enemyHeading) * e.Velocity;


                //Find our enemy's heading changes.
                enemyHeading += enemyHeadingChange;

                //If our predicted coordinates are outside the walls, put them 18 distance units away from the walls as we know 
                //that that is the closest they can get to the wall (Bots are non-rotating 36*36 squares).
                predictedX = Math.Max(Math.Min(predictedX, robot.BattleFieldWidth - 18), 18);
                predictedY = Math.Max(Math.Min(predictedY, robot.BattleFieldHeight - 18), 18);

            }
            //Find the bearing of our predicted coordinates from us.
            return Utils.NormalAbsoluteAngle(Math.Atan2(predictedX - robot.X, predictedY - robot.Y));
        }


    }
}
