/*
* Copyright (C) 2015 The Android Open Source Project
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;
using Android.Content;
using Android.Util;
using Android.Views;
using Android.Widget;

namespace Bootleg.Droid
{
    //public class SeekBarRotator
    //{
    //    public SeekBarRotator()
    //    {
    //    }
    //}

    /*
     *  This ViewGroup contains a single view, which will be rotated by 90 degrees counterclockwise.
     */
    public class SeekBarRotator : FrameLayout
    {
        public SeekBarRotator(Context context):base(context)
    {
    }
        public SeekBarRotator(Context context, IAttributeSet attrs):base(context, attrs)
    {
    }
        public SeekBarRotator(Context context, IAttributeSet attrs, int defStyleAttr): base(context, attrs, defStyleAttr)
    {
    }

    public SeekBarRotator(Context context, IAttributeSet attrs, int defStyleAttr,
                          int defStyleRes) :base(context, attrs, defStyleAttr, defStyleRes)
    {
        
    }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
    {
         View child = GetChildAt(0);
            if (child.Visibility != ViewStates.Gone)
        {
            // swap width and height for child
            MeasureChild(child, heightMeasureSpec, widthMeasureSpec);
            SetMeasuredDimension(
                    child.MeasuredHeightAndState,
                    child.MeasuredWidthAndState);
        }
        else
        {
            SetMeasuredDimension(
                    ResolveSizeAndState(0, widthMeasureSpec, 0),
                    ResolveSizeAndState(0, heightMeasureSpec, 0));
        }
    }


        protected override void OnLayout(bool changed, int l, int t, int r, int b)
    {
        View child = GetChildAt(0);
            if (child.Visibility != ViewStates.Gone)
        {
                // rotate the child 90 degrees counterclockwise around its upper-left

                if (Resources.Configuration.LayoutDirection == Android.Views.LayoutDirection.Rtl)
                    this.Rotation = 180;



                //(child as SeekBar).Rotation = 180;
                child.PivotX = 0;
                child.PivotY = 0;
                child.Rotation = -90;


               
            //else
            //child.Rotation = -90;

            // place the child below this view, so it rotates into view
            int mywidth = r - l;
            int myheight = b - t;
            int childwidth = myheight;
            int childheight = mywidth;


                child.Layout(0, myheight, childwidth, myheight + childheight);
        }
    }
}
}
