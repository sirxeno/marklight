﻿using System.Collections.Generic;

namespace MarkLight.Views.UI
{
    /// <summary>
    /// Group view.
    /// </summary>
    /// <d>The group is used to spacially arrange child views next to each other either horizontally or vertically.</d>
    [HideInPresenter]
    public class Group : UIView
    {
        #region Fields

        /// <summary>
        /// Orientation of the group.
        /// </summary>
        /// <d>The orientation of the group that determines how the child views will be arranged.</d>
        [ChangeHandler("LayoutChanged")]
        public _ElementOrientation Orientation;

        /// <summary>
        /// Indicates how items overflow.
        /// </summary>
        /// <d>Enum indicating how items should overflow as they reach the boundaries of the group container.</d>
        public _OverflowMode Overflow;

        /// <summary>
        /// Spacing between views.
        /// </summary>
        /// <d>Determines the spacing to be added between views in the group.</d>
        [ChangeHandler("LayoutChanged")]
        public _ElementSize Spacing;

        /// <summary>
        /// Horizontal spacing between child views.
        /// </summary>
        /// <d>The horizontal spacing between child views.</d>
        [ChangeHandler("LayoutChanged")]
        public _ElementSize HorizontalSpacing;

        /// <summary>
        /// Vertical spacing between child views.
        /// </summary>
        /// <d>The vertical spacing between child views.</d>
        [ChangeHandler("LayoutChanged")]
        public _ElementSize VerticalSpacing;

        /// <summary>
        /// Content alignment.
        /// </summary>
        /// <d>Determines how the children should be aligned in relation to each other.</d>
        [ChangeHandler("LayoutChanged")]
        public _ElementAlignment ContentAlignment;

        /// <summary>
        /// Sort direction.
        /// </summary>
        /// <d>If children has SortIndex set the sort direction determines which order the views should be arranged in
        /// the group.</d>
        [ChangeHandler("LayoutChanged")]
        public _ElementSortDirection SortDirection;

        /// <summary>
        /// Sets the visibility of children so they are made visible after they are arranged.
        /// </summary>
        /// <d>Boolean indicating that the group should set the visibility of children so they are only made visible
        /// after they are arranged.</d>
        public _bool SetChildVisibility;

        #endregion

        #region Methods

        public override void InitializeInternalDefaultValues()
        {
            base.InitializeInternalDefaultValues();

            LayoutCalculator = new GroupLayoutCalculator();
        }

        public override void SetDefaultValues()
        {
            base.SetDefaultValues();

            Spacing.DirectValue = new ElementSize();
            Orientation.DirectValue = ElementOrientation.Vertical;
            SortDirection.DirectValue = ElementSortDirection.Ascending;
        }

        public override bool CalculateLayoutChanges(LayoutChangeContext context) {

            var layoutCalc = LayoutCalculator as GroupLayoutCalculator;
            if (layoutCalc != null)
            {
                layoutCalc.AdjustToWidth = !Width.IsSet;
                layoutCalc.AdjustToHeight = !Height.IsSet;

                layoutCalc.HorizontalSpacing = HorizontalSpacing.IsSet
                    ? HorizontalSpacing.Value
                    : Spacing.Value;

                layoutCalc.VerticalSpacing = VerticalSpacing.IsSet
                    ? VerticalSpacing.Value
                    : Spacing.Value;

                layoutCalc.Alignment = ContentAlignment.IsSet ? ContentAlignment.Value : ElementAlignment.Top;
                layoutCalc.Orientation = Orientation.Value;
                layoutCalc.Overflow = Overflow.Value;
            }

            return base.CalculateLayoutChanges(context);
        }

        protected override List<UIView> GetContentChildren(View content) {
            return GetContentChildren(content, SortDirection);
        }

        /// <summary>
        /// Initializes the view.
        /// </summary>
        public override void Initialize()
        {
            if (SetChildVisibility)
            {
                // set inactive children as not visible
                Content.ForEachChild<UIView>(x =>
                {
                    if (!x.IsActive)
                    {
                        x.IsVisible.Value = false;
                    }
                }, ViewSearchArgs.NonRecursive);
            }

            base.Initialize();            
        }
        
        #endregion
    }
}
