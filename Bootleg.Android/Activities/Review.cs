/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using AndroidHUD;
using Android.Views;
using Android.Support.V4.View;
using Android.Support.V7.App;
using System;
using Android.Support.Design.Widget;
using Bootleg.API;
using Android.Widget;
using Square.Picasso;
using Bootleg.Droid.Screens;
using Bootleg.Droid.UI;
using Android.Support.V7.Graphics;
using Android.Graphics.Drawables;
using Android.Net;
using Android.Runtime;
using Plugin.Connectivity;
using Android.Support.V4.Content;
using Android.Graphics;
using Bootleg.Droid.Adapters;
using System.Threading;
using System.Linq;
using Android.Util;
using System.IO;
using System.Collections.Generic;
using Android.Media;
using System.Threading.Tasks;
using Android.Content.Res;
using Bootleg.API.Model;
using Bootleg.API.Exceptions;
using Android.Support.V4.Content.Res;

namespace Bootleg.Droid
{

    [Activity(Theme = "@style/Theme.Normal", LaunchMode = LaunchMode.SingleTask)]
    public class Review : AppCompatActivity
    {

        public class GCMRegReceiver : BroadcastReceiver
        {

            public GCMRegReceiver(Application app)
                : base()
            {
                this.App = app;
            }

            public override void OnReceive(Context context, Intent intent)
            {
                if (intent.Extras != null && intent.Extras.GetString("gcmreg") != null)
                {
                    Bootlegger.BootleggerClient.RegisterForPush(intent.Extras.GetString("gcmreg"), Bootlegger.Platform.Android);
                }
            }

            public Application App { get; set; }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            //dont do this if its going to the preview screen:
            Bootlegger.BootleggerClient.CanUpload = false;
            Picasso.With(this).CancelTag(this);
        }

        protected override void OnResume()
        {
            if ((Application as BootleggerApp).TOTALFAIL == true)
            {
                Finish();
                System.Environment.Exit(0);
                return;
            }
            base.OnResume();

            if (Bootlegger.BootleggerClient.CurrentEvent == null)
            {
                Finish();
                return;
            }

            //Analytics.TrackEvent("ReviewScreen");
            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("Review",
                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));


            Window.AddFlags(WindowManagerFlags.KeepScreenOn);

            Picasso.With(this).ResumeTag(this);
            (myclips as IImagePausable).Resume();
            AndHUD.Shared.Dismiss();

            if (Intent.GetBooleanExtra("processed",false))
            {
                _pager.SetCurrentItem(1, false);
            }

            Bootlegger.BootleggerClient.OnCurrentUploadsComplete += Comms_OnCurrentUploadsComplete;

            //disable new edit button if no clips:
            Myclips_OnRefresh();
            //refresh edits
            myedits.Refresh();
            //myingest.ChooserMode = AllClipsFragment.ClipViewMode.INGEST;
            //myingest.Refresh();
        }

     
        private async void Review_Click(object sender, EventArgs ee)
        {
            cancel = new CancellationTokenSource();
            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, cancel.Cancel);

            if ((Application as BootleggerApp).IsReallyConnected && !CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi))
            {
                var diag = new Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.datausagetitle)
                    .SetMessage(Resource.String.datacharge)
                    .SetPositiveButton(Resource.String.continuebtn, async (o, e) => {
                        try
                        {
                            await LoginFuncs.TryLogin(this, cancel.Token);
                            await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, new CancellationTokenSource().Token);
                            await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, CurrentEvent, cancel.Token);
                            Intent i = new Intent(this, typeof(Editor));
                            i.PutExtra(Review.EDIT, "");
                            StartActivityForResult(i, Review.EDIT_RESPONSE);
                            Bootlegger.BootleggerClient.CanUpload = false;

                        }
                        catch (TaskCanceledException)
                        {
                            //nothing, it was cancelled
                        }
                        catch
                        {
                            LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.noconnectionshort)));
                        }
                        finally
                        {
                            AndHUD.Shared.Dismiss();
                        }
                    })
                    .SetCancelable(false)
                    .SetNegativeButton(Android.Resource.String.Cancel, (o, e) => {
                        AndHUD.Shared.Dismiss();
                    })
                    .Show();
            }
            else
            {
                try
                {
                    await LoginFuncs.TryLogin(this, cancel.Token);
                    await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, new System.Threading.CancellationTokenSource().Token);
                    await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, CurrentEvent, cancel.Token);
                    Intent i = new Intent(this, typeof(Editor));
                    i.PutExtra(Review.EDIT, "");
                    StartActivityForResult(i, Review.EDIT_RESPONSE);
                    Bootlegger.BootleggerClient.CanUpload = false;
                }
                catch (TaskCanceledException)
                {
                    //nothing, it was cancelled
                }
                catch
                {
                    LoginFuncs.ShowError(this, Resource.String.noconnectionshort);
                }
                finally
                {
                    AndHUD.Shared.Dismiss();
                }
            }
        }

        CancellationTokenSource cancel = new CancellationTokenSource();

        void RefreshButtons(int page)
        {
            if (page == 0)
            {
                capture.Show();
                newedit.Hide();
                newtag.Hide();
            }
            else if (page == 1)
            {
                capture.Hide();
                newedit.Hide();
                newtag.Show();
            }
            else if (page == 2)
            {
                capture.Hide();
                newtag.Hide();
                if ((CurrentEvent.publicedit && CurrentEvent.numberofclips > 0) || Bootlegger.BootleggerClient.MyMediaEditing.Count > 0)
                {
                    newedit.Show();
                }
                else
                {
                    newedit.Hide();
                }
            }

        }

        private async void CaptureClick(object sender, EventArgs e)
        {
            var Event = Bootlegger.BootleggerClient.CurrentEvent;
            cancel = new CancellationTokenSource();
            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, cancel.Cancel);

            try
            {
                await Bootlegger.BootleggerClient.OfflineConnect(Event.id,cancel.Token);
                StartActivityForResult(typeof(Video), VIDEOCAP);
                Bootlegger.BootleggerClient.CanUpload = false;
            }
            catch (RoleNotSelectedException)
            {
                //connect and start the role screen....

                //only do this if we are actually online:
                if ((Application as BootleggerApp).IsReallyConnected)
                {
                    cancel = new CancellationTokenSource();
                    //start roles screen:
                    Intent i = new Intent(this, typeof(Roles));
                    i.PutExtra("id", Event.id);
                    StartActivityForResult(i, 0);
                    Bootlegger.BootleggerClient.CanUpload = false;
                }
                else
                {
                    Toast.MakeText(this, Resource.String.norolechosen, ToastLength.Long).Show();
                }
            }
            catch (Exception)
            {
                Toast.MakeText(this, Resource.String.noconnectionshort, ToastLength.Long).Show();
            }
            finally
            {
                AndHUD.Shared.Dismiss();
            }
        //}
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

        void CheckUpload()
        {
            if ((Application as BootleggerApp).IsReallyConnected)
            {
                if (CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi))
                {
                    // Do whatever
                    new Android.Support.V7.App.AlertDialog.Builder(this).SetMessage(Resource.String.deleteclipwarning)
                      .SetNegativeButton(Android.Resource.String.Cancel, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                      {
                      //do nothing...
                      }))
                      .SetPositiveButton(Resource.String.continueanyway, new EventHandler<DialogClickEventArgs>(async (oe, eo) =>
                      {
                          try
                          {
                              await LoginFuncs.TryLogin(this, cancel.Token);
                              //await Bootlegger.BootleggerClient.OfflineConnect(Bootlegger.BootleggerClient.CurrentEvent.id);
                              Bootlegger.BootleggerClient.CanUpload = true;
                              oktocontinueon3g = false;
                              //myclips.Redraw();
                              FindViewById<Button>(Resource.Id.uploadbtn).Text = Resources.GetString(Resource.String.pause);
                              receiver.LostWifi += Receiver_LostWifi;
                              receiver.GotWifi += Receiver_GotWifi;
                          }
                          catch
                          {
                              LoginFuncs.ShowError(this, Resource.String.noconnectionshort);
                          }
                      }))
                      .SetTitle(Resource.String.continuetitle)
                      .SetCancelable(false)
                      .Show();
                }
                else
                {
                    new Android.Support.V7.App.AlertDialog.Builder(this).SetMessage(Resource.String.datachargewithwarning)
                   .SetNegativeButton(Resource.String.notnow, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                   {
                   //do nothing...
                    }))
                   .SetPositiveButton(Resource.String.continueanyway, new EventHandler<DialogClickEventArgs>(async (oe, eo) =>
                   {
                       oktocontinueon3g = true;
                       //await connection:
                       await LoginFuncs.TryLogin(this, cancel.Token);
                       //await Bootlegger.BootleggerClient.OfflineConnect(Bootlegger.BootleggerClient.CurrentEvent.id);

                       Bootlegger.BootleggerClient.CanUpload = true;
                       //myclips.Redraw();
                       FindViewById<Button>(Resource.Id.uploadbtn).Text = Resources.GetString(Resource.String.pause);
                       receiver.LostWifi += Receiver_LostWifi;
                       receiver.GotWifi += Receiver_GotWifi;
                   }))
                   .SetTitle(Resource.String.continuetitle)
                   .SetCancelable(false)
                   .Show();
                }
            }
            else
            {
                LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.noconnectionshort)));
            }
        }

        private void Comms_OnCurrentUploadsComplete()
        {
            RunOnUiThread(() =>
            {
                myclips.Refresh();
            });
        }

        IntentFilter filter = new IntentFilter(ConnectivityManager.ConnectivityAction);
        NetworkChangeReceiver receiver = new NetworkChangeReceiver();
        private bool oktocontinueon3g = false;

        private void MyClipsFragment_Click(object sender, EventArgs e)
        {
            if (Bootlegger.BootleggerClient.CanUpload)
            {
                Bootlegger.BootleggerClient.CanUpload = false;
                //myclips.Redraw();
                FindViewById<Button>(Resource.Id.uploadbtn).Text = Resources.GetString(Resource.String.upload);
                receiver.LostWifi -= Receiver_LostWifi;
                receiver.GotWifi -= Receiver_GotWifi;
            }
            else
            {
                CheckUpload();
            }
        }

        public override void OnBackPressed()
        {
            base.OnBackPressed();
            Bootlegger.BootleggerClient.CanUpload = false;
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            
            if (Bootlegger.BootleggerClient.CurrentEvent == null)
            {
                Finish();
                return;
            }

            CurrentEvent = Bootlegger.BootleggerClient.CurrentEvent;
            
            SetTheme(Resource.Style.Theme_Normal);

            SetContentView(Resource.Layout.Review);
            

            //AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {
                // add FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS flag to the window
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                // finally change the color
                Window.SetStatusBarColor(new Color(ContextCompat.GetColor(this,Android.Resource.Color.Transparent)));
            }

            //FindViewById<TextView>(Resource.Id.customTitle).Text = CurrentEvent.name;

            FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).SetTitle(CurrentEvent.name);

            FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).SetExpandedTitleTextAppearance(Resource.Style.ExpandedAppBar);
            Typeface font = ResourcesCompat.GetFont(this,Resource.Font.montserratregular);
            FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).CollapsedTitleTypeface = font;
            FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).ExpandedTitleTypeface = font;

            FindViewById<AppBarLayout>(Resource.Id.appbar).SetExpanded(false, false);

            if (!string.IsNullOrEmpty(CurrentEvent.iconbackground) && !WhiteLabelConfig.REDUCE_BANDWIDTH)
                Picasso.With(this).Load(CurrentEvent.iconbackground).CenterCrop().Fit().MemoryPolicy(MemoryPolicy.NoCache, MemoryPolicy.NoStore).Tag(this).Into(FindViewById<ImageView>(Resource.Id.defaultback), new Action(() =>
                {
                    var bitmap = ((BitmapDrawable)FindViewById<ImageView>(Resource.Id.defaultback).Drawable).Bitmap;
                    Palette palette = Palette.From(bitmap).Generate();
                    int vibrant = palette.GetLightVibrantColor(0);
                    if (vibrant == 0)
                        vibrant = palette.GetMutedColor(0);
                    int dark = palette.GetVibrantColor(0);
                    if (dark == 0)
                        dark = palette.GetLightMutedColor(0);
                    //FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).SetContentScrimColor(vibrant);
                    //FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).SetStatusBarScrimColor(dark);
                }), null);
            else
            {
                Picasso.With(this).Load(Resource.Drawable.user_back).CenterCrop().Fit().Into(FindViewById<ImageView>(Resource.Id.defaultback));
                //FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).SetContentScrimColor(Color.Transparent);
                //FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).SetStatusBarScrimColor(dark);
            }

            FindViewById<TextView>(Resource.Id.organisedby).Text = CurrentEvent.organisedby;
            Picasso.With(this).Load(CurrentEvent.organiserprofile.Replace("sz=50", "")).Tag(this).Fit().Transform(new CircleTransform()).Into(FindViewById<ImageView>(Resource.Id.imgGravatar));

            FindViewById<TextView>(Resource.Id.contributors).Text = Java.Lang.String.Format("%d", CurrentEvent.numberofcontributors);
            FindViewById<TextView>(Resource.Id.contributions).Text = Java.Lang.String.Format("%d",CurrentEvent.numberofclips);

            _pager = FindViewById<ViewPager>(Resource.Id.tabpager);

            capture = FindViewById<FloatingActionButton>(Resource.Id.capture);
            newedit = FindViewById<FloatingActionButton>(Resource.Id.newedit);
            newtag = FindViewById<FloatingActionButton>(Resource.Id.newtag);


            capture.Click += CaptureClick;
            newedit.Click += Review_Click;
            newtag.Click += Newtag_Click;

            var dip16 = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 16, Resources.DisplayMetrics);
            capture.LayoutParameters = new CoordinatorLayout.LayoutParams(capture.LayoutParameters) { Behavior = new MyFABAwareScrollingViewBehavior(this, capture,0, _pager), Gravity = (int)(GravityFlags.End | GravityFlags.Right | GravityFlags.Bottom), MarginEnd=dip16, BottomMargin=dip16 };
            newtag.LayoutParameters = new CoordinatorLayout.LayoutParams(newtag.LayoutParameters) { Behavior = new MyFABAwareScrollingViewBehavior(this, newtag, 1, _pager), Gravity = (int)(GravityFlags.End | GravityFlags.Right | GravityFlags.Bottom), MarginEnd = dip16, BottomMargin = dip16 };
            newedit.LayoutParameters = new CoordinatorLayout.LayoutParams(newedit.LayoutParameters) { Behavior = new MyFABAwareScrollingViewBehavior(this, newedit,2, _pager), Gravity = (int)(GravityFlags.End | GravityFlags.Right | GravityFlags.Bottom), MarginEnd = dip16, BottomMargin = dip16 };

            _tabs = FindViewById<Android.Support.Design.Widget.TabLayout>(Resource.Id.tabs);
            _tabs.TabGravity = 0;
            _tabs.TabMode = 1;
            
            _adapter = new ReviewPageAdapter(SupportFragmentManager, this);
            _pager.Adapter = _adapter;

            if (savedInstanceState == null)
            {
                myclips = new MyClipsFragment(this);
                myclips.OnPreview += Myclips_OnPreview;
                myclips.OnRefresh += Myclips_OnRefresh;
                myclips.OnEventInfoUpdate += Myclips_OnEventInfoUpdate;
                myclips.OnStartUpload += Myclips_OnStartUpload;

                myedits = new MyEditsFragment();
                myedits.OnOpenEdit += Myedits_OnOpenEdit;
                myedits.OnPreview += Myedits_OnPreview;

                myingest = new AllClipsFragment(AllClipsFragment.ClipViewMode.LIST);
                myingest.OnPreview += Myingest_OnPreview;
            }
            else
            {
                myclips = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":0") as MyClipsFragment;
                myclips.OnPreview += Myclips_OnPreview;
                myclips.OnRefresh += Myclips_OnRefresh;
                myclips.OnEventInfoUpdate += Myclips_OnEventInfoUpdate;
                myclips.OnStartUpload += Myclips_OnStartUpload;

                myingest = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":1") as AllClipsFragment;
                myingest.ChooserMode = AllClipsFragment.ClipViewMode.LIST;
                myingest.OnPreview += Myingest_OnPreview;

                myedits = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":2") as MyEditsFragment;
                if (myedits == null)
                {
                    myedits = new MyEditsFragment();
                }

                myedits.OnOpenEdit += Myedits_OnOpenEdit;
                myedits.OnPreview += Myedits_OnPreview;

            }

            //myedits.Reattach();

            _adapter.AddTab(GetString(Resource.String.videos), myclips, ReviewPageAdapter.TabType.CLIPS);
            _adapter.AddTab(GetString(Resource.String.tagging), myingest, ReviewPageAdapter.TabType.INGEST);
            _adapter.AddTab(GetString(Resource.String.edits), myedits, ReviewPageAdapter.TabType.EDITS);

            _pager.Post(() => {
                _tabs.SetupWithViewPager(_pager);
            });

            _pager.PageSelected += _pager_PageSelected;

            if (Intent?.GetBooleanExtra("needsperms", false) ?? false)
            {
                LoginFuncs.ShowError(this, Resource.String.acceptperms);
            }
        }

        private void Myingest_OnPreview(MediaItem arg1, View arg2)
        {
            Intent i = new Intent(this, typeof(Preview));
            i.PutExtra(Review.PREVIEW, arg1.id);
            StartActivity(i);
        }

        private async void Newtag_Click(object sender, EventArgs e)
        {
            if ((Application as BootleggerApp).IsReallyConnected)
            {
                AndHUD.Shared.Show(this, GetText(Resource.String.connecting), -1, MaskType.Black, null, null, true);
                try
                {
                    await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, new System.Threading.CancellationTokenSource().Token);
                    Intent i = new Intent(this, typeof(Ingest));
                    i.PutExtra(EDIT, "");
                    StartActivityForResult(i, INGEST);
                    Bootlegger.BootleggerClient.CanUpload = false;
                }
                catch (Exception)
                {
                    LoginFuncs.ShowError(this, Resource.String.noconnectionshort);
                }
                finally
                {
                    AndHUD.Shared.Dismiss();
                }
            }
            else
            {
                LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.noconnectionshort)));
            }
        }

        private void Myclips_OnStartUpload()
        {
            //start upload:
            MyClipsFragment_Click(null,null);
        }

        private void Myclips_OnEventInfoUpdate(Shoot obj)
        {
            FindViewById<TextView>(Resource.Id.contributors).Text = Java.Lang.String.Format("%d", obj.numberofcontributors);
            FindViewById<TextView>(Resource.Id.contributions).Text = Java.Lang.String.Format("%d", obj.numberofclips);
        }

        MyEditsFragment myedits;
        FloatingActionButton capture;
        FloatingActionButton newedit;
        FloatingActionButton newtag;


        private void Myedits_OnPreview(Edit arg1, View arg2)
        {
            Intent i = new Intent(this, typeof(Preview));
            i.PutExtra(Review.PREVIEW_EDIT, arg1.id);
            StartActivityForResult(i, Review.EDIT_RESPONSE);
        }

        private async void Myedits_OnOpenEdit(Edit obj)
        {
            cancel = new CancellationTokenSource();
            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                cancel.Cancel();
            });

            if ((Application as BootleggerApp).IsReallyConnected && !CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi))
            {
                var diag = new Android.Support.V7.App.AlertDialog.Builder(this)
                    .SetTitle(Resource.String.datausagetitle)
                    .SetMessage(Resource.String.datacharge)
                    .SetPositiveButton(Resource.String.continuebtn, async (o, e) => {
                        try
                        {
                            await LoginFuncs.TryLogin(this, cancel.Token);
                            await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, new System.Threading.CancellationTokenSource().Token);
                            await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, new Shoot() { id = obj.media[0].event_id }, cancel.Token);
                            Intent i = new Intent(this, typeof(Editor));
                            i.PutExtra(Review.EDIT, obj.id);
                            StartActivityForResult(i, Review.EDIT_RESPONSE);
                            Bootlegger.BootleggerClient.CanUpload = false;
                        }
                        catch (TaskCanceledException)
                        {
                            //nothing, it was cancelled
                        }
                        catch
                        {
                            LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.noconnectionshort)));
                        }
                        finally
                        {
                            AndHUD.Shared.Dismiss();
                        }
                    })
                    .SetCancelable(false)
                    .SetNegativeButton(Android.Resource.String.Cancel, (o, e) => {
                        AndHUD.Shared.Dismiss();
                    })
                    .Show();
            }
            else
            {
                try
                {
                    await LoginFuncs.TryLogin(this, cancel.Token);
                    await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, new System.Threading.CancellationTokenSource().Token);
                    await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, new Shoot() { id = obj.media[0].event_id }, cancel.Token);
                    Intent i = new Intent(this, typeof(Editor));
                    i.PutExtra(Review.EDIT, obj.id);
                    StartActivityForResult(i, Review.EDIT_RESPONSE);
                    Bootlegger.BootleggerClient.CanUpload = false;

                }
                catch (TaskCanceledException)
                {
                    //nothing, it was cancelled
                }
                catch
                {
                    LoginFuncs.ShowError(this, Resource.String.noconnectionshort);
                }
                finally
                {
                    AndHUD.Shared.Dismiss();
                }
            }
        }

        private void _pager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            RefreshButtons(e.Position);   
        }

        TabLayout _tabs;
        ViewPager _pager;
        ReviewPageAdapter _adapter;

        private void Myclips_OnRefresh()
        {
            RefreshButtons(_pager.CurrentItem);   
        }

        Shoot CurrentEvent;
        MyClipsFragment myclips;
        AllClipsFragment myingest;

        private void Myclips_OnPreview(MediaItem obj,View v)
        {
            Intent i = new Intent(this, typeof(Preview));
            i.PutExtra(Review.PREVIEW, obj.id);
            StartActivity(i);
        }

        public override bool OnCreateOptionsMenu(Android.Views.IMenu menu)
        {
            if (WhiteLabelConfig.EXTERNAL_LINKS)
            {
                var actionItem1 = menu.Add(Resource.String.help);
                MenuItemCompat.SetShowAsAction(actionItem1, MenuItemCompat.ShowAsActionNever);
            }


            if (WhiteLabelConfig.OFFLINE_CACHE)
            {
                var actionItem2 = menu.Add(0,88,0, Resource.String.cache);
                MenuItemCompat.SetShowAsAction(actionItem2, MenuItemCompat.ShowAsActionNever);

#if DEBUG
                var actionItem3 = menu.Add(0, 99, 0, "Fix Missing");
                MenuItemCompat.SetShowAsAction(actionItem3, MenuItemCompat.ShowAsActionNever);
#endif
            }

            return true;
        }

      

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case 88:
                    Download();
                    return true;
                case 99:
                    FixMissing();
                    return true;
                default:
                    LoginFuncs.ShowHelp(this, "#review");
                    return true;
            }
        }

        static void DirSearch(string sDir, List<string> files)
        {
            try
            {
                foreach (string d in Directory.GetDirectories(sDir))
                {
                    foreach (string f in Directory.GetFiles(d))
                    {
                        if (f.EndsWith(".mp4"))
                            files.Add(f);
                    }
                    DirSearch(d, files);
                }
            }
            catch (System.Exception excpt)
            {
                Console.WriteLine(excpt.Message);
            }
        }

        private async void FixMissing()
        {
            List<string> missing = new List<string>();
            //look in fixing dir
            DirSearch(GetExternalPath() + "/missing", missing);

            await Bootlegger.BootleggerClient.OfflineConnect(CurrentEvent.id, cancel.Token);

            //for each, create new clip
            foreach (var file in missing)
            {
                FileInfo ff = new FileInfo(file);
                var filename = ff.Name;
                var group = ff.Directory.Name;

                //generate thumbnail
                Bootlegger.BootleggerClient.CurrentClientRole = Bootlegger.BootleggerClient.CurrentEvent.roles.First();
                //var ev = await Bootlegger.BootleggerClient.GetEventInfo(CurrentEvent.id,new CancellationTokenSource().Token);
                Bootlegger.BootleggerClient.SetShot(Bootlegger.BootleggerClient.CurrentClientRole.Shots.First());

                Dictionary<string, string> meta = new Dictionary<string, string>();

                try
                {
                    FileStream outt;
                    var bitmap = await ThumbnailUtils.CreateVideoThumbnailAsync(file , Android.Provider.ThumbnailKind.MiniKind);
                    outt = new FileStream(file + ".jpg", FileMode.CreateNew);
                    await bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Jpeg, 90, outt);
                    outt.Close();
                }
                catch
                {

                }
                

                meta.Add("captured_at", ff.CreationTime.ToString("dd/MM/yyyy H:mm:ss.ff tt zz"));
                MediaItem newm = new MediaItem();
                newm.Filename = file;
                //add to upload queue (for the right user)
                var mediaitem = Bootlegger.BootleggerClient.CreateMediaMeta(newm, meta, null, file + ".jpg");
                Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH,true);
            }
            
        }

        string GetExternalPath()
        {
            var dirs = ContextCompat.GetExternalFilesDirs(this, null);
            if (dirs.Count() == 1)
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).Path;
            else if (dirs.Last() != null)
                return dirs.Last().AbsolutePath;
            else
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).Path;
        }

        CancellationTokenSource cancel_download = new CancellationTokenSource();

        private async void Download()
        {

            cancel_download = new CancellationTokenSource();

            AndHUD.Shared.Show(this, "Downloading", 0, MaskType.Black, null, null,true, () =>
            {
                cancel_download.Cancel();
            });


            Bootlegger.BootleggerClient.OnDownloadComplete += BootleggerClient_OnDownloadComplete;
            Bootlegger.BootleggerClient.OnDownloadProgress += BootleggerClient_OnDownloadProgress;

            await Bootlegger.BootleggerClient.CacheVideos(Bootlegger.BootleggerClient.CurrentEvent, cancel_download.Token);

            AndHUD.Shared.Dismiss();
        }

        private void BootleggerClient_OnDownloadProgress(int obj, int count, int total)
        {
            RunOnUiThread(() =>
            {
                AndHUD.Shared.Show(this, count + "/" + total, obj, MaskType.Black, null, null, true, () =>
                {
                    cancel_download.Cancel();
                });
            });
        }

        private void BootleggerClient_OnDownloadComplete(int obj)
        {
            Bootlegger.BootleggerClient.OnDownloadComplete -= BootleggerClient_OnDownloadComplete;
            Bootlegger.BootleggerClient.OnDownloadProgress -= BootleggerClient_OnDownloadProgress;
        }



        protected override void OnPause()
        {
            base.OnPause();
            Picasso.With(this).PauseTag(this);
            (myclips as IImagePausable).Pause();
            //FindViewById<Button>(Resource.Id.uploadbtn).Click -= MyClipsFragment_Click;
            //Bootlegger.BootleggerClient.OnCurrentUploadsComplete -= Comms_OnCurrentUploadsComplete;
            receiver.LostWifi -= Receiver_LostWifi;
            receiver.GotWifi -= Receiver_GotWifi;

            (Application as BootleggerApp).ClearNotifications();
        }

        public const string EDIT = "EDIT";
        public const int EDIT_RESPONSE = 88;
        internal static readonly string INGEST_MODE = "INGESTMODE";
        public const int VIDEOCAP = 5678;
        internal static readonly string READ_ONLY = "READONLY";
        internal static readonly string FROMEDITOR = "FROMEDITOR";
        private readonly int INGEST = 4456;
        public static string PREVIEW = "preview";
        public static string PREVIEW_EDIT = "preview_edit";

        public enum EDITOR_REQUEST { NEW, OPEN };

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);

            if (data?.GetBooleanExtra("needsperms",false)??false && requestCode == VIDEOCAP)
            {
                LoginFuncs.ShowError(this, Resource.String.acceptperms);
            }

            if (requestCode == EDIT_RESPONSE)
            {
                //coming back from edit, if it has been processed, show message
                if (data != null && data.GetBooleanExtra("processed", false))
                {
                    Toast.MakeText(this, Resource.String.editready, ToastLength.Long).Show();
                    //jump to edits tab:
                    
                }

                _pager.SetCurrentItem(2, false);
                myedits.Refresh();
            }

            if (requestCode == VIDEOCAP)
            {
                //refresh uploads list:
                myclips.RefreshUploads();
            }

            if (requestCode == INGEST)
            {
                //myingest.Refresh();
                //myingest.ChooserMode = AllClipsFragment.ClipViewMode.INGEST;
                myingest.Refresh();
            }
        }
    }

    public interface IImagePausable
    {
        void Pause();
        void Resume();
    }
}