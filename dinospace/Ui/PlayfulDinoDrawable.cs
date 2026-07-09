using System;
using Microsoft.Maui.Graphics;

namespace dinospace
{
    // The storybook mascot: a soft sage sauropod with round spots, a long
    // friendly neck and a little smile — drawn in code so it's always crisp
    // at any size and always matches the playful palette.
    public class PlayfulDinoDrawable : IDrawable
    {
        public void Draw(ICanvas canvas, RectF rect)
        {
            float w = rect.Width, h = rect.Height;
            canvas.Antialias = true;

            var body = Color.FromArgb("#A3C493");
            var spot = Color.FromArgb("#7FA470");
            var ink = Color.FromArgb("#3C4633");

            // soft ground shadow
            canvas.FillColor = ink.WithAlpha(0.10f);
            canvas.FillEllipse(w * 0.22f, h * 0.86f, w * 0.58f, h * 0.07f);

            // tail: a thick round-capped sweep that tapers to a point
            canvas.StrokeColor = body;
            canvas.StrokeLineCap = LineCap.Round;
            var tail1 = new PathF();
            tail1.MoveTo(w * 0.72f, h * 0.62f);
            tail1.QuadTo(w * 0.88f, h * 0.60f, w * 0.93f, h * 0.48f);
            canvas.StrokeSize = h * 0.10f;
            canvas.DrawPath(tail1);
            var tail2 = new PathF();
            tail2.MoveTo(w * 0.90f, h * 0.52f);
            tail2.QuadTo(w * 0.96f, h * 0.42f, w * 0.97f, h * 0.34f);
            canvas.StrokeSize = h * 0.045f;
            canvas.DrawPath(tail2);

            // legs: four sturdy rounded stumps
            canvas.FillColor = body;
            void Leg(float cx) => canvas.FillRoundedRectangle(cx - w * 0.035f, h * 0.66f, w * 0.07f, h * 0.24f, w * 0.03f);
            Leg(w * 0.38f); Leg(w * 0.50f); Leg(w * 0.62f); Leg(w * 0.71f);

            // neck: one thick round-capped curve up to where the head sits
            var neck = new PathF();
            neck.MoveTo(w * 0.40f, h * 0.58f);
            neck.QuadTo(w * 0.30f, h * 0.38f, w * 0.30f, h * 0.17f);
            canvas.StrokeColor = body;
            canvas.StrokeSize = h * 0.155f;
            canvas.DrawPath(neck);

            // body
            canvas.FillColor = body;
            canvas.FillEllipse(w * 0.28f, h * 0.40f, w * 0.50f, h * 0.40f);

            // head
            canvas.FillEllipse(w * 0.195f, h * 0.055f, w * 0.225f, h * 0.185f);

            // spots
            canvas.FillColor = spot;
            canvas.FillCircle(w * 0.46f, h * 0.55f, h * 0.035f);
            canvas.FillCircle(w * 0.58f, h * 0.62f, h * 0.043f);
            canvas.FillCircle(w * 0.52f, h * 0.71f, h * 0.030f);
            canvas.FillCircle(w * 0.67f, h * 0.53f, h * 0.030f);

            // face: a round eye with a glint, and a little smile
            canvas.FillColor = ink;
            canvas.FillCircle(w * 0.268f, h * 0.135f, h * 0.017f);
            canvas.FillColor = Colors.White.WithAlpha(0.85f);
            canvas.FillCircle(w * 0.273f, h * 0.128f, h * 0.006f);

            canvas.StrokeColor = ink;
            canvas.StrokeSize = h * 0.008f;
            var smile = new PathF();
            smile.MoveTo(w * 0.285f, h * 0.185f);
            smile.QuadTo(w * 0.305f, h * 0.205f, w * 0.335f, h * 0.19f);
            canvas.DrawPath(smile);
        }
    }
}
