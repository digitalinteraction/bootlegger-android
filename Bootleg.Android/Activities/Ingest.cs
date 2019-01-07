using System.Collections.Generic;

using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Bootleg.API.Model;
using Bootleg.Droid.Adapters;
using Bootleg.Droid.UI;
using static Bootleg.Droid.AllClipsFragment;

namespace Bootleg.Droid.Screens
{

    [Activity(Label = "Ingest", ScreenOrientation = Android.Content.PM.ScreenOrientation.Landscape, LaunchMode = LaunchMode.SingleTask)]
	public class Ingest : AppCompatActivity
	{
        AllClipsFragment allclipsfragment;
        ArrayAdapter<FilterTuple<Bootlegger.MediaItemFilterType, string>> filters;
        ArrayAdapter<FilterTuple<Bootlegger.MediaItemFilterDirection, string>> directions;

        protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

            SetTheme(Resource.Style.Theme_Normal);

            SetContentView(Resource.Layout.Ingest);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            var fragmentTransaction = SupportFragmentManager.BeginTransaction();
            allclipsfragment = new AllClipsFragment(AllClipsFragment.ClipViewMode.INGEST);
            fragmentTransaction.Add(Resource.Id.selector, allclipsfragment);
            fragmentTransaction.Commit();
            (allclipsfragment as IImagePausable).Pause();
            allclipsfragment.OnPreview += Allclipsfragment_OnPreview;
            allclipsfragment.OnBack += Allclipsfragment_OnBack;
            allclipsfragment.OnNext += Allclipsfragment_OnNext;

            IngestWizard.ShowWizard(this, false);

            spinner = FindViewById<Spinner>(Resource.Id.filter_spinner);

            filters = new IconSpinnerAdapter(this);
            foreach (var t in Bootlegger.MediaItemFilter)
            {
                filters.Add(new FilterTuple<Bootlegger.MediaItemFilterType, string>(t.Key, t.Value));
            }

            spinner.Adapter = filters;

            spinner2 = FindViewById<Spinner>(Resource.Id.direction_spinner);

            directions = new IconOrderSpinnerAdapter(this);
            directions.Add(new FilterTuple<Bootlegger.MediaItemFilterDirection, string>(Bootlegger.MediaItemFilterDirection.ASCENDING, ""));
            directions.Add(new FilterTuple<Bootlegger.MediaItemFilterDirection, string>(Bootlegger.MediaItemFilterDirection.DESCENDING, ""));
            
            spinner2.Adapter = directions;


            spinner.ItemSelected += Spinner_ItemSelected;
            spinner2.ItemSelected += Spinner2_ItemSelected;
        }

        private void Spinner2_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            //direction
            allclipsfragment.UpdateSelectionOrder(filters.GetItem(spinner.SelectedItemPosition).Item1, directions.GetItem(spinner2.SelectedItemPosition).Item1);
        }

        private void Spinner_ItemSelected(object sender, AdapterView.ItemSelectedEventArgs e)
        {
            //filter
            allclipsfragment.UpdateSelectionOrder(filters.GetItem(spinner.SelectedItemPosition).Item1, directions.GetItem(spinner2.SelectedItemPosition).Item1);
        }

        Spinner spinner;
        Spinner spinner2;

        private void Allclipsfragment_OnNext()
        {
            //if it came from review:
            //if (string.IsNullOrEmpty(Intent.Extras.GetString(Review.EDIT)) && !Intent.Extras.GetBoolean(Review.FROMEDITOR))
            //{
            //    Intent i = new Intent(this, typeof(Editor));
            //    i.PutExtra(Review.EDIT, currenteditid);
            //    StartActivity(i);
            //}
            Finish();
        }

        private void Allclipsfragment_OnBack()
        {
            Finish();
        }

        string currenteditid = "";
        protected override void OnResume()
        {
            base.OnResume();
            string id = Intent.Extras.GetString(Review.EDIT);
            currenteditid = id;
            Title = Resources.GetString(Resource.String.ingesttitle, Bootlegger.BootleggerClient.CurrentEvent.name);

            Window.AddFlags(WindowManagerFlags.Fullscreen);

            //Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("Ingest"sas);
            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("Ingest", 
                new KeyValuePair<string, string>("editid", id), 
                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));

        }

        const int MEDIAVIEW = 3423;

        MediaItem currentitem;

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            if (requestCode == MEDIAVIEW)
            {
                if (allclipsfragment.sortFilter == Bootlegger.MediaItemFilterType.TOPIC)
                    allclipsfragment.Refresh();
                else
                    //update a single item:
                    allclipsfragment.NotifyUpdate(currentitem);
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    Finish();
                    return true;
                case 99:
                    IngestWizard.ShowWizard(this, true);
                    return true;
                default:
                    LoginFuncs.ShowHelp(this, "#ingest");
                    return true;
            }
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            if (WhiteLabelConfig.EXTERNAL_LINKS)
            {
                var actionItem1 = menu.Add(Resource.String.help);
                MenuItemCompat.SetShowAsAction(actionItem1, MenuItemCompat.ShowAsActionNever);
            }

            var actionItem2 = menu.Add(0,99,0,Resource.String.howitworks);
            MenuItemCompat.SetShowAsAction(actionItem2, MenuItemCompat.ShowAsActionNever);

            return true;
        }

        private void Allclipsfragment_OnPreview(MediaItem arg1, View arg2)
        {
            currentitem = arg1;
            Intent i = new Intent(this, typeof(Preview));
            i.PutExtra(Review.PREVIEW, arg1.id);
            i.PutExtra(Review.INGEST_MODE, true);
            StartActivityForResult(i,MEDIAVIEW);
        }
    }
}