using System.Collections.Generic;
using Robocode;

namespace myrobo
{
    public class BattleEvents
    {
        public List<BulletHitEvent> BulletHitEvents = new List<BulletHitEvent>();
        public List<BulletMissedEvent> BulletMissedEvents = new List<BulletMissedEvent>();
        public List<HitByBulletEvent> HitByBulletEvents = new List<HitByBulletEvent>();
        public List<HitRobotEvent> HitRobotEvents = new List<HitRobotEvent>();
                 
    }
}
