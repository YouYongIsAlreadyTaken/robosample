using System;
using System.Text;
using Robocode;
using Robocode.Util;
//Not ready to add it to handlers, the pattenMatcher not correct for now.
namespace myrobo.Handlers
{
    class Moebius : IHandleScanedRobot
    {
        // Constants
	    static  int 	SEARCH_DEPTH = 30;	// Increasing this slows down game execution - beware!
	    static  int 	MOVEMENT_LENGTH = 150;	// Larger helps on no-aim and nanoLauLectrik - smaller on linear-lead bots
	    static  int	BULLET_SPEED = 11;	// 3 power bullets travel at this speed.
	    static  int	MAX_RANGE = 800;  	// Range where we're guarenteed to get a look-ahead lock
							    // 1200 would be another good value as this is the max radar distance.
							    // Yet too large makes it take longer to hit new movement patterns (Lemon)...
	    static  int	SEARCH_END_BUFFER = SEARCH_DEPTH + MAX_RANGE / BULLET_SPEED;	// How much room to leave for leading
										
		    // Globals
	    static double [] arcLength = new double[100000];
        static StringBuilder s = new StringBuilder("/0/3/6/1/4/7/2/5/b" + (-1) + (-4) + (-7) + (-2) + 
							  (-5) + (-8) + (-3) + (-6) + "This space filler for end buffer." +
							  "The numbers up top assure a 1 length match every time.  This string must be " +
							  "longer than SEARCH_END_BUFFER. - Mike Dorgan");

//        static StringBuffer patternMatcher = new StringBuffer("\0\3\6\1\4\7\2\5\b" + (char)(-1) + (char)(-4) + (char)(-7) + (char)(-2) + 
//							  (char)(-5) + (char)(-8) + (char)(-3) + (char)(-6) + "This space filler for end buffer." +
//							  "The numbers up top assure a 1 length match every time.  This string must be " +
//							  "longer than SEARCH_END_BUFFER. - Mike Dorgan");
        private static string patternMatcher = s.ToString();
        public Confidence Evaluate(AdvancedRobot robot, ScannedRobotEvent e, BattleEvents battleEvents)
        {
            Confidence confidence = Confidence.DonotBlameMeIfILoose;
            return confidence;
        }

        public void OnBulletHit(BulletHitEvent evnt)
        {
            throw new NotImplementedException();
        }

        public Operations HandleScanedRobot(AdvancedRobot robot, ScannedRobotEvent e, ScannedRobotEvent previousScaned,
            Operations operations, BattleEvents battleEvents)
        {
            var calculatedParams = new CalculatedParams(robot, e);
            var newOperations = operations.Clone();
            doMoebius(0, patternMatcher.Length, e, SEARCH_DEPTH, calculatedParams.AbsoluteBearing,newOperations,robot);
            newOperations.TurnRadarRightRadians = Utils.NormalRelativeAngle(calculatedParams.AbsoluteBearing - robot.RadarHeadingRadians) * 2;
            return newOperations;
        }

        private float random()
        {
            Random rd = new Random();
            int a = rd.Next(100);
            float f =( float )(a * 0.01);
            return f;
        }

        private void doMoebius(int matchIndex, int historyIndex, ScannedRobotEvent e, int searchDepth,
            double targetBearing,Operations newOperations,AdvancedRobot robot)
       {
		// Assign ArcMovement here to save a byte with the targetBearing assign.
		double arcMovement = e.Velocity * Math.Sin(e.HeadingRadians - targetBearing);
																										
		// Move in a SHM oscollator pattern with a bit of random thrown in for good measure.
		newOperations.Ahead = Math.Cos(historyIndex>>4) * MOVEMENT_LENGTH * random();
							
		// Try to stay equa-distance to the target -  a slight movement towards
		// target would help with corner death, but no room.
		newOperations.TurnRightRadians = e.BearingRadians + Math.PI/2;

		// Assume small aim increment so we can always fire.  Too much cost for gun turn check
		// Add simple power management code.  This keeps us alive a bit longer against bots we
		// have trouble locking on to.  Helps in melee as well.  It basically gives us 9 more shots.
		// -2 is better, but costs 1 more byte
		newOperations.BulletPower = robot.Energy-1;

		// Cummulative radial velocity relative to us. This is the ArcLength that the enemy traces relative to us.  
		// ArcLength S = Angle (radians) * Radius of circle.
		arcLength[historyIndex+1] = arcLength[historyIndex] + arcMovement;
		
		// Add ArcMovement to lookup buffer.  Typecast to char so it takes 1 entry.
		patternMatcher +=((char)(arcMovement));

		// Do adjustable buffer pattern match.  Use above buffer to save all out of bounds checks... ;)
		do 
		{
			matchIndex = patternMatcher.LastIndexOf(
							patternMatcher.Substring(historyIndex - --searchDepth),
							historyIndex-SEARCH_END_BUFFER);
		}
		while (matchIndex < 0); 

		// Update index to end of search		
		matchIndex += searchDepth;
		
		// Aim at target (asin() in front of sin would be better, but no room at 3 byte cost.)
		newOperations.TurnGunRightRadians = Math.Sin( 
			(arcLength[matchIndex+((int)(e.Distance/BULLET_SPEED))]-arcLength[matchIndex])/e.Distance +
			targetBearing - robot.GunHeadingRadians);
				
		// Lock Radar Infinite style.  About a 99% lock rate - plus good melee coverage.
		
	
		// I'd love to drop a clearAllEvents() here for melee radar locking help, but no space.
	}
    


    }
}
