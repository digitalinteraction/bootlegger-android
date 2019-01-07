/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */

using Android.OS;
using Android.Views;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Graphics;
using System;
using Android.Util;
using Android.Widget;
using Android.Views.Animations;

namespace Bootleg.Droid.UI
{

    public class Camera2Controls : Android.Support.V4.App.Fragment
    {
        Android.Hardware.Camera2.CameraDevice camera;
        private Size pixel;
        private Rect active;
        FrameLayout focusring;
        View inner_ring;
        View outer_ring;

        internal event Action<Rect> OnFocus;

        public Camera2Controls() { }

        public Camera2Controls(Android.Hardware.Camera2.CameraDevice camera)
        {
            this.camera = camera;
        }

        public Camera2Controls(CameraDevice camera, Size pixel,Rect active) : this(camera)
        {
            this.pixel = pixel;
            this.active = active;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.camera_controls, container, false);
            view.Touch += video_Touch;
            focusring = view.FindViewById<FrameLayout>(Resource.Id.focus_ring);
            inner_ring = view.FindViewById(Resource.Id.focus_inner);
            outer_ring = view.FindViewById(Resource.Id.focus_outer);
            focusring.Visibility = ViewStates.Invisible;

            //drawingView = new DrawingView(view.Context, null);
            //view.FindViewById<ViewGroup>(Resource.Id.rootlayout).AddView(drawingView);
            return view;
        }

        public static T Clamp<T>(T val, T min, T max) where T : IComparable<T>
        {
            if (val.CompareTo(min) < 0) return min;
            else if (val.CompareTo(max) > 0) return max;
            else return val;
        }

        bool focussing = false;

        void video_Touch(object sender, View.TouchEventArgs e)
        {
            //TODO CHECK CAN ACTUALLY DO FOCUS...
            if (e.Event.Action == MotionEventActions.Up && !focussing)
            {

                focussing = true;
                Rect rect = active;
                //Log.i("onAreaTouchEvent", "SENSOR_INFO_ACTIVE_ARRAY_SIZE,,,,,,,,rect.left--->" + rect.left + ",,,rect.top--->" + rect.top + ",,,,rect.right--->" + rect.right + ",,,,rect.bottom---->" + rect.bottom);
                Size size = pixel;
                //Log.i("onAreaTouchEvent", "mCameraCharacteristics,,,,size.getWidth()--->" + size.getWidth() + ",,,size.getHeight()--->" + size.getHeight());
                int areaSize = 200;
                int right = rect.Right;
                int bottom = rect.Bottom;
                int viewWidth = View.Width;
                int viewHeight = View.Height;
                int ll, rr;
                Rect newRect;
                int centerX = (int)e.Event.GetX();
                int centerY = (int)e.Event.GetY();
                ll =((centerX * right) - areaSize) / viewWidth;
                rr=((centerY * bottom) - areaSize) / viewHeight;
                int focusLeft = Clamp(ll, 0, right);
                int focusBottom = Clamp(rr, 0, bottom);



                newRect=new Rect(focusLeft, focusBottom, focusLeft + areaSize, focusBottom + areaSize);

                float x = e.Event.GetX();
                float y = e.Event.GetY();
                //Rect touchRect = new Rect(
                //       (int)(x - 70),
                //       (int)(y - 70),
                //       (int)(x + 70),
                //       (int)(y + 70));

               
                

                OnFocus?.Invoke(newRect);

                focusring.LayoutParameters = new FrameLayout.LayoutParams(focusring.LayoutParameters) { MarginStart = (int)x - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Resources.DisplayMetrics), TopMargin = (int)y - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Resources.DisplayMetrics) };
                focusring.Visibility = ViewStates.Visible;

                Animation scaleup = AnimationUtils.LoadAnimation(Context, Resource.Animation.scaleup);
                scaleup.Interpolator = new DecelerateInterpolator();
                scaleup.Duration = 300;
                inner_ring.StartAnimation(scaleup);

                Animation scaledown = AnimationUtils.LoadAnimation(Context, Resource.Animation.scaledown);
                scaledown.Duration = 300;
                scaledown.Interpolator = new DecelerateInterpolator();
                outer_ring.StartAnimation(scaledown);

                //View.SetHaveTouch(true, touchRect);
                //drawingView.Invalidate();
                //        // Remove the square indicator after 1000 msec
                Handler handler = new Handler();
                handler.PostDelayed(() => {
                    //drawingView.setHaveTouch(false, new Rect(0, 0, 0, 0));
                    focusring.Visibility = ViewStates.Invisible;
                    focussing = false;
                    //drawingView.Invalidate();
                }, 800);
            }
        }

        internal void SetCamera(Android.Hardware.Camera2.CameraDevice camera)
        {
            this.camera = camera;
        }
    }


}