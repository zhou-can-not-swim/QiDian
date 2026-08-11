using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace QiDian.Converters
{
    public class CollectionEmptyToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 集合有数据=Visible；空=Collapsed
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isEmpty = true;
            if (value is IEnumerable collection)
            {
                isEmpty = !collection.Cast<object>().Any();//
            }
            bool invert = parameter?.ToString() == "Invert";
            bool show = invert ? isEmpty : !isEmpty;


            return show ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
