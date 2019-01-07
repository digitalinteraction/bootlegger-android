/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System.Collections.Generic;
using Android.Support.V4.App;
using Android.Graphics.Drawables;
using Android.Support.V4.Content;
using Android.Text;
using Android.Content;
using Android.Text.Style;

namespace Bootleg.Droid.Adapters
{
    public class RolePageAdapter : FragmentPagerAdapter
    {
        List<Android.Support.V4.App.Fragment> fragments = new List<Android.Support.V4.App.Fragment>();

        public RolePageAdapter(Android.Support.V4.App.FragmentManager SupportFragmentManager, Context context)
            : base(SupportFragmentManager)
        {
            this.context = context;
        }

        public enum TabType { LIST, MAP };

        public void AddTab(TabType title, Android.Support.V4.App.Fragment frag)
        {
            Titles.Add(title);
            fragments.Add(frag);
            NotifyDataSetChanged();
        }

        List<TabType> Titles = new List<TabType>();
        private Context context;

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            return fragments[position];
        }

        public override int Count
        {
            get { return Titles.Count; }
        }

        //public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        //{
        //    return new Java.Lang.String(Titles[position]);

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        {
            Drawable myDrawable;
            switch (Titles[position])
            {
                case TabType.LIST:

                    myDrawable = ContextCompat.GetDrawable(context, Resource.Drawable.ic_list_white_24dp);
                    break;

                case TabType.MAP:
                default:
                    myDrawable = ContextCompat.GetDrawable(context, Resource.Drawable.ic_map);
                    break;

            }
            var sb = new SpannableString(" "); // space added before text for convenience

            myDrawable.SetBounds(0, 0, myDrawable.IntrinsicWidth, myDrawable.IntrinsicHeight);
            var span = new ImageSpan(myDrawable);
            sb.SetSpan(span, 0, sb.Length(), SpanTypes.ExclusiveExclusive);

            //return new Java.Lang.String("TEST");
            return sb;
            //return new Java.Lang.String(Titles[position]);
        }
    }
}