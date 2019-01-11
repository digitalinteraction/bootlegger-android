/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System;
using Android.OS;
using Android.Views;
using Bootleg.API;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Widget;
using System.Collections.Generic;
using Square.Picasso;
using Android.Content;
using System.Threading;
using Bootleg.Droid.UI;
using Bootleg.Droid.Adapters;
using System.Linq;
using static Android.Support.V7.Widget.GridLayoutManager;
using Android.Support.Design.Widget;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class AllClipsFragment : Android.Support.V4.App.Fragment,IImagePausable
    {
		public AllClipsFragment()
		{
			
		}

        public enum ClipViewMode { INGEST, EDITING, LIST };

        public AllClipsFragment(ClipViewMode v)
        {
            ChooserMode = v;
        }

        Review reviewscreen;

        public AllClipsFragment(Review reviewscreen)
        {
            this.reviewscreen = reviewscreen;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        bool firstload = true;

        CancellationTokenSource cancel = new CancellationTokenSource();

        void RefreshOffline()
        {

            //var dat = new Dictionary<string, List<MediaItem>>() { { "all", Bootlegger.BootleggerClient.QueryMediaByTopic(tagfilter) } };
            var dat = Bootlegger.BootleggerClient.QueryMediaByTopic(tagfilter.Select((arg) => arg.id).ToList())
                       .GroupBy(n => n.Contributor)
                       .OrderBy(a => (a.Key == Bootlegger.BootleggerClient.CurrentUser.displayName) ? 1 : 2)
                       .ToDictionary(n => n.Key, n => n.ToList());

            //var dat = new Dictionary<string, List<MediaItem>>() { { "all",  } };
            listAdapter.UpdateData(dat);

            //listAdapter.UpdateData(dat);

            if (listAdapter.ItemCount == 0)
            {
                theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
            }
            else
            {
                theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;
            }

            theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Post(() =>
            {
                theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Gone;
            });
        }

        void RefreshOnline(bool data)
        {
            try
            {
                loading = true;
                if (data)
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Post(() =>
                    {
                        theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Visible;
                    });
                    cancel = new CancellationTokenSource();
                    Bootlegger.BootleggerClient.GetEveryonesMedia(cancel.Token);
                }


                //var prev = Resources.GetStringArray(Resource.Array.default_edit_topics).ToList();


                //if (string.IsNullOrEmpty(Bootlegger.BootleggerClient.CurrentEvent.topics))
                    //Bootlegger.BootleggerClient.CurrentEvent.topics = string.Join(",", prev);
                //else
                    //prev = Bootlegger.BootleggerClient.CurrentEvent.topics.Split(',').ToList();

                if (ChooserMode == ClipViewMode.INGEST)
                {
                    listAdapter.UpdateData(Bootlegger.BootleggerClient.QueryMedia(sortFilter, sortDirection));
                }
                else if (ChooserMode == ClipViewMode.EDITING)
                {
                    var dat = Bootlegger.BootleggerClient.QueryMediaByTopic(tagfilter.Select((arg) => arg.id).ToList())
                        .GroupBy(n => n.Contributor)
                        .OrderBy(a => (a.Key == Bootlegger.BootleggerClient.CurrentUser.displayName) ? 1 : 2)
                        .ToDictionary(n => n.Key, n => n.ToList());

                    //listAdapter.UpdateData(new Dictionary<string, List<MediaItem>>() { { "all", Bootlegger.BootleggerClient.QueryMediaByTopic(new List<string>()) } });
                    listAdapter.UpdateData(dat);

                }
                else
                {
                    listAdapter.UpdateData(new Dictionary<string, List<MediaItem>>() { { "all", Bootlegger.BootleggerClient.QueryMediaByTopic(new List<string>()) } });
                    //listAdapter.UpdateData(dat);
                }

                if (!data)
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Post(() =>
                    {
                        theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Gone;
                    });

                    if (listAdapter.ItemCount == ((ChooserMode == ClipViewMode.LIST) ? 1 : 0))
                    {
                        theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;
                    }
                }
            }
            catch(Exception e)
            {
                LoginFuncs.ShowError(Activity, e);

            }
            finally
            {
                if (!data)
                    loading = false;
            }
            //if  doing a hard-reload, dont hide progress
            
        }

        private void ListAdapter_OnChosen(MediaItem obj)
        {
            OnChosen?.Invoke(obj);
        }

        public override void OnStart()
        {
            base.OnStart();
            //Bootlegger.BootleggerClient.OnMoreMediaLoaded += Comms_OnMoreMediaLoaded;
            Bootlegger.BootleggerClient.OnMediaLoadingComplete += Comms_OnMediaLoadingComplete;

            if (firstload)
            {
                firstload = false;
                if (ChooserMode == ClipViewMode.INGEST || ChooserMode == ClipViewMode.LIST)
                    RefreshOnline(true);
                else
                    RefreshOffline();
            }
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            Picasso.With(Context).CancelTag(Context);
        }

        public override void OnResume()
        {
            base.OnResume();

            //RefreshOffline();
        }
        View theview;
        ClipAdapter listAdapter;

        public void UpdateEdit(List<MediaItem> ed)
        {
            listAdapter.UpdateEdit(ed);
        }

        private class MySpanSizeLookup : SpanSizeLookup
        {
            ClipAdapter adapter;
            int collumns = 1;

            public MySpanSizeLookup(ClipAdapter adapter, int collumns)
            {
                this.adapter = adapter;
                this.collumns = collumns;
            }

            public override int GetSpanSize(int position)
            {
                if (adapter.GetItemViewType(position) == (int)ClipAdapter.VIEW_TYPE_CONTENT)
                    return 1;
                else
                    return collumns;
            }
        }


        RecyclerView listView;

        MySpanSizeLookup spanLookup;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view;
            if (ChooserMode == ClipViewMode.INGEST)
                view = inflater.Inflate(Resource.Layout.clips_list_themes, container, false);
            else
                view = inflater.Inflate(Resource.Layout.clips_list, container, false);

            view.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refresh += MyEditsFragment_Refresh;

            var cols = Activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? 3 : 2;

            //var prev = Resources.GetStringArray(Resource.Array.default_edit_topics).ToList();
            //if (string.IsNullOrEmpty(Bootlegger.BootleggerClient.CurrentEvent.topics))
            //    Bootlegger.BootleggerClient.CurrentEvent.topics = string.Join(",", prev);
            //else
                //prev = Bootlegger.BootleggerClient.CurrentEvent.topics.Split(',').ToList();

            listAdapter = new ClipAdapter(Activity,  new Dictionary<string, List<MediaItem>>(), ChooserMode, Bootlegger.BootleggerClient.CurrentEvent.topics.ToList());

            listAdapter.OnPreview += _adatper_OnPreview;
            listAdapter.OnChosen += ListAdapter_OnChosen;

            spanLookup = new MySpanSizeLookup(listAdapter, cols);
            var mLayoutManager = new GridLayoutManager(Activity, cols);
            mLayoutManager.SetSpanSizeLookup(spanLookup);

            view.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;

            var listView = view.FindViewById<RecyclerView>(Resource.Id.allclips);

            listView.SetLayoutManager(mLayoutManager);
            listView.SetAdapter(listAdapter);
            listView.SetItemAnimator(null);

            listView = view.FindViewById<RecyclerView>(Resource.Id.allclips);
            listView.HasFixedSize = true;

            theview = view;

            //var options = Resources.GetStringArray(Resource.Array.default_edit_topics).ToList();
            //if (string.IsNullOrEmpty(Bootlegger.BootleggerClient.CurrentEvent.topics))
                //Bootlegger.BootleggerClient.CurrentEvent.topics = string.Join(",", options);
            //else
                //options = Bootlegger.BootleggerClient.CurrentEvent.topics.Split(',').ToList();


            if (ChooserMode == ClipViewMode.INGEST)
            {
                view.FindViewById<FloatingActionButton>(Resource.Id.continuebtn).Click += AllClipsFragment_Click;
            }
            else if (ChooserMode == ClipViewMode.EDITING)
            {
                //setup filters for topics:
               

                var rv = view.FindViewById<RecyclerView>(Resource.Id.list);
                rv.SetLayoutManager(new LinearLayoutManager(Activity, LinearLayoutManager.Horizontal, false));

                var chips = new ChipAdapter(Activity, false);
                chips.Update(Bootlegger.BootleggerClient.CurrentEvent.topics.ToList(), null);
                rv.SetAdapter(chips);
                chips.OnTopicFilterChanged += Chips_OnTopicFilterChanged;

                view.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.filtertoolbar).SetTitle(Resource.String.selectavideotoinsert);

                view.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.filtertoolbar).InflateMenu(Resource.Menu.selectclip);
                view.FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.filtertoolbar).MenuItemClick += AllClipsFragment_MenuItemClick;

            //FindViewById<ImageButton>(Resource.Id.ingestbtn).Click += IngestOpen;



                //     < ImageButton
                //android: layout_height = "48dp"
                //android: layout_width = "48dp"
                //android: padding = "4dp"
                //android: layout_margin = "2dp"
                //android: background = "?attr/selectableItemBackground"
                //android: id = "@+id/ingestbtn"
                //android: src = "@drawable/ic_tag_white_48dp"
                //android: scaleType = "fitCenter"
                //android: adjustViewBounds = "true" />

            }
            else
            {
                view.FindViewById(Resource.Id.filtertoolbar).Visibility = ViewStates.Gone;
            }

            listView.AddOnScrollListener(new PausableScrollListener(Context,listAdapter));

            return view;
        }

        private void AllClipsFragment_MenuItemClick(object sender, Android.Support.V7.Widget.Toolbar.MenuItemClickEventArgs e)
        {
            //open ingest page:
            OnOpenIngest?.Invoke();
        }

        public event Action OnOpenIngest;

        private void AllClipsFragment_Click2(object sender, EventArgs e)
        {
            IngestWizard.ShowWizard(Activity, true);
        }

        List<Topic> tagfilter = new List<Topic>();

        Bootlegger.MediaItemFilterDirection sortDirection;
        public Bootlegger.MediaItemFilterType sortFilter { get; private set; }

        private void Chips_OnTopicFilterChanged(List<Topic> obj)
        {
            //update filter:
            tagfilter = obj;
            RefreshOffline();
        }

        private void AllClipsFragment_Click1(object sender, EventArgs e)
        {
            OnBack?.Invoke();
        }

        private void AllClipsFragment_Click(object sender, EventArgs e)
        {
            OnNext?.Invoke();
        }

        public event Action OnBack;
        public event Action OnNext;

        public class FilterTuple<T1, T2>
        {
            public FilterTuple(T1 param1, T2 param2)
            {
                Item1 = param1;
                Item2 = param2;
            }

            public T1 Item1
            {
                get; set;
            }
            public T2 Item2
            {
                get; set;
            }

            public override string ToString()
            {
                return Item2.ToString();
            }
        }

        private void Comms_OnMediaLoadingComplete(int total)
        {
            loading = false;
            Activity.RunOnUiThread(() =>
            {
                if (ChooserMode == ClipViewMode.EDITING)
                {
                    //when selecting clips in the editor
                    var dat = Bootlegger.BootleggerClient.QueryMediaByTopic(tagfilter.Select((arg) => arg.id).ToList())
                        .GroupBy(n => n.Contributor)
                        .OrderBy(a => (a.Key == Bootlegger.BootleggerClient.CurrentUser.displayName) ? 1 : 2)
                        .ToDictionary(n => n.Key, n => n.ToList());

                    //var dat = new Dictionary<string, List<MediaItem>>() { { "all",  } };
                    listAdapter.UpdateData(dat);
                }
                else if (ChooserMode == ClipViewMode.INGEST)
                {
                    //when in the add tag screen:
                    var dat = Bootlegger.BootleggerClient.QueryMedia(sortFilter, sortDirection);
                    listAdapter.UpdateData(dat);
                }
                else
                {
                    //when viewing clips in the review tab
                    var dat = new Dictionary<string, List<MediaItem>>() { { "all", Bootlegger.BootleggerClient.QueryMediaByTopic(tagfilter.Select((arg) => arg.id).ToList()) } };
                    listAdapter?.UpdateData(dat);
                }

                View.FindViewById<View>(Resource.Id.progressBar).Visibility = ViewStates.Gone;

                if (listAdapter.ItemCount == ((ChooserMode == ClipViewMode.LIST) ? 1 : 0))
                {
                    theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
                }
                else
                {
                    theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;
                }
            });
        }

        public void UpdateSelectionOrder(Bootlegger.MediaItemFilterType mt, Bootlegger.MediaItemFilterDirection md)
        {
            sortDirection = md;
            sortFilter = mt;

            if (!loading)
                if (ChooserMode == ClipViewMode.INGEST)
                    RefreshOnline(false);
                else
                    RefreshOffline();

            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("ClipsListUpdateFilter",
                new KeyValuePair<string, string>("sort", md.ToString()),
                new KeyValuePair<string, string>("filter", mt.ToString()),
                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        

        //private void Comms_OnMoreMediaLoaded(List<MediaItem> moremedia)
        //{
        //    //if (reviewscreen != null)
        //    //{
        //    //Console.WriteLine("more media");
        //    if (ChooserMode == ClipViewMode.INGEST)
        //    {
        //        //listAdapter.UpdateData((Activity.Application as BootleggerApp).Comms.QueryMedia());
        //        Activity.RunOnUiThread(() =>
        //        {

        //            var dat = Bootlegger.BootleggerClient.QueryMedia(sortFilter, sortDirection);

        //            //if (listAdapter.ItemCount > 0)
        //            //reviewscreen.ChangeTab(1, "All Clips (" + (listAdapter.ItemCount - dat.Count) + ")");
        //        });
        //    }
        //}

        private void MyEditsFragment_Refresh(object sender, EventArgs e)
        {
            if (ChooserMode == ClipViewMode.INGEST || ChooserMode == ClipViewMode.LIST)
                RefreshOnline(true);
            else
                RefreshOffline();

            theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refreshing = false;
        }

        public override void OnPause()
        {
            base.OnPause();
            try
            {
                cancel.Cancel();
            }
            catch { }
            //Bootlegger.BootleggerClient.OnMoreMediaLoaded -= Comms_OnMoreMediaLoaded;
            Bootlegger.BootleggerClient.OnMediaLoadingComplete -= Comms_OnMediaLoadingComplete;
        }

        public event Action<MediaItem,View> OnPreview;
        public event Action<MediaItem> OnChosen;


        private void _adatper_OnPreview(MediaItem obj,View v)
        {
            //start preview:
            OnPreview?.Invoke(obj, v);
        }

        public void Pause()
        {
            Picasso picasso = Picasso.With(Context);
            picasso.PauseTag(Context);
        }

        public void Resume()
        {
            Picasso picasso = Picasso.With(Context);
            picasso.ResumeTag(Context);
        }

        private bool loading = true;
        public ClipViewMode ChooserMode { get; set; }
        //private Review review;

        public override void OnLowMemory()
        {
            base.OnLowMemory();
        }

        public void NotifyUpdate(MediaItem item)
        {
            //var tmp = ;
            //var index = 
            listAdapter.UpdateItem(listAdapter.IndexOf(item), item);

            //listAdapter.NotifyItemChanged(listAdapter.IndexOf(item));
        }

        internal void Refresh()
        {
            RefreshOnline(false);
        }
    }

    internal class ScrollListener : RecyclerView.OnScrollListener
    {
        Context context;
        public ScrollListener(Context context)
        {
            this.context = context;
        }

        public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
        {
            Picasso picasso = Picasso.With(context);

            if (newState ==  (int)ScrollState.Idle || newState == (int)ScrollState.TouchScroll)
            {
                picasso.ResumeTag(context);
            }
            else
            {
                picasso.PauseTag(context);
            }
        }
    }

}