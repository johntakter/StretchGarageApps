using System;
using System.Collections.Generic;
using System.Text;

namespace StretchGarage.Shared
{
    public class CheckLocation
    {
        public int Interval { get; set; }
        public bool CheckSpeed { get; set; }
        public bool IsParked { get; set; }

        public CheckLocation(int interval, bool checkSpeed, bool isParked)
        {
            Interval = interval;
            CheckSpeed = checkSpeed;
            IsParked = isParked;
        }
    }
}
