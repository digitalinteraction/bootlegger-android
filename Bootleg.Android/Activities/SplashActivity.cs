/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V4.View;
using Android.Support.V4.App;
using RestSharp.Extensions.MonoHttp;
using System.Linq;
using Bootleg.API;
using Bootleg.Droid.UI;
using Android.Net.Wifi;
using static Android.Net.Wifi.WifiManager;
using System.Collections.Generic;
using Android.Gms.Common;
using System.Threading.Tasks;
using Plugin.Connectivity;
using Android.Graphics;
using System.ComponentModel;
using System.Threading;

namespace Bootleg.Droid
{
    public partial class SplashActivity : FragmentActivity
    {
        //Action cancellationFunc;
        CancellationTokenSource cancelCheck = new CancellationTokenSource();

        protected async override void OnResume()
        {
            if ((Application as BootleggerApp).TOTALFAIL == true)
            {
                Finish();
                System.Environment.Exit(0);
                return;
            }
            base.OnResume();

            if (WhiteLabelConfig.LOCAL_SERVER)
            {
                //if has current user and current shoot cached, then can continue:
                if (Bootlegger.BootleggerClient.CurrentUser == null || !(Bootlegger.BootleggerClient.ShootHistory?.Count > 0))
                {

                    FindViewById(Resource.Id.offlineChecks).Visibility = ViewStates.Visible;
                    FindViewById(Resource.Id.onlineChecks).Visibility = ViewStates.Gone;


                    bool checkNetwork = false, checkIP = false, checkApplication = false, checkConnection = false;
                    while (!(checkNetwork && checkIP && checkConnection))
                    {
                        //check network:
                        checkNetwork = CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi) || CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.Desktop);
                        RunOnUiThread(() =>
                        {
                            var test = FindViewById<TextView>(Resource.Id.chkNetwork).GetCompoundDrawablesRelative();

                            try
                            {
                                FindViewById<TextView>(Resource.Id.chkNetwork).GetCompoundDrawablesRelative()[0].Mutate().SetColorFilter(Color.Transparent, ((checkNetwork) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                            }
                            catch (Exception e)
                            {

                            }
                        });


                        if (checkNetwork)
                        {
                            checkIP = Bootlegger.BootleggerClient.CheckLocalIP();
                            RunOnUiThread(() =>
                            {
                                try
                                {
                                    FindViewById<TextView>(Resource.Id.chkIP).GetCompoundDrawablesRelative()[0].Mutate().SetColorFilter(Color.Transparent, ((checkIP) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                }
                                catch { }
                            });
                        }

                        //if (checkNetwork && checkIP)
                        //{
                        //    checkApplication = await CrossConnectivity.Current.IsReachable(WhiteLabelConfig.SERVER.Replace("http://",""), 5000);
                        //    RunOnUiThread(() =>
                        //    {
                        //        try
                        //        {
                        //            FindViewById<TextView>(Resource.Id.chkApplication).GetCompoundDrawablesRelative()[0].Mutate().SetColorFilter(Color.Transparent, ((checkApplication) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                        //        }
                        //        catch { }
                        //    });
                        //}

                        if (checkNetwork && checkIP) // && checkApplication)
                        {
                            checkConnection = await Bootlegger.BootleggerClient.CheckApplication();
                            RunOnUiThread(() =>
                            {
                            try
                                {
                                    FindViewById<TextView>(Resource.Id.chkApplication).GetCompoundDrawablesRelative()[0].Mutate().SetColorFilter(Color.Transparent, ((checkApplication) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                }
                                catch { }
                                try
                            {
                                    FindViewById<TextView>(Resource.Id.chkConnection).GetCompoundDrawablesRelative()[0].Mutate().SetColorFilter(Color.Transparent, ((checkConnection) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                }
                                catch { }
                            });
                        }

                        //if still not satistfied, then wait a bit longer:
                        if (!(checkNetwork && checkIP && checkConnection))
                            await Task.Delay(5000);


                    }

                    StartActivity(typeof(Login));
                }
                else
                {
                    StartActivity(typeof(Login));
                }
            }
            else
            {
                //if has current user and current shoot cached, then can continue:
                if (Bootlegger.BootleggerClient.CurrentUser == null || !(Bootlegger.BootleggerClient.ShootHistory?.Count > 0))
                {

                    FindViewById(Resource.Id.offlineChecks).Visibility = ViewStates.Gone;
                    FindViewById(Resource.Id.onlineChecks).Visibility = ViewStates.Visible;

                    bool checkNetwork = false, checkIP = false, checkApplication = false, checkConnection = false;
                    while (!(checkNetwork && checkIP && checkApplication && checkConnection))
                    {
                        //check network:
                        checkNetwork = CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.Cellular) || CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi) || CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.Desktop);
                        RunOnUiThread(() =>
                        {

                            try
                            {
                                FindViewById<TextView>(Resource.Id.ochkNetwork).GetCompoundDrawables()[0].Mutate().SetColorFilter(Color.Transparent, ((checkNetwork) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                            }
                            catch
                            {

                            }
                        });


                        if (checkNetwork)
                        {
                            checkIP = await CrossConnectivity.Current.IsRemoteReachable("http://google.com", 5000);
                            RunOnUiThread(() =>
                            {
                                try
                                {
                                    FindViewById<TextView>(Resource.Id.ochkNetwork).GetCompoundDrawables()[0].Mutate().SetColorFilter(Color.Transparent, ((checkNetwork) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                    FindViewById<TextView>(Resource.Id.ochkIP).GetCompoundDrawables()[0].Mutate().SetColorFilter(Color.Transparent, ((checkIP) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                }
                                catch { }
                            });
                        }

                        if (checkNetwork && checkIP)
                        {
                            checkApplication = await CrossConnectivity.Current.IsRemoteReachable(WhiteLabelConfig.SERVER, 5000);
                            RunOnUiThread(() =>
                            {
                                try
                                {
                                    FindViewById<TextView>(Resource.Id.ochkApplication).GetCompoundDrawables()[0].Mutate().SetColorFilter(Color.Transparent, ((checkApplication) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                }
                                catch { }
                            });
                        }

                        if (checkNetwork && checkIP && checkApplication)
                        {
                            checkConnection = await Bootlegger.BootleggerClient.CheckApplication();
                            RunOnUiThread(() =>
                            {
                                try
                                {
                                    FindViewById<TextView>(Resource.Id.ochkConnection).GetCompoundDrawables()[0].Mutate().SetColorFilter(Color.Transparent, ((checkConnection) ? PorterDuff.Mode.SrcOver : PorterDuff.Mode.SrcIn));
                                }
                                catch { }
                            });
                        }

                        //if still not satistfied, then wait a bit longer:
                        if (!(checkNetwork && checkIP && checkApplication && checkConnection))
                            await Task.Delay(5000);


                    }

                    StartActivity(typeof(Login));
                }
                else
                {
                    StartActivity(typeof(Login));
                }
            }
        }

        public bool IsPlayServicesAvailable()
        {
            int resultCode = GoogleApiAvailability.Instance.IsGooglePlayServicesAvailable(this);
            if (resultCode != ConnectionResult.Success)
            {
                if (GoogleApiAvailability.Instance.IsUserResolvableError(resultCode))
                    LoginFuncs.ShowError(this, new Exception(GoogleApiAvailability.Instance.GetErrorString(resultCode)));
                else
                {
                    //msgText.Text = "This device is not supported";
                    LoginFuncs.ShowError(this, new Exception(GetString(Resource.String.googleplayerror)));
                    Finish();
                }
                return false;
            }
            else
            {
                return true;
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            cancelCheck.Cancel();

        }

        protected override async void OnCreate(Bundle savedInstanceState)
        {
            RequestWindowFeature(WindowFeatures.NoTitle);

            base.OnCreate(savedInstanceState);
            //SetContentView(Resource.Layout.FirstRun);
            SetContentView(Resource.Layout.ConnectionChecklist);

            //BranchAndroid.Init(this, Resources.GetString(Resource.String.BRANCHKEY), this);

            IsPlayServicesAvailable();

            var allprefs = GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);


            if (WhiteLabelConfig.LOCAL_SERVER)
            {
                Uri connectionuri = new Uri($"{WhiteLabelConfig.SERVER}:{WhiteLabelConfig.PORT}");
                Bootlegger.BootleggerClient.StartWithLocal(connectionuri);
            }

            var firstrun = true;

            var prefs = allprefs.GetBoolean("firstrun", false);

            firstrun = prefs;

            //detect wanting to go straight to uploads
            if (Intent.Extras != null && Intent.Extras.ContainsKey("upload"))
            {
                (Application as BootleggerApp).ReturnState = new BootleggerApp.ApplicationReturnState() { ReturnsTo = BootleggerApp.ReturnType.OPEN_UPLOAD, Payload = Intent.Extras.GetString("eventid") };
                firstrun = true;
            }

            if (Intent.Extras != null && Intent.Extras.ContainsKey("advert"))
            {
                (Application as BootleggerApp).ADVERT = Intent.Extras.GetString("advert");
                firstrun = true;
            }
            else
            {
                (Application as BootleggerApp).ADVERT = "";
            }

            //detect login intent:
            //Console.WriteLine(Intent.Data?.Scheme);
            //Console.WriteLine(Intent.Data?.Host);

            if (Intent.Data != null && Intent.Data.Scheme == WhiteLabelConfig.DATASCHEME && Intent.Data.Host != "open")
            {
                var url = Intent.Data;

                if (!url.QueryParameterNames.Contains("eventid"))
                {
                    var session = url.Query;
                    session = session.TrimEnd('=');

                    //(Application as BootleggerApp).loginsession = HttpUtility.UrlEncode(session);

                    (Application as BootleggerApp).ReturnState.Session = HttpUtility.UrlEncode(session);
                    var returnstate = (Application as BootleggerApp).ReturnState;
                    //(Application as BootleggerApp).ReturnState = new BootleggerApp.ApplicationReturnState() { ReturnsTo = BootleggerApp.ReturnType.SIGN_IN_ONLY, Session = HttpUtility.UrlEncode(session) };

                    firstrun = true;
                }
                else
                {
                    //if its a create new shoot connect:
                    var eventid = url.GetQueryParameter("eventid");
                    (Application as BootleggerApp).ReturnState = new BootleggerApp.ApplicationReturnState() { ReturnsTo = BootleggerApp.ReturnType.OPEN_SHOOT, Payload = eventid };
                }
            }

            //edit invite:
            if (Intent.Data != null && Intent.Data.Host == WhiteLabelConfig.SERVERHOST && Intent.Data.PathSegments.Contains("watch"))
            {
                var editid = Intent.Data.PathSegments.Last();
                (Application as BootleggerApp).ReturnState = new BootleggerApp.ApplicationReturnState() { ReturnsTo = BootleggerApp.ReturnType.OPEN_EDIT, Payload = editid };

            }

            string state = Android.OS.Environment.ExternalStorageState;
            if (state != Android.OS.Environment.MediaMounted)
            {
                AlertDialog.Builder builder = new AlertDialog.Builder(this);
                builder.SetMessage(Resource.String.nostorageavail);
                builder.SetNeutralButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>((o, q) =>
                {
                    Finish();
                }));
                builder.SetCancelable(false);
                builder.Show();
            }
            else
            {
                (Application as BootleggerApp).Start();
                Bootlegger.BootleggerClient.OnSessionLost += Comms_OnSessionLost;

                //check for firstrun:
                if (!firstrun && Bootlegger.BootleggerClient.CurrentUser == null && WhiteLabelConfig.ONBOARDING)
                {
                    FindViewById(Resource.Id.theroot).Visibility = ViewStates.Visible;
                    FindViewById<Button>(Resource.Id.skip).Click += SplashActivity_Click;
                    FindViewById<Button>(Resource.Id.ok).Click += OkActivity_Click;
                    FindViewById<Button>(Resource.Id.next).Click += NextActivity_Click;
                    var mAdapter = new WizardPagerAdapter();
                    ViewPager mPager = FindViewById<ViewPager>(Resource.Id.pager);
                    mPager.OffscreenPageLimit = 4;
                    //mPager.SetOnPageChangeListener(this);

                    mPager.Adapter = mAdapter;
                    mPager.PageSelected += MPager_PageSelected;
                }
                else
                {
                    //StartActivity(typeof(Login));
                }
            }
        }

        bool sessionlostdialogopen = false;

        void Comms_OnSessionLost()
        {
            //start login screen:
            if (!sessionlostdialogopen)
            {
                sessionlostdialogopen = true;
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity);
                builder.SetPositiveButton(Android.Resource.String.Ok, (o, e) =>
                {
                    sessionlostdialogopen = false;
                    if (Bootlegger.BootleggerClient.CurrentUser != null)
                        LoginFuncs.OpenLogin(this, Bootlegger.BootleggerClient.CurrentUser?.profile["provider"].ToString());
                });
                var diag = builder.Create();
                diag.SetTitle(Resource.String.connectionissuetitle);
                diag.SetMessage(GetString(Resource.String.connectionissuebody));
                diag.SetCancelable(false);
                diag.Show();
            }
        }

        internal class WizardPagerAdapter : PagerAdapter
        {

            public override void DestroyItem(View container, int position, Java.Lang.Object @object)
            {
                //base.DestroyItem(container, position, @object);
            }

            public override void DestroyItem(ViewGroup container, int position, Java.Lang.Object @object)
            {
                //base.DestroyItem(container, position, @object);
            }

            public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
            {
                int resId = 0;
                switch (position)
                {
                    case 0:
                        resId = Resource.Id.page_one;
                        break;
                    case 1:
                        resId = Resource.Id.page_two;
                        break;
                    case 2:
                        resId = Resource.Id.page_three;
                        break;
                }

                return container.FindViewById(resId);
            }


            public override int Count
            {
                get { return 3; }
            }

            public override bool IsViewFromObject(View view, Java.Lang.Object @object)
            {
                return view == ((View)@object);
            }
        }

        private void NextActivity_Click(object sender, EventArgs e)
        {
            ViewPager mPager = FindViewById<ViewPager>(Resource.Id.pager);
            if (mPager.CurrentItem < mPager.ChildCount - 1)
                mPager.SetCurrentItem(mPager.CurrentItem + 1, true);
        }

        private void OkActivity_Click(object sender, EventArgs e)
        {
            var allprefs = GetSharedPreferences("bootlegger", FileCreationMode.Private);
            allprefs.Edit().PutBoolean("firstrun", true).Commit();
            StartActivity(typeof(Login));
        }

        void SplashActivity_Click(object sender, EventArgs e)
        {
            var allprefs = GetSharedPreferences("bootlegger", FileCreationMode.Private);
            allprefs.Edit().PutBoolean("firstrun", true).Commit();
            StartActivity(typeof(Login));
        }

        void MPager_PageSelected(object o, ViewPager.PageSelectedEventArgs page)
        {
            //if its the last page, show the ok button...
            //throw new NotImplementedException();
            ViewPager mPager = FindViewById<ViewPager>(Resource.Id.pager);
            if (mPager.CurrentItem == mPager.ChildCount - 1)
            {
                FindViewById(Resource.Id.next).Visibility = ViewStates.Gone;
                FindViewById(Resource.Id.ok).Visibility = ViewStates.Visible;
            }
            else
            {
                FindViewById(Resource.Id.next).Visibility = ViewStates.Visible;
                FindViewById(Resource.Id.ok).Visibility = ViewStates.Gone;
            }
        }

        // Ensure we get the updated link identifier when the app becomes active
        // due to a Branch link click after having been in the background
        protected override void OnNewIntent(Intent intent)
        {
            this.Intent = intent;
        }

        public void InitSessionComplete(Dictionary<string, object> data)
        {
            //DO DEEP LINKING HERE...

            if (data.ContainsKey("eventid"))
            {
                //open shoot info page directly from link:
                //if (Intent.Data != null && Intent.Data.Host == WhiteLabelConfig.BEACONHOST)
                //{
                //open the shoot
                //var shortcode = Intent.Data.PathSegments.Last();
                //var eventid = await Bootlegger.BootleggerClient.GetEventFromShortcode(shortcode);
                (Application as BootleggerApp).ReturnState = new BootleggerApp.ApplicationReturnState() { ReturnsTo = BootleggerApp.ReturnType.OPEN_SHOOT, Payload = data["eventid"].ToString() };

                //(Application as BootleggerApp).ConnectEventId = data["eventid"].ToString();
                StartActivity(typeof(Login));
                //}

                //show event info
            }

            //var intent = new Intent(this, typeof(BranchActivity));
            //intent.PutExtra("BranchData", JsonConvert.SerializeObject(data));

            //StartActivity(intent);
        }

        //public void SessionRequestError(BranchError error)
        //{
        //    Console.WriteLine("Branch session initialization error: " + error.ErrorCode);
        //    Console.WriteLine(error.ErrorMessage);
        //}
    }
}