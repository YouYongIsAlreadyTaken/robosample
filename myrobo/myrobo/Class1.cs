using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;
using Robocode.Util;

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

    public class Operations
    {
        public int Direction { get; set; }
        public double? BulletPower { get; set; }
        public double? Ahead { get; set; }
        public double? MaxVelocity { get; set; }
        public double? TurnRightRadians { get; set; }
        
        public double? TurnGunRightRadians { get; set; }
        public double? TurnRadarRightRadians { get; set; }
        


    }
    public class MyRobot : AdvancedRobot
    {
        private ScannedRobotEvent lastScannedRobotEvent;
        private Operations operations = new Operations() { Direction = 1, BulletPower = 3};
        private Random rnd= new Random(DateTime.Now.Millisecond);
        public override void Run()
        {
            // -- Initialization of the robot --
            IsAdjustGunForRobotTurn = true;
            IsAdjustRadarForGunTurn = true;
            SetTurnRadarRightRadians(double.MaxValue);
            

            // Infinite loop making sure this robot runs till the end of the battle round
            //while (true)
            //{
            //    // -- Commands that are repeated forever --
            //}
        }

        // Robot event handler, when the robot sees another robot
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            var calculatedParams = new CalculatedParams(this, e);
            double turn = calculatedParams.AbsoluteBearing+ Math.PI / 2;
            turn -= Math.Max(0.5, (1 / e.Distance) * 100) * operations.Direction;
            operations.TurnRightRadians = Utils.NormalRelativeAngle(turn - HeadingRadians);

            if (lastScannedRobotEvent != null && lastScannedRobotEvent.Energy > e.Energy)
            {
                EnemyFired(e, operations);
            }

            //This line makes us slow down when we need to turn sharply.
            operations.MaxVelocity = 400 / TurnRemaining;

            operations.Ahead = 100*operations.Direction;
            if (operations.BulletPower.HasValue)
            {
                operations.TurnGunRightRadians =
                    Utils.NormalRelativeAngle(
                        GetCircularTargeting(e, lastScannedRobotEvent != null ? lastScannedRobotEvent.HeadingRadians : 0,
                            operations.BulletPower.Value, calculatedParams) - GunHeadingRadians);
            }
            operations.TurnRadarRightRadians = Utils.NormalRelativeAngle(calculatedParams.AbsoluteBearing - RadarHeadingRadians)*2;


            ApplyOperations(operations);

            lastScannedRobotEvent = e;
        }

        protected void ApplyOperations(Operations op)
        {
            if (op.Ahead.HasValue)
            {
                SetAhead(op.Ahead.Value);
            }
            if (op.MaxVelocity.HasValue)
            {
                MaxVelocity = op.MaxVelocity.Value;
            }
            if (op.TurnRightRadians.HasValue)
            {
                SetTurnRightRadians(op.TurnRightRadians.Value);
            }
            if (op.TurnGunRightRadians.HasValue)
            {
                SetTurnGunRightRadians(op.TurnGunRightRadians.Value);
            }
            if (op.TurnRadarRightRadians.HasValue)
            {
                SetTurnRadarRightRadians(op.TurnRadarRightRadians.Value);
            }
            if (op.BulletPower.HasValue)
            {
                SetFire(op.BulletPower.Value);
            }
        }

        private void EnemyFired(ScannedRobotEvent e, Operations op)
        {
            if (rnd.NextDouble() > 200 / e.Distance)
            {
                op.Direction = -op.Direction;
            }
        }
        

        private double GetBulletSpeed(double power)
        {
            return 20 - 3*power;
        }
        private double GetCircularTargeting(ScannedRobotEvent e, double lastHeadingRadians, double power, CalculatedParams calculated)
        {
            //Finding the heading and heading change.
            double enemyHeading = e.HeadingRadians;
            double enemyHeadingChange = enemyHeading - lastHeadingRadians;
       

            /*This method of targeting is know as circular targeting; you assume your enemy will
             *keep moving with the same speed and turn rate that he is using at fire time.The 
             *base code comes from the wiki.
            */
            double deltaTime = 0;
            double predictedX = X + e.Distance * Math.Sin(calculated.AbsoluteBearing);
            double predictedY = Y + e.Distance * Math.Cos(calculated.AbsoluteBearing);
            double speed = GetBulletSpeed(power);
            while ((++deltaTime) * speed < Math.Sqrt(Math.Pow(X-predictedX,2)+ Math.Pow(Y-predictedY,2))) 
            {

                //Add the movement we think our enemy will make to our enemy's current X and Y
                predictedX += Math.Sin(enemyHeading) * e.Velocity;
                predictedY += Math.Cos(enemyHeading) * e.Velocity;


                //Find our enemy's heading changes.
                enemyHeading += enemyHeadingChange;

                //If our predicted coordinates are outside the walls, put them 18 distance units away from the walls as we know 
                //that that is the closest they can get to the wall (Bots are non-rotating 36*36 squares).
                predictedX = Math.Max(Math.Min(predictedX, BattleFieldWidth - 18), 18);
                predictedY = Math.Max(Math.Min(predictedY, BattleFieldHeight - 18), 18);

            }
            //Find the bearing of our predicted coordinates from us.
            return Utils.NormalAbsoluteAngle(Math.Atan2(predictedX - X, predictedY - Y));
        }
        public override void OnBulletHit(BulletHitEvent evnt)
        {
            
            base.OnBulletHit(evnt);
        }

        public override void OnBulletMissed(BulletMissedEvent evnt)
        {
            base.OnBulletMissed(evnt);
        }

        public override void OnHitByBullet(HitByBulletEvent evnt)
        {
            
            base.OnHitByBullet(evnt);
        }

        public override void OnHitRobot(HitRobotEvent evnt)
        {
            base.OnHitRobot(evnt);
        }

        public override void OnHitWall(HitWallEvent evnt)
        {
            operations.Direction = -operations.Direction; 
            base.OnHitWall(evnt);
        }
    }
}
