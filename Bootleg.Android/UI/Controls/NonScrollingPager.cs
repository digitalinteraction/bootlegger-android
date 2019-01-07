/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using Android.Content;
using Android.Views;
using Android.Support.V4.View;
using Android.Util;

namespace Bootleg.Droid.UI
{
    public class NonScrollingPager :ViewPager
    {
        public NonScrollingPager(Context context):base(context)
        {
            PagingEnabled = false;
        }

        public NonScrollingPager(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            PagingEnabled = false;
        }

        public bool PagingEnabled {get; set; }
        
        public override bool OnTouchEvent(MotionEvent e) {
                return this.PagingEnabled && base.OnTouchEvent(e);
            }

       
        public override bool OnInterceptTouchEvent(MotionEvent e) {
            return this.PagingEnabled && base.OnInterceptTouchEvent(e);
        }
    }
}