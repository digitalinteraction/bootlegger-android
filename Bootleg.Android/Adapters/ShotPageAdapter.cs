/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;
using System.Linq;
using Android.Support.V4.App;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class ShotPageAdapter : FragmentPagerAdapter
    {

        List<Shot> shots;
        public ShotPageAdapter(Android.Support.V4.App.FragmentManager fm, List<Shot> shots)
            : base(fm)
        {
            this.shots = shots.GroupBy(i => i.image).Select(group => group.First()).ToList();
        }

        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            return new ShotFragment(shots[position]);
        }

        public override int Count
        {
            get
            {
                return shots.Count;
            }
        }

    }
}