using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Robocode;

namespace myrobo
{
    interface IHandleScanedRobot
    {
        Operations HandleScanedRobot(AdvancedRobot robot, ScannedRobotEvent e, ScannedRobotEvent previousScaned, Operations operations);

    }
}
