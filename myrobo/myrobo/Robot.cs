using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;
using Robocode.Util;


namespace myrobo
{


    public class MyRobot : AdvancedRobot
    {
        private ScannedRobotEvent lastScannedRobotEvent;
        private Operations operations = new Operations() { Direction = 1, BulletPower = 3};
        

        private IHandleScanedRobot handler = new Handlers.Ram();
        public override void Run()
        {
            // -- Initialization of the robot --
            IsAdjustGunForRobotTurn = true;
            IsAdjustRadarForGunTurn = true;
            SetTurnRadarRightRadians(double.MaxValue);

        }

        // Robot event handler, when the robot sees another robot
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            ApplyOperations(handler.HandleScanedRobot(this, e, lastScannedRobotEvent, operations));
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
            operations = op;
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
