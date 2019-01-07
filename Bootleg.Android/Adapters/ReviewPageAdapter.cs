/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System.Collections.Generic;
using Android.Support.V4.App;
using Android.Text;
using Android.Text.Style;
using Android.Content;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;

namespace Bootleg.Droid.Adapters
{
    public class ReviewPageAdapter : FragmentPagerAdapter
    {
        List<Android.Support.V4.App.Fragment> fragments = new List<Android.Support.V4.App.Fragment>();
        Context context;

        public enum TabType { CLIPS, INGEST, EDITS };

        public ReviewPageAdapter(Android.Support.V4.App.FragmentManager SupportFragmentManager,Context context)
            : base(SupportFragmentManager)
        {
            this.context = context;
        }

        public void AddTab(string title, Android.Support.V4.App.Fragment frag, TabType tp)
        {
            Titles.Add(title);
            fragments.Add(frag);
            TabTypes.Add(tp);
            NotifyDataSetChanged();
        }

        public void RemoveTab(int index)
        {
            Titles.RemoveAt(index);
            fragments.RemoveAt(index);
            TabTypes.RemoveAt(index);
            NotifyDataSetChanged();
        }

        List<string> Titles = new List<string>();
        List<TabType> TabTypes = new List<TabType>();

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            return fragments[position];
        }

        public override int Count
        {
            get { return Titles.Count; }
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        {
            Drawable myDrawable;
            switch (TabTypes[position])
            {
                case TabType.EDITS:
                    myDrawable = ContextCompat.GetDrawable(context, Resource.Drawable.ic_film_roll);
                    break;
                case TabType.INGEST:
                    myDrawable = ContextCompat.GetDrawable(context, Resource.Drawable.ic_tag_white_24dp);
                    break;
                case TabType.CLIPS:
                default:
                    myDrawable = ContextCompat.GetDrawable(context, Resource.Drawable.ic_photo_camera_white_24dp);
                    break;
            }
            var sb = new SpannableString(" "); // space added before text for convenience

            myDrawable.SetBounds(0, 0, myDrawable.IntrinsicWidth, myDrawable.IntrinsicHeight);
            var span = new ImageSpan(myDrawable);
            sb.SetSpan(span, 0, 1, SpanTypes.ExclusiveExclusive);

            return sb;
            //return new Java.Lang.String(Titles[position]);
        }
    }
}