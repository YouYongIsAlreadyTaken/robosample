using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Robocode;
using Robocode.Util;

namespace myrobo.Handlers
{
    public class SuperM : IHandleScanedRobot
    {
        static double FIRE_POWER = 2;
        static double FIRE_SPEED = 20 - FIRE_POWER * 3;
        static double BULLET_DAMAGE = 10;

        static double enemyEnergy = 100;

        /*
	     * An ArrayList can hold a list of objects. 
	     * We'll be using the first one to hold all the waves that we wish to keep track of for movement, and the
	     * second for the targeting waves.
	     */
        List<MovementWave> moveWaves = new List<MovementWave>();
        List<GunWave> gunWaves = new List<GunWave>();

        /*
	     * This Array will hold the most recent movement angle for every lateral velocity segment;
	     */
        static double[] gunAngles = new double[16];

        public Confidence Evaluate(AdvancedRobot robot, ScannedRobotEvent e, BattleEvents battleEvents)
        {
            Confidence confidence = Confidence.DonotBlameMeIfILoose;
            if (e.Distance >= 200)
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


        /*
	    * In this robot, as in many others, onScannedRobot is used as the main place to put actions done every tick.
	    */
        public Operations HandleScanedRobot(AdvancedRobot robot, ScannedRobotEvent e, ScannedRobotEvent previousScaned,
            Operations operations, BattleEvents battleEvents)
        {
            var newOperations = operations.Clone();

            SetFirePowerBasedOnEnergy(robot, e,battleEvents);

            double absBearing = e.BearingRadians + robot.HeadingRadians;

            /*
		    * ==================Movement Section============================
		    * This makes us put a log into our log when we notice the enemy firing. 
		    * To see the actual logging of the wave, look at the logWave method.
		    */
            double energyChange = (enemyEnergy - (enemyEnergy = e.Energy));
            MovementWave w;
            if (energyChange <= 3 && energyChange >= 0.1)
            {
                logMovementWave(e, energyChange, robot);
            }

            /*
		    * After we are done checking to see if we need to log any waves, we'll decide where to move.
		    * To see this process take a peek at the chooseDirection method.
		    */
            chooseDirection(project(new PointF((float)robot.X, (float)robot.Y), e.Distance, absBearing), newOperations, robot);

            /*
		    * logs a gun wave when we fire;
		    */
            if (robot.GunHeat == 0)
            {
                logFiringWave(e, robot);
            }
            /*
		    * This method checks our waves to see if they have reached the enemy yet.
		    */
            checkFiringWaves(project(new PointF((float)robot.X, (float)robot.Y), e.Distance, absBearing), robot);

            /*
		    * Aiming our gun and firing
		    */
            newOperations.TurnGunRightRadians = Utils.NormalRelativeAngle(absBearing - robot.GunHeadingRadians)
                                         + gunAngles[8 + (int)(e.Velocity * Math.Sin(e.HeadingRadians - absBearing))];
            if (robot.Energy >= 5)
                newOperations.BulletPower = FIRE_POWER;
            else //don't fire gun if we are about to die, hoping that enemy will reach to diable first
                newOperations.BulletPower = null;

            newOperations.TurnRadarRightRadians = Utils.NormalRelativeAngle(absBearing - robot.RadarHeadingRadians) * 2;

            return newOperations;
        }

        private static void SetFirePowerBasedOnEnergy(AdvancedRobot robot, ScannedRobotEvent e,BattleEvents battleEvents)
        {
            
            if (robot.Energy > 70 )
            {
                FIRE_POWER = 3;
            }
            else if (robot.Energy <= 70 && robot.Energy > 50)
            {
                FIRE_POWER = 1;
            }
            else if (robot.Energy <= 50 && robot.Energy >= 30 || e.Energy < 15)
            {
                FIRE_POWER = 0.5;
            }
            else
            {
                FIRE_POWER = 0.1;
            }


        }


        /*
	    * This helps us keep from being confused about the enemy's energy after hitting them with a bullet.
	    */
        public void OnBulletHit(BulletHitEvent e)
        {
            enemyEnergy -= BULLET_DAMAGE;
        }

        /*
	    * This method receives a ScannedRobotEvent and uses that information to create a new wave and place it in
	    * our log. Basically we're going to take all the information we'll need to know later to figure out where
	    * to move to and store it in one object so we can use it easily later.
	    */
        public void logMovementWave(ScannedRobotEvent e, double energyChange, AdvancedRobot robot)
        {
            double absBearing = e.BearingRadians + robot.HeadingRadians;
            MovementWave w = new MovementWave();
            //This is the spot that the enemy was in when they fired.
            w.origin = project(new PointF((float)robot.X, (float)robot.Y), e.Distance, absBearing);
            //20-3*bulletPower is the formula to find a bullet's speed.
            w.speed = 20 - 3 * energyChange;
            //The time at which the bullet was fired.
            w.startTime = robot.Time;
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

         /*
	     * This method looks at all the directions we could go, then rates them based on how close they will take us
	     * to simulated bullets fired with both linear and head-on targeting generated by the waves we have logged.
	     * It is the core of our movement.
	     */
        public void chooseDirection(PointF enemyLocation, Operations operations, AdvancedRobot robot)
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

                    //This part will remove waves that have passed our robot, so we no longer keep taking into account old ones
                    if (new PointF((float)robot.X, (float)robot.Y).Distance(w.origin) < (robot.Time - w.startTime) * w.speed + w.speed)
                    {
                        moveWaves.Remove(w);
                    }
                    else
                    {
                        /*
					     * This adds two risks for each wave: one based on the distance from where a head-on targeting
					     * bullet would be, and one for where a linear targeting bullet would be.
					     */
                        rating += 1D /
                                  Math.Pow(
                                      movePoint.Distance(project(w.origin, movePoint.Distance(w.origin), w.angle)), 2);
                        rating += 1D /
                                  Math.Pow(
                                      movePoint.Distance(project(w.origin, movePoint.Distance(w.origin),
                                          w.angle + w.latVel)), 2);
                    }
                }
                //This adds a risk associated with being to close to the other robot if there are no waves.
                if (moveWaves.Count == 0)
                {
                    rating = 1D / Math.Pow(movePoint.Distance(enemyLocation), 2);
                }
                //This part tells us to go in the direction if it is better than the previous best option and is reachable.
                if (rating < bestRating &&
                    new RectangleF(50, 50, (float)(robot.BattleFieldWidth - 100), (float)(robot.BattleFieldHeight - 100)).Contains(
                        movePoint))
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
            w.startTime = robot.Time;
            gunWaves.Add(w);
        }

        /*
	    * This method checks firing waves to see if they have passed the enemy yet.
	    */
        public void checkFiringWaves(PointF ePos, AdvancedRobot robot)
        {
            GunWave w;
            for (int i = 0; i < gunWaves.Count; i++)
            {
                w = gunWaves[i];
                if ((robot.Time - w.startTime) * w.speed >= w.origin.Distance(ePos))
                {
                    gunAngles[w.velSeg + 8] =
                        Utils.NormalRelativeAngle(
                            Utils.NormalAbsoluteAngle(Math.Atan2(ePos.X - w.origin.X, ePos.Y - w.origin.Y)) -
                            w.absBearing);
                    gunWaves.Remove(w);
                }
            }
        }

        /*
	    * This extremely useful method lets us project one point from another given a specific angle and distance.
	    */
        public PointF project(PointF origin, double dist, double angle)
        {
            return new PointF((float)(origin.X + dist * Math.Sin(angle)), (float)(origin.Y + dist * Math.Cos(angle)));
        }

        /*
	    * This class is the data we will need to use our movement waves.
	    */
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
}
