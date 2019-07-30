/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Android.Content.PM;
using Android.Provider;
using Android.Support.V4.View;
using Android.Net;
using Xamarin;
using Android.App;
using Android.Support.V4.Content;
using Android.Graphics;
using Plugin.Connectivity;
using System.Linq;
using Bootleg.API;
using Bootleg.Droid.UI;

namespace Bootleg.Droid
{
    [Activity]
    public class Uploads : AppCompatActivity
    {
        public override bool OnCreateOptionsMenu(Android.Views.IMenu menu)
        {
            if (WhiteLabelConfig.EXTERNAL_LINKS)
            {
                var actionItem1 = menu.Add(Resource.String.help);
                MenuItemCompat.SetShowAsAction(actionItem1, MenuItemCompat.ShowAsActionNever);
            }
            return base.OnCreateOptionsMenu(menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;

                default:
                        Intent myIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(Resources.GetString(Resource.String.HelpLink) + "#uploads"));
                        myIntent.PutExtra(Browser.ExtraApplicationId, "com.android.browser");
                        StartActivity(myIntent);
                    return base.OnOptionsItemSelected(item);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            //(Application as BootleggerApp).Comms.CanUpload = false;
            receiver.LostWifi -= Receiver_LostWifi;
            receiver.GotWifi -= Receiver_GotWifi;
            Bootlegger.BootleggerClient.OnGlobalUploadProgress -= Comms_OnGlobalUploadProgress;
            Bootlegger.BootleggerClient.OnCurrentUploadsComplete -= Comms_OnCurrentUploadsComplete;
            Bootlegger.BootleggerClient.OnCurrentUploadsFailed -= Comms_OnCurrentUploadsFailed;
        }

        bool oktocontinueon3g = false;

        void CheckUpload()
        {
            //ConnectivityManager connManager = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
            //NetworkInfo mWifi = connManager.GetNetworkInfo(ConnectivityType.Wifi);
            if (CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi))
            {
                // Do whatever
                Bootlegger.BootleggerClient.CanUpload = true;
                FindViewById<ImageButton>(Resource.Id.cancelbtn).Visibility = ViewStates.Visible;
            }
            else
            {
                new Android.Support.V7.App.AlertDialog.Builder(this).SetMessage(Resource.String.datacharge)
               .SetNegativeButton(Resource.String.notnow, new EventHandler<DialogClickEventArgs>((oe, eo) =>
               {
                   Finish();
               }))
               .SetPositiveButton(Resource.String.continueanyway, new EventHandler<DialogClickEventArgs>((oe, eo) =>
               {
                   oktocontinueon3g = true;
                   Bootlegger.BootleggerClient.CanUpload = true;
                   FindViewById<ImageButton>(Resource.Id.cancelbtn).Visibility = ViewStates.Visible;

                   //BE AWARE -- THIS MAY CAUSE BUGS!!!! -- IT WAS REMOVED IN ONE VERSION AS BUG COULD NOT BE TRACED
                   receiver.LostWifi += Receiver_LostWifi;
                   receiver.GotWifi += Receiver_GotWifi;
               }))
               .SetTitle(Resource.String.continuetitle)
               .SetCancelable(false)
               .Show();               
            } 
        }

        protected override void OnResume()
        {
            Bootlegger.BootleggerClient.OnGlobalUploadProgress += Comms_OnGlobalUploadProgress;
            Bootlegger.BootleggerClient.OnCurrentUploadsComplete += Comms_OnCurrentUploadsComplete;
            Bootlegger.BootleggerClient.OnCurrentUploadsFailed += Comms_OnCurrentUploadsFailed;

            oktocontinueon3g = false;

            //Insights.Track("UploadScreen");
            //(Application as BootleggerApp).reset.OPENUPLOAD = false;
            if ((Application as BootleggerApp).TOTALFAIL == true)
            {
                System.Environment.Exit(0);
                Finish();
            }
            base.OnResume();

            try
            {
                Bootlegger.BootleggerClient.UnSelectRole(true, true);
            }
            catch
            {
                //error unselecting role from current event -- means event might have been removed from server...
            }

            var uploads = Bootlegger.BootleggerClient.UploadQueue;

            Window.AddFlags(WindowManagerFlags.KeepScreenOn);


            if (uploads.Count == 0)
            {
                Finish();
            }
            else
            {
                FindViewById<View>(Resource.Id.nofootage).Visibility = ViewStates.Gone;
                FindViewById<View>(Resource.Id.msg1).Visibility = ViewStates.Visible;
                //FindViewById<View>(Resource.Id.msg2).Visibility = ViewStates.Visible;
                FindViewById<View>(Resource.Id.count).Visibility = ViewStates.Visible;
                FindViewById<View>(Resource.Id.uploadprogress).Visibility = ViewStates.Visible;

                FindViewById<TextView>(Resource.Id.count).Text = Resources.GetString(Resource.String.ofcount,0, uploads.Count);
                FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).Value = 0;
            }

            if (!pressedok && !Bootlegger.BootleggerClient.CanUpload && !dialogdisplayed)
            {
                dialogdisplayed = true;
                try
                {
                    new Android.Support.V7.App.AlertDialog.Builder(this).SetMessage(Resource.String.sendeverything)
                       .SetNegativeButton(Resource.String.illreview, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                       {
                           dialogdisplayed = false;
                           Finish();
                           return;
                       }))
                       .SetPositiveButton(Resource.String.goahead, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                       {
                           dialogdisplayed = false;
                           CheckUpload();
                           pressedok = true;
                       }))
                       .SetTitle(Resource.String.continuetitle)
                       .SetCancelable(false)
                       .Show();
                }
                catch (Exception)
                {

                }
            }

            if (!Bootlegger.BootleggerClient.CanUpload)
            {
                FindViewById<ImageButton>(Resource.Id.cancelbtn).Visibility = ViewStates.Gone;
            }
            else
            {
                FindViewById<ImageButton>(Resource.Id.cancelbtn).Visibility = ViewStates.Visible;
            }
        }

        bool dialogdisplayed = false;
        bool pressedok = false;

        protected override void OnStart()
        {
            base.OnStart();
            RegisterReceiver(receiver, filter);
            
        }

        protected override void OnStop()
        {
            base.OnStop();
            
            pressedok = false;
            UnregisterReceiver(receiver);
        }

       

        public override void FinishFromChild(Activity child)
        {
            base.FinishFromChild(child);

            //when video gets logged out:
            if (!Bootlegger.BootleggerClient.Connected)
            {
                this.Finish();
            }
            //else rechoose role...
        }

        IntentFilter filter = new IntentFilter(ConnectivityManager.ConnectivityAction);
        NetworkChangeReceiver receiver = new NetworkChangeReceiver();

        protected override void OnCreate(Bundle bundle)
        {                       
           
            SetTheme(Resource.Style.Theme_Normal);

            base.OnCreate(bundle);
            
            SetContentView(Resource.Layout.Uploads);
            SetTitle(Resource.String.uploads);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            //(Application as BootleggerApp).OPENUPLOAD = false;

            var col = new Color(ContextCompat.GetColor(this, Resource.Color.blue));
            FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).ProgressColor = col;

            FindViewById<ImageButton>(Resource.Id.cancelbtn).Click += Uploads_Click;
        }

        private void Uploads_Click(object sender, EventArgs e)
        {
            Bootlegger.BootleggerClient.CanUpload = false;
            (Application as BootleggerApp).ClearNotifications();
            FindViewById<ImageButton>(Resource.Id.cancelbtn).Visibility = ViewStates.Gone;
            Finish();
        }

        private void Comms_OnCurrentUploadsFailed()
        {
            RunOnUiThread(() =>
            {
                new Android.Support.V7.App.AlertDialog.Builder(this).SetMessage(Resource.String.uploadfailmsg)
            .SetNegativeButton(Android.Resource.String.Cancel, new EventHandler<DialogClickEventArgs>((oe, eo) =>
            {
                Finish();
                return;
            }))
            .SetPositiveButton(Resource.String.goahead, new EventHandler<DialogClickEventArgs>((oe, eo) =>
            {
                Bootlegger.BootleggerClient.CanUpload = true;
            }))
            .SetTitle(Resource.String.failedtoupload)
            .SetCancelable(false)
        .Show();
            });
        }

        private void Comms_OnCurrentUploadsComplete()
        {
            //RunOnUiThread(() =>
            //{
            //    FindViewById<View>(Resource.Id.nofootage).Visibility = ViewStates.Visible;
            //    FindViewById<View>(Resource.Id.msg1).Visibility = ViewStates.Gone;
            //    //FindViewById<View>(Resource.Id.msg2).Visibility = ViewStates.Gone;
            //    FindViewById<View>(Resource.Id.count).Visibility = ViewStates.Gone;
            //    FindViewById<View>(Resource.Id.uploadprogress).Visibility = ViewStates.Gone;
            //    FindViewById<ImageButton>(Resource.Id.cancelbtn).Visibility = ViewStates.Gone;
            //});
            Finish();
        }

        private void Comms_OnGlobalUploadProgress(double arg1, int arg2, int arg3)
        {
            RunOnUiThread(() =>
            {
                FindViewById<TextView>(Resource.Id.count).Text = Resources.GetString(Resource.String.ofcount,arg2,arg3);
                FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).Value = (float)arg1 * 100;
            });
        }

        private void Receiver_GotWifi()
        {
            Bootlegger.BootleggerClient.CanUpload = true;
        }

        private void Receiver_LostWifi()
        {
            if (!oktocontinueon3g)
            {
                Bootlegger.BootleggerClient.CanUpload = false;
            }
        }
    }
}