/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Bootleg.API;
using Android.Support.V4.View;
using Android.Content.PM;
using AndroidHUD;
using Android.Provider;
using Android.Net;
using Square.Picasso;
using Bootleg.API.Exceptions;
using System.Threading;
using Android.Support.Design.Widget;
using Bootleg.Droid.Adapters;
using Android.Widget;
using Bootleg.Droid.UI;
using Bootleg.Droid.Screens;
using Android.Runtime;
using Android.Support.V4.Content;
using Plugin.Connectivity;
using System.Linq;
using Android.Graphics;
using Android.Util;
using Java.Util;
using Bootleg.Droid.Fragments.Home;
using Bootleg.API.Model;

[assembly: Permission(Name = "@PACKAGE_NAME@.permission.C2D_MESSAGE")]
[assembly: UsesPermission(Name = "@PACKAGE_NAME@.permission.C2D_MESSAGE")]
[assembly: UsesPermission(Name = "com.google.android.c2dm.permission.RECEIVE")]

//GET_ACCOUNTS is only needed for android versions 4.0.3 and below
[assembly: UsesPermission(Name = "android.permission.GET_ACCOUNTS")]
[assembly: UsesPermission(Name = "android.permission.INTERNET")]
[assembly: UsesPermission(Name = "android.permission.WAKE_LOCK")]

namespace Bootleg.Droid
{
    [Activity(Label = "", MainLauncher = false, LaunchMode = LaunchMode.SingleTask, Theme = "@style/Theme.Normal", WindowSoftInputMode = SoftInput.AdjustPan)]
    //<data android:scheme="grid6.us" />
    public class Login : AppCompatActivity
    {
        private enum LoginState { UNKNOWN, LOGGEDIN, PUSH_UPLOAD, PUSH_OPEN, LOGIN_SESSION, GO_TO_EVENT, TOTALFAIL, OFFLINE_EVENT,ADVERT, CONNECT_EVENT, CONNECT_EDIT, JOINCODE };
        private LoginState CurrentState = LoginState.UNKNOWN;

        //bool connectinglocal = false;

        //bool grav_loaded = false;

        object gravlock = new object();
         
        void SetGravatar(string img,string userid)
        {
            if (img != "")
            {
                Picasso.With(this).Load(img.Replace("sz=50", "")).Fit().Transform(new CircleTransform()).StableKey(userid).Into(FindViewById<ImageView>(Resource.Id.imgGravatar));
                FindViewById<ImageView>(Resource.Id.imgGravatar).Visibility = ViewStates.Visible;
            }
            else
            {
                FindViewById<ImageView>(Resource.Id.imgGravatar).Visibility = ViewStates.Gone;
            }
        }

        private async void Contributions_Edit(Shoot Event,bool needsperms)
        {
            //open shot review / edit screen....
            cancel = new CancellationTokenSource();
            AndHUD.Shared.Show(this, GetText(Resource.String.connecting), -1, MaskType.Black, null, null, true);
            try
            {
                (Application as BootleggerApp).ClearNotifications();
                await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, Event,new CancellationTokenSource().Token);
                var currentevent = Bootlegger.BootleggerClient.CurrentEvent;
                Intent i = new Intent(this.ApplicationContext, typeof(Review));
                if (needsperms)
                    i.PutExtra("needsperms", true);
                StartActivityForResult(i, Review.EDIT_RESPONSE);
            }
            catch (Exception e)
            {
                LoginFuncs.ShowError(this, e);
            }
            finally
            {
                AndHUD.Shared.Dismiss();
            }
        }

        CancellationTokenSource cancel = new CancellationTokenSource();
        private async void Contributions_ItemClick(Shoot Event)
        {
            //offline or online:
            //take 1 off the index as the uploads one is there really
            //var Shoot = contributions.visibleitems[e];
            cancel = new CancellationTokenSource();
            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                cancel.Cancel();
            });
            if (!Event.offline)
            {
                ConnectivityManager cm = this.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                NetworkInfo activeNetwork = cm.ActiveNetworkInfo;
                bool isConnected = activeNetwork != null && activeNetwork.IsConnectedOrConnecting;
                if (isConnected)
                {
                    try
                    {
                        cancel = new CancellationTokenSource();
                        await LoginFuncs.TryLogin(this, cancel.Token);

                        Intent i = new Intent(this, typeof(Roles));
                        i.PutExtra("id", Event.id);
                        StartActivityForResult(i, 0);
                        //await (Application as BootleggerApp).Comms.ConnectToEvent(Shoot, true, cancel.Token, false);
                        //StartActivity(typeof(Roles));
                    } 
                    catch (Exception e)
                    {
                        
                       LoginFuncs.ShowError(this,e);
                    }
                    finally
                    {
                        AndHUD.Shared.Dismiss();
                    }
                }
                else
                {
                    LoginFuncs.ShowError(this, new NoNetworkException());
                    AndHUD.Shared.Dismiss();
                }
            }
            else
            {
                //var allprefs = GetSharedPreferences("bootlegger", FileCreationMode.WorldWriteable);
                //var prefs = allprefs.GetString("sails.sid", "");
                //if (prefs != "")
                //{
                 //   Cookie mycookie = new Cookie("sails.sid", prefs.Replace("sails.sid=", ""), "/", (Application as BootleggerApp).Comms.LoginUrl.Host);

                    try
                    {
                    await Bootlegger.BootleggerClient.OfflineConnect(Event.id, cancel.Token);
                        StartActivity(typeof(Video));
                    }
                    catch (RoleNotSelectedException e)
                    {
                        //connect and start the role screen....
                        //only do this if we are actually online:
                        //ConnectivityManager cm = this.GetSystemService(Context.ConnectivityService) as ConnectivityManager;
                        //NetworkInfo activeNetwork = cm.ActiveNetworkInfo;
                        //bool isConnected = activeNetwork != null && activeNetwork.IsConnectedOrConnecting;
                        if ((Application as BootleggerApp).IsReallyConnected)
                        {
                            cancel = new CancellationTokenSource();
                            //await (Application as BootleggerApp).Comms.ConnectToEvent(Shoot, true, cancel.Token);
                            //start roles screen:
                            Intent i = new Intent(this, typeof(Roles));
                            i.PutExtra("id", Event.id);
                            StartActivityForResult(i, 0);
                            //StartActivity(typeof(Roles));
                        }
                        else
                        {
                            LoginFuncs.ShowError(this, e);
                        //Toast.MakeText(this, Resource.String.norolechosen, ToastLength.Long).Show();
                        }
                    }
                    catch (Exception e)
                    {
                        //Toast.MakeText(this, Resource.String.cantconnect, ToastLength.Long).Show();
                        LoginFuncs.ShowError(this, e);

                }
                finally
                    {
                        AndHUD.Shared.Dismiss();
                        ShowButtons();
                    }
            }
        }

        void ShowButtons()
        {
            AndHUD.Shared.Dismiss();
            if (Bootlegger.BootleggerClient.CurrentUser != null)
            {
                FindViewById<ImageButton>(Resource.Id.loginbtn).Visibility = ViewStates.Gone;
                FindViewById<TextView>(Resource.Id.logintext).Text = Bootlegger.BootleggerClient.CurrentUser.displayName;
                //FindViewById<Button>(Resource.Id.chooseagain).Visibility = ViewStates.Visible;
                FindViewById<ImageButton>(Resource.Id.loginbtn).Visibility = ViewStates.Gone;
                SetGravatar(Bootlegger.BootleggerClient.CurrentUser.ProfileImg, Bootlegger.BootleggerClient.CurrentUser.id);

                _pager.Post(() =>
                {
                    if (_adapter.Count == 1)
                    {
                        _adapter.AddTab("", myshoots,HomePageAdapter.TabType.MY_SHOOTS);
                        _adapter.AddTab("", myedits,HomePageAdapter.TabType.EDITS);
                        //_pager.Adapter = null;
                        //_pager.Adapter = _adapter;
                        _tabs.SetupWithViewPager(_pager);


                        if (Bootlegger.BootleggerClient.ShootHistory.Count > 0)
                        {
                            //_pager.CurrentItem = page;
                            _pager.SetCurrentItem(lastpage, false);
                        }
                    }

                    //if (Bootlegger.BootleggerClient.UploadQueue.Count > 0 && Resource.)
                        //FindViewById<AppBarLayout>(Resource.Id.appbar).SetExpanded(true);

                    if (WhiteLabelConfig.ALLOW_CREATE_OWN && _pager.CurrentItem != 2)
                        if (WhiteLabelConfig.ALLOW_CREATE_OWN)
                            FindViewById<View>(Resource.Id.newshoot).Visibility = ViewStates.Visible;
                    else
                        FindViewById<View>(Resource.Id.newshoot).Visibility = ViewStates.Gone;

                });
            }
            else
            {
                //FindViewById(Resource.Id.chooseagain).Visibility = ViewStates.Gone;
                //FindViewById<View>(Resource.Id.newshoot).Visibility = ViewStates.Gone;
                FindViewById<TextView>(Resource.Id.logintext).Text = Resources.GetString(Resource.String.guest);
                FindViewById<ImageButton>(Resource.Id.loginbtn).Visibility = ViewStates.Visible;
                FindViewById<ImageView>(Resource.Id.imgGravatar).Visibility = ViewStates.Gone;
                _pager.Post(() =>
                {
                    if (_adapter.Count == 3)
                    {
                        _adapter.RemoveTab(1);
                        _adapter.RemoveTab(1);

                        //_pager.Adapter = null;
                        _pager.Adapter = _adapter;
                        _tabs.SetupWithViewPager(_pager);
                    }
                });
            }

            if (!WhiteLabelConfig.ALLOW_CREATE_OWN)
                FindViewById<View>(Resource.Id.newshoot).Visibility = ViewStates.Gone;

            var i = Bootlegger.BootleggerClient.UploadQueue.Count;

            //FindViewById<View>(Resource.Id.defaultback).Visibility = ViewStates.Visible;

            //Picasso.With(this).Load(Resource.Drawable.user_back).CenterCrop().Config(Bitmap.Config.Rgb565).Priority(Picasso.Priority.High).Fit().Into(FindViewById<ImageView>(Resource.Id.defaultback));



            //DEBUG
            //i = 5;

            if (i == 0)
            {
                FindViewById<View>(Resource.Id.uploadtile).Visibility = ViewStates.Gone;
            }
            else
            {
                FindViewById<TextView>(Resource.Id.uploadcountmain).Text = Java.Lang.String.Format("%d", i);
                FindViewById<View>(Resource.Id.uploadtile).Visibility = ViewStates.Visible;

            }

            //collapse toolbar if in landscape:
            //if (Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
            //    FindViewById<AppBarLayout>(Resource.Id.appbar).SetExpanded(false, false);






            //FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).coll


        }

        //private void Login_Click(object sender, EventArgs e)
        //{
        //    LoginFuncs.NewShoot(this);
        //}

        //System.Net.Cookie logincookie = null;

        LoginState WhichState()
        {
            var  CS = LoginState.LOGGEDIN;

            if (!Bootlegger.BootleggerClient.Connected)
                CS = LoginState.UNKNOWN;

            if ((Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.OPEN_UPLOAD)
                return LoginState.PUSH_UPLOAD;

            if ((Application as BootleggerApp).TOTALFAIL == true)
                return LoginState.TOTALFAIL;

            //if app re-opened and the user is at an event (offline or online)
            if ((Application as BootleggerApp).ReturnState.ReturnsTo != BootleggerApp.ReturnType.OPEN_UPLOAD && Bootlegger.BootleggerClient.CurrentClientRole != null)
                return LoginState.GO_TO_EVENT;

            //connected by not at an event
            if ((Application as BootleggerApp).ReturnState.ReturnsTo != BootleggerApp.ReturnType.OPEN_UPLOAD && Bootlegger.BootleggerClient.Connected && Bootlegger.BootleggerClient.CurrentClientRole != null)
                CS = LoginState.LOGGEDIN;

            if (Bootlegger.BootleggerClient.CurrentUser != null)
                CS = LoginState.OFFLINE_EVENT;

            //var returnstate = (Application as BootleggerApp).ReturnState;

            if (((Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.SIGN_IN_ONLY || (Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.JOIN_CODE || (Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.CREATE_SHOOT) && !string.IsNullOrEmpty((Application as BootleggerApp).ReturnState.Session))
                CS = LoginState.LOGIN_SESSION;

            if ((Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.OPEN_SHOOT)
                CS = LoginState.CONNECT_EVENT;

            if ((Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.OPEN_EDIT)
                CS = LoginState.CONNECT_EDIT;

            return CS;
        }

        //EventAdapter contributions;
        bool loadedonce = false;

        private void ShowUpdateMessage(int message)
        {
            if (!updatemessageshowing)
            {
                updatemessageshowing = true;
                var builder = new Android.Support.V7.App.AlertDialog.Builder(this);
                builder.SetMessage(message);
                //FindViewById(Resource.Id.events).Visibility = ViewStates.Gone;
                var currentversion = PackageManager.GetPackageInfo(PackageName, 0).VersionCode;

                var allprefs = GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
                var prefs = allprefs.Edit();
                prefs.PutInt("updateneeded", currentversion);
                prefs.Apply();

                builder.SetNeutralButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>((o, q) =>
                {
                    Finish();
                    try
                    {
                        StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse("market://details?id=" + PackageName)));
                    }
                    catch (ActivityNotFoundException)
                    {
                        StartActivity(new Intent(Intent.ActionView, Android.Net.Uri.Parse("https://play.google.com/store/apps/details?id=" + PackageName)));
                    }
                }));
                builder.SetCancelable(false);
                builder.Show();
            }
        }

        private void Comms_OnApiVersionChanged()
        {
            ShowUpdateMessage(Resource.String.tryupdatefeatures);
        }

        private void Comms_OnApiKeyInvalid()
        {
            ShowUpdateMessage(Resource.String.tryupdate);
        }

        bool updatemessageshowing = false;

        protected async override void OnResume()
        {
            base.OnResume();
            ShowButtons();

            //Bootlegger.BootleggerClient.DisconnectForReview();

            var currentversion = PackageManager.GetPackageInfo(PackageName, 0).VersionCode;
            var allprefs = GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
            var updateversion = allprefs.GetInt("updateneeded",-1);

            if (updateversion !=-1 && currentversion == updateversion)
            {
                //show update message
                ShowUpdateMessage(Resource.String.tryupdatefeatures);
            }
            else
            {
                //clear update message
                allprefs.Edit().Remove("updateneeded").Apply();
                updatemessageshowing = false;
            }

            //Analytics.TrackEvent("LoginScreen");
            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("Login");
            var CurrentState = LoginState.LOGGEDIN;
            //setup logic:
            CurrentState = WhichState();

            if (!loadedonce)
            {
                EventListFragment.EventUpdateDelegate del = Bootlegger.BootleggerClient.GetShootHistory;
                myshoots.SetEvents("ShootHistory", del, cancel.Token, EventAdapter.EventViewType.MYEVENT);
            }

            try
            {
                if (_tabs.SelectedTabPosition != -1)
                {
                    var asf = _adapter.GetItem(_tabs.SelectedTabPosition);
                    if (asf is IImagePausable) (asf as IImagePausable).Resume();
                    //( as IImagePausable).Resume();
                }
            }
            catch (Exception)
            {

            }

            switch (CurrentState)
            {
                case LoginState.LOGGEDIN:

                    break;

                case LoginState.GO_TO_EVENT:

                        StartActivity(typeof(Video));
                        return;

                case LoginState.TOTALFAIL:
                    Finish();
                    return;

                case LoginState.PUSH_UPLOAD:
                    // try login:
                    try{
                        AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                            cancel.Cancel();
                        });
                        await LoginFuncs.TryLogin(this, cancel.Token);
                        //if success, then do this:
                        cancel = new CancellationTokenSource();
                        await Bootlegger.BootleggerClient.ConnectToEvent(new Shoot() { id = (Application as BootleggerApp).ReturnState.Payload }, true,cancel.Token, false);
                        StartActivity(typeof(Uploads));
                    }
                    catch (Exception e)
                    {
                        LoginFuncs.ShowError(this, e);
                    }
                    break;

                case LoginState.CONNECT_EVENT:
                    try
                    {
                        AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                            cancel.Cancel();
                        });
                        await LoginFuncs.TryLogin(this, cancel.Token);
                        //if success, then do this:
                        cancel = new CancellationTokenSource();
                        //await (Application as BootleggerApp).Comms.ConnectToEvent(new Shoot() { id = (Application as BootleggerApp).ConnectEventId }, true,cancel.Token, false);
                        Intent ii = new Intent(this, typeof(Roles));
                        ii.PutExtra("id", (Application as BootleggerApp).ReturnState.Payload);
                        StartActivityForResult(ii, 0);

                        //(Application as BootleggerApp).ConnectEventId = "";
                        //StartActivity(typeof(Roles));
                    }
                    catch (Exception e)
                    {
                        LoginFuncs.ShowError(this, e);
                    }
                    break;

                case LoginState.CONNECT_EDIT:
                    try
                    {
                        AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                            cancel.Cancel();
                        });

                        if ((Application as BootleggerApp).IsReallyConnected && !CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi))
                        {
                            var diag = new Android.Support.V7.App.AlertDialog.Builder(this)
                                .SetTitle(Resource.String.datausagetitle)
                                .SetMessage(Resource.String.datacharge)
                                .SetPositiveButton(Resource.String.continuebtn, async (o, e) => {
                                    try { 
                                        await LoginFuncs.TryLogin(this, cancel.Token);
                                        //if success, then do this:
                                        cancel = new CancellationTokenSource();
                                        //await (Application as BootleggerApp).Comms.ConnectToEvent(new Shoot() { id = (Application as BootleggerApp).ConnectEventId }, true,cancel.Token, false);

                                        await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, new Shoot() { id = (Application as BootleggerApp).ReturnState.Payload }, cancel.Token);

                                        Intent ii = new Intent(this, typeof(Review));
                                        ii.PutExtra("id", (Application as BootleggerApp).ReturnState.Payload);
                                        (Application as BootleggerApp).ResetReturnState();
                                        StartActivityForResult(ii, Review.EDIT_RESPONSE);
                                        //StartActivity(typeof(Review));
                                    }
                                    catch (Exception ex)
                                    {
                                        LoginFuncs.ShowError(this, ex);
                                    }
                                })
                                .SetNegativeButton(Android.Resource.String.Cancel, (o, e) => { })
                                .Show();
                        }
                        else
                        {
                            await LoginFuncs.TryLogin(this, cancel.Token);
                            //if success, then do this:
                            cancel = new CancellationTokenSource();
                            //await (Application as BootleggerApp).Comms.ConnectToEvent(new Shoot() { id = (Application as BootleggerApp).ConnectEventId }, true,cancel.Token, false);

                            await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, new Shoot() { id = (Application as BootleggerApp).ReturnState.Payload }, cancel.Token);

                            Intent ii = new Intent(this, typeof(Review));
                            ii.PutExtra("id", (Application as BootleggerApp).ReturnState.Payload);
                            (Application as BootleggerApp).ResetReturnState();
                            StartActivityForResult(ii, Review.EDIT_RESPONSE);
                            //StartActivity(typeof(Review));
                        }
                        //should ad
                    }
                    catch (Exception e)
                    {
                        LoginFuncs.ShowError(this, e);
                    }
                    break;

                case LoginState.LOGIN_SESSION:
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                        cancel.Cancel();
                    });
                    //Console.WriteLine("in: " + (Application as BootleggerApp).loginsession);
                    Bootlegger.BootleggerClient.SessionCookie = new System.Net.Cookie("sails.sid", (Application as BootleggerApp).ReturnState.Session, "/", Bootlegger.BootleggerClient.LoginUrl.Host);
                    //(Application as BootleggerApp).ReturnState.Value = "";
                    try
                    {
                        await LoginFuncs.TryLogin(this, cancel.Token);
                        var returnsto = (Application as BootleggerApp).ReturnState.ReturnsTo;
                        if ((Application as BootleggerApp).ReturnState.ReturnsTo == BootleggerApp.ReturnType.JOIN_CODE)
                        {
                            Highlighted_OnEnterCode((Application as BootleggerApp).ReturnState.Payload);
                            (Application as BootleggerApp).ResetReturnState();
                        }
                        else if ((Application as BootleggerApp).eventtoconnectoafterloggingin !="")
                        {
                            DoConnectToShoot(new Shoot() { id = (Application as BootleggerApp).eventtoconnectoafterloggingin });
                            (Application as BootleggerApp).eventtoconnectoafterloggingin = "";
                        }
                        else
                        {
                            // if there is only one event available
                            if (Bootlegger.BootleggerClient.FeaturedEvents.Count == 1 && WhiteLabelConfig.ALLOW_SINGLE_SHOOT)
                            {
                                var diag = new Android.Support.V7.App.AlertDialog.Builder(this)
                               .SetTitle(Resource.String.single_shoot_title)
                               .SetMessage(Resource.String.single_shoot_msg)
                               .SetPositiveButton(Resource.String.continuebtn, (o, e) =>
                               {
                                   AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                                   {
                                       cancel.Cancel();
                                   });

                                   try
                                   {
                                       cancel = new CancellationTokenSource();
                                       //await LoginFuncs.TryLogin(this, cancel.Token, ShowError, RegisterWithGCM, DoClose);
                                       Intent i = new Intent(this, typeof(Roles));
                                       var ev = Bootlegger.BootleggerClient.FeaturedEvents[0];
                                       i.PutExtra("id", (string.IsNullOrEmpty(ev.group)) ? ev.id : ev.events[0].id);
                                       StartActivityForResult(i, 0);
                                   }
                                   catch (ServerErrorException)
                                   {
                                       AndHUD.Shared.Dismiss();
                                   }
                                   catch (Exception ex)
                                   {
                                       AndHUD.Shared.Dismiss();
                                       LoginFuncs.ShowError(this, ex);
                                   }
                               })
                               .SetNegativeButton(Android.Resource.String.Cancel, (o, e) =>
                               {

                               })
                               .Show();
                            }
                            else
                            {
                                //more than 1 shoot avaialble

                                //more than 6 available -> send right to list...
                                //if (Bootlegger.BootleggerClient.FeaturedEvents.Count > 6)
                                    //StartActivity(typeof(Events));
                            }
                        }
                        AndHUD.Shared.Dismiss();
                    }
                    catch (ApiKeyException)
                    {
                        //do nothing, caught elsewhere
                    }
                    catch (NotSupportedException)
                    {
                        //do nothing, caught elsewhere
                    }
                    catch (Exception e)
                    {
                        LoginFuncs.ShowError(this, e);
                    }
                    break;

                case LoginState.OFFLINE_EVENT:

                    break;
               
                case LoginState.UNKNOWN:

                    
                    break;
            }

            ShowButtons();

            if ((Application as BootleggerApp).ADVERT != "")
            {
                Dialog dialog = new Dialog(this, Android.Resource.Style.ThemeBlackNoTitleBarFullScreen);
                var img = new ImageView(this);
                dialog.SetContentView(img);
                //UrlImageViewHelper.SetUrlDrawable(img, (Application as BootleggerApp).ADVERT,Resource.Drawable.ic_action_picture);
                (Application as BootleggerApp).ADVERT = "";
                dialog.Show();
            }
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            if (Bootlegger.BootleggerClient.CurrentUser == null)
            {
                menu.FindItem(99).SetVisible(true);
                menu.FindItem(88).SetVisible(false);
            }
            else
            {
                menu.FindItem(99).SetVisible(false);
                menu.FindItem(88).SetVisible(true);
            }

            return base.OnPrepareOptionsMenu(menu);
        }

        public override bool OnCreateOptionsMenu(Android.Views.IMenu menu)
        {
            var actionItem1 = menu.Add(0, 77, 0, Resource.String.aboutbtn);
            MenuItemCompat.SetShowAsAction(actionItem1, MenuItemCompat.ShowAsActionAlways);
            actionItem1.SetIcon(Resource.Drawable.ic_action_about);

            var actionItem4 = menu.Add(0, 99, 1, Resource.String.loginbtn);
            MenuItemCompat.SetShowAsAction(actionItem4, MenuItemCompat.ShowAsActionNever);

            var actionItem3 = menu.Add(0, 88, 2, Resource.String.logoutbtn);
            MenuItemCompat.SetShowAsAction(actionItem3, MenuItemCompat.ShowAsActionNever);

            if (WhiteLabelConfig.EXTERNAL_LINKS)
            {
                var actionItem2 = menu.Add(0, Resource.Id.action_help, 3, Resource.String.help);
                MenuItemCompat.SetShowAsAction(actionItem2, MenuItemCompat.ShowAsActionNever);
            }

            if (WhiteLabelConfig.FORCE_LANG)
            {
                var locales = Resources.Assets.GetLocales();

                int index = 0;
                foreach (var lang in locales)
                {
                    var actionItem5 = menu.Add(0, 300 + index, Menu.None, lang);
                    MenuItemCompat.SetShowAsAction(actionItem5, MenuItemCompat.ShowAsActionNever);
                    index++;
                }
            }

            return base.OnCreateOptionsMenu(menu);
        }

        protected async override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            //TODO: this is killing the current event in the camera screen when switching apps...

            //if (resultCode != Review.EDITOR_REQUEST)

            //if (!Bootlegger.BootleggerClient.InBackground)
            //{
            Bootlegger.BootleggerClient.DisconnectForReview();
            //}




            //from video screen:
            if (data?.GetBooleanExtra("needsperms", false) ?? false)
            {
                LoginFuncs.ShowError(this, new NeedsPermissionsException());
            }

            //from video screen -- therefore send to review:
            if (data?.GetBooleanExtra("videocap", false) ?? false)
            {
                Contributions_Edit(new Shoot() {id = data?.GetStringExtra("eventid")}, data?.GetBooleanExtra("needsperms", false) ?? false);
                return;
            }

            if (requestCode == LoginFuncs.NEW_SHOOT_REQUEST)
            {
                if (!string.IsNullOrEmpty(data?.GetStringExtra("event_id")))
                {
                    //connect directly to this shoot:
                    var tmp_event = new Shoot() { id = data.GetStringExtra("event_id") };
                    //TODO list my events (i.e. update)
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                    {
                        cancel.Cancel();
                    });
                    if (Bootlegger.BootleggerClient.CurrentUser!=null)
                        await Bootlegger.BootleggerClient.GetShootHistory(cancel.Token);
                    //ShowShotoInfoDialog(tmp_event);
                    DoConnectToShoot(tmp_event);
                }
            }
            else if (resultCode == Result.FirstUser)
            {
                LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.noconnectionshort)));
            }
            else
            {
                //dont reset if its camera screen
                //if ((Application as BootleggerApp).ReturnState.ReturnsTo != BootleggerApp.ReturnType.OPEN_SHOOT)
                (Application as BootleggerApp).ResetReturnState();
                CurrentState = WhichState();

                if (!string.IsNullOrEmpty(data?.GetStringExtra("connectto")))
                {
                    //get shoot obj:
                    var tmp_event = new Shoot() { id = data.GetStringExtra("connectto") };
                    DoConnectToShoot(tmp_event);
                }
            }
        }

        public override bool OnOptionsItemSelected(Android.Views.IMenuItem item)
        {
            if (item.ItemId == Resource.Id.action_help)
            {
                LoginFuncs.ShowHelp(this, "#login");
                return true;
            }
            if (item.ItemId == 77)
            {
                StartActivity(typeof(About));
                return true;
            }
            if (item.ItemId == 88)
            {
                Logout_Click(null,null);
                return true;
            }
            if (item.ItemId == 99)
            {
                Login_Button_Click(null, null);
                return true;
            }
            if (item.ItemId >= 300)
            {
                var locales = Resources.Assets.GetLocales();

                var index = item.ItemId - 300;
                if (index < locales.Count())
                {
                    //Resources res = context.getResources();
                    // Change locale settings in the app.
                    DisplayMetrics dm = Resources.DisplayMetrics;
                    var conf = Resources.Configuration;
                    conf.SetLocale(new Locale(locales[index])); // API 17+ only.
                        // Use conf.locale = new Locale(...) if targeting lower versions
                    Resources.UpdateConfiguration(conf, dm);
                }
            }
            return true;
        }

        public void OnCancel(IDialogInterface dialog)
        {
            Finish();
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        int lastpage = 1;

        protected override void OnCreate(Bundle bundle)
        {
            this.SetTheme(Resource.Style.Theme_Normal);

            base.OnCreate(bundle);

            lastpage = bundle?.GetInt("page", 1) ?? 1;

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity = this;

            SetContentView(Resource.Layout.Login);


            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            // Sets the Toolbar to act as the ActionBar for this Activity window.
            // Make sure the toolbar exists in the activity and is not null
            SetSupportActionBar(toolbar);

            //AndroidBug5497Workaround.AssistActivity(this);

            FindViewById<ImageButton>(Resource.Id.loginbtn).Click += Login_Button_Click;

            // clear FLAG_TRANSLUCENT_STATUS flag:
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);
            Window.ClearFlags(WindowManagerFlags.TranslucentNavigation);
            Window.ClearFlags(WindowManagerFlags.LayoutNoLimits);

            Bootlegger.BootleggerClient.OnApiKeyInvalid += Comms_OnApiKeyInvalid;
            Bootlegger.BootleggerClient.OnApiVersionChanged += Comms_OnApiVersionChanged;

            if (Build.VERSION.SdkInt >=  Android.OS.BuildVersionCodes.Lollipop)
            {
                //Window.AddFlags(WindowManagerFlags.TranslucentStatus);
                //Window.AddFlags(WindowManagerFlags.TranslucentNavigation);

                // add FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS flag to the window
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);
                //Window.AddFlags(WindowManagerFlags.LayoutNoLimits);


                // finally change the color
                Window.SetStatusBarColor(new Color(ContextCompat.GetColor(this,Android.Resource.Color.Transparent)));
            }

            //CREATE TABS:

            //if (WhiteLabelConfig.ALLOW_CREATE_OWN)
            //{
            //    FindViewById<FloatingActionButton>(Resource.Id.newshoot).Click += Login_Click;
            //}
            //else
            //{
                FindViewById<FloatingActionButton>(Resource.Id.newshoot).LayoutParameters = new CoordinatorLayout.LayoutParams(FindViewById<FloatingActionButton>(Resource.Id.newshoot).LayoutParameters) { Behavior=null };
            //}

            if (bundle==null)
            {
                highlighted = new HomePageList();
                myshoots = new EventListFragment();
                myedits = new MyEditsFragment();
            }
            else
            {
                highlighted = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":0") as HomePageList;
                myshoots = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":1") as EventListFragment;
                myedits = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":2") as MyEditsFragment;
            }

            //fix for if this page was never loaded first time:
            if (myedits==null)
                myedits = new MyEditsFragment();

            if (myshoots == null)
                myshoots = new EventListFragment();

            _tabs = FindViewById<Android.Support.Design.Widget.TabLayout>(Resource.Id.tabs);
            _tabs.TabGravity = 0;
            _tabs.TabMode = 1;
            _pager = FindViewById<ViewPager>(Resource.Id.tabpager);
            _adapter = new HomePageAdapter(SupportFragmentManager,this);
            _pager.Adapter = _adapter;

            highlighted.OnEnterCode += Highlighted_OnEnterCode;
            highlighted.OnConnect += ShowShotoInfoDialog;

            myedits.OnPreview += Myedits_OnPreview;

            _adapter.AddTab("", highlighted, HomePageAdapter.TabType.ALL_SHOOTS);

            myshoots.OnConnect += Myshoots_OnConnect;
            myshoots.OnReview += Myshoots_OnReview;
            //myshoots.OnStartNew += Myshoots_OnStartNew;
            myshoots.OnError += Myshoots_OnError;

            _pager.Post(() => {
                _tabs.SetupWithViewPager(_pager);
                ShowButtons();
            });
            _pager.PageSelected += _pager_PageSelected;

            FindViewById<View>(Resource.Id.uploadtile).Click += Login_For_Upload;
        }

        private void Myshoots_OnError(Exception obj)
        {
            LoginFuncs.ShowError(this, obj);
        }

        private async void Highlighted_OnEnterCode(string obj)
        {
            cancel = new CancellationTokenSource();

            //check valid code:

            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                cancel.Cancel();
            });


            //do this if logged in, else go do login:
            if (Bootlegger.BootleggerClient.CurrentUser == null)
            {
                (Application as BootleggerApp).ReturnState = new BootleggerApp.ApplicationReturnState()
                {
                    ReturnsTo = BootleggerApp.ReturnType.JOIN_CODE,
                    Payload = obj
                };
                Login_Button_Click(null, null);
            }
            else
            {
                if ((Application as BootleggerApp).IsReallyConnected)
                {
                    try
                    {
                        await LoginFuncs.TryLogin(this, cancel.Token);
                    }
                    catch (Exception)
                    {
                        LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.loginagain)));
                    }
                    try
                    {
                        var eventid = await Bootlegger.BootleggerClient.JoinSharedEvent(obj, cancel.Token);
                        Intent i = new Intent(this, typeof(Roles));
                        //i.AddFlags(ActivityFlags.ForwardResult);
                        i.PutExtra("id", eventid);
                        StartActivityForResult(i, 0);
                    }
                    catch
                    {
                        LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.invalidcode)));
                        AndHUD.Shared.CurrentDialog.Dismiss();
                    }
                }
                else
                {
                    LoginFuncs.ShowError(this, new NoNetworkException());
                    AndHUD.Shared.Dismiss();
                }
            }
        }

        //private void Myshoots_OnStartNew()
        //{
        //    LoginFuncs.NewShoot(this);
        //}

        //const int requestpermissionscode = 0;

        private void Highlighted_OnShowMore()
        {
            List_Shoots_Click(null, null);
        }

        private void Myedits_OnPreview(Edit arg1, View arg2)
        {
            if ((Application as BootleggerApp).IsReallyConnected)
            {
                Intent i = new Intent(this, typeof(Preview));
                i.PutExtra(Review.PREVIEW_EDIT, arg1.id);
                StartActivityForResult(i, Review.EDIT_RESPONSE);
            }
            else
            {
                LoginFuncs.ShowError(this, new NoNetworkException());
            }
        }


        MyEditsFragment myedits;

        private async void Login_For_Upload(object sender, EventArgs e)
        {
            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () => {
                cancel.Cancel();
            });
            try
            {
                await LoginFuncs.TryLogin(this, cancel.Token);
                Intent i = new Intent(this.ApplicationContext, typeof(Uploads));
                StartActivity(i);
            }
            catch (Exception ex)
            {
                LoginFuncs.ShowError(this, ex);
            }
            finally
            {
                AndHUD.Shared.Dismiss();
            }
        }

        private void _pager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            if (e.Position != 2)
            {
                if (WhiteLabelConfig.ALLOW_CREATE_OWN)
                    FindViewById<FloatingActionButton>(Resource.Id.newshoot).Show();
            }
            else
                FindViewById<FloatingActionButton>(Resource.Id.newshoot).Hide();


            if (e.Position == 1)
            {
                
                //open login dialog if my shoots tab selected
                if (Bootlegger.BootleggerClient.CurrentUser == null)
                {
                    Login_Button_Click(null, null);
                    _pager.PostDelayed(() => { _pager.SetCurrentItem(0,false); _tabs.GetTabAt(0).Select(); },200);
                }
            }
            else
            {

            }
        }

        private void Myshoots_OnReview(Shoot obj)
        {
            Contributions_Edit(obj,false);
        }

        private void Myshoots_OnConnect(Shoot obj)
        {
            Contributions_ItemClick(obj);
        }

        

        private async void DoConnectToShoot(Shoot obj)
        {
            if (Bootlegger.BootleggerClient.CurrentUser == null)
            {
                (Application as BootleggerApp).eventtoconnectoafterloggingin = obj.id;
                Login_Button_Click(null, null);
            }
            else
            {
                //if has contributed, do this
                if (Bootlegger.BootleggerClient.ShootHistory.Contains(obj))
                {
                    Contributions_ItemClick(Bootlegger.BootleggerClient.ShootHistory[Bootlegger.BootleggerClient.ShootHistory.IndexOf(obj)]);
                }
                else
                {
                    Shoot selected = obj;
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                    {
                        cancel.Cancel();
                    });

                    try
                    {
                        cancel = new CancellationTokenSource();
                        await LoginFuncs.TryLogin(this, cancel.Token);
                        Intent i = new Intent(this, typeof(Roles));
                        i.PutExtra("id", obj.id);
                        StartActivityForResult(i, 0);
                    }
                    catch (ServerErrorException)
                    {
                        AndHUD.Shared.Dismiss();
                    }
                    catch (Exception e)
                    {
                        AndHUD.Shared.Dismiss();
                        LoginFuncs.ShowError(this, new NoNetworkException());
                    }
                }
            }
        }

        private void ShowShotoInfoDialog(Shoot obj)
        {
            if ((Application as BootleggerApp).IsReallyConnected)
            {
                //change to the workflow for showing info dialog:
                ShootInfo info = ShootInfo.NewInstance(obj.id);
                ShootInfo.ClearDels();
                ShootInfo.OnConnect += (s) => DoConnectToShoot(s);
                info.Show(SupportFragmentManager, "fragment_edit_name");
            }
            else
            {
                LoginFuncs.ShowError(this, new NoNetworkException());
            }
        }

        Android.Support.Design.Widget.TabLayout _tabs;
        ViewPager _pager;
        HomePageAdapter _adapter;
        EventListFragment myshoots;
        HomePageList highlighted;

        private void Login_Button_Click(object sender, EventArgs e)
        {
            if ((Application as BootleggerApp).IsReallyConnected)
            {
                if (WhiteLabelConfig.LOCAL_LOGIN && !WhiteLabelConfig.GOOGLE_LOGIN && !WhiteLabelConfig.FACEBOOK_LOGIN)
                {
                    //jump straight to local login -- as no other auth providers available:

                    LoginFuncs.OpenLogin(this, LoginFuncs.LOGIN_PROVIDER_LOCAL);
                }
                else
                {
                    Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
                    FrameLayout frameView = new FrameLayout(this);
                    builder.SetView(frameView);
                    var diag = builder.Create();
                    diag.SetTitle(Resource.String.logintocontrib);
                    LayoutInflater inflater = diag.LayoutInflater;
                    View dialoglayout = inflater.Inflate(Resource.Layout.logindialog, frameView);
                    if (!WhiteLabelConfig.GOOGLE_LOGIN)
                        dialoglayout.FindViewById<ImageButton>(Resource.Id.loginbtn).Visibility = ViewStates.Gone;

                    if (!WhiteLabelConfig.FACEBOOK_LOGIN)
                        dialoglayout.FindViewById<ImageButton>(Resource.Id.loginbtn2).Visibility = ViewStates.Gone;

                    if (!WhiteLabelConfig.LOCAL_LOGIN)
                        dialoglayout.FindViewById<Button>(Resource.Id.loginbtn3).Visibility = ViewStates.Gone;

                    dialoglayout.FindViewById<ImageButton>(Resource.Id.loginbtn).Click += (o, ex) => { diag.Dismiss(); Login_Google(o, ex); };
                    dialoglayout.FindViewById<ImageButton>(Resource.Id.loginbtn2).Click += (o, ex) => { diag.Dismiss(); Login_Facebook(o, ex); };
                    dialoglayout.FindViewById<Button>(Resource.Id.loginbtn3).Click += (o, ex) => { diag.Dismiss(); Login_Local(o, ex); };

                    diag.SetCancelable(true);
                    diag.Show();
                }
            }
            else
            {
                LoginFuncs.ShowError(this, new NoNetworkException());

                //Toast.MakeText(this, Resource.String.notconnected, ToastLength.Short).Show();
                AndHUD.Shared.Dismiss();
            }
        }

        private async void List_Shoots_Click(object sender, EventArgs ex)
        {
            if (Bootlegger.BootleggerClient.CurrentUser == null)
                Login_Button_Click(null, null);
            else
            {
                try
                {
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                    {
                        cancel.Cancel();
                    });
                    await LoginFuncs.TryLogin(this, cancel.Token);
                    StartActivityForResult(typeof(Events),0);
                    AndHUD.Shared.Dismiss();
                }
                catch (TaskCanceledException)
                {
                    LoginFuncs.ShowError(this, new Exception(Resources.GetString(Resource.String.conncancel)));
                }
                catch (Exception e)
                {
                    LoginFuncs.ShowError(this, e);
                }
            }
        }

        private void Link(object sender, EventArgs e)
        {
            Intent myIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(Bootlegger.BootleggerClient.server + "/?viewonly=true"));
            myIntent.PutExtra(Browser.ExtraApplicationId, "com.android.browser");
            StartActivity(myIntent);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutInt("page", _pager.CurrentItem);
            base.OnSaveInstanceState(outState);
        }

        protected override void OnStart()
        {
            base.OnStart();
            //DEBUG:
            //try
            //{
            //    if (Bootlegger.BootleggerClient.CurrentUser != null && Bootlegger.BootleggerClient.MyEdits.ContainsKey(Bootlegger.BootleggerEditStatus.InProgress))
            //    {
            //        var obj = Bootlegger.BootleggerClient.MyEdits[Bootlegger.BootleggerEditStatus.InProgress].First();
            //        await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, new System.Threading.CancellationTokenSource().Token);
            //        await Bootlegger.BootleggerClient.ConnectForReview(WhiteLabelConfig.REDUCE_BANDWIDTH, new Shoot() { id = obj.media[0].event_id }, cancel.Token);
            //        Intent i = new Intent(this, typeof(Editor));
            //        i.PutExtra(Review.EDIT, obj.id);
            //        StartActivityForResult(i, Review.EDIT_RESPONSE);
            //    }
            //}
            //catch (Exception e)
            //{
            //    LoginFuncs.ShowError(this, e);
            //}
        }

        async void Logout_Click(object sender, EventArgs ee)
        {

            //if (Bootlegger.BootleggerClient.UploadQueue.Count > 0)
            //{
            var diag = new Android.Support.V7.App.AlertDialog.Builder(this)
            .SetTitle(Resource.String.logouttitle)
            .SetMessage(Resource.String.logoutmsg)
            .SetPositiveButton(Resource.String.continuebtn, async (o, e) =>
            {
                //grav_loaded = false;
                AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loggingout), -1, MaskType.Black);
                //    connectinglocal = false;
                await Task.Delay(1000);
                Bootlegger.BootleggerClient.Logout();
                // CookieManager.Instance.RemoveAllCookie();
                //reload login page
                var allprefs = GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
                var editor = allprefs.Edit();
                editor.Clear();
                editor.Apply();
                editor.Commit();

                EventListFragment.EventUpdateDelegate del = Bootlegger.BootleggerClient.GetShootHistory;
                myshoots.SetEvents("ShootHistory", del, cancel.Token, EventAdapter.EventViewType.MYEVENT);

                ShowButtons();
                //Insights.Identify(Insights.Traits.GuestIdentifier, null);
                AndHUD.Shared.Dismiss();
            })
            .SetNegativeButton(Android.Resource.String.Cancel, (o, e) => { })
            .Show();
            //}
            //else
            //{
            //    //grav_loaded = false;
            //    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loggingout), -1, MaskType.Black);
            //    //    connectinglocal = false;
            //    await Task.Delay(1000);
            //    Bootlegger.BootleggerClient.Logout();
            //    // CookieManager.Instance.RemoveAllCookie();
            //    //reload login page
            //    var allprefs = GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
            //    var editor = allprefs.Edit();
            //    editor.Clear();
            //    editor.Apply();
            //    editor.Commit();

            //    EventListFragment.EventUpdateDelegate del = Bootlegger.BootleggerClient.GetShootHistory;
            //    myshoots.SetEvents("ShootHistory", del, cancel.Token, EventAdapter.EventViewType.MYEVENT);

            //    ShowButtons();
            //    //Insights.Identify(Insights.Traits.GuestIdentifier, null);
            //    AndHUD.Shared.Dismiss();
            //}
           
        }

        void Login_Google(object sender, EventArgs e)
        {
            LoginFuncs.OpenLogin(this,LoginFuncs.LOGIN_PROVIDER_GOOGLE);
        }

        void Login_Facebook(object sender, EventArgs e)
        {
            LoginFuncs.OpenLogin(this, LoginFuncs.LOGIN_PROVIDER_FACEBOOK);
        }

        void Login_Local(object sender, EventArgs e)
        {
            LoginFuncs.OpenLogin(this, LoginFuncs.LOGIN_PROVIDER_LOCAL);
        }

        protected override void OnPause()
        {
            base.OnPause();
            Bootlegger.BootleggerClient.UnregisterForEditUpdates();
            try
            {
                for (int i = 0; i < _adapter.Count; i++)
                {
                    var asf = _adapter.GetItem(i);
                    if (asf is IImagePausable) (asf as IImagePausable).Pause();
                }
            }
            catch (Exception)
            {

            }
        }
    }
}