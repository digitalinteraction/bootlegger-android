/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;

using Android.App;
using Android.Views;
using Android.Widget;
using Bootleg.API.Model;
using Square.Picasso;

namespace Bootleg.Droid
{
    public class LargeShotAdapter : BaseAdapter<Shot>
        {
            List<Shot> items;
            Activity context;
            public LargeShotAdapter(Activity context, List<Shot> items)
                : base()
            {
                this.context = context;
                this.items = items;
            }

            public override long GetItemId(int position)
            {
                return position;
            }

            //public override TableItem this[int position]
            //{
            //    get { return items[position]; }
            //}

            public override int Count
            {
                get { return items.Count; }
            }

            public override View GetView(int position, View convertView, ViewGroup parent)
            {
                var item = items[position];
                ImageView view = convertView as ImageView;
                if (view == null) // no view to re-use, create new
                {
                    view = new ImageView(context);
                //replace with local assets...
                //view.SetUrlDrawable(item.IconUri.ToString(), Resource.Drawable.abc_ic_search);
                Picasso.With(view.Context).Load("file://" + item.image).Fit().Into(view);
                    view.SetScaleType(ImageView.ScaleType.FitXy);
                }
                return view;
            }

            public override Shot this[int position]
            {
                get { return items[position]; }
            }

        }
}