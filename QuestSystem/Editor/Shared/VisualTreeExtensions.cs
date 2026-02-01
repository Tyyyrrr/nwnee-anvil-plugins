using System.Windows.Media;
using System.Windows;

namespace QuestEditor.Shared
{
    public static class VisualTreeExtensions
    {
        public static T? FindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T parent)
                    return parent;
                child = VisualTreeHelper.GetParent(child);
            }
            return null;
        }
        public static T? FindChild<T>(this DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if (child is T typed)
                    return typed;

                var result = FindChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        public static IReadOnlyList<T> FindChildren<T>(this DependencyObject parent) where T : DependencyObject
        {
            List<T> result = [];

            for(int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);

                if(child is T typed)
                    result.Add(typed);

                result.AddRange(FindChildren<T>(child));
            }

            return result;
        }
    }
}