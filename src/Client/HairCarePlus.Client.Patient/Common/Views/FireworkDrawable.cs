using System;
using System.Linq;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace HairCarePlus.Client.Patient.Common.Views
{
    class FireworkDrawable : IDrawable
    {
        readonly ProgressRing _own;
        readonly Random _rnd = new();
        Particle[] _parts = Array.Empty<Particle>();
        float _t;

        struct Particle
        {
            public float Angle;
            public float Dist;
            public float Size;
            public Color Color;
        }

        const int   COUNT   = 32;
        const float MAXDIST = 36f;
        const float STARTSZ = 3f;
        const float Deg2Rad = MathF.PI / 180f;

        public FireworkDrawable(ProgressRing owner) => _own = owner;

        public void Reset()
        {
            _parts = Enumerable.Range(0, COUNT).Select(_ => NewParticle()).ToArray();
            _t = 0;
        }

        public void Tick(double t) => _t = (float)t;

        Particle NewParticle()
        {
            var baseColor = _own.CardColor;
            Color[] pal = {
                baseColor.WithLuminosity(1.2f),
                baseColor.WithLuminosity(1.0f),
                baseColor.WithLuminosity(0.8f),
                Colors.White.WithAlpha(0.9f),
                baseColor.WithAlpha(0.7f)
            };
            return new Particle
            {
                Angle = (float)(_rnd.NextDouble() * 360),
                Dist  = (float)(_rnd.NextDouble() * (MAXDIST * 0.4f) + MAXDIST * 0.6f),
                Size  = STARTSZ,
                Color = pal[_rnd.Next(pal.Length)]
            };
        }

        public void Draw(ICanvas canvas, RectF rect)
        {
            if (_parts.Length == 0) return;
            float cx = rect.Center.X;
            float cy = rect.Center.Y;
            float t = _t;

            foreach (var p in _parts)
            {
                float ease = (float)Easing.SinOut.Ease(t);
                float dx   = MathF.Cos(p.Angle * Deg2Rad) * p.Dist * ease;
                float dy   = MathF.Sin(p.Angle * Deg2Rad) * p.Dist * ease;
                float r    = p.Size * (1f - t);
                canvas.FillColor = p.Color.WithAlpha(1f - t);
                canvas.FillCircle(cx + dx, cy + dy, r);
            }
        }
    }
} 