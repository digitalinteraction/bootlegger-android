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
using Android.Support.V7.Widget;
using Square.Picasso;
using Android.Graphics;
using static Bootleg.API.Bootlegger;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class SingleEditAdapter : RecyclerView.Adapter
    {
        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            SingleEditAdapter adpt;
            public MediaItem currentitem;
            public event Action<MediaItem, Shot.ShotTypes> OnChange;
            public event Action<MediaItem> OnPreview;
            public event Action<MediaItem> OnTrim;
            public event Action<MediaItem> OnDelete;

            public ViewHolder(View itemView, SingleEditAdapter adpt) : base(itemView)
            {
                view = itemView;
                this.adpt = adpt;
                view.FindViewById<ImageButton>(Resource.Id.editbtn).Click += ReplaceClick;
                view.FindViewById<ImageButton>(Resource.Id.trimbtn).Click += TrimClick;
                view.FindViewById<CardView>(Resource.Id.cardview).Click += View_Click;
                view.FindViewById<ImageButton>(Resource.Id.imageadd).Click += ImageAdd;
                view.FindViewById<ImageButton>(Resource.Id.titleadd).Click += TitleAdd;
                view.FindViewById<ImageButton>(Resource.Id.deletebtn).Click += ViewHolder_Click;

            }

            private void ViewHolder_Click(object sender, EventArgs e)
            {
                if (!adpt.trimmode)
                    OnDelete?.Invoke(currentitem);
            }

            private void TitleAdd(object sender, EventArgs e)
            {
                //add title
                OnChange?.Invoke(currentitem, Shot.ShotTypes.TITLE);
            }

            private void ImageAdd(object sender, EventArgs e)
            {
                //add media
                OnChange?.Invoke(currentitem, Shot.ShotTypes.VIDEO);
            }

            private void TrimClick(object sender, EventArgs e)
            {
                //trim
                OnTrim?.Invoke(currentitem);
            }

            private void ReplaceClick(object sender, EventArgs e)
            {
                //change
                OnChange?.Invoke(currentitem,currentitem.MediaType == Shot.ShotTypes.TITLE ? Shot.ShotTypes.TITLE : Shot.ShotTypes.VIDEO);
            }

            internal void SetItem(MediaItem item)
            {
                currentitem = item;

                
                view.FindViewById<View>(Resource.Id.touchstart).Visibility = ViewStates.Gone;

                if (item.Status != MediaItem.MediaStatus.PLACEHOLDER)
                {
                    view.FindViewById<TextView>(Resource.Id.outpoint).Text = adpt.Countuptocurrent(AdapterPosition).ToString(@"mm\:ss");
                    view.FindViewById<TextView>(Resource.Id.outpoint).Visibility = ViewStates.Visible;
                    view.FindViewById(Resource.Id.placeholderbuttons).Visibility = ViewStates.Gone;
                    view.FindViewById<ImageButton>(Resource.Id.editbtn).Visibility = ViewStates.Visible;
                    view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Visible;

                    //get index of the label for the media:
                    var topics = BootleggerClient.CurrentEvent.topics;

                    if (string.IsNullOrEmpty(item.titletext))
                    {
                        view.FindViewById<ImageButton>(Resource.Id.editbtn).SetImageResource(Resource.Drawable.ic_video_library_black_24dp);
                    }
                    else
                    {
                        view.FindViewById<ImageButton>(Resource.Id.editbtn).SetImageResource(Resource.Drawable.baseline_text_fields_black_24);
                    }

                    var realmedia = BootleggerClient.GetMediaItem(item.id);
                    try
                    {
                        var mtopics = realmedia.Static_Meta[$"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}"].TrimStart(',').Split(',');
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

                    view.FindViewById<View>(Resource.Id.colorbar).Visibility = ViewStates.Visible;

                    //set playing flag:
                    if (adpt.currentplaying == currentitem)
                    {
                        view.FindViewById(Resource.Id.playindicator).Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        view.FindViewById(Resource.Id.playindicator).Visibility = ViewStates.Invisible;
                    }

                    if (item.MediaType == Shot.ShotTypes.TITLE)
                    {
                        view.FindViewById<ImageView>(Resource.Id.image).Visibility = ViewStates.Gone;
                        view.FindViewById<TextView>(Resource.Id.title).Visibility = ViewStates.Visible;
                        //view.FindViewById<TextView>(Resource.Id.image).Visibility = ViewStates.Gone;

                        view.FindViewById<TextView>(Resource.Id.title).Text = item.titletext;

                        view.FindViewById<ImageButton>(Resource.Id.trimbtn).Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        Picasso.With(view.Context).
                            Load(currentitem.Thumb + "?s=" + WhiteLabelConfig.THUMBNAIL_SIZE).
                            Config(Bitmap.Config.Rgb565).
                            Fit().
                            CenterCrop().
                            Into(view.FindViewById<ImageView>(Resource.Id.image));
                        view.FindViewById<ImageView>(Resource.Id.image).SetScaleType(ImageView.ScaleType.CenterCrop);
                        view.FindViewById<ImageButton>(Resource.Id.editbtn).Visibility = ViewStates.Visible;
                        view.FindViewById<ImageButton>(Resource.Id.trimbtn).Visibility = ViewStates.Visible;
                        view.FindViewById<ImageView>(Resource.Id.image).Visibility = ViewStates.Visible;
                        view.FindViewById<TextView>(Resource.Id.title).Visibility = ViewStates.Gone;
                    }

                    if (adpt.trimmode)
                    {

                        if (adpt.currenttrim != currentitem)
                        {
                            view.Alpha = 0.2f;
                            view.FindViewById<ImageButton>(Resource.Id.editbtn).Visibility = ViewStates.Visible;

                        }
                        else
                        {
                            view.FindViewById<ImageButton>(Resource.Id.editbtn).Visibility = ViewStates.Invisible;
                            view.Alpha = 1f;
                        }
                    }
                    else
                    {
                        view.FindViewById<ImageButton>(Resource.Id.editbtn).Visibility = ViewStates.Visible;
                        view.Alpha = 1f;
                    }
                }
                else
                {
                    view.FindViewById<TextView>(Resource.Id.outpoint).Visibility = ViewStates.Invisible;
                    view.FindViewById<ImageView>(Resource.Id.image).SetImageDrawable(null);
                    view.FindViewById<ImageButton>(Resource.Id.editbtn).Visibility = ViewStates.Gone;
                    view.FindViewById<ImageButton>(Resource.Id.trimbtn).Visibility = ViewStates.Gone;
                    view.FindViewById(Resource.Id.placeholderbuttons).Visibility = ViewStates.Visible;
                    view.FindViewById<TextView>(Resource.Id.title).Visibility = ViewStates.Gone;
                    view.FindViewById(Resource.Id.image).Visibility = ViewStates.Gone;
                    view.FindViewById<View>(Resource.Id.colorbar).Visibility = ViewStates.Gone;
                    view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Gone;
                }



            }

            private void View_Click(object sender, EventArgs e)
            {
                if (currentitem.Status != MediaItem.MediaStatus.PLACEHOLDER && !adpt.trimmode)
                {
                    OnPreview?.Invoke(currentitem);
                }
                else if (adpt.currenttrim == currentitem)
                {
                    OnTrim?.Invoke(currentitem);
                }
            }
        }

        MediaItem currentplaying;

        internal void UpdatePlaying(MediaItem current)
        {
            currentplaying = current;
            NotifyDataSetChanged();
        }

        private int GetIndexForItem(MediaItem item)
        {
            return allitems.IndexOf(item);
        }

        public MediaItem GetItemFromIndex(int index)
        {
            return allitems[index];
        }

        List<MediaItem> allitems;

        public void UpdateData(List<MediaItem> items)
        {
            allitems = null;
            allitems = new List<MediaItem>();
            allitems = items;
            if (allitems.Count == 0 || allitems.Last().Status != MediaItem.MediaStatus.PLACEHOLDER)
                allitems.Add(new MediaItem() { Status = MediaItem.MediaStatus.PLACEHOLDER });

            //if (allitems.Last().Status == MediaItem.MediaStatus.DONE)
            //    allitems.Add(new MediaItem() { Status = MediaItem.MediaStatus.PLACEHOLDER });
            NotifyDataSetChanged();
        }

        public void OnItemDismiss(int position)
        {
            //OnDelete?.Invoke(allitems[position]);
            allitems.RemoveAt(position);
            NotifyItemRemoved(position);
        }

        public bool OnItemMove(int fromPosition, int toPosition)
        {
            allitems.Swap(fromPosition, toPosition);
            NotifyItemMoved(fromPosition, toPosition);
            return true;
        }


        public SingleEditAdapter(Activity @context)
                : base()
        {
            allitems = new List<MediaItem>();
            //allitems = items;
            //add blank one on the end if its not already blank
        }

        public event Action<MediaItem> OnPreview;
        public event Action<MediaItem> OnTrim;
        public event Action<MediaItem, int> OnDelete;

        public override int ItemCount
        {
            get
            {
                return allitems.Count();
            }
        }

        public event Action<MediaItem,Shot.ShotTypes> OnChange;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = allitems[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
        }

        private void View_OnTrim(MediaItem obj)
        {
            OnTrim?.Invoke(obj);
        }

        private void View_OnPreview(MediaItem obj)
        {
            OnPreview?.Invoke(obj);
        }

        private void View_OnChange(MediaItem obj, Shot.ShotTypes tp)
        {
            OnChange?.Invoke(obj,tp);
        }

        private TimeSpan Countuptocurrent(int index)
        {
            TimeSpan total = TimeSpan.Zero;

            for (int i = 0; i < index+1; i++)
            {
                total += (((allitems[i].outpoint != TimeSpan.Zero && allitems[i].outpoint != TimeSpan.MaxValue) ? allitems[i].outpoint : allitems[i].ClipLength) - allitems[i].inpoint);
            }
            return total;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent,int viewtype)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.edititem_inline, parent, false);
            ViewHolder vh = new ViewHolder(itemView,this);
            vh.OnChange += View_OnChange;
            vh.OnPreview += View_OnPreview;
            vh.OnTrim += View_OnTrim;
            vh.OnDelete += Vh_OnDelete;
            return vh;
        }



        private void Vh_OnDelete(MediaItem obj)
        {
            var index = allitems.IndexOf(obj);
            allitems.RemoveAt(index);
            NotifyItemRemoved(index);
            OnDelete?.Invoke(obj, index);
        }

        public override long GetItemId(int position)
        {
            return allitems[position].GetHashCode();
        }

        internal void NotifyChanges(List<MediaItem> media)
        {
            allitems = media;
            NotifyDataSetChanged();
        }

        bool trimmode = false;
        MediaItem currenttrim;

        internal void TrimMode(MediaItem obj, bool v)
        {
            currenttrim = obj;
            trimmode = v;
            NotifyDataSetChanged();
        }
    }
}