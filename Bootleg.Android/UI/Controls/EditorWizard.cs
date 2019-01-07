using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Bootleg.Droid.SplashActivity;
using Android.Support.V4.View;
using ViewPagerIndicator;
using Square.Picasso;

namespace Bootleg.Droid.UI
{
    public static class EditorWizard
    {
        class Wizard
        {

            View thisview;
            ViewPager mPager;
            Dialog thedialog;
            public void StartWizard(Activity context, bool force)
            {
                var settings = context.GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);

                if (force || !settings.Contains("editor_help"))
                {

                    var dialog = new Android.Support.V7.App.AlertDialog.Builder(context);
                    View di = context.LayoutInflater.Inflate(Resource.Layout.EditorWizard, null);
                    thisview = di;

                    di.FindViewById(Resource.Id.theroot).Visibility = ViewStates.Visible;
                    di.FindViewById<Button>(Resource.Id.ok).Click += EditorWizard_Click1;
                    di.FindViewById<Button>(Resource.Id.next).Click += EditorWizard_Click;
                    di.FindViewById<Button>(Resource.Id.skip).Click += EditorWizard_Click1;

                    var mAdapter = new WizardPagerAdapter();

                    mPager = di.FindViewById<ViewPager>(Resource.Id.pager);
                    mPager.OffscreenPageLimit = 4;
                    //mPager.SetOnPageChangeListener(this);

                    mPager.Adapter = mAdapter;
                    mPager.PageSelected += MPager_PageSelected;

                    var indicator = di.FindViewById<CirclePageIndicator>(Resource.Id.indicator);
                    indicator.SetViewPager(mPager);
                    indicator.SetSnap(true);

                    dialog.SetView(di)
                    .SetCancelable(true);

                    var editor = settings.Edit();
                    editor.PutBoolean("editor_help",true);
                    editor.Commit();
                    thedialog = dialog.Show();

                    Picasso.With(context).Load(Resource.Drawable.edit_page1).Fit().CenterInside().NoFade().Into(thedialog.FindViewById<ImageView>(Resource.Id.editpage1));
                    Picasso.With(context).Load(Resource.Drawable.edit_page2).Fit().CenterInside().NoFade().Into(thedialog.FindViewById<ImageView>(Resource.Id.editpage2));
                    Picasso.With(context).Load(Resource.Drawable.edit_page3).Fit().CenterInside().NoFade().Into(thedialog.FindViewById<ImageView>(Resource.Id.editpage3));

                }

            }

            private void EditorWizard_Click1(object sender, EventArgs e)
            {
                thedialog.Dismiss();
            }

            private void EditorWizard_Click(object sender, EventArgs e)
            {
                ViewPager mPager = thisview.FindViewById<ViewPager>(Resource.Id.pager);
                if (mPager.CurrentItem < mPager.ChildCount - 1)
                    mPager.SetCurrentItem(mPager.CurrentItem + 1, true);
            }

            private void MPager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
            {
                ViewPager mPager = sender as ViewPager;
                if (mPager.CurrentItem == mPager.ChildCount - 1)
                {
                    thisview.FindViewById(Resource.Id.next).Visibility = ViewStates.Gone;
                    thisview.FindViewById(Resource.Id.ok).Visibility = ViewStates.Visible;
                }
                else
                {
                    thisview.FindViewById(Resource.Id.next).Visibility = ViewStates.Visible;
                    thisview.FindViewById(Resource.Id.ok).Visibility = ViewStates.Gone;
                }
            }
        }


        public static void ShowWizard(Activity context, bool force)
        {
            new Wizard().StartWizard(context, force);
        }

        
    }
}