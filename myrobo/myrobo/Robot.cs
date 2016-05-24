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
        private Random rnd = new Random(DateTime.Now.Millisecond);
        private BattleEvents battleEvents = new BattleEvents();

        private IList<IHandleScanedRobot> handlers = new List<IHandleScanedRobot>(){new Handlers.Ram()};
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
            var results =
                handlers.Select(handler => new { Evaludation = (int)handler.Evaluate(this, e, battleEvents), Handler = handler })
                    .OrderBy(item => item.Evaludation);
            var topones = results.Where(result => result.Evaludation == results.First().Evaludation);
            var choosen = topones.ElementAt(Convert.ToInt32(rnd.NextDouble() * topones.Count())).Handler;
            ApplyOperations(choosen.HandleScanedRobot(this, e, lastScannedRobotEvent, operations, battleEvents));
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
            battleEvents.BulletHitEvents.Add(evnt);
            base.OnBulletHit(evnt);
        }

        public override void OnBulletMissed(BulletMissedEvent evnt)
        {
            battleEvents.BulletMissedEvents.Add(evnt);
            base.OnBulletMissed(evnt);
        }

        public override void OnHitByBullet(HitByBulletEvent evnt)
        {
            battleEvents.HitByBulletEvents.Add(evnt);
            base.OnHitByBullet(evnt);
        }

        public override void OnHitRobot(HitRobotEvent evnt)
        {
            battleEvents.HitRobotEvents.Add(evnt);
            base.OnHitRobot(evnt);
        }

        public override void OnHitWall(HitWallEvent evnt)
        {
            operations.Direction = -operations.Direction; 
            base.OnHitWall(evnt);
        }
    }
}
