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
using Android.Views.Animations;

namespace Bootleg.Droid.UI
{
    public class ResizeAnimation : Animation
    {
        View view;
    int startH;
    int endH;
    int diff;

    public ResizeAnimation(View v, int newh)
    {
        view = v;
        startH = v.LayoutParameters.Height;
        endH = newh;
        diff = endH - startH;
    }

        protected override void ApplyTransformation(float interpolatedTime, Transformation t)
        {
            view.LayoutParameters.Height = startH + (int)(diff * interpolatedTime);
            //view.LayoutParameters.Height = endH;
            view.RequestLayout();
        }

        public override void Initialize(int width, int height, int parentWidth, int parentHeight)
        {
            base.Initialize(width, height, parentWidth, parentHeight);
        }

        public override bool WillChangeBounds()
        {
            return true;
        }
    }
}