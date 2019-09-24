/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;
using Android.App;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Square.Picasso;
using Android.Graphics;
using Android.Support.V4.Content;
using Android.Support.V7.Widget;
using System;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class ShotSelectAdapter : RecyclerView.Adapter
    {
            public List<Shot> Items {get;set;}
            Activity context;

        public event Action<Shot> OnShotSelected;

        public ShotSelectAdapter(Activity context)
            : base()
        {
            this.context = context;
        }


        public class ViewHolder : RecyclerView.ViewHolder
        {
            private View view;
            private ShotSelectAdapter adpt;
            public ViewHolder(View itemView, ShotSelectAdapter adpt) : base(itemView)
            {
                view = itemView;
                this.adpt = adpt;
                view.Click += View_Click;
            }

            private void View_Click(object sender, EventArgs e)
            {
                adpt.FireClick(currentitem);
            }

            Shot currentitem;

            public void SetItem(Shot item)
            {
                currentitem = item;
                var counter = new TextView(view.Context);
                Picasso.With(view.Context).Load("file://" + item.icon).Into(view.FindViewById<ImageView>(Resource.Id.im));
                //backPic.setColorFilter(ContextCompat.getColor(context, R.color.red),
                //android.graphics.PorterDuff.Mode.MULTIPLY);
                //view.FindViewById<ImageView>(Resource.Id.im).SetScaleType(ImageView.ScaleType.);

                switch (item.shot_type)
                {
                    case Shot.ShotTypes.PHOTO:
                        view.FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Android.Resource.Drawable.IcMenuCamera);
                        view.FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                        break;
                    case Shot.ShotTypes.AUDIO:
                        view.FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Android.Resource.Drawable.IcButtonSpeakNow);
                        view.FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                        break;

                    default:
                        view.FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Gone;
                        break;
                }

                view.FindViewById<TextView>(Resource.Id.txt).Text = item.name;

                //if (adpt.Items.Count > position)
                //{
               float val = 0;
                view.PostDelayed(() =>
                {
                    try
                    {
                        val = (float)(Bootlegger.BootleggerClient.GetShotsQuantityByType(item.id, adpt.tmpmedia) * 100);
                    }
                    catch
                    {
                        //do nothing -- for some reason, cant load this info
                    }

                    view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.progress).ProgressColor = new Color(ContextCompat.GetColor(view.Context, Resource.Color.blue));
                    if (val < 100)
                    {
                        view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.progress).Value = val;
                        view.FindViewById<View>(Resource.Id.completed).Visibility = ViewStates.Gone;

                    }
                    else
                    {
                        view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.progress).Value = 0;
                        view.FindViewById<View>(Resource.Id.completed).Visibility = ViewStates.Visible;
                    }
                },100);
                //}
                //else
                //{
                //    view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.progress).Value = 0;
                //}
            }
        }

        private void FireClick(Shot currentitem)
        {
            OnShotSelected?.Invoke(currentitem);
        }

        internal List<MediaItem> tmpmedia;
        public void UpdateData(List<Shot> items)
        {
            Items = items;
            tmpmedia = Bootlegger.BootleggerClient.MyMediaEditing;
            NotifyDataSetChanged();
        }

        public override long GetItemId(int position)
        {
            return position;
        }

        public override int ItemCount
        {
            get
            {
                return Items.Count;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = Items[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewtype)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.shotselector, parent, false);
            ViewHolder vh = new ViewHolder(itemView, this);
            return vh;
        }
    }
}