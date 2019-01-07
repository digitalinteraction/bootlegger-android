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
using Android.Content.PM;
using Android.Support.V7.App;
using Android.Provider;
using Android.Support.V4.View;
using Square.Picasso;
using Android.Text.Method;
using Bootleg.Droid.UI;
using Android.Support.V4.App;
using System.IO;
using Microsoft.AppCenter.Analytics;

namespace Bootleg.Droid
{
    [Activity(Label = "@string/abouttitle", MainLauncher = false, Theme = "@style/Theme.Normal")]
    public class About : AppCompatActivity
    {
        public override bool OnCreateOptionsMenu(Android.Views.IMenu menu)
        {
            if (WhiteLabelConfig.EXTERNAL_LINKS)
            {
                var actionItem1 = menu.Add(Resource.String.help);
                MenuItemCompat.SetShowAsAction(actionItem1, MenuItemCompat.ShowAsActionNever);
            }

            var actionItem2 = menu.Add(0, 88, 1, Resource.String.opensource);
            MenuItemCompat.SetShowAsAction(actionItem2, MenuItemCompat.ShowAsActionNever);


            return base.OnCreateOptionsMenu(menu);
        }

         public override bool OnOptionsItemSelected(IMenuItem item)
         {
             switch (item.ItemId)
             {
                 case Android.Resource.Id.Home:
                     Finish(); 
                     return true;

                case 88:
                    var message = new StreamReader(Assets.Open("credits.txt")).ReadToEnd();
                    new Android.Support.V7.App.AlertDialog.Builder(this)
                        .SetNeutralButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                        {

                        })).SetMessage(message)
                        .SetTitle(Resource.String.opensource)
                        .Show();
                    return true;

                 default:
                    LoginFuncs.ShowHelp(this, "#about");
                        
                     return base.OnOptionsItemSelected(item);
             }
         }

        protected override void OnCreate(Bundle bundle)
        {
            //Insights.Track("AboutScreen");
            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("AboutScreen");
            //Analytics.TrackEvent("AboutScreen");
            this.SetTheme(Resource.Style.Theme_Normal);
            base.OnCreate(bundle);


            SetContentView(Resource.Layout.About);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            FindViewById<TextView>(Resource.Id.textView1).MovementMethod = new ScrollingMovementMethod();

            string from = "SideLoad";

            var pkg = PackageManager.GetInstallerPackageName(PackageName);

            if (pkg != null)
            {
                from = "Google Play";
            }

            var version = PackageManager.GetPackageInfo(PackageName, 0);
            FindViewById<TextView>(Resource.Id.versiontext).Text = "v" + version.VersionCode + " / " + version.VersionName + " / " + from;

            FindViewById<TextView>(Resource.Id.linkbtn).Click += About_Click;

            FindViewById(Resource.Id.link).Click += Link_Click;

            if (!WhiteLabelConfig.EXTERNAL_LINKS)
            {
                FindViewById(Resource.Id.links).Visibility = ViewStates.Gone;
            }

            //Picasso.With(this).Load(Resource.Drawable.logo_white).NoFade().Fit().CenterInside().Into(FindViewById<ImageView>(Resource.Id.logo1));
            Picasso.With(this).Load(Resource.Drawable.openlab_dark).NoFade().Into(FindViewById<ImageView>(Resource.Id.logo2));

            //FeedbackManager.Register(this,WhiteLabelConfig.INSIGHTSKEY);


            FindViewById<Button>(Resource.Id.feedback_btn).Visibility = ViewStates.Gone;

            //Button feedbackButton = FindViewById<Button>(Resource.Id.feedback_btn);
            //feedbackButton.Click += (o,e) =>
            //{
            //    FeedbackManager.ShowFeedbackActivity(this);
            //};
        }

        void Link_Click(object sender, EventArgs e)
		{
            LoginFuncs.ShowHelp(this, "");
		}

        void About_Click(object sender, EventArgs e)
        {
            Intent emailIntent = new Intent(Intent.ActionSend);
            /* Fill it with Data */
            emailIntent.SetType("text/plain");
            emailIntent.PutExtra(Intent.ExtraEmail, new String[] { Resources.GetString(Resource.String.Email) });
            emailIntent.PutExtra(Intent.ExtraSubject, "Info about "+Resources.GetString(Resource.String.ApplicationName));

            /* Send it off to the Activity-Chooser */
            StartActivity(Intent.CreateChooser(emailIntent, "Send mail..."));
        }
    }
}