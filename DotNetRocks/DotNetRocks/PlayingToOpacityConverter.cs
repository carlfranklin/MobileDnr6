using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Xamarin.Forms;

namespace DotNetRocks
{
    public class PlayingToOpacityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if ((bool)value)
            {
                if (parameter != null && System.Convert.ToInt32(parameter) == 1)
                {
                    return (double)0.4; // Play button Opacity when IsPlaying is true
                }
                else
                {
                    return (double)1.0; // Stop button Opacity when IsPlaying is true
                }
            }
            else
            {
                if (parameter != null && System.Convert.ToInt32(parameter) == 1)
                {
                    return (double)1.0; // Play button Opacity when IsPlaying is false
                }
                else
                {
                    return (double)0.4; // Stop button Opacity when IsPlaying is false
                }
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
