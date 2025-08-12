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

            foreach (var child in element.GetVisualTreeDescendantsInternal())
            {
                yield return child;

                foreach (var descendant in GetVisualTreeDescendants(child))
                {
                    yield return descendant;
                }
            }
        }

        private static IEnumerable<Element> GetVisualTreeDescendantsInternal(this Element element)
        {
#pragma warning disable CS0618
            // Fallback for current MAUI: LogicalChildren still used in some cases; suppress obsoletion warning.
            foreach (var child in element.LogicalChildren)
            {
                yield return child;
            }
#pragma warning restore CS0618
        }
    }
} 