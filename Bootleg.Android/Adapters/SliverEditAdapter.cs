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
using Android.Support.V7.Widget;
using Android.Graphics;
using Android.Support.V4.Graphics;
using static Bootleg.API.Bootlegger;
using Bootleg.API.Model;
using Android.Content;

namespace Bootleg.Droid
{
    public class SliverEditAdapter : RecyclerView.Adapter
    {
        //public static Color GetColorForIndex(Context context, int index)
        //{
        //    try
        //    {
        //        //change to use resources first, then use this:
        //        var r = context.Resources.GetStringArray(Resource.Array.topic_colors);
        //        if (index > r.Length)
        //        {
        //            var col = index * 50;
        //            var col_f = col % 360;
        //            var color = ColorUtils.HSLToColor(new float[] { col_f, 0.55f, 0.76f });
        //            var c = new Color(color);
        //            return c;
        //        }
        //        else
        //        {
        //            return Color.ParseColor(r[index]);
        //        }
        //    }
        //    catch 
        //    {
        //        return Color.Gray;
        //    }
        //}

        //public static Color GetDimColorForIndex(Context context,int index)
        //{
        //    try
        //    {
        //        var r = context.Resources.GetStringArray(Resource.Array.topic_colors);
        //        if (index > r.Length)
        //        {
        //            var col = index * 50;
        //            var col_f = col % 360;
        //            var color = ColorUtils.HSLToColor(new float[] { col_f, 0.40f, 0.76f });
        //            var c = new Color(color);
        //            return c;
        //        }
        //        else
        //        {
        //            var color = Color.ParseColor(r[index]);
        //            var hsl = new float[3];
        //            ColorUtils.ColorToHSL(color, hsl);
        //            hsl[1] -= 0.2f;
        //            return new Color(ColorUtils.HSLToColor(hsl));
        //        }
        //    }
        //    catch
        //    {
        //        return Color.Gray;
        //    }
        //}

        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            SliverEditAdapter adpt;
            MediaItem currentitem;
            //Random rand = new Random((int)DateTime.Now.Ticks);


            public ViewHolder(View itemView, SliverEditAdapter adpt) : base(itemView)
            {
                view = itemView;
                this.adpt = adpt;
                
            }

            //int lastrandom = 0;
            internal void SetItem(MediaItem item)
            {
                currentitem = item;
                //set size:


                var realmedia = BootleggerClient.GetMediaItem(item.id);
                try
                {
                    var mtopics = realmedia.Static_Meta[$"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}"].TrimStart(',').Split(',');
                    if (mtopics.Length > 0)
                    {
                        var index = BootleggerClient.CurrentEvent.topics.Find((t) => t.id==mtopics.First());
                        view.SetBackgroundColor(Color.ParseColor(index.color));
                    }
                    else
                    {
                        view.SetBackgroundColor(Color.Silver);
                    }
                }
                catch
                {
                    view.SetBackgroundColor(Color.Silver);
                }

                //view.SetBackgroundColor(GetColorForIndex(adpt.GetIndexForItem(currentitem)));

                view.LayoutParameters.Width = (int)(((((item.outpoint!=TimeSpan.Zero)?item.outpoint : item.ClipLength) - item.inpoint).TotalMilliseconds / adpt.TotalMilis) * adpt.context.FindViewById<View>(Resource.Id.timeline).MeasuredWidth) - 2;
            }
        }

        List<MediaItem> allitems;

        public void UpdateData(List<MediaItem> items)
        {
            allitems = items;
            NotifyDataSetChanged();
        }

        public void OnItemDismiss(int position)
        {
            allitems.RemoveAt(position);
            NotifyItemRemoved(position);
        }

        public bool OnItemMove(int fromPosition, int toPosition)
        {
            allitems.Swap(fromPosition, toPosition);
            NotifyItemMoved(fromPosition, toPosition);
            return true;
        }

        private Activity context;

        public SliverEditAdapter(Activity context)
                : base()
        {
            this.context = context;
            allitems = new List<MediaItem>();
            //allitems = items;
            //add blank one on the end if its not already blank
        }

        //public event Action<MediaItem> OnPreview;
        //public event Action<MediaItem> OnTrim;


        public override int ItemCount
        {
            get
            {
                return allitems.Count();
            }
        }

        public double TotalMilis { get
            {
                return allitems.Sum(o=> (((o.outpoint != TimeSpan.Zero) ? o.outpoint : o.ClipLength) - o.inpoint).TotalMilliseconds);
            }
        }
        
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = allitems[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
        }

        public int GetIndexForItem(MediaItem item)
        {
            return allitems.IndexOf(item);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent,int viewtype)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.edit_sliver, parent, false);
            ViewHolder vh = new ViewHolder(itemView,this);
            return vh;
        }

        internal void NotifyChanges(List<MediaItem> media)
        {
            allitems = media;
            NotifyDataSetChanged();
        }
    }
}