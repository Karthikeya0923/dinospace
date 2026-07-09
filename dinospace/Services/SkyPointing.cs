using System;
#if ANDROID
using Android.Content;
using Android.Hardware;
using Android.Runtime;
#endif

namespace dinospace.Services
{
    // Where the back camera points, as altitude + TRUE-north azimuth.
    //
    // This deliberately does not use MAUI's OrientationSensor: on Android that
    // prefers the *game* rotation vector, whose yaw is relative to however the
    // phone happened to be held — not to north. Every session the whole sky
    // overlay was swung by a different random angle, which is why Scan Sky
    // could label the moon in a spot nowhere near the visible moon. The
    // magnetometer-fused ROTATION_VECTOR sensor is north-referenced, and the
    // local magnetic declination on top converts magnetic north to true north,
    // which is the azimuth the astronomy engine speaks.
    public sealed class SkyPointing
#if ANDROID
        : Java.Lang.Object, ISensorEventListener
#endif
    {
        // (altitude deg, azimuth-from-true-north deg), sensor rate.
        public event Action<double, double>? Reading;

        // True while the magnetometer reports poor calibration. A drifting or
        // plain wrong heading is almost always this — the fix is waving the
        // phone in a figure-8, so the page shows that hint while it's true.
        public bool NeedsCalibration { get; private set; }

#if ANDROID
        private SensorManager? _sm;
        private Sensor? _sensor;
        private readonly float[] _r = new float[9];
        private readonly float[] _vals = new float[4];
        private float _declination;

        public bool Start(double lat, double lon)
        {
            try
            {
                _sm = (SensorManager?)Android.App.Application.Context.GetSystemService(Context.SensorService);
                _sensor = _sm?.GetDefaultSensor(SensorType.RotationVector);
                if (_sm == null || _sensor == null) return false;
                _declination = new GeomagneticField(
                    (float)lat, (float)lon, 0f,
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()).Declination;
                return _sm.RegisterListener(this, _sensor, SensorDelay.Game);
            }
            catch { return false; }
        }

        public void Stop()
        {
            try { _sm?.UnregisterListener(this); } catch { }
        }

        public void OnAccuracyChanged(Sensor? sensor, [GeneratedEnum] SensorStatus accuracy)
            => NeedsCalibration = accuracy is SensorStatus.Unreliable or SensorStatus.AccuracyLow;

        public void OnSensorChanged(SensorEvent? e)
        {
            if (e?.Sensor?.Type != SensorType.RotationVector || e.Values == null) return;
            int n = Math.Min(4, e.Values.Count);
            for (int i = 0; i < n; i++) _vals[i] = e.Values[i];
            SensorManager.GetRotationMatrixFromVector(_r, _vals);

            // The rotation matrix maps device coords to world coords
            // (X = east, Y = magnetic north, Z = up). The back camera looks
            // along device -Z, i.e. minus the matrix's third column.
            double east = -_r[2], north = -_r[5], up = -_r[8];
            double len = Math.Sqrt(east * east + north * north + up * up);
            if (len < 1e-6) return;
            double alt = Math.Asin(Math.Clamp(up / len, -1, 1)) * 180.0 / Math.PI;
            double az = (Math.Atan2(east, north) * 180.0 / Math.PI + _declination + 360.0) % 360.0;
            Reading?.Invoke(alt, az);
        }
#else
        public bool Start(double lat, double lon) => false;
        public void Stop() { }
#endif
    }
}
