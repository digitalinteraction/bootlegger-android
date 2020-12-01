/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Support.V7.App;
using Android.Views;
using Bootleg.API;
using AndroidHUD;
using Android.Support.V4.App;
using Android.Support.V4.View;
using SearchView = Android.Support.V7.Widget.SearchView;
using Java.Interop;
using System.Threading;
using Bootleg.Droid.UI;
using Bootleg.Droid.Screens;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    [Activity]
    public class Events : AppCompatActivity
    {
        public override void OnBackPressed()
        {
            if (_pager.CurrentItem == 1)
                _pager.CurrentItem = 0;
            else
                base.OnBackPressed();

            if (!cancel.IsCancellationRequested)
                cancel.Cancel();
        }

        protected override void OnResume()
        {
            //Analytics.TrackEvent("EventsScreen");
            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("EventsList");
            //Insights.Track("EventsScreen");
            if ((Application as BootleggerApp).TOTALFAIL == true)
            {
                Finish();
                System.Environment.Exit(0);
                return;
            }
            base.OnResume();

            cancel = new CancellationTokenSource();
            AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true, () =>
            {
                cancel.Cancel();
                Finish();
            });

            //load the data...
            //await Task.Delay(5000);

            // await mainlist.RefreshData(cancel.Token);
            Utils.DissmissHud();

            _pager.CurrentItem = 0;
            Utils.DissmissHud();
        }

        public override void FinishFromChild(Activity child)
        {
            base.FinishFromChild(child);
            if (!Bootlegger.BootleggerClient.Connected)
            {
                this.Finish();
                return;
            }
        }

        public class MyPagerAdapter : FragmentPagerAdapter
        {
            private Android.Support.V4.App.FragmentManager SupportFragmentManager;
            List<Android.Support.V4.App.Fragment> fragments = new List<Android.Support.V4.App.Fragment>();

            public MyPagerAdapter(Android.Support.V4.App.FragmentManager SupportFragmentManager)
                : base(SupportFragmentManager)
            {
                this.SupportFragmentManager = SupportFragmentManager;
            }


            public void AddTab(string title, Android.Support.V4.App.Fragment frag)
            {
                Titles.Add(title);
                fragments.Add(frag);
                NotifyDataSetChanged();
            }

            List<string> Titles = new List<string>();

            public override Android.Support.V4.App.Fragment GetItem(int position)
            {
                return fragments[position];
            }

            public override int Count
            {
                get { return Titles.Count; }
            }

            public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
            {
                return new Java.Lang.String(Titles[position]);
            }
        }

        void Roles_Click(object sender, EventArgs e)
        {
            //go to uploads screen
            Intent i = new Intent(this.ApplicationContext, typeof(Uploads));
            StartActivity(i);
        }

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);


            // Create your application here

            SetTheme(Resource.Style.Theme_Normal);

            SetContentView(Resource.Layout.Events);
            SetTitle(Resource.String.findevent);

            Android.Support.V7.Widget.Toolbar toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            _pager = FindViewById<ViewPager>(Resource.Id.tabpager);
            var _adapter = new MyPagerAdapter(SupportFragmentManager);
            _pager.Adapter = _adapter;

            if (bundle == null)
            {
                mainlist = new EventListFragment();
                sublist = new EventListFragment();
            }
            else
            {
                mainlist = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":0") as EventListFragment;
                sublist = SupportFragmentManager.FindFragmentByTag("android:switcher:" + Resource.Id.tabpager + ":1") as EventListFragment;
            }

            mainlist.OnConnect += Mainlist_OnConnect;
            mainlist.OnSub += Mainlist_OnSub;
            sublist.OnConnect += Mainlist_OnConnect;

            _adapter.AddTab("Shoot List", mainlist);
            _adapter.AddTab("Sub Events", sublist);

            var listtype = Intent.Extras.GetString("listtype", "all");

            switch (listtype)
            {
                case "featured":
                    EventListFragment.EventUpdateDelegate del1 = Bootlegger.BootleggerClient.UpdateFeatured;
                    mainlist.SetEvents("FeaturedEvents", del1, cancel.Token, EventAdapter.EventViewType.FEATURED_LIST);
                    Title = "Featured Shoots";
                    break;
                case "nearby":
                    EventListFragment.EventUpdateDelegate del2 = Bootlegger.BootleggerClient.UpdateFeatured;
                    mainlist.SetEvents("Nearby", del2, cancel.Token, EventAdapter.EventViewType.NEARBY);
                    Title = "Nearby Shoots";
                    break;

                case "all":
                default:
                    EventListFragment.EventUpdateDelegate del3 = Bootlegger.BootleggerClient.ListMyEvents;
                    mainlist.SetEvents("MyEvents", del3, cancel.Token, EventAdapter.EventViewType.LIST);
                    break;
            }
        }

        ViewPager _pager;
        EventListFragment sublist;
        EventListFragment mainlist;

        private void Mainlist_OnSub(Shoot obj)
        {
            // -- change page and set sub list...
            //set sublist...
            sublist.LoadEvents(obj.events, obj,EventAdapter.EventViewType.LIST);
            _pager.Post(() => {
                _pager.CurrentItem = 1;
            });
            //_pager.CurrentItem = 1;
        }
        CancellationTokenSource cancel = new CancellationTokenSource();

        private void Mainlist_OnConnect(Shoot obj)
        {
            //NEW VERSION -- ONLY OPEN ROLES SCREEN, WITHOUT CONNECTING...

            //Intent i = new Intent(this, typeof(Roles));
            //i.PutExtra("id", obj.id);
            //StartActivityForResult(i, 0);

            ShootInfo info = ShootInfo.NewInstance(obj.id);
            ShootInfo.ClearDels();
            ShootInfo.OnConnect += (s) =>
            {
                Bundle conData = new Bundle();
                conData.PutString("connectto", s.id);
                Intent intent = new Intent();
                intent.PutExtras(conData);
                SetResult(Result.Ok, intent);
                Finish();
            };
            info.Show(SupportFragmentManager, "fragment_edit_name");
        }

        private void lo_Click(object sender, EventArgs e)
        {
            //(Application as BootleggerApp).Comms.Logout();
            this.Finish();
            return;
        }

        public override bool OnCreateOptionsMenu(Android.Views.IMenu menu)
        {
   
            MenuInflater.Inflate(Resource.Layout.event_search_view, menu);
            
            IMenuItem searchItem = menu.FindItem(Resource.Id.action_search);
            var mm = MenuItemCompat.GetActionView(searchItem);
            var searchView = mm.JavaCast<SearchView>();
            //SearchView searchView = mm as SearchView;
            searchView.QueryRefinementEnabled = false;
            searchView.QueryTextChange += SearchView_QueryTextChange;
            searchView.QueryTextSubmit += SearchView_QueryTextSubmit;
            searchView.Close += SearchView_Close;

            if (!WhiteLabelConfig.EXTERNAL_LINKS)
            {
                IMenuItem helpitem = menu.FindItem(Resource.Id.action_help);
                helpitem.SetVisible(false);
            }

            return base.OnCreateOptionsMenu(menu);
        }

        private void SearchView_Close(object sender, SearchView.CloseEventArgs e)
        {
            mainlist.ClearFilter();
            sublist.ClearFilter();
        }

        private void SearchView_QueryTextSubmit(object sender, SearchView.QueryTextSubmitEventArgs e)
        {
            //hide keyboard??
            (sender as SearchView).ClearFocus();
        }

        private void SearchView_QueryTextChange(object sender, SearchView.QueryTextChangeEventArgs e)
        {
            //adjust the list:
            if (e.NewText.Length > 0)
            {
                mainlist.SetFilter(e.NewText);
                sublist.SetFilter(e.NewText);
            }
            else
            {
                mainlist.ClearFilter();
                sublist.ClearFilter();
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    if (_pager.CurrentItem == 1)
                        _pager.CurrentItem = 0;
                    else
                        Finish();

                    if (!cancel.IsCancellationRequested)
                        cancel.Cancel();
                    
                    return true;

                default:
                    if (item.ItemId == Resource.Id.action_help)
                    {
                        LoginFuncs.ShowHelp(this, "#events");
                    }
                    return true;
            }
        }
    }
}