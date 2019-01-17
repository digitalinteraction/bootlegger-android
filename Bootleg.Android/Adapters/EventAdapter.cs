/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Android.Support.V7.Widget;
using Square.Picasso;
using Android.Content;
using Bootleg.Droid.UI;
using Android.Graphics;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class EventAdapter : RecyclerView.Adapter
    {

        public enum TileType : int { NEWSHOOT = 0x01, ENTERCODE = 0x02, EVENT = 0x00, MORESHOOTS = 0x03, EMPTY = 0x04, MYEVENTSTITLE=0x05, EVENT_FEATURED=0x06 };

        public class HeaderEventItem
        {
            public Shoot Event { get; set; }
            internal TileType Tiletype { get; set; }

        }

        public enum EventViewType { MYEVENT, FEATURED_LIST, FEATURED, LIST, NEARBY, NEARBY_SINGLE };
        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            EventViewType buttons;
            Action<Shoot> capture;
            Action<Shoot> edit;
            Action<Shoot> share;
            Action<string> entercode;
            Action showmore;
            EventAdapter adpt;
            Shoot currentevent;
            public ViewHolder(View itemView, Action<Shoot> capture, Action<Shoot> edit, Action showmore, Action<Shoot> share, Action<string> entercode, EventViewType buttons, EventAdapter adpt, int tiletype) : base(itemView)
            {

                this.capture = capture;
                this.edit = edit;
                view = itemView;
                this.buttons = buttons;
                this.showmore = showmore;
                this.adpt = adpt;
                this.share = share;
                this.entercode = entercode;

                if (tiletype == (int)TileType.MYEVENTSTITLE)
                {
                    //do nothing -- its the title
                }
                else
                {

                    if (buttons == EventViewType.MYEVENT)
                    {
                        view.Click += (sender, e) =>
                        {
                            if (currentevent != null)
                                edit(currentevent);
                        };

                        //view.FindViewById<ImageButton>(Resource.Id.capturebtn).Click += (sender, e) =>
                        //{
                        //    if (currentevent != null)
                        //        capture(currentevent);
                        //};
                        //view.FindViewById<ImageButton>(Resource.Id.editbtn).Click += (sender, e) =>
                        //{
                        //    if (currentevent != null)
                        //        edit(currentevent);
                        //};


                    }
                    else
                    {
                        view.Click += View_Click;

                    }

                    if (view.FindViewById<ImageButton>(Resource.Id.sharebtn) != null)
                    {
                        if (WhiteLabelConfig.EXTERNAL_LINKS)
                        {
                            view.FindViewById<ImageButton>(Resource.Id.sharebtn).Click += ViewHolder_Click;
                        }
                        else
                        {
                            view.FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Gone;
                        }
                    }

                    if (view.FindViewById<EditText>(Resource.Id.code) != null)
                    {
                        view.FindViewById<EditText>(Resource.Id.code).TextChanged += ViewHolder_TextChanged;

                    }
                }
            }

            private void ViewHolder_TextChanged(object sender, Android.Text.TextChangedEventArgs e)
            {
                if (e.Text.Count() == 4)
                {
                    entercode?.Invoke(e.Text.ToString());
                    view.FindViewById<EditText>(Resource.Id.code).Text = "";
                }

            }

            private void ViewHolder_Click(object sender, EventArgs e)
            {
                //share:
                share?.Invoke(currentevent);
            }

            private void View_Click(object sender, EventArgs e)
            {
                if (currentevent != null)
                    capture(currentevent);
                else
                    showmore?.Invoke();
            }

            internal void SetItem(HeaderEventItem item)
            {
                currentevent = item.Event;
                if (item.Tiletype == TileType.EVENT || item.Tiletype == TileType.EVENT_FEATURED)
                {

                    if (view.FindViewById<TextView>(Resource.Id.joincode) != null)
                    { 
                        if (string.IsNullOrEmpty(item.Event.joincode) || WhiteLabelConfig.LOCAL_SERVER || item.Event.ispublic)
                        {
                            view.FindViewById<TextView>(Resource.Id.joincode).Visibility = ViewStates.Gone;
                        }
                        else
                        {
                            view.FindViewById<TextView>(Resource.Id.joincode).Visibility = ViewStates.Visible;
                            view.FindViewById<TextView>(Resource.Id.joincode).Text = item.Event.joincode;
                        }
                    }

                    if (view.FindViewById<ImageButton>(Resource.Id.sharebtn) != null)
                    {
                        if (currentevent.ispublic && WhiteLabelConfig.EXTERNAL_LINKS)
                            view.FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Visible;
                        else
                            view.FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Gone;
                    }

                    //view.FindViewById<ImageView>(Resource.Id.event_icon).SetImageResource(Resource.Drawable.ic_event_white_48dp);


                    string msg = "";

                    if (buttons == EventViewType.MYEVENT)
                    {
                        if (currentevent.myclips > 0)
                        {
                            view.FindViewById<TextView>(Resource.Id.clipcount).Text = Java.Lang.String.Format("%d", currentevent.myclips);
                            view.FindViewById<TextView>(Resource.Id.clipcount).Visibility = ViewStates.Visible;
                            view.FindViewById<ImageView>(Resource.Id.myclipsimg).Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            view.FindViewById<TextView>(Resource.Id.clipcount).Visibility = ViewStates.Invisible;
                            view.FindViewById<ImageView>(Resource.Id.myclipsimg).Visibility = ViewStates.Invisible;
                        }

                        var ups = Bootlegger.BootleggerClient.GetNumUploadsForShoot(currentevent);
                        if (ups > 0)
                        {
                            msg = Java.Lang.String.Format("%d", ups);
                        }

                        if (msg != "")
                        {
                            view.FindViewById<TextView>(Resource.Id.uploadcount).Visibility = ViewStates.Visible;
                            view.FindViewById<TextView>(Resource.Id.uploadcount).Text = msg;
                            view.FindViewById<View>(Resource.Id.uploadimg).Visibility = ViewStates.Visible;
                        }
                        else
                        {
                            view.FindViewById<TextView>(Resource.Id.uploadcount).Visibility = ViewStates.Gone;
                            view.FindViewById<View>(Resource.Id.uploadimg).Visibility = ViewStates.Gone;

                        }

                        var imgs = Bootlegger.BootleggerClient.GetSampleMediaForEvent(currentevent).ToList().OrderBy(x => x.CreatedAt);

                        if (imgs.Count() > 0)
                            Picasso.With(view.Context).Load(imgs.First().Thumb + "?s=" + WhiteLabelConfig.THUMBNAIL_SIZE).CenterCrop().Fit().Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                        else
                        {
                            if (!string.IsNullOrEmpty(currentevent.iconbackground))
                                //Picasso.With(view.Context).Load(Resource.Drawable.event_back).CenterCrop().Fit().Tag(adpt).Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                            //else
                                Picasso.With(view.Context).Load(currentevent.iconbackground).CenterCrop().Fit().Tag(adpt).Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                                //Picasso.With(view.Context).Load("https://ifrc.bootlegger.tv/backgrounds/ifrc.jpg").CenterCrop().Fit().Tag(adpt).Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                        }
                    }

                    if (!string.IsNullOrEmpty(currentevent.group) && buttons != EventViewType.MYEVENT)
                    {
                        view.FindViewById<TextView>(Resource.Id.organiser).Text = adpt.context.Resources.GetQuantityString(Resource.Plurals.shootcount, currentevent.events.Count, currentevent.events.Count);
                        view.FindViewById<TextView>(Resource.Id.firstLine).Text = currentevent.group;
                        //view.FindViewById<TextView>(Resource.Id.secondLine).Text = adpt.context.Resources.GetQuantityString(Resource.Plurals.shootcount, currentevent.events.Count, currentevent.events.Count);
                        //view.FindViewById<TextView>(Resource.Id.secondLine).Visibility = ViewStates.Visible;
                        Picasso.With(view.Context).Load(currentevent.icon).CenterCrop().Fit().Tag(adpt).Config(Bitmap.Config.Argb4444).Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                        Picasso.With(view.Context).Load(Resource.Drawable.ic_video_library_black_24dp).Fit().Tag(adpt).Transform(new CircleTransform()).Into(view.FindViewById<ImageView>(Resource.Id.organisericon));

                    }
                    else
                    {
                        if (buttons != EventViewType.MYEVENT)
                        {

                            if (!string.IsNullOrEmpty(currentevent.iconbackground))
                                //Picasso.With(view.Context).Load(Resource.Drawable.event_back).CenterCrop().Fit().Tag(adpt).Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                            //else
                                Picasso.With(view.Context).Load(currentevent.iconbackground).CenterCrop().Fit().Tag(adpt).Into(view.FindViewById<ImageView>(Resource.Id.event_background));
                        }

                        view.FindViewById<TextView>(Resource.Id.organiser).Text = currentevent.organisedby;  
                        view.FindViewById<TextView>(Resource.Id.firstLine).Text = currentevent.name;
                    }

                    //if (!string.IsNullOrEmpty(currentevent.icon) && view.FindViewById<ImageView>(Resource.Id.event_icon) != null)
                    //{
                    //    Picasso.With(view.Context).Load(currentevent.icon).Fit().Tag(adpt).Config(Bitmap.Config.Argb4444).Transform(new CircleTransform()).Into(view.FindViewById<ImageView>(Resource.Id.event_icon));
                    //}

                    Picasso.With(view.Context).Load(currentevent.organiserprofile).Fit().Tag(adpt).Transform(new CircleTransform()).Into(view.FindViewById<ImageView>(Resource.Id.organisericon));
                }
                else
                {

                }
            }
        }

        List<HeaderEventItem> allitems;
        public List<HeaderEventItem> Visibleitems {
            get; set; }
        Context context;
        public event EventHandler<Shoot> Capture;
        public event EventHandler<Shoot> Edit;
        public event Action GoToUploads;

        public EventViewType ViewType { get; private set; }

        public void UpdateData(List<Shoot> data, EventViewType viewtype)
        {
            ViewType = viewtype;
            allitems = new List<HeaderEventItem>();
            allitems.Clear();

            //if (viewtype == EventViewType.FEATURED && WhiteLabelConfig.ALLOW_CODE_JOIN)
            //    allitems.Add(new HeaderEventItem() { Tiletype = TileType.ENTERCODE });

            if (viewtype == EventViewType.MYEVENT)
                allitems.Add(new HeaderEventItem() { Tiletype = TileType.MYEVENTSTITLE });
            

            //if ((context.ApplicationContext as BootleggerApp).Comms.CurrentUser!=null && (viewtype == EventViewType.MYEVENT) && WhiteLabelConfig.ALLOW_CREATE_OWN)
            //    allitems.Add(new HeaderEventItem() { Tiletype = TileType.NEWSHOOT });

            if (data?.Count==0)
                allitems.Add(new HeaderEventItem() { Tiletype = TileType.EMPTY });

            if (viewtype == EventViewType.FEATURED)
                data = data.Take(3).ToList();


            if (viewtype == EventViewType.FEATURED)
            {
                if (data != null)
                    foreach (var r in data)
                        allitems.Add(new HeaderEventItem() { Event = r, Tiletype = TileType.EVENT_FEATURED });
            }
            else
            {
                if (data != null)
                    foreach (var r in data)
                        allitems.Add(new HeaderEventItem() { Event = r });
            }


            Visibleitems = allitems;
            NotifyDataSetChanged();
        }

        public EventAdapter():base()
        {
            Visibleitems = new List<HeaderEventItem>();
        }

        public override long GetItemId(int position)
        {
            return allitems[position].Event?.id?.GetHashCode() ?? 99;
        }


        public EventAdapter(Context context)
                : base()
        {
            this.context = context;
            Visibleitems = new List<HeaderEventItem>();
        }

        public void FlushFilter()
        {
            Visibleitems = new List<HeaderEventItem>();
            Visibleitems.AddRange(allitems);
            NotifyDataSetChanged();
        }

        public void SetFilter(string queryText)
        {

            Visibleitems  = new List<HeaderEventItem>();

            var results = from ev in allitems where 
                          ev.Tiletype == TileType.EVENT &&
                          (
                          (ev.Event.name!=null && (ev.Event.name.ToLower().Contains(queryText.ToLower()) || ev.Event.description.ToLower().Contains(queryText.ToLower()))) 
                          || (ev.Event.@group!=null && ev.Event.@group.ToLower().Contains(queryText.ToLower()) || (ev.Event.@group != null && EventContains(queryText.ToLower(),ev.Event.events)))) select ev;

            Visibleitems.AddRange(results.ToList());
            NotifyDataSetChanged();
        }

        private static bool EventContains(string queryText, IEnumerable<Shoot> events)
        {
            return (from ev in events where (ev.name != null && (ev.name.ToLower().Contains(queryText.ToLower()) || ev.description.ToLower().Contains(queryText.ToLower()))) select ev).ToList().Count > 0;
        }

        public override int ItemCount
        {
            get
            {
                return Visibleitems.Count();
            }
        }

        void OnCapture(Shoot position)
        {
            Capture?.Invoke(this, position);
        }

        void OnEdit(Shoot position)
        {
            Edit?.Invoke(this, position);
        }

        public event Action ShowMore;
        public event Action MakeShoot;
        public event Action<Shoot> Share;
        public event Action<string> OnEnterCode;

        void OnShowMore()
        {
            ShowMore?.Invoke();
        }

        void OnMakeShoot()
        {
            MakeShoot?.Invoke();
        }

        void OnShowUploads()
        {
            GoToUploads?.Invoke();
        }

        void OnShare(Shoot e)
        {
            Share(e);
        }


        public override int GetItemViewType(int position)
        {
            return (int)allitems[position].Tiletype;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = Visibleitems[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView;
            ViewHolder vh;

            switch (viewType)
            {
                //case (int)TileType.NEWSHOOT:
                    //itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.makeshoot, parent, false);
                    //vh = new ViewHolder(itemView, null, null, OnMakeShoot, null,null, ViewType, this, viewType);
                    //return vh;

                case (int)TileType.EMPTY:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.emptyshoots, parent, false);
                    vh = new ViewHolder(itemView, null, null, null, null, null, ViewType, this, viewType);
                    return vh;

                case (int)TileType.MORESHOOTS:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.moreshoots, parent, false);
                    vh = new ViewHolder(itemView, null, null, OnShowMore, null, null, ViewType, this, viewType);
                    return vh;

                case (int)TileType.ENTERCODE:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.enter_code, parent, false);
                    vh = new ViewHolder(itemView, null, null, null, null, OnEnterCode, ViewType, this, viewType);
                    return vh;

                case (int)TileType.MYEVENTSTITLE:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.myeventstitle, parent, false);
                    vh = new ViewHolder(itemView, null, null, null, null, null, ViewType, this, viewType);
                    return vh;
                
                case (int)TileType.EVENT:
                case (int)TileType.EVENT_FEATURED:
                default:

                    if (ViewType == EventViewType.MYEVENT)
                    {
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.eventitem_small, parent, false);
                        vh = new ViewHolder(itemView, OnCapture, OnEdit, null, OnShare,null, ViewType, this, viewType);
                        return vh;
                    }
                    else
                    {
                        //make less than 100% width when displaying as featured...
                        itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.eventitem, parent, false);

                        if (viewType == (int)TileType.EVENT_FEATURED)
                        {
                            if (context.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape)
                            {
                                itemView.LayoutParameters.Width = (parent.Width - Utils.dp2px(context, (34 * 2))) / 2;
                                itemView.LayoutParameters = new FrameLayout.LayoutParams(itemView.LayoutParameters) { MarginStart = Utils.dp2px(context, 4), MarginEnd = Utils.dp2px(context, 4), TopMargin = Utils.dp2px(context, 4) };
                            }
                            else
                            {
                                itemView.LayoutParameters.Width = parent.Width - Utils.dp2px(context, (34 * 2));
                                itemView.LayoutParameters = new FrameLayout.LayoutParams(itemView.LayoutParameters) { MarginStart = Utils.dp2px(context, 4), MarginEnd = Utils.dp2px(context, 4), TopMargin = Utils.dp2px(context, 4) };
                            }
                        }
                        vh = new ViewHolder(itemView, OnCapture, OnEdit, null, OnShare,null, ViewType, this, viewType);
                        return vh;
                    }
            }
        }
    }
}