using System;
using Navigay.Config;

namespace Navigay.Guidance
{
    public sealed class Countdown
    {
        int _shown = int.MaxValue;

        public int Shown => _shown == int.MaxValue ? 0 : _shown;

        public void Reset()
        {
            _shown = int.MaxValue;
        }

        public int Update(double remainingM, bool walkedBack, NavConfig config)
        {
            var rounded = Round(remainingM);
            if (_shown == int.MaxValue || rounded < _shown || (walkedBack && remainingM > config.countdown_back_m && rounded > _shown))
            {
                _shown = rounded;
            }

            return _shown;
        }

        public static int Round(double remainingM)
        {
            if (remainingM <= 0d)
            {
                return 0;
            }

            if (remainingM >= 50d)
            {
                return (int)(Math.Round(remainingM / 5d) * 5d);
            }

            return Math.Max(1, (int)Math.Round(remainingM));
        }
    }
}
