/*
 * Copyright 2015 The Android Open Source Project
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */
using Android.Content;
using Android.Support.Design.Widget;
using Android.Support.V4.View;
using Android.Util;
using Android.Views;
using Bootleg.API;
using Java.Lang;
using System.Collections.Generic;
using static Android.Support.Design.Widget.Snackbar;

namespace Bootleg.Droid.UI
{

    public class MyFABAwareScrollingViewBehavior : CoordinatorLayout.Behavior
    {
        public MyFABAwareScrollingViewBehavior(Context context, IAttributeSet attrs):base()
        {
            
        }

        FloatingActionButton thisbutton;
        int page;
        ViewPager pager;

        public MyFABAwareScrollingViewBehavior(Context context, FloatingActionButton thisbutton, int page, ViewPager pager) : base()
        {
            this.thisbutton = thisbutton;
            this.page = page;
            this.pager = pager;
        }

        public override bool LayoutDependsOn(CoordinatorLayout parent, Object child, View dependency)
        {
            return dependency is SnackbarLayout;
        }

        public override bool OnDependentViewChanged(CoordinatorLayout parent, Object child, View dependency)
        {
            float translationY = Math.Min(0, dependency.TranslationY - dependency.Height);
            (child as View).TranslationY = translationY;
            return true;
        }

        public override bool OnStartNestedScroll(CoordinatorLayout coordinatorLayout, Object child, View directTargetChild, View target, int nestedScrollAxes)
        {
            return nestedScrollAxes == ViewCompat.ScrollAxisVertical || base.OnStartNestedScroll(coordinatorLayout, child, directTargetChild, target, nestedScrollAxes);
        }

        public override void OnNestedScroll(CoordinatorLayout coordinatorLayout, Object child, View target, int dxConsumed, int dyConsumed, int dxUnconsumed, int dyUnconsumed)
        {
            base.OnNestedScroll(coordinatorLayout, child, target, dxConsumed, dyConsumed, dxUnconsumed, dyUnconsumed);
            //if (dyConsumed > 0 && (child as FloatingActionButton).Visibility  == ViewStates.Visible)
            //{
            //    // User scrolled down and the FAB is currently visible -> hide the FAB
            //    if (thisbutton != null)
            //    {
            //        if (thisbutton == child && pager.CurrentItem == page)
            //            thisbutton.Hide();
            //        else
            //            thisbutton.Hide();
            //    }
            //    else
            //        (child as FloatingActionButton).Hide();
            //}
            //else if (dyConsumed < 0 && (child as FloatingActionButton).Visibility != ViewStates.Visible)
            //{
            //    // User scrolled up and the FAB is currently not visible -> show the FAB
            //    if (Bootlegger.BootleggerClient.CurrentUser != null)
            //        if (thisbutton != null)
            //            if (thisbutton == child && pager.CurrentItem == page)
            //                thisbutton.Show();
            //            else
            //                thisbutton.Hide();
            //        else
            //            (child as FloatingActionButton).Show();
            //}
        }
    }
}