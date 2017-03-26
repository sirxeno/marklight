using System.Collections.Generic;
using MarkLight.Views.UI;
using UnityEngine;

namespace MarkLight
{
    /// <summary>
    /// Arranges child view layout horizontally or vertically within a parent view.
    /// </summary>
    public class GroupLayoutCalculator : LayoutCalculator
    {

        #region Fields

        /// <summary>
        /// Determine if the Width should be affected.
        /// </summary>
        public bool AdjustToWidth;

        /// <summary>
        /// Determine if the Height should be affected.
        /// </summary>
        public bool AdjustToHeight;

        /// <summary>
        /// Alignment of child elements.
        /// </summary>
        public ElementAlignment Alignment;

        /// <summary>
        /// Child layout orientation.
        /// </summary>
        public ElementOrientation Orientation;

        /// <summary>
        /// Get or set how child layout overflow is handled.
        /// </summary>
        public OverflowMode Overflow;

        /// <summary>
        /// Get or set the container padding when setting width or height.
        /// </summary>
        public ElementMargin Padding;

        /// <summary>
        /// Get or set the scroll content view, if any.
        /// </summary>
        public UIView ScrollContent;

        #endregion

        #region Methods

        public override bool CalculateLayoutChanges(UIView view, IList<UIView> children, LayoutChangeContext context)
        {
            var parentLayout = view.Layout;

            var maxWidth = 0f;
            var maxHeight = 0f;
            var totalWidth = 0f;
            var totalHeight = 0f;

            var isHorizontal = Orientation == ElementOrientation.Horizontal;

            // get size of content and set content offsets and alignment
            var childCount = children.Count;
            var childIndex = 0;
            var firstItem = true;
            float xOffset = 0;
            float yOffset = 0;
            float maxColumnWidth = 0;
            float maxRowHeight = 0;

            var rows = new List<Row>();
            var row = new Row();

            var isLeftAligned = Alignment.HasFlag(ElementAlignment.Left);
            var isTopAligned = Alignment.HasFlag(ElementAlignment.Top);
            var isRightAligned = Alignment.HasFlag(ElementAlignment.Right);
            var isBottomAligned = Alignment.HasFlag(ElementAlignment.Bottom);

            var padLeft = Padding == null ? 0f : parentLayout.WidthToPixels(Padding.Left);
            var padTop = Padding == null ? 0f : parentLayout.WidthToPixels(Padding.Top);
            var padRight = Padding == null ? 0f : parentLayout.WidthToPixels(Padding.Right);
            var padBottom = Padding == null ? 0f : parentLayout.WidthToPixels(Padding.Bottom);

            for (var i = 0; i < childCount; i++)
            {
                var viewIndex = isRightAligned ? childCount - 1 - i : i;
                var child = children[viewIndex];

                // don't group disabled or destroyed views
                if (!child.IsActive || child.IsDestroyed || !CanEffectChild(child))
                    continue;

                var boxWidth = child.Layout.AspectPixelWidth;
                var boxHeight = child.Layout.AspectPixelHeight;
                var topMargin = child.Layout.MarginTopPixels;
                var bottomMargin = child.Layout.MarginBottomPixels;
                var leftMargin = child.Layout.MarginLeftPixels;
                var rightMargin = child.Layout.MarginRightPixels;
                var displacedWidth = boxWidth + leftMargin + rightMargin;
                var displacedHeight = boxHeight + topMargin + bottomMargin;

                // set offsets and alignment
                var offset = new ElementMargin(
                    new ElementSize(
                        (isRightAligned ? -1 : 1) * (isHorizontal ? totalWidth : 0f),
                        ElementSizeUnit.Pixels),
                    new ElementSize(!isHorizontal ? totalHeight : 0f,
                        ElementSizeUnit.Pixels));

                if (isLeftAligned)
                {
                    offset.Left.Value += padLeft;
                }
                else if (isRightAligned)
                {
                    offset.Right.Value += padRight;
                }

                if (isTopAligned)
                {
                    offset.Top.Value += padTop;
                }
                else if (isBottomAligned)
                {
                    offset.Bottom.Value += padBottom;
                }

                child.Layout.OffsetFromParent = offset;
                child.Layout.Alignment = Alignment;

                if (Overflow == OverflowMode.Overflow)
                {
                    // get size of content
                    totalWidth += displacedWidth;
                    maxWidth = Mathf.Max(maxWidth, displacedWidth);

                    totalHeight += displacedHeight;
                    maxHeight = Mathf.Max(maxHeight, displacedHeight);

                    if (!isHorizontal)
                    {
                        if (row.Children.Count > 0)
                            rows.Add(row);

                        row = new Row();
                    }
                }
                else
                {
                    // overflow mode is set to wrap

                    // set offsets of item
                    if (isHorizontal)
                    {
                        if (firstItem)
                        {
                            // first item, don't check for overflow
                            xOffset = 0;
                            firstItem = false;
                        }
                        else if (xOffset + displacedWidth > parentLayout.AspectPixelWidth)
                        {
                            if (row.Children.Count > 0)
                                rows.Add(row);

                            row = new Row();

                            // item overflows to the next row
                            xOffset = 0;
                            yOffset += maxRowHeight;
                            maxRowHeight = 0;
                        }

                        // set offset
                        child.Layout.OffsetFromParent = new ElementMargin(
                            ElementSize.FromPixels(xOffset),
                            ElementSize.FromPixels(yOffset));

                        xOffset += displacedWidth;
                        maxRowHeight = Mathf.Max(maxRowHeight, displacedHeight);
                        maxWidth = Mathf.Max(maxWidth, xOffset);
                        maxHeight = Mathf.Max(maxHeight, yOffset + displacedHeight);
                    }
                    else
                    {
                        if (row.Children.Count > 0)
                        {
                            rows.Add(row);
                            row = new Row();
                        }

                        if (firstItem)
                        {
                            yOffset = 0;
                            firstItem = false;
                        }
                        else if (yOffset + displacedHeight > parentLayout.AspectPixelHeight)
                        {
                            // overflow to next column
                            yOffset = 0;
                            xOffset += maxColumnWidth;
                            maxColumnWidth = 0;
                        }

                        // set offset
                        child.Layout.OffsetFromParent = new ElementMargin(
                            ElementSize.FromPixels(xOffset),
                            ElementSize.FromPixels(yOffset));

                        yOffset += displacedHeight;
                        maxColumnWidth = Mathf.Max(maxColumnWidth, displacedWidth);
                        maxWidth = Mathf.Max(maxWidth, xOffset + displacedWidth);
                        maxHeight = Mathf.Max(maxHeight, yOffset);
                    }
                }

                row.AddChild(child, displacedWidth);

                // update child layout
                context.NotifyLayoutUpdated(child);
                ++childIndex;
            }

            // horizontally centered
            if (!isLeftAligned && !isRightAligned)
            {
                if (row.Children.Count > 0)
                    rows.Add(row);

                // adjust left offset for center alignment
                foreach (var r in rows)
                {
                    var center = r.Width / 2f - r.FirstChildWidth / 2f;
                    foreach (var child in r.Children)
                    {
                        child.Layout.OffsetFromParent.Left.Value -= center;
                    }
                }
            }

            var updateScrollContent = false;

            if (Overflow == OverflowMode.Overflow)
            {
                var widthPadding = GetHorizontalPadding(parentLayout) + padLeft + padRight;

                totalWidth += widthPadding;
                maxWidth += widthPadding;

                // set width and height of list
                if (AdjustToWidth)
                {
                    // adjust width to content
                    view.Layout.Width = new ElementSize(isHorizontal
                        ? totalWidth
                        : maxWidth, ElementSizeUnit.Pixels);
                }
                else if (ScrollContent != null)
                {
                    // adjust width of scrollable area to size
                    ScrollContent.Layout.Width = new ElementSize(isHorizontal
                        ? totalWidth
                        : maxWidth, ElementSizeUnit.Pixels);

                    updateScrollContent = true;
                }

                var heightPadding = GetVerticalPadding(parentLayout) + padTop + padBottom;

                totalHeight += heightPadding;
                maxHeight += heightPadding;

                if (AdjustToHeight)
                {
                    // if height is not explicitly set then adjust to content
                    view.Layout.Height = new ElementSize(isHorizontal
                        ? maxHeight
                        : totalHeight, ElementSizeUnit.Pixels);
                }
                else if (ScrollContent != null)
                {
                    // adjust width of scrollable area to size
                    ScrollContent.Layout.Height = new ElementSize(isHorizontal
                        ? maxHeight
                        : totalHeight, ElementSizeUnit.Pixels);

                    updateScrollContent = true;
                }
            }
            else // Wrap
            {
                // adjust size to content
                if (isHorizontal)
                {
                    maxHeight += GetVerticalPadding(parentLayout) + padTop + padBottom;

                    if (ScrollContent != null)
                    {
                        ScrollContent.Layout.Height = ElementSize.FromPixels(maxHeight);
                        updateScrollContent = true;
                    }
                    else if (AdjustToHeight)
                    {
                        parentLayout.Height = ElementSize.FromPixels(maxHeight);
                    }
                }
                else // Vertical
                {
                    maxWidth += GetHorizontalPadding(parentLayout) + padLeft + padRight;

                    if (ScrollContent != null)
                    {
                        ScrollContent.Layout.Width = ElementSize.FromPixels(maxWidth);
                        updateScrollContent = true;
                    }
                    else if (AdjustToWidth)
                    {
                        parentLayout.Width = ElementSize.FromPixels(maxWidth);
                    }
                }
            }

            if (updateScrollContent)
            {
                context.NotifyLayoutUpdated(ScrollContent);
            }

            return parentLayout.IsDirty;
        }

        /// <summary>
        /// Get the sum of all horizontal margins and padding.
        /// </summary>
        /// <param name="layout">The layout data to get margins and padding from.</param>
        protected virtual float GetHorizontalPadding(LayoutData layout)
        {
            return 0f;
        }

        /// <summary>
        /// Get the sum of all vertical margins and padding.
        /// </summary>
        /// <param name="layout">The layout data to get margins and padding from.</param>
        protected virtual float GetVerticalPadding(LayoutData layout)
        {
            return 0f;
        }

        #endregion

        #region Properties

        public override bool IsChildLayout
        {
            get { return true; }
        }

        public override bool IsAffectedByChildren
        {
            get { return true; }
        }

        #endregion

        #region Classes

        /// <summary>
        /// Layout row data context.
        /// </summary>
        private class Row
        {
            public float Width;
            public float FirstChildWidth;
            public readonly List<UIView> Children = new List<UIView>(10);

            public void AddChild(UIView child, float width) {

                if (Children.Count == 0)
                    FirstChildWidth = width;

                Children.Add(child);
                Width += width;
            }
        }

        #endregion
    }
}