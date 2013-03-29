using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace SuperAlarm
{
    public static class Extensions
    {
        public static List<T> AllChildren<T>(this FrameworkElement ele, Func<DependencyObject, bool> whereFunc = null) where T : class
        {
            if (ele == null)
                return null;
            var output = new List<T>();
            var c = VisualTreeHelper.GetChildrenCount(ele);
            for (var i = 0; i < c; i++)
            {
                var ch = VisualTreeHelper.GetChild(ele, i);
                if (whereFunc != null)
                {
                    if (!whereFunc(ch))
                    {
                        continue;
                    }
                }
                if ((ch is T))
                    output.Add(ch as T);
                if (!(ch is FrameworkElement))
                    continue;

                output.AddRange((ch as FrameworkElement).AllChildren<T>(whereFunc));
            }
            return output;
        }
    }
}
