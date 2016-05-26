using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using Robocode;
using Robocode.Util;

namespace myrobo.Handlers
{
    class Mercutio : IHandleScanedRobot
    {
        static double FIRE_POWER=1.5;
	    static double FIRE_SPEED=20-FIRE_POWER*3;
	    static double BULLET_DAMAGE=10;
	    /*
	     * change these statistics to see different graphics.
	     */
	    static bool PAINT_MOVEMENT=true;
	    static bool PAINT_GUN=false;
        List<MovementWave> moveWaves = new List<MovementWave>();
        List<GunWave> gunWaves = new List<GunWave>();
        static double [] gunAngles = new double[16];
        //Must need to set gunAngles length to 16?

        public Confidence Evaluate(AdvancedRobot robot, ScannedRobotEvent e, BattleEvents battleEvents)
        {
            Confidence confidence = Confidence.DonotBlameMeIfILoose;
            if (e.Distance > 200)
            {
                if (robot.Energy < e.Energy)
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
                if (robot.Energy < e.Energy)
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

        public Operations HandleScanedRobot(AdvancedRobot robot, ScannedRobotEvent e, ScannedRobotEvent previousScaned, Operations operations, BattleEvents battleEvents)
        {
            var calculatedParams = new CalculatedParams(robot, e);
            var newOperations = operations.Clone();
            
            double absBearing = e.BearingRadians + robot.HeadingRadians;
            if (previousScaned != null)
            {
                double energyChange = previousScaned.Energy - e.Energy;
                if (energyChange <= 3 && energyChange >= 0.1)
                {
                    logMovementWave(e, energyChange, absBearing, robot);
                }
            }
    

            /*
             * ==================Movement Section============================
             * This makes us put a log into our log when we notice the enemy firing. 
             * To see the actual logging of the wave, look at the logWave method.
             */
                
           
            /*
             * After we are done checking to see if we need to log any waves, we'll decide where to move.
             * To see this process take a peek at the chooseDirection method.
             */
            newOperations = chooseDirection(project(new PointF((float)robot.X, (float)robot.Y), e.Distance, absBearing),robot,newOperations);

            /*
             * logs a gun wave when we fire;
             */
            if (robot.GunHeat == 0)
            {
                logFiringWave(e,robot);
            }
            /*
             * This method checks our waves to see if they have reached the enemy yet.
             */
            checkFiringWaves(project(new PointF((float)robot.X, (float)robot.Y), e.Distance, absBearing));

            /*
             * Aiming our gun and firing
             */
            newOperations.TurnGunRightRadians = Utils.NormalRelativeAngle(absBearing - robot.GunHeadingRadians)
                    + gunAngles[8 + (int)(e.Velocity * Math.Sin(e.HeadingRadians - absBearing))];
            newOperations.BulletPower = FIRE_POWER;

            newOperations.TurnRadarRightRadians = Utils.NormalRelativeAngle(calculatedParams.AbsoluteBearing - robot.RadarHeadingRadians) * 2;
            return newOperations;

        }

        public void logMovementWave(ScannedRobotEvent e, double energyChange, double absBearing, AdvancedRobot robot)
        {
            MovementWave w = new MovementWave();
            //This is the spot that the enemy was in when they fired.
            w.origin = project(new PointF((float)robot.X, (float)robot.Y), e.Distance, absBearing);
            //20-3*bulletPower is the formula to find a bullet's speed.
            w.speed = 20 - 3 * energyChange;
            //The time at which the bullet was fired.
            w.startTime = GetUTCTime();
            //The absolute bearing from the enemy to us can be found by adding Pi to our absolute bearing.
            w.angle = Utils.NormalRelativeAngle(absBearing + Math.PI);
            /*
             * Our lateral velocity, used to calculate where a bullet fired with linear targeting would be.
             * Note that the speed has already been factored into the calculation.
             */
            w.latVel = (robot.Velocity * Math.Sin(robot.HeadingRadians - w.angle)) / w.speed;
            //This actually adds the wave to the list.
            moveWaves.Add(w);
        }

        public Operations chooseDirection(PointF enemyLocation, AdvancedRobot robot, Operations operations)
        {
            MovementWave w;
            //This for loop rates each angle individually
            double bestRating = Double.PositiveInfinity;
            for (double moveAngle = 0; moveAngle < Math.PI * 2; moveAngle += Math.PI / 16D)
            {
                double rating = 0;

                //Movepoint is position we would be at if we were to move one robot-length in the given direction. 
                PointF movePoint = project(new PointF((float)robot.X, (float)robot.Y), 36, moveAngle);

                /*
                 * This loop will iterate through each wave and add a risk for the simulated bullets on each one
                 * to the total risk for this angle.
                 */
                for (int i = 0; i < moveWaves.Count; i++)
                {
                    w = moveWaves[i];

                    double distance = GetDistanceBetweenPoints(new PointF((float)robot.X, (float)robot.Y),
                        w.origin);
                    //This part will remove waves that have passed our robot, so we no longer keep taking into account old ones
                    if (distance < (GetUTCTime() - w.startTime) * w.speed + w.speed)
                    {
                        moveWaves.Remove(w);
                    }
                    else
                    {
                        /*
                         * This adds two risks for each wave: one based on the distance from where a head-on targeting
                         * bullet would be, and one for where a linear targeting bullet would be.
                         */
                        rating += 1D / Math.Pow(GetDistanceBetweenPoints(movePoint, project(w.origin, GetDistanceBetweenPoints(movePoint, w.origin), w.angle)), 2);
                        rating += 1D / Math.Pow(GetDistanceBetweenPoints(movePoint, project(w.origin, GetDistanceBetweenPoints(movePoint, w.origin), w.angle + w.latVel)), 2);
                    }
                }
                //This adds a risk associated with being to close to the other robot if there are no waves.
                if (moveWaves.Count == 0)
                {
                    rating = 1D / Math.Pow(GetDistanceBetweenPoints(movePoint, enemyLocation), 2);
                }
                //This part tells us to go in the direction if it is better than the previous best option and is reachable.
                if (rating < bestRating && new RectangleF(50, 50, (float)robot.BattleFieldWidth - 100, (float)robot.BattleFieldHeight - 100).Contains(movePoint))
                {
                    bestRating = rating;
                    /*
                     * These next three lines are a very codesize-efficient way to 
                     * choose the best direction for moving to a point.
                     */
                    int pointDir;
                    operations.Ahead = 1000 * (pointDir = (Math.Abs(moveAngle - robot.HeadingRadians) < Math.PI / 2 ? 1 : -1));
                    operations.TurnRightRadians = Utils.NormalRelativeAngle(moveAngle + (pointDir == -1 ? Math.PI : 0) - robot.HeadingRadians);
                }
            }
            return operations;
        }

        /*
        * This method will log a firing wave.
        */
        public void logFiringWave(ScannedRobotEvent e, AdvancedRobot robot)
        {
            GunWave w = new GunWave();
            w.absBearing = e.BearingRadians + robot.HeadingRadians;
            w.speed = FIRE_SPEED;
            w.origin = new PointF((float)robot.X, (float)robot.Y);
            w.velSeg = (int)(e.Velocity * Math.Sin(e.HeadingRadians - w.absBearing));
            w.startTime = GetUTCTime();
            gunWaves.Add(w);
        }

        public void checkFiringWaves(PointF ePos)
        {
            GunWave w;
            for (int i = 0; i < gunWaves.Count; i++)
            {
                w = gunWaves[i];
                if ((GetUTCTime() - w.startTime) * w.speed >= GetDistanceBetweenPoints(w.origin, ePos))
                {
                    gunAngles[w.velSeg + 8] = Utils.NormalRelativeAngle(Utils.NormalAbsoluteAngle(Math.Atan2(ePos.X - w.origin.X, ePos.Y - w.origin.Y)) - w.absBearing);
                    gunWaves.Remove(w);
                }
            }
        }

        public static long GetUTCTime()  {            
            //获取同java gettime()一样的 长整型时间             
            long time = (DateTime.UtcNow.Ticks - new DateTime(1970, 1, 1).Ticks) / 10000;             
            return time;         
        }

 

        

        public static double GetDistanceBetweenPoints(PointF p, PointF q)
        {
            double a = p.X - q.X;
            double b = p.Y - q.Y;
            double distance = Math.Sqrt(a * a + b * b);
            return distance;
        }

        public PointF project(PointF origin, double dist, double angle)
        {
            return new PointF((float)(origin.X + dist * Math.Sin(angle)), (float)(origin.Y + dist * Math.Cos(angle)));
        }

       
    }

    public class MovementWave
    {
        public PointF origin;
        public double startTime;
        public double speed;
        public double angle;
        public double latVel;
    }
    /*
     * This class is the data we will need to use for our targeting waves.
     */
    public class GunWave
    {
        public double speed;
        public PointF origin;
        public int velSeg;
        public double absBearing;
        public double startTime;
    }
}
