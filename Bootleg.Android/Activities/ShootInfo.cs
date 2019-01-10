using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Threading;
using Bootleg.API;
using Square.Picasso;
using Android.Graphics;
using Bootleg.Droid.UI;
using Bootleg.API.Model;

namespace Bootleg.Droid.Screens
{
    [Activity(Label = "ShootInfo")]
    public class ShootInfo : Android.Support.V4.App.DialogFragment
    {
        public ShootInfo()
        {

        }

        Shoot currentshoot;

        public static ShootInfo NewInstance(string shootid)
        {
            ShootInfo frag = new ShootInfo();
            Bundle args = new Bundle();
            args.PutString("eventid", shootid);
            frag.Arguments = args;
            return frag;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetStyle((int)DialogFragmentStyle.Normal, Resource.Style.ShootDialog);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.shootinfo, container);
        }

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            var shootid = Arguments.GetString("eventid");
            //show loader
            view.FindViewById<TextView>(Resource.Id.contributors).Text = "";
            view.FindViewById<TextView>(Resource.Id.contributions).Text = "";
            view.FindViewById<TextView>(Resource.Id.organiser).Text = "";
            view.FindViewById<TextView>(Resource.Id.datetime).Text = "";
            view.FindViewById<TextView>(Resource.Id.title).Text = "";
            view.FindViewById<TextView>(Resource.Id.description).Text = "";

            view.FindViewById(Resource.Id.loading).Visibility = ViewStates.Visible;
            try
            {
                var item = await Bootleg.API.Bootlegger.BootleggerClient.GetEventInfo(shootid, cancel.Token);
                currentshoot = item;
                view.FindViewById(Resource.Id.loading).Visibility = ViewStates.Gone;
                //hide loader

                switch (currentshoot.MyParticipation)
                {
                    case Shoot.Participation.INVITED:
                        view.FindViewById<ImageView>(Resource.Id.accessimg).SetImageResource(Resource.Drawable.ic_email_black_48dp);
                        break;

                    case Shoot.Participation.OWNER:
                        view.FindViewById<ImageView>(Resource.Id.accessimg).SetImageResource(Resource.Drawable.ic_edit_black_24dp);
                        break;

                    case Shoot.Participation.PUBLIC:
                        view.FindViewById<ImageView>(Resource.Id.accessimg).SetImageResource(Resource.Drawable.ic_lock_open_black_48dp);
                        break;
                }
                view.FindViewById<TextView>(Resource.Id.contributors).Text = Java.Lang.String.Format("%d", currentshoot.numberofcontributors);
                view.FindViewById<TextView>(Resource.Id.contributions).Text = Java.Lang.String.Format("%d", currentshoot.numberofclips);

                view.FindViewById<TextView>(Resource.Id.organiser).Text = item.organisedby;

                if (item.RealEnds != null)
                {
                    view.FindViewById<TextView>(Resource.Id.datetime).Text = item.RealEnds?.LocalizeTimeDiff();
                }
                else
                {
                    view.FindViewById<TextView>(Resource.Id.datetime).Text = item.starts + " - " + item.ends;
                }

                view.FindViewById<Button>(Resource.Id.navigateto).Click += (o, e) =>
                {
                    String url = "https://www.google.com/maps/dir/?api=1&origin=" + item.location;
                    Intent i = new Intent(Intent.ActionView);
                    i.SetData(Android.Net.Uri.Parse(url));
                    StartActivity(i);
                };

                if (string.IsNullOrEmpty(item.iconbackground))
                    Picasso.With(view.Context).Load(Resource.Drawable.user_back).CenterCrop().Fit().Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                else
                    Picasso.With(view.Context).Load(item.iconbackground).Placeholder(Resource.Drawable.user_back).CenterCrop().Fit().Into(view.FindViewById<ImageView>(Resource.Id.event_background));

                view.FindViewById<TextView>(Resource.Id.title).Text = item.name;
                view.FindViewById<TextView>(Resource.Id.description).Text = item.description;


                //}
                if (!string.IsNullOrEmpty(item.icon))
                {
                    Picasso.With(view.Context).Load(item.icon).Fit().Config(Bitmap.Config.Argb4444).Transform(new CircleTransform()).Into(view.FindViewById<ImageView>(Resource.Id.event_icon));
                }
                else
                {
                    if (!string.IsNullOrEmpty(item.organiserprofile))
                        Picasso.With(view.Context).Load(item.organiserprofile).Fit().Transform(new CircleTransform()).Into(view.FindViewById<ImageView>(Resource.Id.event_icon));
                    else
                        view.FindViewById<ImageView>(Resource.Id.event_icon).SetImageDrawable(null);
                }

                view.FindViewById(Resource.Id.shootclosed).Visibility = ViewStates.Gone;

                if (DateTime.Now < (item.RealStarts??DateTime.MaxValue) || DateTime.Now > (item.RealEnds??DateTime.MaxValue))
                {
                    view.FindViewById(Resource.Id.shootclosed).Visibility = ViewStates.Visible;
                    view.FindViewById<Button>(Resource.Id.startshooting).Enabled = false;
                    view.FindViewById<Button>(Resource.Id.startshooting).Text = Resources.GetString(Resource.String.closed);
                }
                else if (Bootlegger.BootleggerClient.CurrentUser == null)
                {
                    //view.FindViewById<Button>(Resource.Id.startshooting).Click += ShootInfo_Click1;
                    view.FindViewById<Button>(Resource.Id.startshooting).Text = Resources.GetString(Resource.String.logintocontribute);
                }
                else
                {
                    view.FindViewById<Button>(Resource.Id.startshooting).Text = Resources.GetString(Resource.String.startshooting);
                }

                view.FindViewById<Button>(Resource.Id.startshooting).Click += ShootInfo_Click;

                if (currentshoot.location!=null)
                {
                    view.FindViewById<View>(Resource.Id.locationinfo).Visibility = ViewStates.Visible;
                    //TODO: distance to shoot
                    //view.FindViewById<TextView>(Resource.Id.distance).Text = currentshoot.distance;
                }
                else
                    view.FindViewById<View>(Resource.Id.locationinfo).Visibility = ViewStates.Gone;

            }
            catch(Exception)
            {
                //dismiss dialog
                LoginFuncs.ShowError(view.Context, Resource.String.noconnectionshort);
                Dismiss();
            }
        }

        internal static void ClearDels()
        {
            OnConnect = null;
        }

        public static event Action<Shoot> OnConnect;

        private void ShootInfo_Click(object sender, EventArgs e)
        {
            OnConnect?.Invoke(currentshoot);
            Dismiss();
        }

        CancellationTokenSource cancel = new CancellationTokenSource();
    }
}