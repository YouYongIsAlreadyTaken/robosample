using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using myrobo.Handlers;
using Robocode;
using Robocode.Util;


namespace myrobo
{
    public class WebGo_Alpha : AdvancedRobot
    {
        private ScannedRobotEvent lastScannedRobotEvent;
        private Operations operations = new Operations() { Direction = 1, BulletPower = 3};
        private Random rnd = new Random(DateTime.Now.Millisecond);
        private BattleEvents battleEvents = new BattleEvents();
        private const int minticks = 5;
        private const int ticksRange = 10;
        private int tickcount = 0;
        static int LONG_TICK_WINDOW = 500;
        static int SHORT_TICK_WINDOW = 10;
        private IHandleScanedRobot currentHandler;

        private IList<IHandleScanedRobot> handlers = new List<IHandleScanedRobot>() { new Handlers.RamMinRisk()};
        //private IList<IHandleScanedRobot> handlers = new List<IHandleScanedRobot>() { new Handlers.Mercutio() };
        public override void Run()
        {
            // -- Initialization of the robot --
            IsAdjustGunForRobotTurn = true;
            IsAdjustRadarForGunTurn = true;
            SetTurnRadarRightRadians(double.MaxValue);

            while (true)
            {
                if (RadarTurnRemainingRadians == 0)
                {
                    SetTurnRadarRightRadians(double.MaxValue);
                }
                Execute();
            }

        }

        // Robot event handler, when the robot sees another robot
        public override void OnScannedRobot(ScannedRobotEvent e)
        {
            //let each handler to handle onScaned event to build up complete data for all tick
            foreach (IHandleScanedRobot handleScanedRobot in handlers)
            {
                handleScanedRobot.HandleScanedRobot(this, e, lastScannedRobotEvent, operations, battleEvents);
            }

            if (tickcount == 0)
            {
                tickcount = minticks + (int) rnd.NextDouble()*ticksRange;
                var results =
                    handlers.Select(
                        handler => new {Evaludation = (int) handler.Evaluate(this, e, battleEvents), Handler = handler})
                        .OrderBy(item => item.Evaludation);
                var topones = results.Where(result => result.Evaludation == results.First().Evaludation);
                var choosen = topones.ElementAt((int) Math.Floor(rnd.NextDouble()*topones.Count())).Handler;
                currentHandler = choosen;
            }
            else
            {
                tickcount--;
            }

            ApplyOperations(currentHandler.HandleScanedRobot(this, e, lastScannedRobotEvent, operations, battleEvents));
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
            currentHandler.OnBulletHit(evnt);
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
