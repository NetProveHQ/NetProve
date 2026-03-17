// Resolve ambiguity between WPF and WinForms types when UseWindowsForms is enabled
global using Application = System.Windows.Application;
global using Brush = System.Windows.Media.Brush;
global using Brushes = System.Windows.Media.Brushes;
global using Color = System.Windows.Media.Color;
global using Pen = System.Windows.Media.Pen;
global using Control = System.Windows.Controls.Control;
global using Button = System.Windows.Controls.Button;
global using RadioButton = System.Windows.Controls.RadioButton;
global using MessageBox = System.Windows.MessageBox;
global using HorizontalAlignment = System.Windows.HorizontalAlignment;
global using Point = System.Windows.Point;
global using Size = System.Windows.Size;
global using FlowDirection = System.Windows.FlowDirection;
