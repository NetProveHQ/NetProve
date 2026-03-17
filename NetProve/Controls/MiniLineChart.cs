using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Windows;
using System.Windows.Media;

namespace NetProve.Controls
{
    /// <summary>
    /// A minimal real-time line chart for displaying metric history.
    /// Pure WPF, no external libraries.
    /// </summary>
    public sealed class MiniLineChart : FrameworkElement
    {
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(nameof(Data), typeof(ObservableCollection<double>),
                typeof(MiniLineChart),
                new FrameworkPropertyMetadata(null, OnDataChanged));

        public static readonly DependencyProperty LineColorProperty =
            DependencyProperty.Register(nameof(LineColor), typeof(Brush), typeof(MiniLineChart),
                new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty FillColorProperty =
            DependencyProperty.Register(nameof(FillColor), typeof(Brush), typeof(MiniLineChart),
                new FrameworkPropertyMetadata(null,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty MaxValueProperty =
            DependencyProperty.Register(nameof(MaxValue), typeof(double), typeof(MiniLineChart),
                new FrameworkPropertyMetadata(100.0, FrameworkPropertyMetadataOptions.AffectsRender));

        public ObservableCollection<double>? Data
        {
            get => (ObservableCollection<double>?)GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
        public Brush LineColor
        {
            get => (Brush)GetValue(LineColorProperty);
            set => SetValue(LineColorProperty, value);
        }
        public Brush? FillColor
        {
            get => (Brush?)GetValue(FillColorProperty);
            set => SetValue(FillColorProperty, value);
        }
        public double MaxValue
        {
            get => (double)GetValue(MaxValueProperty);
            set => SetValue(MaxValueProperty, value);
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var chart = (MiniLineChart)d;
            if (e.OldValue is ObservableCollection<double> oldCol)
                oldCol.CollectionChanged -= chart.OnCollectionChanged;
            if (e.NewValue is ObservableCollection<double> newCol)
                newCol.CollectionChanged += chart.OnCollectionChanged;
            chart.InvalidateVisual();
        }

        private void OnCollectionChanged(object? s, NotifyCollectionChangedEventArgs e)
            => Dispatcher.Invoke(InvalidateVisual);

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);
            var data = Data;
            if (data == null || data.Count < 2) return;

            double w = ActualWidth, h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            var pts = new Point[data.Count];
            double maxV = MaxValue > 0 ? MaxValue : 1;
            double step = w / (data.Count - 1);

            for (int i = 0; i < data.Count; i++)
                pts[i] = new Point(i * step, h - Math.Clamp(data[i] / maxV, 0, 1) * h);

            // Fill area
            if (FillColor != null)
            {
                var fillGeo = new StreamGeometry();
                using var fillCtx = fillGeo.Open();
                fillCtx.BeginFigure(new Point(pts[0].X, h), true, true);
                foreach (var pt in pts) fillCtx.LineTo(pt, false, false);
                fillCtx.LineTo(new Point(pts[pts.Length - 1].X, h), false, false);
                fillGeo.Freeze();
                dc.DrawGeometry(FillColor, null, fillGeo);
            }

            // Line
            var lineGeo = new StreamGeometry();
            using (var lineCtx = lineGeo.Open())
            {
                lineCtx.BeginFigure(pts[0], false, false);
                for (int i = 1; i < pts.Length; i++)
                    lineCtx.LineTo(pts[i], true, false);
            }
            lineGeo.Freeze();
            dc.DrawGeometry(null, new Pen(LineColor, 2) { LineJoin = PenLineJoin.Round }, lineGeo);
        }
    }
}
