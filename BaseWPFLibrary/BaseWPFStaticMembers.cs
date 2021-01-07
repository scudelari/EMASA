using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace BaseWPFLibrary
{
    public static class BaseWPFStaticMembers
    {
        public static bool AreAllChildrenValid(this DependencyObject parent)
        {
            if (Validation.GetHasError(parent))
                return false;

            // Validate all the bindings on the children
            for (int i = 0; i != VisualTreeHelper.GetChildrenCount(parent); ++i)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (!AreAllChildrenValid(child)) { return false; }
            }

            return true;
        }

        public static bool GetAnyChildHasFocus(this UIElement inElement, bool inIncludeSelf = true)
        {
            Window ownerWindow = Window.GetWindow(inElement);
            if (ownerWindow == null) throw new InvalidOperationException($"Could not get the window that is the owner of {inElement}");

            var k = FocusManager.GetFocusedElement(ownerWindow);
            if (k == null) throw new InvalidOperationException($"Could not get the focused element.");

            if (inIncludeSelf && k == inElement) return true;

            // Gets recursively all children
            List<UIElement> allChildren = inElement.GetAllChildren();

            return allChildren.Any(a => a == k);
        }

        /// <summary>
        /// Analyzes both visual and logical tree in order to find all elements of a given
        /// type that are descendants of the <paramref name="source"/> item.
        /// </summary>
        /// <typeparam name="T">The type of the queried items.</typeparam>
        /// <param name="source">The root element that marks the source of the search. If the
        /// source is already of the requested type, it will not be included in the result.</param>
        /// <returns>All descendants of <paramref name="source"/> that match the requested type.</returns>
        public static IEnumerable<T> FindDescendants<T>(this DependencyObject source) where T : DependencyObject
        {
            if (source != null)
            {
                var childs = GetChildObjects(source);
                foreach (DependencyObject child in childs)
                {
                    //analyze if children match the requested type
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    //recurse tree
                    foreach (T descendant in FindDescendants<T>(child))
                    {
                        yield return descendant;
                    }
                }
            }
        }
        /// <summary>
        /// This method is an alternative to WPF's
        /// <see cref="VisualTreeHelper.GetChild"/> method, which also
        /// supports content elements. Keep in mind that for content elements,
        /// this method falls back to the logical tree of the element.
        /// </summary>
        /// <param name="parent">The item to be processed.</param>
        /// <returns>The submitted item's child elements, if available.</returns>
        public static IEnumerable<DependencyObject> GetChildObjects(this DependencyObject parent)
        {
            if (parent == null) yield break;

            if (parent is ContentElement || parent is FrameworkElement)
            {
                //use the logical tree for content / framework elements
                foreach (object obj in LogicalTreeHelper.GetChildren(parent))
                {
                    var depObj = obj as DependencyObject;
                    if (depObj != null) yield return (DependencyObject)obj;
                }
            }
            else
            {
                //use the visual tree per default
                int count = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < count; i++)
                {
                    yield return VisualTreeHelper.GetChild(parent, i);
                }
            }
        }

        public static List<UIElement> GetAllChildren(this DependencyObject inElement)
        {
            var list = new List<UIElement> { };
            for (int count = 0; count < VisualTreeHelper.GetChildrenCount(inElement); count++)
            {
                var child = VisualTreeHelper.GetChild(inElement, count);
                if (child is UIElement)
                {
                    list.Add(child as UIElement);
                }
                list.AddRange(GetAllChildren(child));
            }
            return list;
        }
        
        public static string DoubleToEngineering(double value, string displayPrecision)
        {
            string Retval;

            if (value == 0d) return "0";
            if (double.IsNaN(value)) return "NaN";

            if (double.IsInfinity(value)
                || double.IsNegativeInfinity(value)
                || double.IsPositiveInfinity(value)
            )
            {
                Retval = String.Format("{0:" + "F" + displayPrecision + "}", value);
                return Retval;
            }

            bool isNeg = value < 0;
            if (isNeg) value = -value;

            int exp = (int)(Math.Floor(Math.Log10(value) / 3.0) * 3.0);
            int powerToRaise = -exp;
            double newValue = value;
            // Problem: epsilon is something-324
            // The biggest possible number is somethinge306
            // You simply can't do a Math.Power (10, 324), it becomes infiniity.
            if (powerToRaise > 300)
            {
                powerToRaise -= 300;
                newValue = newValue * Math.Pow(10.0, 300);
            }

            newValue = newValue * Math.Pow(10.0, powerToRaise);

            // I don't know when this below is triggered.
            if (newValue >= 1000.0)
            {
                newValue = newValue / 1000.0;
                exp = exp + 3;
            }

            var fmt = "{0:F" + displayPrecision + "}";
            Retval = string.Format(fmt, newValue);
            if (exp != 0) Retval += string.Format("e{0}", exp);
            if (isNeg) Retval = "-" + Retval;
            return Retval;
        }
    }
}
