/* Copyright (C) 2014 Newcastle University
*
* This software may be modified and distributed under the terms
* of the MIT license. See the LICENSE file for details.
*/
using Android.OS;
using Android.Views;
using Android.Widget;
using Bootleg.API.Model;
using Square.Picasso;

namespace Bootleg.Droid
{
    public class ShotFragment : Android.Support.V4.App.Fragment
    {
        Shot s;
        public ShotFragment(Shot s)
        {
            this.s = s;
        }

        public ShotFragment()
        {

        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            ImageView view = new ImageView(Activity);
            
            view.SetPadding(0, 0, 0, 0);
            if (s != null)
            {
                Picasso.With(view.Context).Load("file://" + s.image).Placeholder(Resource.Drawable.loading).Into(view);
            }
            view.LayoutParameters = new FrameLayout.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent);
            view.SetScaleType(ImageView.ScaleType.FitXy);
            view.SetAlpha(120);
            return view;
        }
    }
}