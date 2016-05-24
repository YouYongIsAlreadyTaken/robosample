using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;

namespace myrobo
{
    class BattleEvents
    {
        public List<BulletHitEvent> BulletHitEvents = new List<BulletHitEvent>();
        public List<BulletMissedEvent> BulletMissedEvents = new List<BulletMissedEvent>();
        public List<HitByBulletEvent> HitByBulletEvents = new List<HitByBulletEvent>();
        public List<HitRobotEvent> HitRobotEvents = new List<HitRobotEvent>();
                 
    }
}
