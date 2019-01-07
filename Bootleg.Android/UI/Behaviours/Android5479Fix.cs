using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Widget;
using static Android.Views.ViewTreeObserver;

namespace Bootleg.Droid.UI
{
    public class AndroidBug5497Workaround
    {

        // For more information, see https://code.google.com/p/android/issues/detail?id=5497
        // To use this class, simply invoke assistActivity() on an Activity that already has its content view set.

        public static void AssistActivity(Activity activity)
        {
            new AndroidBug5497Workaround(activity);
        }

        private class Listener : Java.Lang.Object,IOnGlobalLayoutListener
        {
            private AndroidBug5497Workaround bug;
            public Listener(AndroidBug5497Workaround bug)
            {
                this.bug = bug;
            }

            public void OnGlobalLayout() => bug.possiblyResizeChildOfContent();
        }

        private View mChildOfContent;
        private int usableHeightPrevious;
        private FrameLayout.LayoutParams frameLayoutParams;

        private AndroidBug5497Workaround(Activity activity)
        {
            FrameLayout content = activity.FindViewById<FrameLayout>(Android.Resource.Id.Content);
            mChildOfContent = content.GetChildAt(0);
            mChildOfContent.ViewTreeObserver.AddOnGlobalLayoutListener(new Listener(this));
            frameLayoutParams = (FrameLayout.LayoutParams)mChildOfContent.LayoutParameters;
        }

    private void possiblyResizeChildOfContent()
    {
        int usableHeightNow = computeUsableHeight();
        if (usableHeightNow != usableHeightPrevious)
        {
            
            int navigationBarHeight = 0;
            //int resourceId = mChildOfContent.Resources.GetIdentifier("navigation_bar_height", "dimen", "android");
            //if (resourceId > 0)
            //{
            //    navigationBarHeight = mChildOfContent.Resources.GetDimensionPixelSize(resourceId);
            //}

            int usableHeightSansKeyboard = mChildOfContent.RootView.Height - navigationBarHeight;

            int heightDifference = usableHeightSansKeyboard - usableHeightNow;
            if (heightDifference > (usableHeightSansKeyboard / 4))
            {
                // keyboard probably just became visible
                frameLayoutParams.Height = usableHeightSansKeyboard - heightDifference;
            }
            else
            {
                // keyboard probably just became hidden
                frameLayoutParams.Height = usableHeightSansKeyboard;
            }
            mChildOfContent.RequestLayout();
            usableHeightPrevious = usableHeightNow;
        }
    }

    private int computeUsableHeight()
    {
        Rect r = new Rect();
        mChildOfContent.GetWindowVisibleDisplayFrame(r);
       
        return (r.Bottom - r.Top);
    }

}
}