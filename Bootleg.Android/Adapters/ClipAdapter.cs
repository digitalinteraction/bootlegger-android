/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Android.Support.V7.Widget;
using Square.Picasso;
//using Com.Tonicartos.Superslim;
//using System.Globalization;
using static Bootleg.Droid.AllClipsFragment;
using Bootleg.Droid.Adapters;
using Android.Graphics;
using static Bootleg.API.Bootlegger;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class ClipAdapter : RecyclerView.Adapter
    {
        public static int VIEW_TYPE_HEADER = 0x01;

        public static int VIEW_TYPE_CONTENT = 0x00;

        public static int VIEW_TYPE_LIST_HEADER = 0x02;


        //private static int LINEAR = 0;

        public class HeaderMediaItem : IEquatable<HeaderMediaItem>
        {
            public MediaItem MediaItem { get; set; }
            public bool IsHeader { get; set; }
            public string HeaderText { get; set; }
            public int SectionFirstPosition { get; set; }
            public int SectionManager { get; set; }
            public string SubText { get; set; }

            public bool Equals(HeaderMediaItem other)
            {
                return MediaItem == other.MediaItem;
            }
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            ClipAdapter adpt;
            public event Action<MediaItem, View> OnPreview;
            public event Action<MediaItem> OnChosen;
            ChipAdapter chips;
            List<Topic> options;

            public ViewHolder(View itemView, ClipAdapter adpt, List<Topic> options) : base(itemView)
            {
                this.options = options;
                view = itemView;
                this.adpt = adpt;
                //itemView.Click += ItemView_Click;
                if (itemView.FindViewById<ImageButton>(Resource.Id.choosebtn) != null)
                    itemView.FindViewById<ImageButton>(Resource.Id.choosebtn).Click += ViewHolder_Click;

                if (view.FindViewById<ImageView>(Resource.Id.image) != null)
                    view.FindViewById<ImageView>(Resource.Id.image).Click += ItemView_Click;

                if (itemView.FindViewById<RecyclerView>(Resource.Id.list)!=null)
                {
                    var rv = itemView.FindViewById<RecyclerView>(Resource.Id.list);
                    rv.SetLayoutManager(new LinearLayoutManager(adpt.context, LinearLayoutManager.Horizontal, false));
                    chips = new ChipAdapter(adpt.context, true);
                    rv.SetAdapter(chips);
                }
            }

            private void ViewHolder_Click(object sender, EventArgs e)
            {
                if (media != null)
                    OnChosen?.Invoke(media);
            }

            private void ItemView_Click(object sender, EventArgs e)
            {
                if (media != null)
                    OnPreview?.Invoke(media, view);
            }

            MediaItem media;

            internal void SetItem(HeaderMediaItem item)
            {
                if (item.IsHeader)
                {
                    media = null;
                    view.FindViewById<TextView>(Resource.Id.header).Text = (item.HeaderText ==  BootleggerClient.CurrentUser.displayName)?view.Context.GetString(Resource.String.me): item.HeaderText;
                    if (view.FindViewById<TextView>(Resource.Id.subheader)!=null)
                        view.FindViewById<TextView>(Resource.Id.subheader).Text = item.SubText;
                }
                else
                {
                    media = item.MediaItem;
                    Picasso.With(adpt.context).
                    Load(item.MediaItem.Thumb + "?s=" + WhiteLabelConfig.THUMBNAIL_SIZE).
                    Tag(adpt).
                    Config(Android.Graphics.Bitmap.Config.Rgb565).
                    Fit().
                    CenterCrop().
                    Into(view.FindViewById<ImageView>(Resource.Id.image));
                    if (chips != null)
                    {
                        chips.Update(null, item.MediaItem);
                        chips.NotifyDataSetChanged();
                    }

                    view.FindViewById<TextView>(Resource.Id.cliplength).Text = item.MediaItem.ClipLength.ToString(@"mm\:ss");

                    if (adpt.chooser_mode == ClipViewMode.EDITING)
                    {
                        try
                        {
                            //if (item.MediaItem.CreatedAt!=null)
                            //    view.FindViewById<TextView>(Resource.Id.title).Text = item.MediaItem.CreatedAt.ToString("hhtt ddd dd MMM yy");

                            view.FindViewById<TextView>(Resource.Id.title).Text = item.MediaItem.CreatedAt.LocalizeTimeDiff(); //("ha E d MMM yy");
                        }
                        catch
                        {
                            view.FindViewById<TextView>(Resource.Id.title).Text = item.MediaItem.Static_Meta["captured_at"].ToString();
                        }

                        view.FindViewById<TextView>(Resource.Id.date).Text = (item.MediaItem.created_by == Bootlegger.BootleggerClient.CurrentUser.id) ? view.Context.GetString(Resource.String.me) : item.MediaItem.Contributor;

                        view.FindViewById<TextView>(Resource.Id.date).Visibility = ViewStates.Gone;
                        view.FindViewById<ImageButton>(Resource.Id.choosebtn).Visibility = ViewStates.Visible;

                        //if it is in the timeline:
                        if (adpt.IsInEdit(item.MediaItem))
                            view.FindViewById(Resource.Id.usedtick).Visibility = ViewStates.Visible;
                        else
                            view.FindViewById(Resource.Id.usedtick).Visibility = ViewStates.Gone;

                        

                        try
                        {
                            var mtopics = item.MediaItem.Static_Meta[$"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}"].TrimStart(',').Split(',');
                            if (mtopics.Length > 0)
                            {
                                //var index = topics.IndexOf(mtopics.First());
                                //var index = Array.IndexOf(BootleggerClient.CurrentEvent.topics.Split(','), mtopics.First());
                                //view.FindViewById<View>(Resource.Id.colorbar).SetBackgroundColor(SliverEditAdapter.GetColorForIndex(view.Context,index));


                                var index = BootleggerClient.CurrentEvent.topics.Find((t) => t.id == mtopics.First());
                                view.FindViewById<View>(Resource.Id.colorbar).SetBackgroundColor(Color.ParseColor(index.color));

                            }
                            else
                            {
                                view.FindViewById<View>(Resource.Id.colorbar).SetBackgroundColor(Color.Silver);
                            }
                        }
                        catch
                        {
                            view.FindViewById<View>(Resource.Id.colorbar).SetBackgroundColor(Color.Silver);
                        }

                    }

                    //show chips:


                }
            }
        }

        public void UpdateEdit(List<MediaItem> ed)
        {
            this.CurrentEdit = ed;
            NotifyDataSetChanged();
        }

        List<MediaItem> CurrentEdit;

        private bool IsInEdit(MediaItem item)
        {
            if (CurrentEdit != null)
            {
                //check if this item (id) is in the edit:
                return CurrentEdit.Select((n) => n.id).Contains(item.id);
                //return CurrentEdit.Contains(item);
            }
            else
                return false;
        }

        List<HeaderMediaItem> allitems;

        Activity context;

        internal void UpdateData(Dictionary<string, List<MediaItem>> items, List<MediaItem> changed = null)
        {
            allitems = null;
            allitems = new List<HeaderMediaItem>();
            
            int headerCount = 0;
            int sectionFirstPosition = 0;

            if (chooser_mode == ClipViewMode.LIST)
            {
                allitems.Add(new HeaderMediaItem() { HeaderText = context.GetString(Resource.String.mytags), IsHeader = true });
            }

            int i = 0;
            foreach (var keyval in items)
            {
                if (keyval.Value.Count > 0)
                {
                    sectionFirstPosition = i + headerCount;
                    headerCount++;
                    if (items.Count > 1)
                        allitems.Add(new HeaderMediaItem() { HeaderText = keyval.Key, IsHeader = true, SectionFirstPosition = sectionFirstPosition, SubText = Java.Lang.String.Format("%d", keyval.Value.Count()) });

                    foreach (var m in keyval.Value)
                    {
                        //Console.WriteLine("lowres-out: " + m.lowres);
                        allitems.Add(new HeaderMediaItem() { MediaItem = m, SectionFirstPosition = sectionFirstPosition });
                        i++;
                    }
                }
            }

            //notify all the headers:
            if (changed == null)
            {
                NotifyDataSetChanged();
            }
            else
            {
                foreach (var s in changed)
                {
                    NotifyItemInserted(allitems.IndexOf(new HeaderMediaItem() { MediaItem = s }));
                }
            }
        }

        ClipViewMode chooser_mode;

        public ClipAdapter(Activity context, Dictionary<string, List<MediaItem>> items, ClipViewMode chooser_mode, List<Topic> topics)
                : base()
        {
            this.context = context;
            this.options = topics;
            allitems = new List<HeaderMediaItem>();
            UpdateData(items);
            this.chooser_mode = chooser_mode;
        }

        public ClipAdapter() : base()
        {
        }

        public override int ItemCount
        {
            get
            {
                return allitems.Count();
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = allitems[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
        }

        public event Action<MediaItem, View> OnPreview;
        public event Action<MediaItem> OnChosen;

        List<Topic> options;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView;
            if (viewType == VIEW_TYPE_CONTENT)
            {
                if (chooser_mode == ClipViewMode.INGEST || chooser_mode == ClipViewMode.LIST)
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.clipitem, parent, false);
                else
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.clipitem_editing, parent, false);
            }
            else
            {
                if (viewType == VIEW_TYPE_LIST_HEADER)
                {
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.myeditstitle, parent, false);
                    itemView.SetPadding(Utils.dp2px(context,36), Utils.dp2px(context,36), Utils.dp2px(context,36), Utils.dp2px(context,36));
                }
                else
                {
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.videoheaderfixed, parent, false);
                }
            }
            ViewHolder vh = new ViewHolder(itemView, this, options);
            vh.OnPreview += Vh_OnPreview;
            vh.OnChosen += Vh_OnChosen;



            return vh;
        }

        private void Vh_OnChosen(MediaItem obj)
        {
            OnChosen?.Invoke(obj);
        }

        private void Vh_OnPreview(MediaItem obj, View v)
        {
            OnPreview?.Invoke(obj, v);
        }

        public override int GetItemViewType(int position)
        {
            if (chooser_mode == ClipViewMode.LIST)
            {
                return allitems[position].IsHeader ? VIEW_TYPE_LIST_HEADER : VIEW_TYPE_CONTENT;
            }
            else
            {
                return allitems[position].IsHeader ? VIEW_TYPE_HEADER : VIEW_TYPE_CONTENT;
            }
        }

        internal int IndexOf(MediaItem item)
        {
            return allitems.IndexOf(allitems.Find(n => n.MediaItem == item));
        }

        internal void UpdateItem(int v, MediaItem item)
        {
            var tmp = Bootlegger.BootleggerClient.GetMediaItem(item.id);
            allitems[v].MediaItem = tmp;
            NotifyItemChanged(v);
        }
    }
}