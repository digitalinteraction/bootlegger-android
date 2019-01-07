/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Graphics;
using Android.Widget;
using Android.Views.Animations;
using Android.Util;
using Android.Support.V4.View;

#pragma warning disable CS0618 // Camera 1 API is obsolete, use only when Camera2 isn't available
namespace Bootleg.Droid.UI
{
    public class Camera1Controls : Android.Support.V4.App.Fragment, Android.Hardware.Camera.IAutoFocusCallback
    {
        Android.Hardware.Camera camera;

        public Camera1Controls() { }

        public Camera1Controls(Android.Hardware.Camera camera)
        {
            this.camera = camera;
        }

        public void OnAutoFocus(bool success, Android.Hardware.Camera camera)
        {
            if (success)
            {
                try {
                    camera.CancelAutoFocus();
                }
                catch
                {

                }
            }

            focussing = false;
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.camera_controls, container, false);
            view.Touch += video_Touch;
            focusring = view.FindViewById<FrameLayout>(Resource.Id.focus_ring);
            inner_ring = view.FindViewById(Resource.Id.focus_inner);
            outer_ring = view.FindViewById(Resource.Id.focus_outer);
            focusring.Visibility = ViewStates.Invisible;
            return view;
        }

        FrameLayout focusring;
        View inner_ring;
        View outer_ring;
        bool focussing = false;

        void video_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up && camera.GetParameters().SupportedFocusModes.Contains(Android.Hardware.Camera.Parameters.FocusModeAuto) && !focussing)
            {
                focussing = true;
                try
                {
                    camera.CancelAutoFocus();
                }
                catch
                {

                }

                float x = e.Event.GetX();
            float y = e.Event.GetY();
            Rect touchRect = new Rect(
                   (int)(x - 70),
                   (int)(y - 70),
                   (int)(x + 70),
                   (int)(y + 70));
            Rect targetFocusRect = new Rect(
                    touchRect.Left * 2000 / View.Width - 1000,
                    touchRect.Top * 2000  / View.Height - 1000,
                    touchRect.Right * 2000 / View.Width - 1000,
                    touchRect.Bottom * 2000 / View.Height - 1000);

                List<Android.Hardware.Camera.Area> focusList = new List<Android.Hardware.Camera.Area>();
                Android.Hardware.Camera.Area focusArea = new Android.Hardware.Camera.Area(targetFocusRect, 1000);
                focusList.Add(focusArea);

                Android.Hardware.Camera.Parameters param = camera.GetParameters();
                param.FocusAreas  =focusList;
                param.MeteringAreas = focusList;

                if (ViewCompat.GetLayoutDirection(focusring) == ViewCompat.LayoutDirectionRtl)
                {
                    focusring.LayoutParameters = new FrameLayout.LayoutParams(focusring.LayoutParameters) { MarginStart =  Resources.DisplayMetrics.WidthPixels - ( (int)x + (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Resources.DisplayMetrics)), TopMargin = (int)y - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Resources.DisplayMetrics) };
                }
                else
                {
                    focusring.LayoutParameters = new FrameLayout.LayoutParams(focusring.LayoutParameters) { MarginStart = (int)x - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Resources.DisplayMetrics), TopMargin = (int)y - (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Resources.DisplayMetrics) };
                }

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
               


                try
                {
                    camera.SetParameters(param);
                    camera.AutoFocus(this);
                    //drawingView.setHaveTouch(true, touchRect);
                    //drawingView.Invalidate();
                    // Remove the square indicator after 1000 msec
                   
                }
                catch(Exception ef)
                {
                    Console.WriteLine(ef);
                    focussing = false;
                }

                Handler handler = new Handler();
                handler.PostDelayed(() => {

                    focusring.Visibility = ViewStates.Invisible;

                }, 800);
            }
      
            }

        internal void SetCamera(Android.Hardware.Camera camera)
        {
            this.camera = camera;
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete