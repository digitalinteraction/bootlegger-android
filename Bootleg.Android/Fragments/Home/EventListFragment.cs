/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;
using Bootleg.API;
using Android.Support.V4.Widget;
using System.Threading;
using System.Threading.Tasks;
using Square.Picasso;
using Bootleg.Droid.UI;
using Android.Content;
using static Android.Support.V7.Widget.GridLayoutManager;
using Android.App;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class EventListFragment : Android.Support.V4.App.Fragment,IImagePausable
    {
        public event Action<Shoot> OnConnect;
        public event Action<Shoot> OnSub;
        public event Action<Shoot> OnReview;
        public event Action OnStartNew;
        bool loadedalready = false;

        List<Shoot> events = new List<Shoot>();
        public EventListFragment()
        {
            events = new List<Shoot>();
        }

        public EventListFragment(EventAdapter.EventViewType ev)
        {
            events = new List<Shoot>();
            this.eventviewtype = ev;
        }

        public override void OnPause()
        {
            try
            {
                cancel.Cancel();
            }
            catch{
            }
            base.OnPause();
        }

        Shoot parent;
        //EventAdapter.EventViewType viewtype;

        public void LoadEvents(List<Shoot> events, Shoot parent, EventAdapter.EventViewType ev)
        {
            this.events = events;
            this.parent = parent;
            this.eventviewtype = ev;
            if (listAdapter == null)
                listAdapter = new EventAdapter(Activity);
            listAdapter.UpdateData(events,ev);
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        EventAdapter listAdapter;
        
        public void SetFilter(string q)
        {
            listAdapter.SetFilter(q);
        }

        public void ClearFilter()
        {
            listAdapter.FlushFilter();
        }

        CancellationTokenSource cancel = new CancellationTokenSource();

        public delegate Task EventUpdateDelegate(CancellationToken cancel);

        private EventUpdateDelegate updater;
        private string propertyname;
        private EventAdapter.EventViewType eventviewtype;
        private CancellationToken canceller;
        public void SetEvents(string propertyname,EventUpdateDelegate updater,CancellationToken cancel, EventAdapter.EventViewType viewtype)
        {
            this.updater = updater;
            this.propertyname = propertyname;
            this.eventviewtype = viewtype;
            this.canceller = cancel;
            this.eventviewtype = viewtype;
        }

        public async Task RefreshMe(bool manually)
        {
            try
            {
                if (!string.IsNullOrEmpty(propertyname))
                {
                    if (listAdapter == null)
                        listAdapter = new EventAdapter(Activity);

                    listAdapter.FlushFilter();
                    events = Bootlegger.BootleggerClient.GetType().GetProperty(propertyname).GetValue(Bootlegger.BootleggerClient) as List<Shoot>;
                    //events = data;

                    listAdapter.UpdateData(events, eventviewtype);

                    //run loader on initial set:
                    Loading = true;

                    if (manually)
                    {
                        if ((Context.ApplicationContext as BootleggerApp).IsReallyConnected)
                        {
                            try
                            {
                                await updater(canceller);
                            }
                            catch (TaskCanceledException)
                            {
                                //do nothing...
                            }
                            catch (Exception e)
                            {
                                OnError?.Invoke(e);
                            }
                        }
                        events = Bootlegger.BootleggerClient.GetType().GetProperty(propertyname).GetValue(Bootlegger.BootleggerClient) as List<Shoot>;
                        listAdapter.UpdateData(events, eventviewtype);

                    }
                    //listAdapter.UpdateData(events, eventviewtype);
                    Loading = false;

                    // Whitelabel - if there is only 1 event:
                    if (eventviewtype == EventAdapter.EventViewType.FEATURED && WhiteLabelConfig.ALLOW_SINGLE_SHOOT)
                    {
                        if (events.Count == 1 && Bootlegger.BootleggerClient.CurrentUser != null)
                        {
                            var diag = new Android.Support.V7.App.AlertDialog.Builder(Context)
                           .SetTitle(Resource.String.single_shoot_title)
                           .SetMessage(Resource.String.single_shoot_msg)
                           .SetPositiveButton(Resource.String.continuebtn, (o, e) =>
                           {
                               var ev = events[0];
                               OnConnect((string.IsNullOrEmpty(ev.group)) ? ev : ev.events[0]);
                           })
                           .SetNegativeButton(Android.Resource.String.Cancel, (o, e) =>
                           {

                           })
                           .Show();
                        }
                    }
                }
            }
            catch (Exception e)
            {
                if (Activity != null)
                {
                    //failed to refresh for some reason
                    OnError?.Invoke(e);
                }  
            }
        }

        public event Action<Exception> OnError;

        private bool Loading
        {
            set
            {
                if (value)
                {
                    try
                    {
                        try
                        {
                            //View.FindViewById<View>(Resource.Id.empty).Visibility = ViewStates.Gone;
                            //View.FindViewById<View>(Resource.Id.makenew).Visibility = ViewStates.Gone;
                            //View.FindViewById<View>(Resource.Id.swiperefresh).Visibility = ViewStates.Gone;
                        }
                        catch (Exception)
                        {

                        }
                        View.FindViewById<ProgressBar>(Resource.Id.loading).Visibility = ViewStates.Visible;
                    }
                    catch { }
                }
                else
                {
                    try {
                        View.FindViewById<ProgressBar>(Resource.Id.loading).Visibility = ViewStates.Gone;
                        //View.FindViewById<View>(Resource.Id.swiperefresh).Visibility = ViewStates.Visible;
                    }
                    catch
                    {

                    }
                }
            }
        }

        async void Events_Refresh(object sender, EventArgs e)
        {
            try
            {
                try
                {
                    await updater(canceller);
                }
                catch (TaskCanceledException)
                {
                    //do nothing...
                }
                catch (Exception ex)
                {
                    OnError?.Invoke(ex);
                }

                if (parent==null)
                {
                    LoadEvents(Bootlegger.BootleggerClient.GetType().GetProperty(propertyname).GetValue(Bootlegger.BootleggerClient) as List<Shoot>,null, eventviewtype);
                }
                else
                {
                    LoadEvents((Bootlegger.BootleggerClient.GetType().GetProperty(propertyname).GetValue(Bootlegger.BootleggerClient) as List<Shoot>).Find(ec=> ec.group == parent.group ).events, parent, eventviewtype);
                }
            }
            catch (Exception ex)
            {
                try {
                    LoginFuncs.ShowError(Activity, ex);
                }
                catch
                {
                    //failed if fragment destroyed
                }
            }
            finally
            {
                theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refreshing = false;
            }
        }

        View theview;

        RecyclerView listview;
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.EventList, container, false);

            listview = view.FindViewById<RecyclerView>(Resource.Id.eventsView);

            if (Context.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
            {
                listview.LayoutParameters.Height = LayoutParams.WrapContent; 
            }

            theview = view;

            listAdapter = new EventAdapter(container.Context);
            
            listAdapter.UpdateData(events, eventviewtype);
            listAdapter.Capture += ListAdapter_Capture;
            listAdapter.Edit += ListAdapter_Edit;
            listAdapter.Share += ListAdapter_Share;
            listAdapter.ShowMore += ListAdapter_ShowMore;
            listAdapter.MakeShoot += ListAdapter_MakeShoot;
            listAdapter.OnEnterCode += ListAdapter_OnEnterCode;
            listAdapter.HasStableIds = true;

            // use a linear layout manager
            if (eventviewtype == EventAdapter.EventViewType.FEATURED)
            {
                var lm = new LinearLayoutManager(Context, (int)Orientation.Horizontal, false);
                listview.SetLayoutManager(lm);
                view.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Enabled = false;
                listview.SetPadding(Utils.dp2px(Context, 30), 0, Utils.dp2px(Context, 30), 0);
                listview.NestedScrollingEnabled = true;
            }
            else
            {
                var mLayoutManager = new GridLayoutManager(container.Context, Activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? 2 : 1);
                mLayoutManager.SetSpanSizeLookup(new MySpanSizeLookup(Activity,listAdapter));
                listview.SetLayoutManager(mLayoutManager);
            }

            if (eventviewtype == EventAdapter.EventViewType.NEARBY_SINGLE)
            {
                //do nothing?
                var lm = new LinearLayoutManager(Context, (int)Orientation.Horizontal, false);
                listview.SetLayoutManager(lm);
                view.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Enabled = false;
            }

            listview.SetAdapter(listAdapter);

            listview.AddOnScrollListener(new PausableScrollListener(Context,listAdapter));
            view.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refresh += Events_Refresh;
            
            view.FindViewById<View>(Resource.Id.swiperefresh).Visibility = ViewStates.Visible;

            return view;
        }

        private void ListAdapter_OnEnterCode(string obj)
        {
            OnEnterCode?.Invoke(obj);
        }

        private class MySpanSizeLookup : SpanSizeLookup
        {
            EventAdapter adapter;
            Activity activity;

            public MySpanSizeLookup(Activity activity, EventAdapter adapter)
            {
                this.adapter = adapter;
                this.activity = activity;
            }

            public override int GetSpanSize(int position)
            {
                //if (adapter)
                //return activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? 1 : 2;

                if (adapter.GetItemViewType(position) == (int)EventAdapter.TileType.EVENT)
                    return 1;
                else
                    return activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? 2 : 1;
                    //return ((adapter.GetItemViewType(position) == (int)EventAdapter.TileType.ENTERCODE) || (adapter.GetItemViewType(position) == (int)EventAdapter.TileType.EMPTY) || (adapter.GetItemViewType(position) == (int)EventAdapter.TileType.NEWSHOOT)) ? 2 : 1;
            }
        }

        private void ListAdapter_Share(Shoot obj)
        {
            Intent sharingIntent = new Intent(Intent.ActionSend);
            sharingIntent.SetType("text/plain");
            sharingIntent.PutExtra(Intent.ExtraSubject, obj.name);
            sharingIntent.PutExtra(Intent.ExtraText, Bootlegger.BootleggerClient.server + "/s/" + obj.offlinecode);
            //StartActivity(Intent.CreateChooser(sharingIntent, Resources.GetString(Resource.String.sharevia)));
        }

        private void ListAdapter_MakeShoot()
        {
            OnStartNew?.Invoke();
        }

        public override void OnStart()
        {
            base.OnStart();
           
        }

        public event Action OnShowMore;
        public event Action<string> OnEnterCode;

        private void ListAdapter_ShowMore()
        {
            OnShowMore?.Invoke();
        }

        private void ListAdapter_Edit(object sender, Shoot e)
        {
            OnReview?.Invoke(e);
        }

        public async override void OnResume()
        {
            base.OnResume();

            if (!loadedalready)
            {
                await RefreshMe(false);
                loadedalready = true;
                await RefreshMe(true);
            }
            else
            {
                await RefreshMe(false);
            }
        }

        private void ListAdapter_Capture(object sender, Shoot e)
        {

            if (e.group != null)
            {
                OnSub?.Invoke(e);
            }
            else
            {
                OnConnect?.Invoke(e);
            }
        }

        public void Pause()
        {
            Picasso.With(Context).PauseTag(listAdapter);
        }

        public void Resume()
        {
            Picasso.With(Context).ResumeTag(listAdapter);
        }
    }
}