using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup.Primitives;
using System.Windows.Media;

namespace Sap2000Library.StaticHelpers
{
    public static class DependencyObjectHelper
    {
        public static List<BindingBase> GetBindingObjects(Object element)
        {
            List<BindingBase> bindings = new List<BindingBase>();
            List<DependencyProperty> dpList = new List<DependencyProperty>();
            dpList.AddRange(GetDependencyProperties(element));
            dpList.AddRange(GetAttachedProperties(element));

            foreach (DependencyProperty dp in dpList)
            {
                BindingBase b = BindingOperations.GetBindingBase(element as DependencyObject, dp);
                if (b != null)
                {
                    bindings.Add(b);
                }
            }

            return bindings;
        }

        public static List<DependencyProperty> GetDependencyProperties(Object element)
        {
            List<DependencyProperty> properties = new List<DependencyProperty>();
            MarkupObject markupObject = MarkupWriter.GetMarkupObjectFor(element);
            if (markupObject != null)
            {
                foreach (MarkupProperty mp in markupObject.Properties)
                {
                    if (mp.DependencyProperty != null)
                    {
                        properties.Add(mp.DependencyProperty);
                    }
                }
            }

            return properties;
        }

        public static List<DependencyProperty> GetAttachedProperties(Object element)
        {
            List<DependencyProperty> attachedProperties = new List<DependencyProperty>();
            MarkupObject markupObject = MarkupWriter.GetMarkupObjectFor(element);
            if (markupObject != null)
            {
                foreach (MarkupProperty mp in markupObject.Properties)
                {
                    if (mp.IsAttached)
                    {
                        attachedProperties.Add(mp.DependencyProperty);
                    }
                }
            }

            return attachedProperties;
        }

        public static IEnumerable<BindingBase> GetBindingsRecursive(DependencyObject dObj)
        {
            foreach (BindingBase item in GetBindingObjects(dObj))
            {
                yield return item;
            }

            int childrenCount = VisualTreeHelper.GetChildrenCount(dObj);
            if (childrenCount > 0)
            {
                for (int i = 0; i < childrenCount; i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(dObj, i);
                    foreach (BindingBase item in GetBindingsRecursive(child)) yield return item;
                }
            }
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChild<T>(DependencyObject parent, string childName)
           where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }
        public static IEnumerable<T> FindVisualChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depObj, i);
                    if (child != null && child is T)
                    {
                        yield return (T)child;
                    }

                    foreach (T childOfChild in FindVisualChildren<T>(child))
                    {
                        yield return childOfChild;
                    }
                }
            }
        }
        public static IEnumerable<T> FindLogicalChildren<T>(DependencyObject depObj) where T : DependencyObject
        {
            if (depObj != null)
            {
                foreach (object rawChild in LogicalTreeHelper.GetChildren(depObj))
                {
                    if (rawChild is DependencyObject)
                    {
                        DependencyObject child = (DependencyObject)rawChild;
                        if (child is T)
                        {
                            yield return (T)child;
                        }

                        foreach (T childOfChild in FindLogicalChildren<T>(child))
                        {
                            yield return childOfChild;
                        }
                    }
                }
            }
        }
    }
}
