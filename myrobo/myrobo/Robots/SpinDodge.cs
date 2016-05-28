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
    public class SuperSpinBot : AdvancedRobot
    {
        //gun variables
        static double[,] enemyVelocities = new double[400, 4];
        static int currentEnemyVelocity;
        static int aimingEnemyVelocity;
        double velocityToAimAt;
        bool fired;
        double oldTime;
        int count;
        int averageCount;
        Random rnd = new Random(DateTime.Now.Millisecond);

        //movement variables
        static double turn = 2;
        int turnDir = 1;
        int moveDir = 1;
        double oldEnemyHeading;
        double oldEnergy = 100;

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
            double absBearing = e.BearingRadians + HeadingRadians;
            //Graphics2D g = getGraphics();

            //increase our turn speed amount each tick,to a maximum of 8 and a minimum of 4
            turn += 0.2 * rnd.NextDouble();
            if (turn > 8)
            {
                turn = 2;
            }

            //when the enemy fires, we randomly change turn direction and whether we go forwards or backwards
            if (oldEnergy - e.Energy <= 3 && oldEnergy - e.Energy >= 0.1)
            {
                if (rnd.NextDouble() > .5)
                {
                    turnDir *= -1;
                }
                if (rnd.NextDouble() > .8)
                {
                    moveDir *= -1;
                }
            }

            //we set our maximum speed to go down as our turn rate goes up so that when we turn slowly, we speed up and vice versa;
            MaxTurnRate = turn;
            MaxVelocity = 12 - turn;
            SetAhead(90 * moveDir);
            SetTurnLeft(90 * turnDir);
            oldEnergy = e.Energy;


            //find our which velocity segment our enemy is at right now
            if (e.Velocity < -2)
            {
                currentEnemyVelocity = 0;
            }
            else if (e.Velocity > 2)
            {
                currentEnemyVelocity = 1;
            }
            else if (e.Velocity <= 2 && e.Velocity >= -2)
            {
                if (currentEnemyVelocity == 0)
                {
                    currentEnemyVelocity = 2;
                }
                else if (currentEnemyVelocity == 1)
                {
                    currentEnemyVelocity = 3;
                }
            }

            //update the one we are using to determine where to store our velocities if we have fired and there has been enough time for a bullet to reach an enemy
            //(only a rough approximation of bullet travel time);
            if (Time - oldTime > e.Distance / 12.8 && fired == true)
            {
                aimingEnemyVelocity = currentEnemyVelocity;
            }
            else
            {
                fired = false;
            }

            //record a new enemy velocity and raise the count
            enemyVelocities[count, aimingEnemyVelocity] = e.Velocity;
            count++;
            if (count == 400)
            {
                count = 0;
            }

            //calculate our average velocity for our current segment
            averageCount = 0;
            velocityToAimAt = 0;
            while (averageCount < 400)
            {
                velocityToAimAt += enemyVelocities[averageCount, currentEnemyVelocity];
                averageCount++;
            }
            velocityToAimAt /= 400;


            //pulled straight out of the circular targeting code on the Robowiki. Note that all I did was replace the enemy velocity and
            //put in pretty graphics that graph the enemies predicted movement(actually the average of their predicted movement) 
            //Press paint on the robot console to see the debugging graphics.
            //Note that this gun can be improved by adding more segments and also averaging turn rate.
            double bulletPower = Math.Min(2.4, Math.Min(e.Energy / 4, Energy / 10));
            double myX = X;
            double myY = Y;
            double enemyX = Y + e.Distance * Math.Sin(absBearing);
            double enemyY = Y + e.Distance * Math.Cos(absBearing);
            double enemyHeading = e.HeadingRadians;
            double enemyHeadingChange = enemyHeading - oldEnemyHeading;
            oldEnemyHeading = enemyHeading;
            double deltaTime = 0;
            double battleFieldHeight = BattleFieldHeight,
                   battleFieldWidth = BattleFieldWidth;
            double predictedX = enemyX, predictedY = enemyY;
            while ((++deltaTime) * (20.0 - 3.0 * bulletPower) <
                  new PointF((float)myX, (float)myY).Distance(new PointF((float)predictedX, (float)predictedY)))
            {
                predictedX += Math.Sin(enemyHeading) * velocityToAimAt;
                predictedY += Math.Cos(enemyHeading) * velocityToAimAt;
                enemyHeading += enemyHeadingChange;
                if (predictedX < 18.0
                    || predictedY < 18.0
                    || predictedX > battleFieldWidth - 18.0
                    || predictedY > battleFieldHeight - 18.0)
                {

                    predictedX = Math.Min(Math.Max(18.0, predictedX),
                        battleFieldWidth - 18.0);
                    predictedY = Math.Min(Math.Max(18.0, predictedY),
                        battleFieldHeight - 18.0);
                    break;
                }
            }
            double theta = Utils.NormalAbsoluteAngle(Math.Atan2(
                predictedX - X, predictedY - Y));

            SetTurnRadarRightRadians(Utils.NormalRelativeAngle(
                absBearing - RadarHeadingRadians) * 2);
            SetTurnGunRightRadians(Utils.NormalRelativeAngle(
                theta - GunHeadingRadians));
            if (GunHeat == 0)
            {
                Fire(bulletPower);
                fired = true;
            }
        }
    }
}
