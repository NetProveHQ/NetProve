using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace NetProve.Controls
{
    /// <summary>
    /// A circular arc gauge control that displays a percentage value 0–100.
    /// Lightweight pure-WPF implementation with no external dependencies.
    /// </summary>
    public sealed class CircularGauge : Control
    {
        static CircularGauge()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(CircularGauge),
                new FrameworkPropertyMetadata(typeof(CircularGauge)));
        }

        // ── Dependency Properties ─────────────────────────────────────────────
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(nameof(Value), typeof(double), typeof(CircularGauge),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender,
                    OnValueChanged));

        public static readonly DependencyProperty TrackColorProperty =
            DependencyProperty.Register(nameof(TrackColor), typeof(Brush), typeof(CircularGauge),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x2D, 0x30, 0x48)), // default; overridden by DynamicResource TrackBg in XAML
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty ArcColorProperty =
            DependencyProperty.Register(nameof(ArcColor), typeof(Brush), typeof(CircularGauge),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty StrokeWidthProperty =
            DependencyProperty.Register(nameof(StrokeWidth), typeof(double), typeof(CircularGauge),
                new FrameworkPropertyMetadata(10.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(CircularGauge),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(CircularGauge),
                new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty UnitProperty =
            DependencyProperty.Register(nameof(Unit), typeof(string), typeof(CircularGauge),
                new FrameworkPropertyMetadata("%", FrameworkPropertyMetadataOptions.AffectsRender));

        public double Value
        {
            get => (double)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }
        public Brush TrackColor
        {
            get => (Brush)GetValue(TrackColorProperty);
            set => SetValue(TrackColorProperty, value);
        }
        public Brush ArcColor
        {
            get => (Brush)GetValue(ArcColorProperty);
            set => SetValue(ArcColorProperty, value);
        }
        public double StrokeWidth
        {
            get => (double)GetValue(StrokeWidthProperty);
            set => SetValue(StrokeWidthProperty, value);
        }
        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        public string Unit
        {
            get => (string)GetValue(UnitProperty);
            set => SetValue(UnitProperty, value);
        }

        private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
            => (d as CircularGauge)?.InvalidateVisual();

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double sw = StrokeWidth;
            double cx = w / 2, cy = h / 2;
            double r = Math.Min(cx, cy) - sw / 2 - 2;
            if (r <= 0) return;

            double maxV = MaxValue > 0 ? MaxValue : 100.0;
            double pct = Math.Clamp(Value, 0, maxV) / maxV;

            // ── Track arc (full circle) ───────────────────────────────────
            var trackPen = new Pen(TrackColor, sw) { LineJoin = PenLineJoin.Round };
            DrawArc(dc, cx, cy, r, 0, 360, trackPen);

            // ── Value arc ─────────────────────────────────────────────────
            if (pct > 0)
            {
                var valuePen = new Pen(ArcColor, sw) { LineJoin = PenLineJoin.Round };
                DrawArc(dc, cx, cy, r, -90, pct * 360 - 90, valuePen);
            }

            // ── Center text ───────────────────────────────────────────────
            var valueFt = new FormattedText(
                $"{Value:F0}{Unit}",
                System.Globalization.CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                r * 0.45,
                Foreground ?? Brushes.White,
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            dc.DrawText(valueFt,
                new Point(cx - valueFt.Width / 2, cy - valueFt.Height / 2 - (Label.Length > 0 ? 8 : 0)));

            // ── Label beneath value ────────────────────────────────────────
            if (!string.IsNullOrEmpty(Label))
            {
                var labelFt = new FormattedText(
                    Label,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    new Typeface("Segoe UI"),
                    r * 0.22,
                    Themes.ThemeManager.GetBrush("TextSub"),
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(labelFt,
                    new Point(cx - labelFt.Width / 2, cy + valueFt.Height / 2 - 6));
            }
        }

        private static void DrawArc(DrawingContext dc, double cx, double cy, double r,
            double startDeg, double endDeg, Pen pen)
        {
            // Convert degrees to radians
            double startRad = startDeg * Math.PI / 180.0;
            double endRad = endDeg * Math.PI / 180.0;

            var startPt = new Point(cx + r * Math.Cos(startRad), cy + r * Math.Sin(startRad));
            var endPt = new Point(cx + r * Math.Cos(endRad), cy + r * Math.Sin(endRad));

            double sweepDeg = endDeg - startDeg;
            bool isLargeArc = sweepDeg > 180;

            var geo = new StreamGeometry();
            using (var ctx = geo.Open())
            {
                ctx.BeginFigure(startPt, false, false);
                ctx.ArcTo(endPt, new Size(r, r), 0, isLargeArc, SweepDirection.Clockwise, true, false);
            }
            geo.Freeze();
            dc.DrawGeometry(null, pen, geo);
        }
    }
}
