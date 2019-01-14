/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Android.OS;
using Android.Views;
using Android.Support.V7.Widget;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class RoleListFragment : Android.Support.V4.App.Fragment
    {
		public RoleListFragment()
		{
			
		}

        MetaPhase thephase;
        Shoot CurrentEvent;
        bool dark;
        public RoleListFragment(Shoot currentevent,MetaPhase phase,bool dark)
        {
            this.CurrentEvent = currentevent;
            this.thephase = phase;
            this.dark = dark;
        }

        public void Update()
        {
            listAdapter?.NotifyDataSetChanged();
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RetainInstance = true;
        }

        public event Action<Role> OnRoleSelected;

        RoleAdapter listAdapter;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.ListFragment, container, false);
            if (CurrentEvent != null)
            {
                List<Role> roles;
                if (thephase != null)
                    roles = (from n in CurrentEvent.roles where thephase.roles.Contains(n.id) select n).ToList();
                else
                    roles = CurrentEvent.roles;

                listAdapter = new RoleAdapter(roles, dark);
                listAdapter.OnChosen += ListAdapter_OnChosen;
                var listview = view.FindViewById<RecyclerView>(Resource.Id.eventsView);
                listview.SetAdapter(listAdapter);
                listview.SetLayoutManager(new LinearLayoutManager(Activity));
                listview.AddItemDecoration(new Android.Support.V7.Widget.DividerItemDecoration(Activity, Android.Support.V7.Widget.DividerItemDecoration.Vertical));
            }
            return view;
        }

        private void ListAdapter_OnChosen(Role obj)
        {
            OnRoleSelected?.Invoke(obj);
        }
    }
}