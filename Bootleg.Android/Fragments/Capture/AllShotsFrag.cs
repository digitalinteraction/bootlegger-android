/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */

using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Bootleg.API;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class AllShotsFrag : Android.Support.V4.App.Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your fragment here
            RetainInstance = true;
        }

        View theview;
       

        internal void RefreshShots()
        {
            _adapter?.UpdateData(Bootlegger.BootleggerClient.CurrentEvent._shottypes);
        }

        ShotAdapter _adapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.AllShots, container, false);
            

            //view.FindViewById<GridView>(Resource.Id.shotslist).ItemClick += AllShotsFrag_ItemClick;


            _adapter = new ShotAdapter(Activity);
            _adapter.OnShotSelected += _adapter_OnShotSelected;
            view.FindViewById<RecyclerView>(Resource.Id.shotslist).SetLayoutManager(new GridLayoutManager(Context, 3));
            view.FindViewById<RecyclerView>(Resource.Id.shotslist).SetAdapter(_adapter);
            
            //start adapters:
            if (Bootlegger.BootleggerClient.CurrentClientRole != null && Bootlegger.BootleggerClient.CurrentEvent!=null)
            {
                _adapter.UpdateData(Bootlegger.BootleggerClient.CurrentEvent?._shottypes);

            }
            theview = view;
            return view;
        }

        private void _adapter_OnShotSelected(Shot obj)
        {
            (Activity as IFragmentController).Video_ItemSelected(obj);
        }
    }
}