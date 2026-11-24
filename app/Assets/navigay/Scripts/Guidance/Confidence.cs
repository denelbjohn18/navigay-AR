using System;
using Navigay.Config;
using Navigay.Position;

namespace Navigay.Guidance
{
    public enum ConfidenceLevel
    {
        Full,
        Short,
        Hold,
    }

    public sealed class Confidence
    {
        ConfidenceLevel _level = ConfidenceLevel.Hold;
        ConfidenceLevel _pending = ConfidenceLevel.Hold;
        double _since;

        public ConfidenceLevel Level => _level;

        public void Update(PositionEstimate estimate, bool cameraFine, bool photoMatchFresh, NavConfig config, double time)
        {
            var desired = Desired(estimate, cameraFine, photoMatchFresh, config);
            if (Rank(desired) < Rank(_level))
            {
                _level = desired;
                _pending = desired;
                _since = time;
                return;
            }

            if (desired == _level)
            {
                _pending = desired;
                _since = time;
                return;
            }

            if (_pending != desired)
            {
                _pending = desired;
                _since = time;
                return;
            }

            if (time - _since >= config.confidence_up_s)
            {
                _level = desired;
            }
        }

        static ConfidenceLevel Desired(PositionEstimate estimate, bool cameraFine, bool photoMatchFresh, NavConfig config)
        {
            if (!cameraFine)
            {
                return ConfidenceLevel.Hold;
            }

            var photo = false;
            switch (estimate.Source)
            {
                case PositionSource.Gps:
                    photo = false;
                    break;
                case PositionSource.PhotoMap:
                    photo = photoMatchFresh;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(estimate.Source), estimate.Source, null);
            }

            if (photo || (estimate.PositionErrorM <= config.full_error_m && estimate.HeadingErrorDeg <= config.full_heading_deg))
            {
                return ConfidenceLevel.Full;
            }

            if (estimate.PositionErrorM <= config.short_error_m && estimate.HeadingErrorDeg <= config.short_heading_deg)
            {
                return ConfidenceLevel.Short;
            }

            return ConfidenceLevel.Hold;
        }

        static int Rank(ConfidenceLevel level)
        {
            switch (level)
            {
                case ConfidenceLevel.Hold:
                    return 0;
                case ConfidenceLevel.Short:
                    return 1;
                case ConfidenceLevel.Full:
                    return 2;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}
