using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace myrobo
{
    public class Operations
    {
        public int Direction { get; set; }
        public double? BulletPower { get; set; }
        public double? Ahead { get; set; }
        public double? MaxVelocity { get; set; }
        public double? TurnRightRadians { get; set; }

        public double? TurnGunRightRadians { get; set; }
        public double? TurnRadarRightRadians { get; set; }

        public Operations Clone()
        {
            return new Operations()
            {
                Direction = this.Direction,
                BulletPower = this.BulletPower,
                Ahead = this.Ahead,
                MaxVelocity = this.MaxVelocity,
                TurnRightRadians = this.TurnRightRadians,
                TurnGunRightRadians = this.TurnGunRightRadians,
                TurnRadarRightRadians = this.TurnRadarRightRadians
            };
        }



    }
}
