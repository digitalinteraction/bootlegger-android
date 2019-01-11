using System;
using System.Linq;
using Plugin.Permissions.Abstractions;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using System.Threading;
using Plugin.Geolocator;
using System.Threading.Tasks;
using Plugin.Permissions;
using Bootleg.Droid.UI;
using Android.Support.V4.Widget;
using Bootleg.API.Model;

namespace Bootleg.Droid.Fragments.Home
{
    public class HomePageList : Android.Support.V4.App.Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here

            //EventListFragment.EventUpdateDelegate del1 = Bootlegger.BootleggerClient.UpdateFeatured;
            //featured.SetEvents("FeaturedEvents", del1, new CancellationTokenSource().Token, EventAdapter.EventViewType.FEATURED);

            //featured.OnConnect += Featured_OnConnect;
            nearby.OnConnect += Featured_OnConnect;

        }

        //private async Task CalculateNearby(CancellationToken cancel)
        //{
        //    Bootleg

        //    //HACK FOR DEMO
        //    //if (Bootlegger.BootleggerClient.FeaturedEvents.Count > 0)
        //        //Bootlegger.BootleggerClient.NearbySingle = new List<Shoot>() { Bootlegger.BootleggerClient.FeaturedEvents.First() };
        //}

        bool hasposition = false;


        public async override void OnResume()
        {
            base.OnResume();
            Plugin.Geolocator.Abstractions.Position pos;

            try
            {
                if (WhiteLabelConfig.LOCATION_SHOOTS_ENABLED)
                {
                    var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Permission.Location);
                    if (status != PermissionStatus.Denied)
                    {
                        if (await CrossPermissions.Current.ShouldShowRequestPermissionRationaleAsync(Permission.Location))
                        {
                            await LoginFuncs.ShowSnackbar(this.Activity, this.Activity.FindViewById(Resource.Id.main_content), "Location required to locate nearby shoots");
                        }

                        var results = await CrossPermissions.Current.RequestPermissionsAsync(Permission.Location);
                        //Best practice to always check that the key exists
                        if (results.ContainsKey(Permission.Location))
                            status = results[Permission.Location];
                    }

                    if (status == PermissionStatus.Granted)
                    {
                        //var results = await CrossGeolocator.Current.GetPositionAsync(10000);
                        //LabelGeolocation.Text = "Lat: " + results.Latitude + " Long: " + results.Longitude;
                        hasposition = true;
                        pos = await CrossGeolocator.Current.GetLastKnownLocationAsync();
                        if (pos != null)
                            Bootlegger.BootleggerClient.UserLocation = new Tuple<double, double>(pos.Latitude, pos.Longitude);
                    }
                    else if (status != PermissionStatus.Unknown && status != PermissionStatus.Denied)
                    {
                        //no permissions
                        //await DisplayAlert("Location Denied", "Can not continue, try again.", "OK");
                        UI.LoginFuncs.ShowError(this.Activity, new Exception("Enable location to find local shoots"));
                    }
                }
            }
            catch (Exception)
            {

                //LabelGeolocation.Text = "Error: " + ex;
            }


            //await CalculateNearby(new CancellationTokenSource().Token);

            EventListFragment.EventUpdateDelegate del2 = Bootlegger.BootleggerClient.UpdateFeatured;
            nearby.SetEvents("FeaturedEvents", del2, new CancellationTokenSource().Token, EventAdapter.EventViewType.FEATURED);

            // try to get live position:
            if (hasposition)
            {
                try
                {
                    pos = await CrossGeolocator.Current.GetPositionAsync(TimeSpan.FromSeconds(10));
                    if (pos != null)
                        Bootlegger.BootleggerClient.UserLocation = new Tuple<double, double>(pos.Latitude, pos.Longitude);
                    //await CalculateNearby(new CancellationTokenSource().Token);
                    EventListFragment.EventUpdateDelegate del = Bootlegger.BootleggerClient.UpdateFeatured;
                    nearby.SetEvents("FeaturedEvents", del, new CancellationTokenSource().Token, EventAdapter.EventViewType.FEATURED);
                }
                catch
                {
                    //do nothing, cannot get location
                }
            }

            await nearby.RefreshMe(true);
        }

        EventListFragment featured = new EventListFragment();
        EventListFragment nearby = new EventListFragment(EventAdapter.EventViewType.FEATURED);

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.FeaturedHomePage, container, false);

            //ChildFragmentManager.BeginTransaction().Replace(Resource.Id.featuredlayout, featured as Android.Support.V4.App.Fragment).Commit();
            ChildFragmentManager.BeginTransaction().Replace(Resource.Id.featuredlayout, nearby as Android.Support.V4.App.Fragment).Commit();

            //view.FindViewById<Button>(Resource.Id.seeallshoots).Clic += HomePageList_Click;

            view.FindViewById<Button>(Resource.Id.seenearbybtn).Click += HomePageList_Click;

            //view.FindViewById<Button>(Resource.Id.seeallfeaturedbtn).Click += HomePageList_Click2;

            view.FindViewById<EditText>(Resource.Id.code).TextChanged += HomePageList_TextChanged;

            if (!WhiteLabelConfig.ALLOW_CODE_JOIN)
            {
                view.FindViewById(Resource.Id.joincodeheader).Visibility = ViewStates.Gone;
                view.FindViewById(Resource.Id.joincodebody).Visibility = ViewStates.Gone;
            }

            view.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refresh += HomePageList_Refresh;

            return view;
        }

        private async void HomePageList_Refresh(object sender, EventArgs e)
        {
            try
            {
                View.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refreshing = false;

                //refresh featured:
                if ((Context.ApplicationContext as BootleggerApp).IsReallyConnected)
                {
                    await nearby.RefreshMe(true);
                }
            }
            catch (Exception ex)
            {
                LoginFuncs.ShowError(Activity, ex);
            }
            finally
            {
            }

        }

        private async void HomePageList_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
        {
            if (e.Text.Any())
                View.FindViewById<ImageView>(Resource.Id.edit_status).SetImageDrawable(null);

            if (e.Text.Count() == 4)
            {
                try
                {
                    View.FindViewById<ImageView>(Resource.Id.edit_status).SetImageDrawable(null);

                    View.FindViewById(Resource.Id.edit_progress).Visibility = ViewStates.Visible;
                    //show spinner:
                    await Task.Delay(1000);
                    var result = await Bootlegger.BootleggerClient.JoinSharedEvent(e.Text.ToString(), new CancellationTokenSource(TimeSpan.FromSeconds(30)).Token);
                    View.FindViewById<ImageView>(Resource.Id.edit_status).SetImageResource(Resource.Drawable.appbar_check);
                    OnEnterCode?.Invoke(e.Text.ToString());
                }
                catch (Exception)
                {
                    View.FindViewById<ImageView>(Resource.Id.edit_status).SetImageResource(Resource.Drawable.ic_error_black_24dp);
                    View.FindViewById(Resource.Id.edit_progress).Visibility = ViewStates.Gone;
                }
                finally
                {
                    View.FindViewById(Resource.Id.edit_progress).Visibility = ViewStates.Gone;

                    View.FindViewById<EditText>(Resource.Id.code).Text = "";
                }
            }
        }

        public event Action<Shoot> OnConnect;
        public event Action<string> OnEnterCode;

        private void Featured_OnConnect(Shoot obj)
        {
            OnConnect?.Invoke(obj);
        }

        private void HomePageList_Click(object sender, EventArgs e)
        {
            if ((Context.ApplicationContext as BootleggerApp).IsReallyConnected)
            {
                var intent = new Intent(Context, typeof(Events));
                intent.PutExtra("listtype", "all");
                StartActivityForResult(intent, UI.LoginFuncs.NEW_SHOOT_REQUEST);
            }
            else
            {
                LoginFuncs.ShowError(Activity, new Exception(Resources.GetString(Resource.String.noconnectionshort)));
            }
        }
    }
}