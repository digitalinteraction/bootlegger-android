/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 
using Android.Content;
using Android.Graphics;
using Android.Util;
using Android.Views;
using Android.Widget;
namespace Bootleg.Droid
{
    public class VerticalSeekBar : SeekBar
    {

        public VerticalSeekBar(Context context)
            : base(context)
        {

        }

        public VerticalSeekBar(Context context, IAttributeSet attrs, int defStyle)
            : base(context, attrs, defStyle)
        {

        }

        public VerticalSeekBar(Context context, IAttributeSet attrs)
            : base(context, attrs)
        {

        }

        protected override void OnSizeChanged(int w, int h, int oldw, int oldh)
        {
            base.OnSizeChanged(h, w, oldh, oldw);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            base.OnMeasure(heightMeasureSpec, widthMeasureSpec);
            SetMeasuredDimension(MeasuredHeight, MeasuredWidth);
        }

        protected override void OnDraw(Canvas c)
        {
            c.Rotate(-90);
            c.Translate(-Height, 0);

            base.OnDraw(c);
        }


        public override bool OnTouchEvent(MotionEvent ev)
        {
            if (!Enabled)
            {
                return false;
            }

            switch (ev.Action)
            {
                case MotionEventActions.Down:
                case MotionEventActions.Move:
                case MotionEventActions.Up:
                    int i = 0;
                    i = Max - (int)(Max * ev.GetY() / Height);
                    Progress = i;
                    //Log.Info("Progress", Progress + "");
                    OnSizeChanged(Width, Height, 0, 0);
                    break;

                case MotionEventActions.Cancel:
                    break;
            }
            return true;
        }

    }
}