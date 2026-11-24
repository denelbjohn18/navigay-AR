using Navigay.Config;

namespace Navigay.Guidance
{
    public sealed class Pace
    {
        double _lastAlong = double.NaN;
        double _lastTime;
        float _speed;

        public float SpeedMps => _speed;

        public void Reset(NavConfig config)
        {
            _lastAlong = double.NaN;
            _speed = config.walk_speed_mps;
        }

        public float Update(double alongM, double time, NavConfig config)
        {
            if (double.IsNaN(_lastAlong))
            {
                _lastAlong = alongM;
                _lastTime = time;
                _speed = config.walk_speed_mps;
                return _speed;
            }

            var dt = time - _lastTime;
            var ds = alongM - _lastAlong;
            _lastAlong = alongM;
            _lastTime = time;
            if (dt > 0.2d && ds > 0d)
            {
                var sample = (float)(ds / dt);
                _speed = (_speed * 0.8f) + (sample * 0.2f);
                if (_speed < config.walk_speed_min_mps)
                {
                    _speed = config.walk_speed_min_mps;
                }
                else if (_speed > config.walk_speed_max_mps)
                {
                    _speed = config.walk_speed_max_mps;
                }
            }

            return _speed;
        }

        public static int Minutes(double remainingM, float speedMps)
        {
            if (remainingM <= 0d || speedMps <= 0f)
            {
                return 0;
            }

            return System.Math.Max(1, (int)System.Math.Round(remainingM / speedMps / 60d));
        }
    }
}
