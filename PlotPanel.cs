using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.Windows.Forms;

namespace FunctionAnalyzerMono
{
    public sealed class PlotPanel : Panel
    {
        private CompiledFunction function;
        private double startX, endX, pointX, pointY;
        private bool hasPoint;
        private const int Samples = 720;

        public PlotPanel()
        {
            DoubleBuffered = true;
            BackColor = Color.White;
            ResizeRedraw = true;
        }

        public void ShowFunction(CompiledFunction f, double a, double b,
            bool markPoint, double x, double y)
        {
            function = f; startX = a; endX = b;
            hasPoint = markPoint && NumericValidation.IsFinite(x) && NumericValidation.IsFinite(y);
            pointX = x; pointY = y;
            Invalidate();
        }

        public void ClearPlot()
        {
            function = null; hasPoint = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle plot = new Rectangle(65, 15, Math.Max(1, Width - 83), Math.Max(1, Height - 58));
            if (plot.Width < 70 || plot.Height < 60)
                return;
            if (function == null)
            {
                using (Brush brush = new SolidBrush(Color.Gray))
                    g.DrawString("Нажмите «Найти», чтобы построить график.", Font, brush, 75, 30);
                return;
            }
            try { DrawPlot(g, plot); }
            catch (Exception ex)
            {
                using (Brush brush = new SolidBrush(Color.DarkRed))
                    g.DrawString("Ошибка отрисовки: " + ex.Message, Font, brush, 70, 22);
            }
        }

        private void DrawPlot(Graphics g, Rectangle r)
        {
            // Отрисовка выполняется независимо от численных алгоритмов.
            double[] xValues = new double[Samples + 1];
            double[] yValues = new double[Samples + 1];
            double[] sorted = new double[Samples + 1];
            bool[] valid = new bool[Samples + 1];
            int validCount = 0;
            double range = endX - startX;
            if (!(range > 0) || !NumericValidation.IsFinite(range)) return;
            for (int i = 0; i <= Samples; i++)
            {
                double x = startX + range * (i / (double)Samples);
                xValues[i] = x;
                try
                {
                    double y = function.Evaluate(x);
                    valid[i] = true;
                    yValues[i] = y;
                    sorted[validCount++] = y;
                }
                catch (ExpressionException) { valid[i] = false; }
            }
            if (validCount == 0)
            {
                g.DrawString("Нет допустимых значений функции на интервале.", Font, Brushes.DarkRed, r.X, r.Y);
                return;
            }
            Array.Sort(sorted, 0, validCount);
            // Устойчивое авто-масштабирование: пики в окрестности разрывов
            // не должны скрыть основную часть кривой.
            double yMin = sorted[(int)((validCount - 1) * 0.03)];
            double yMax = sorted[(int)((validCount - 1) * 0.97)];
            if (hasPoint)
            {
                if (pointY < yMin) yMin = pointY;
                if (pointY > yMax) yMax = pointY;
            }
            double span = yMax - yMin;
            if (!NumericValidation.IsFinite(span) || span <= 0)
            {
                double scale = Math.Max(Math.Abs(yMin), 1.0);
                yMin -= scale * 0.1;
                yMax += scale * 0.1;
                span = yMax - yMin;
            }
            if (yMin > 0 && yMin <= span * 3) yMin = 0;
            if (yMax < 0 && -yMax <= span * 3) yMax = 0;
            span = yMax - yMin;
            double pad = span * 0.08;
            if (NumericValidation.IsFinite(pad))
            {
                yMin -= pad;
                yMax += pad;
            }
            if (!(yMax > yMin) || !NumericValidation.IsFinite(yMax - yMin)) return;

            using (Pen grid = new Pen(Color.FromArgb(226, 232, 237)))
            using (Pen axis = new Pen(Color.FromArgb(95, 104, 112), 1.5f))
            using (Pen curve = new Pen(Color.FromArgb(35, 100, 210), 1.9f))
            using (Pen bound = new Pen(Color.FromArgb(180, 170, 75), 1f))
            using (Pen mark = new Pen(Color.DarkRed, 1.3f))
            using (Brush text = new SolidBrush(Color.FromArgb(78, 84, 90)))
            {
                bound.DashStyle = DashStyle.Dash;
                DrawTicks(g, r, yMin, yMax, grid, text);
                float xZero = ToPixelX(0, r);
                float yZero = ToPixelY(0, yMin, yMax, r);
                if (xZero >= r.Left && xZero <= r.Right) g.DrawLine(axis, xZero, r.Top, xZero, r.Bottom);
                if (yZero >= r.Top && yZero <= r.Bottom) g.DrawLine(axis, r.Left, yZero, r.Right, yZero);
                g.DrawRectangle(Pens.DarkGray, r);
                g.DrawLine(bound, r.Left, r.Top, r.Left, r.Bottom);
                g.DrawLine(bound, r.Right, r.Top, r.Right, r.Bottom);

                GraphicsState state = g.Save();
                g.SetClip(r);
                for (int i = 1; i <= Samples; i++)
                {
                    if (!valid[i - 1] || !valid[i]) continue;
                    float x1 = ToPixelX(xValues[i - 1], r), x2 = ToPixelX(xValues[i], r);
                    float y1 = ToPixelY(yValues[i - 1], yMin, yMax, r);
                    float y2 = ToPixelY(yValues[i], yMin, yMax, r);
                    // Не рисуем линию через недопустимую середину или
                    // резкий скачок в окрестности полюса/разрыва.
                    if (Math.Abs(y2 - y1) > r.Height * 0.75) continue;
                    double midX = xValues[i - 1] + (xValues[i] - xValues[i - 1]) / 2;
                    double midY;
                    try { midY = function.Evaluate(midX); }
                    catch (ExpressionException) { continue; }
                    float ym = ToPixelY(midY, yMin, yMax, r);
                    if (Math.Abs(ym - (y1 + y2) / 2) > r.Height * 0.40) continue;
                    g.DrawLine(curve, x1, y1, x2, y2);
                }
                if (hasPoint && pointX >= startX && pointX <= endX)
                {
                    float px = ToPixelX(pointX, r);
                    float py = ToPixelY(pointY, yMin, yMax, r);
                    g.DrawLine(mark, px - 7, py, px + 7, py);
                    g.DrawLine(mark, px, py - 7, px, py + 7);
                    g.FillEllipse(Brushes.DarkRed, px - 3.5f, py - 3.5f, 7, 7);
                }
                g.Restore(state);
                DrawBoundaryMarkerLabel(g, "a", r.Left, r.Top + 6, text);
                DrawBoundaryMarkerLabel(g, "b", r.Right, r.Top + 6, text);
                SizeF xSize = g.MeasureString("x", Font);
                g.DrawString("x", Font, text, r.Right - xSize.Width, r.Bottom + 23);
                g.DrawString("f(x)", Font, text, r.Left - 48, r.Top - 12);
            }
        }

        private void DrawTicks(Graphics g, Rectangle r, double yMin, double yMax, Pen grid, Brush label)
        {
            double dx = NiceStep((endX - startX) / 7.0);
            double dy = NiceStep((yMax - yMin) / 6.0);
            if (dx > 0)
            {
                double first = Math.Ceiling(startX / dx) * dx;
                for (int k = 0; k < 30; k++)
                {
                    double x = first + k * dx;
                    if (x > endX || !NumericValidation.IsFinite(x)) break;
                    float px = ToPixelX(x, r);
                    g.DrawLine(grid, px, r.Top, px, r.Bottom);
                    string s = FormatTick(x);
                    SizeF size = g.MeasureString(s, Font);
                    g.DrawString(s, Font, label, px - size.Width / 2, r.Bottom + 4);
                }
            }
            if (dy > 0)
            {
                double first = Math.Ceiling(yMin / dy) * dy;
                for (int k = 0; k < 30; k++)
                {
                    double y = first + k * dy;
                    if (y > yMax || !NumericValidation.IsFinite(y)) break;
                    float py = ToPixelY(y, yMin, yMax, r);
                    g.DrawLine(grid, r.Left, py, r.Right, py);
                    string s = FormatTick(y);
                    SizeF size = g.MeasureString(s, Font);
                    g.DrawString(s, Font, label, r.Left - size.Width - 5, py - size.Height / 2);
                }
            }
        }

        private float ToPixelX(double x, Rectangle r)
        {
            double relative = (x - startX) / (endX - startX);
            return ClampPixel(r.Left + r.Width * relative);
        }

        private static float ToPixelY(double y, double min, double max, Rectangle r)
        {
            double relative = (y - min) / (max - min);
            return ClampPixel(r.Bottom - r.Height * relative);
        }

        private static float ClampPixel(double value)
        {
            if (double.IsNaN(value)) return 0;
            return (float)Math.Max(-100000, Math.Min(100000, value));
        }

        private static double NiceStep(double approximate)
        {
            if (approximate <= 0 || !NumericValidation.IsFinite(approximate)) return 0;
            double power = Math.Pow(10, Math.Floor(Math.Log10(approximate)));
            if (power <= 0 || !NumericValidation.IsFinite(power)) return 0;
            double scaled = approximate / power;
            return (scaled <= 1 ? 1 : scaled <= 2 ? 2 : scaled <= 5 ? 5 : 10) * power;
        }

        private void DrawBoundaryMarkerLabel(Graphics g, string labelText, float boundaryX, float topY, Brush textBrush)
        {
            SizeF size = g.MeasureString(labelText, Font);
            float x = boundaryX - size.Width / 2f;
            x = Math.Max(2f, Math.Min(Width - size.Width - 2f, x));
            RectangleF background = new RectangleF(x - 2f, topY - 1f, size.Width + 4f, size.Height + 2f);
            using (Brush backgroundBrush = new SolidBrush(Color.FromArgb(235, Color.White)))
            {
                g.FillRectangle(backgroundBrush, background);
            }
            g.DrawString(labelText, Font, textBrush, x, topY);
        }

        private static string FormatTick(double value)
        {
            if (Math.Abs(value) < 1e-14) value = 0;
            return value.ToString("G4", CultureInfo.InvariantCulture);
        }
    }
}
