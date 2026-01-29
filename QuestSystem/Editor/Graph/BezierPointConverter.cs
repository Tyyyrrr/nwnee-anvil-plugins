using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace QuestEditor.Graph
{
    public readonly struct FromToPoint(Point from, Point to)
    {
        public readonly Point From = from;
        public readonly Point To = to;
    }

    public sealed class BezierPointConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!int.TryParse(parameter?.ToString(), out var i))
                throw new ArgumentException("ConverterParameter must be an integer", nameof(parameter));

            if (value is not FromToPoint ftp) throw new ArgumentException(value.ToString(), nameof(value));

            return i switch
            {
                1 => ftp.From,
                2 => GetFirstCurvePoint(ftp),
                3 => GetSecondCurvePoint(ftp),
                4 => ftp.To,
                _ => throw new ArgumentException(i.ToString(), nameof(parameter)),
            };
        }

        private static Point GetFirstCurvePoint(FromToPoint ftp) => new Point (ftp.From.X + ((ftp.To.X - ftp.From.X) / 3), ftp.From.X >= ftp.To.X ? ftp.To.Y : ftp.From.Y);
        private static Point GetSecondCurvePoint(FromToPoint ftp) => new Point (ftp.From.X + ((ftp.To.X - ftp.From.X) / 3 * 2), ftp.From.X >= ftp.To.X ? ftp.From.Y : ftp.To.Y);

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
