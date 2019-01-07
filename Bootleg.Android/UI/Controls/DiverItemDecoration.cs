/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;
using Android.Graphics.Drawables;
using Java.Lang;
using Android.Graphics;
using Android.Content.Res;

namespace Bootleg.Droid.UI
{
    public class DividerItemDecoration : RecyclerView.ItemDecoration
    {

        private static int[] ATTRS = new int[] { Android.Resource.Attribute.ListDivider };

    public static int HORIZONTAL_LIST = LinearLayoutManager.Horizontal;

    public static int VERTICAL_LIST = LinearLayoutManager.Vertical;

    private Drawable mDivider;

    private int mOrientation;

    public DividerItemDecoration(Context context, int orientation)
    {
        TypedArray a = context.ObtainStyledAttributes(ATTRS);
        mDivider = a.GetDrawable(0);
        a.Recycle();
        setOrientation(orientation);
    }

    public void setOrientation(int orientation)
    {
        if (orientation != HORIZONTAL_LIST && orientation != VERTICAL_LIST)
        {
            throw new IllegalArgumentException("invalid orientation");
        }
        mOrientation = orientation;
    }

        

    public void drawVertical(Canvas c, RecyclerView parent)
    {
         int left = parent.PaddingLeft;
         int right = parent.Width - parent.PaddingRight;

         int childCount = parent.ChildCount;
        for (int i = 0; i < childCount; i++)
        {
           View child = parent.GetChildAt(i);
           RecyclerView.LayoutParams p = (RecyclerView.LayoutParams)child.LayoutParameters;
           int top = child.Bottom + p.BottomMargin;
           int bottom = top + mDivider.IntrinsicHeight;
           mDivider.SetBounds(left, top, right, bottom);
           mDivider.Draw(c);
        }
    }

    public void drawHorizontal(Canvas c, RecyclerView parent)
    {
         int top = parent.PaddingTop;
         int bottom = parent.Height - parent.PaddingBottom;

         int childCount = parent.ChildCount;
        for (int i = 0; i < childCount; i++)
        {
             View child = parent.GetChildAt(i);
             RecyclerView.LayoutParams p = (RecyclerView.LayoutParams)child
                    .LayoutParameters;
             int left = child.Right + p.RightMargin;
             int right = left + mDivider.IntrinsicHeight;
            mDivider.SetBounds(left, top, right, bottom);
            mDivider.Draw(c);
        }
    }


        public override void OnDraw(Canvas cValue, RecyclerView parent)
        {
            if (mOrientation == VERTICAL_LIST)
            {
                drawVertical(cValue, parent);
            }
            else {
                drawHorizontal(cValue, parent);
            }
        }


        public override void GetItemOffsets(Rect outRect, int itemPosition, RecyclerView parent)
        {
            if (mOrientation == VERTICAL_LIST)
            {
                outRect.Set(0, 0, 0, mDivider.IntrinsicHeight);
            }
            else {
                outRect.Set(0, 0, mDivider.IntrinsicWidth, 0);
            }
        }
    }
}