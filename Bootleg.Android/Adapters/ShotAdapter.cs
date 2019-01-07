/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Bootleg.API.Model;
using Square.Picasso;

namespace Bootleg.Droid
{
    public class ShotAdapter : RecyclerView.Adapter
    {
            List<Shot> allitems;
            Activity context;


            public class ViewHolder : RecyclerView.ViewHolder
            {
                private View view;
            private ShotAdapter adpt;
                public ViewHolder(View itemView, ShotAdapter adpt) : base(itemView)
                {
                    view = itemView;
                this.adpt = adpt;
                view.Click += View_Click;
                }

            private void View_Click(object sender, EventArgs e)
            {
                adpt.FireClick(currentitem);
            }

            public Shot currentitem;

                public void SetItem(Shot item)
                {

                currentitem = item;
                    view.FindViewById<ImageView>(Resource.Id.star).Visibility = ViewStates.Gone;

                    switch (item.shot_type)
                    {
                        case Shot.ShotTypes.PHOTO:
                        view.FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Resource.Drawable.ic_photo_camera_white_24dp);
                        view.FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                            break;
                        case Shot.ShotTypes.AUDIO:
                        view.FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Resource.Drawable.ic_mic_white_48dp);
                        view.FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                            break;

                        default:
                        view.FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Gone;
                            break;
                    }

                    Picasso.With(view.Context).Load("file://" + item.icon).CenterCrop().Fit().NoFade().Into(view.FindViewById<ImageView>(Resource.Id.im));
                }
            }


        public void UpdateData(List<Shot> items)
        {
            allitems = items;
            NotifyDataSetChanged();
        }

        public event Action<Shot> OnShotSelected;

        private void FireClick(Shot currentitem)
        {
            OnShotSelected?.Invoke(currentitem);
        }

        public ShotAdapter(Activity context)
                : base()
            {
                this.context = context;
            }

            public override long GetItemId(int position)
            {
                return position;
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

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewtype)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.shot, parent, false);
            ViewHolder vh = new ViewHolder(itemView, this);
            return vh;
        }

        //public override View GetView(int position, View convertView, ViewGroup parent)
        //    {
        //        var item = items[position];
        //        //FrameLayout view = convertView as FrameLayout;
        //        if (convertView == null) // no view to re-use, create new
        //        {
        //            convertView = context.LayoutInflater.Inflate(Resource.Layout.shot,null);
        //        }

                
        //        return convertView;
        //    }

            //public override Shot this[int position]
            //{
            //    get { return items[position]; }
            //}

        }
}