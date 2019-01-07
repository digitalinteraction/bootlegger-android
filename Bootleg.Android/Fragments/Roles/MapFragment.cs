/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Square.Picasso;
using System.Collections.Generic;
using Android.Graphics;
using Android.Util;
using Bootleg.Droid.UI;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class MapFragment : Android.Support.V4.App.Fragment, ICallback
    {
        Shoot CurrentEvent;
        bool noscroll;
        public MapFragment()
        {

        }

        public MapFragment(Shoot current,bool noscroll)
        {
            CurrentEvent = current;
            this.noscroll = noscroll;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RetainInstance = true;
            //SetRetainInstance(true);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.MapFragment, container, false);
            view.FindViewById<ImageView>(Resource.Id.image).Post(() =>
            {
                if (CurrentEvent != null)
                {
                    view.FindViewById(Resource.Id.mapprogress).Visibility = ViewStates.Visible;
                    view.FindViewById<ImageView>(Resource.Id.image).SetScaleType(ImageView.ScaleType.Center);
                    if (noscroll)
                    {
                        Picasso.With(view.Context).Load(CurrentEvent.localroleimage).Error(Resource.Drawable.ic_errorstatus).Into(view.FindViewById<ImageView>(Resource.Id.image), this);
                    }
                    else
                    {
                        Picasso.With(view.Context).Load(CurrentEvent.localroleimage).NetworkPolicy(NetworkPolicy.NoCache).Error(Resource.Drawable.ic_errorstatus).Into(view.FindViewById<ImageView>(Resource.Id.image), this);
                    }
                }
            });

            view.FindViewById<FrameLayout>(Resource.Id.selectionmask).Touch += Roles_Touch;
            return view;

        }

        void Roles_Touch(object sender, View.TouchEventArgs e)
        {
            if (e.Event.Action == MotionEventActions.Up && loaded)
            {
                //map this touch against roles in bootlegger:
                var touch = new System.Drawing.PointF(e.Event.GetX() / View.FindViewById<ImageView>(Resource.Id.image).Width, e.Event.GetY() / View.FindViewById<ImageView>(Resource.Id.image).Height);

                //find closest one from the given roles:
                List<Role> roles;
                if (CurrentEvent.CurrentPhase != null && CurrentEvent.CurrentPhase.roles !=null && CurrentEvent.CurrentPhase.roles.Count()>0)
                {
                   roles = (from n in CurrentEvent.roles where CurrentEvent.CurrentPhase.roles.Contains(n.id) orderby (n.Position.DistanceTo(touch)) select n).ToList();
                }
                else
                {
                   roles = (from n in CurrentEvent.roles orderby (n.Position.DistanceTo(touch)) select n).ToList();
                }

                if (roles.Count() > 0)
                {
                    var selected = roles.First();
                    OnRoleSelected?.Invoke(selected);
                }

            }
        }

        bool loaded = false;

        public event Action<Role> OnRoleSelected;


        public void OnError()
        {
            View.FindViewById(Resource.Id.mapprogress).Visibility = ViewStates.Gone;
            LoginFuncs.ShowError(Context, Resource.String.noconnectionshort);
        }

        public void OnSuccess()
        {
            try
            {
                //resize:
                int newheight = 0;
                int newwidth = 0;

                if (noscroll)
                {
                    //get ratio of image
                    var flo = View.FindViewById<ImageView>(Resource.Id.image).Drawable.IntrinsicWidth / (double)View.FindViewById<ImageView>(Resource.Id.image).Drawable.IntrinsicHeight;
                    Rect r = new Rect();
                    // get dimensions of viewport
                    View.FindViewById<View>(Resource.Id.eventimg).GetGlobalVisibleRect(r);
                    //set new height to that of viewport
                    newheight = r.Height();
                    //set new width to be height * ratio
                    newwidth = (int)(newheight * flo);

                    //explicit scale on image
                    View.FindViewById<ImageView>(Resource.Id.image).SetScaleType(ImageView.ScaleType.FitXy);

                }
                else
                {
                    var imageAspect = View.FindViewById<ImageView>(Resource.Id.image).Drawable.IntrinsicWidth / (double)View.FindViewById<ImageView>(Resource.Id.image).Drawable.IntrinsicHeight;

                    //Console.WriteLine(imageAspect);
                    Rect viewRectangle = new Rect();
                    View.FindViewById<View>(Resource.Id.eventimg).GetGlobalVisibleRect(viewRectangle);

                    var screenAspect = viewRectangle.Width() / (double)viewRectangle.Height();

                    //if (Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
                    if (imageAspect < screenAspect)
                    {
                        newheight = viewRectangle.Height();
                        newwidth = (int)(newheight * imageAspect);
                    }
                    else 
                    {
                        newwidth = viewRectangle.Width();
                        newheight = (int)(newwidth / imageAspect);
                    }
                    
                    View.FindViewById<ImageView>(Resource.Id.image).SetScaleType(ImageView.ScaleType.FitXy);

                }

                loaded = true;

                View.FindViewById<ImageView>(Resource.Id.image).LayoutParameters.Width = newwidth;
                View.FindViewById<ImageView>(Resource.Id.image).LayoutParameters.Height = newheight;

                View.FindViewById<View>(Resource.Id.selectionmask).LayoutParameters.Height = newheight;
                View.FindViewById<View>(Resource.Id.selectionmask).LayoutParameters.Width = newwidth;


                View.FindViewById(Resource.Id.mapprogress).Visibility = ViewStates.Gone;

                Update();
            }
            catch (Exception)
            {

            }
        }

        internal void Update()
        {
            if (Bootlegger.BootleggerClient.CurrentClientRole != null)
            {
                if (!WhiteLabelConfig.SHOW_ALL_SHOTS)
                    View.FindViewById<ImageView>(Resource.Id.tick).Visibility = ViewStates.Gone;

                var left = Bootlegger.BootleggerClient.CurrentClientRole.position[0] * View.FindViewById<ImageView>(Resource.Id.image).LayoutParameters.Width - TypedValue.ApplyDimension(ComplexUnitType.Dip, 30, Resources.DisplayMetrics);
                var top = Bootlegger.BootleggerClient.CurrentClientRole.position[1] * View.FindViewById<ImageView>(Resource.Id.image).LayoutParameters.Height - TypedValue.ApplyDimension(ComplexUnitType.Dip, 30, Resources.DisplayMetrics);
                View.FindViewById<ImageView>(Resource.Id.tick).LayoutParameters = new FrameLayout.LayoutParams(View.FindViewById<ImageView>(Resource.Id.tick).LayoutParameters) { MarginStart = (int)left, TopMargin = (int)top };
            }
        }
    }
}