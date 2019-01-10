/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using AndroidHUD;
using Android.Support.V4.View;
using Android.Provider;
using Bootleg.Droid.Fragments;
using Bootleg.API;
using System.Threading;
using Android.Support.Design.Widget;
using Android.Widget;
using System;
using Square.Picasso;
using Android.Graphics.Drawables;
using Android.Support.V7.Graphics;
using System.Collections.Generic;
using Bootleg.API.Model;
using Android.Support.V4.Content.Res;
using Android.Graphics;

namespace Bootleg.Droid
{
    [Activity(Label = "", NoHistory = true)]

    public class Roles : AppCompatActivity
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
                    //Intent i = new Intent(this.ApplicationContext, typeof(Login));
                    //StartActivity(i);
                    return true;

                default:
                    
                    Intent myIntent = new Intent(Intent.ActionView, Android.Net.Uri.Parse(Resources.GetString(Resource.String.HelpLink) + "#roles"));
                    myIntent.PutExtra(Browser.ExtraApplicationId, "com.android.browser");
                    StartActivity(myIntent);
                    return base.OnOptionsItemSelected(item);
            }
        }

        public override void OnBackPressed()
        {
            if (!Bootlegger.BootleggerClient.CurrentEvent?.offline ?? false)
                Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH, true);

            Finish();
            //Intent i = new Intent(this.ApplicationContext, typeof(Login));
            //StartActivity(i);
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
            Bootlegger.BootleggerClient.CurrentClientRole = null;
        }

        CancellationTokenSource cancel;

        Shoot CurrentEvent;

        public override void FinishFromChild(Activity child)
        {
            base.FinishFromChild(child);

            //when video gets logged out:
            if (!Bootlegger.BootleggerClient.Connected)
            {
                this.Finish();
                return;
            }
            //else rechoose role...
        }

        protected override void OnStart()
        {
            base.OnStart();
            //if ((Application as BootleggerApp).Comms.CurrentEvent.roleimg != null)
                
        }

        //MyPagerAdapter pageadapter;

        protected async override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
          

            SetTheme(Resource.Style.Theme_Normal);

            SetContentView(Resource.Layout.Roles_Activity);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            // Sets the Toolbar to act as the ActionBar for this Activity window.
            // Make sure the toolbar exists in the activity and is not null
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            if (Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.Lollipop)
            {

                // add FLAG_DRAWS_SYSTEM_BAR_BACKGROUNDS flag to the window
                Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                // finally change the color
                Window.SetStatusBarColor(Resources.GetColor(Android.Resource.Color.Transparent));
            }

            //create fragment:

            string id = Intent.Extras?.GetString("id");
            if (id != "")
            {
                //Analytics.TrackEvent("ChooseRole");
                
                //load event info:
                cancel = new CancellationTokenSource();
                AndHUD.Shared.Show(this, Resources.GetString(Resource.String.connecting), -1, MaskType.Black, null, null, true, () =>
                {
                    cancel.Cancel();
                    Finish();
                });

                try
                {
                    CurrentEvent = await Bootlegger.BootleggerClient.GetEventInfo(id, cancel.Token);
                }
                catch (Exception)
                {
                    SetResult(Result.FirstUser);
                    Finish();
                    return;
                }
                finally
                {
                    AndHUD.Shared.Dismiss();
                }
            }
            else
            {
                CurrentEvent = Bootlegger.BootleggerClient.CurrentEvent;
            }

            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("ChooseRole",
                    new KeyValuePair<string, string>("eventid", CurrentEvent.id));

            CollapsingToolbarLayout collapsingToolbar = FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar);
            collapsingToolbar.SetTitle(CurrentEvent.name);
            //collapsingToolbar.SetCollapsedTitleTextColor(Color.Transparent);
            collapsingToolbar.SetExpandedTitleTextAppearance(Resource.Style.ExpandedAppBar);
            Typeface font = ResourcesCompat.GetFont(this, Resource.Font.montserratregular);
            FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).CollapsedTitleTypeface = font;
            FindViewById<CollapsingToolbarLayout>(Resource.Id.collapsing_toolbar).ExpandedTitleTypeface = font;

            if (!string.IsNullOrEmpty(CurrentEvent.roleimg))
            {
                FindViewById<AppBarLayout>(Resource.Id.appbar).SetExpanded(false,false);
            }

            if (!string.IsNullOrEmpty(CurrentEvent.iconbackground) && !WhiteLabelConfig.REDUCE_BANDWIDTH)
                Picasso.With(this).Load(CurrentEvent.iconbackground).CenterCrop().Fit().MemoryPolicy(MemoryPolicy.NoCache, MemoryPolicy.NoStore).Into(FindViewById<ImageView>(Resource.Id.defaultback), new Action(() => {
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
                }),null);
            else
                Picasso.With(this).Load(Resource.Drawable.user_back).CenterCrop().Fit().Into(FindViewById<ImageView>(Resource.Id.defaultback));

            AndHUD.Shared.Dismiss();



            SelectRoleFrag myrole;

            if (bundle == null)
            {
                myrole = new SelectRoleFrag(CurrentEvent, false);
                myrole.OnRoleChanged += Myrole_OnRoleChanged;
                Android.Support.V4.App.FragmentTransaction ft = SupportFragmentManager.BeginTransaction();
                try
                {
                    ft.Add(Resource.Id.roles_frag_holder, myrole, "rolefragment").Commit();
                }
                catch
                {
                    //failed dont know why!
                }
            }
            else
            {
                myrole = SupportFragmentManager.FindFragmentByTag("rolefragment") as SelectRoleFrag;
                myrole.OnRoleChanged += Myrole_OnRoleChanged; 
            }
        }

        public const string FROM_ROLE = "fromrole";

        private void Myrole_OnRoleChanged()
        {
            Intent intent = new Intent(this, typeof(Video));
            intent.AddFlags(ActivityFlags.ForwardResult);
            intent.PutExtra(FROM_ROLE, true);
            StartActivity(intent);
            Finish();
        }
    }
}