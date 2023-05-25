﻿using Android.Content.Res;
using Android.Views;
using AndroidX.ViewPager2.Widget;

namespace WB.UI.Shared.Extensions.Activities.Carousel;

public class CarouselIPageTransformer : Java.Lang.Object, ViewPager2.IPageTransformer
{
    public void TransformPage(View page, float position)
    {
        var nextItemVisiblePx = page.Resources.GetDimension(Resource.Dimension.carousel_next_item_visible);
        var currentItemHorizontalMarginPx = page.Resources.GetDimension(Resource.Dimension.carousel_current_item_horizontal_margin);
        var pageTranslationX = nextItemVisiblePx + currentItemHorizontalMarginPx;

        if (page.Parent is View viewParent)
        {
            var minHeight = page.Resources.GetDimension(Resource.Dimension.carousel_current_item_min_height);
            page.TranslationY = (viewParent.MeasuredHeight - minHeight - page.MeasuredHeight) / 2;
        }

        page.TranslationY = position <= 0 ? -pageTranslationX * position : pageTranslationX * position;
        page.TranslationX = -pageTranslationX * position;
        page.ScaleY = 1 - (0.25f * Math.Abs(position));
        page.Alpha = 0.25f + (1 - Math.Abs(position));
        //page.Alpha = 0.50f + (1 - Math.Abs(position));
    }
}