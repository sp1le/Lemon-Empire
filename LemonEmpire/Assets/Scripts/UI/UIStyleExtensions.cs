using UnityEngine;
using UnityEngine.UIElements;

namespace LemonEmpire.UI
{
    public static class UIStyleExtensions
    {
        public static void SetBorderRadius(this IStyle style, float radius)
        {
            style.borderTopLeftRadius = radius;
            style.borderTopRightRadius = radius;
            style.borderBottomLeftRadius = radius;
            style.borderBottomRightRadius = radius;
        }

        public static void SetBorderWidth(this IStyle style, float width)
        {
            style.borderLeftWidth = width;
            style.borderRightWidth = width;
            style.borderTopWidth = width;
            style.borderBottomWidth = width;
        }

        public static void SetBorderColor(this IStyle style, Color color)
        {
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
        }

        public static void SetBorderColor(this IStyle style, StyleColor color)
        {
            style.borderLeftColor = color;
            style.borderRightColor = color;
            style.borderTopColor = color;
            style.borderBottomColor = color;
        }
    }
}
