using System.Collections.Generic;
using Microsoft.Maui.Controls;

namespace HairCarePlus.Client.Patient.Common.Utils
{
    public static class VisualTreeExtensions
    {
        public static IEnumerable<Element> GetVisualTreeDescendants(this Element element)
        {
            if (element == null)
                yield break;

            foreach (var child in element.LogicalChildren)
            {
                yield return child;

                foreach (var descendant in GetVisualTreeDescendants(child))
                {
                    yield return descendant;
                }
            }
        }
    }
} 