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
    }
}
