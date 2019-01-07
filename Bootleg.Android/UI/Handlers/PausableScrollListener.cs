/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V7.Widget;
using Square.Picasso;

namespace Bootleg.Droid.UI
{
    internal class PausableScrollListener : RecyclerView.OnScrollListener
    {
        Context context;
        Java.Lang.Object tag;
        public PausableScrollListener(Context context, Java.Lang.Object tag)
        {
            this.context = context;
            this.tag = tag;
        }

        public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
        {
            Picasso picasso = Picasso.With(context);

            if (newState == (int)ScrollState.Idle || newState == (int)ScrollState.TouchScroll)
            {
                picasso.ResumeTag(tag);
            }
            else
            {
                picasso.PauseTag(tag);
            }
        }
    }
}