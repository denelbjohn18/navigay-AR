using System.Collections.Generic;
using System.IO;
using Navigay.Guidance;
using Newtonsoft.Json;

namespace Navigay.Dev
{
    public sealed class WalkSample
    {
        public double Time;
        public bool HasGps;
        public double Latitude;
        public double Longitude;
        public float GpsAccuracyM;
        public bool HasCompass;
        public float HeadingDeg;
        public float HeadingAccuracyDeg;
        public float ArX;
        public float ArY;
        public float ArZ;
        public float ArQx;
        public float ArQy;
        public float ArQz;
        public float ArQw;
        public string Tracking;
        public bool HasSnap;
        public double SnapX;
        public double SnapY;
        public string SnapEdgeId;
        public double? AlongM;
        public int? CountdownM;
        public ConfidenceLevel Confidence;
    }

    public sealed class WalkRecorder
    {
        readonly string _path;
        double _last = double.NegativeInfinity;

        public WalkRecorder(string path)
        {
            _path = path;
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }

        public string PathToFile => _path;

        public void Capture(WalkSample sample)
        {
            if (sample.Time - _last < 0.1d)
            {
                return;
            }

            _last = sample.Time;
            var row = new Dictionary<string, object>
            {
                ["t"] = sample.Time,
                ["gps"] = sample.HasGps
                    ? new Dictionary<string, object>
                    {
                        ["lat"] = sample.Latitude,
                        ["lon"] = sample.Longitude,
                        ["accuracy_m"] = sample.GpsAccuracyM,
                    }
                    : null,
                ["compass"] = sample.HasCompass
                    ? new Dictionary<string, object>
                    {
                        ["heading_deg"] = sample.HeadingDeg,
                        ["accuracy_deg"] = sample.HeadingAccuracyDeg,
                    }
                    : null,
                ["arkit"] = new Dictionary<string, object>
                {
                    ["x"] = sample.ArX,
                    ["y"] = sample.ArY,
                    ["z"] = sample.ArZ,
                    ["qx"] = sample.ArQx,
                    ["qy"] = sample.ArQy,
                    ["qz"] = sample.ArQz,
                    ["qw"] = sample.ArQw,
                    ["tracking"] = sample.Tracking,
                },
                ["photo_map"] = null,
                ["snap"] = sample.HasSnap
                    ? new Dictionary<string, object>
                    {
                        ["x"] = sample.SnapX,
                        ["y"] = sample.SnapY,
                        ["edge_id"] = sample.SnapEdgeId,
                    }
                    : null,
                ["along_m"] = sample.AlongM,
                ["countdown_m"] = sample.CountdownM,
                ["confidence"] = sample.Confidence.ToString(),
            };
            File.AppendAllText(_path, JsonConvert.SerializeObject(row) + "\n");
        }
    }
}
