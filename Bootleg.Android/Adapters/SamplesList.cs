/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;
using Android.Content;
using Android.Views;
using Android.Widget;
using Square.Picasso;
using Android.Util;
using Android.Content.Res;
using Android.Graphics;
using Bootleg.API.Model;

namespace Bootleg.Droid.Adapters
{
    public class SamplesList:ArrayAdapter<MediaItem>
    {
        public SamplesList(Context context,List<MediaItem> items):base(context,Android.Resource.Layout.SimpleListItem1,items)
        {

        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            ImageView imageView = new ImageView(Context);
            GridView.LayoutParams layoutParams;
            layoutParams = new GridView.LayoutParams((int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 40, Context.Resources.DisplayMetrics), (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, 27, Resources.System.DisplayMetrics));
            //layoutParams = new FrameLayout.LayoutParams(FrameLayout.LayoutParams.MatchParent, FrameLayout.LayoutParams.WrapContent);
            imageView.LayoutParameters = layoutParams;
            imageView.SetAdjustViewBounds(true);
            imageView.SetScaleType(ImageView.ScaleType.FitXy);
            Picasso.With(Context).Load(GetItem(position).Thumb).Priority(Picasso.Priority.Low).NoPlaceholder().NoFade().Config(Bitmap.Config.Rgb565).Fit().CenterCrop().MemoryPolicy(MemoryPolicy.NoCache).Into(imageView);
            //imageView.SetBackgroundColor(new Color(0, 0, 0, 128));
            return imageView;
        }
    }
}