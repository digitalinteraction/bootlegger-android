/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Android.Support.V7.Widget;
using System;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class RoleAdapter : RecyclerView.Adapter
    {
        public class ViewHolder : RecyclerView.ViewHolder
        {
            public event Action<Role> OnChosen;
            RoleAdapter adpt;
            View view;
            Role item;
            bool dark;
            public ViewHolder(View itemView, RoleAdapter adpt, bool dark) : base(itemView)
            {
                view = itemView;
                view.Click += View_Click;
                this.adpt = adpt;
                this.dark = dark;
            }

            private void View_Click(object sender, EventArgs e)
            {
                OnChosen?.Invoke(item);
            }

            public void SetItem(Role role)
            {
                item = role;
                view.FindViewById<TextView>(Resource.Id.firstLine).Text = role.name;
                if (dark)
                {
                    view.FindViewById<TextView>(Resource.Id.firstLine).SetTextColor(Android.Graphics.Color.WhiteSmoke);
                    //if (role.id == Bootlegger.BootleggerClient.CurrentClientRole.id)
                    //{
                    //    view.FindViewById<ImageView>(Resource.Id.tick).Visibility = ViewStates.Visible;
                    //}
                    //else
                    //{
                    //    view.FindViewById<ImageView>(Resource.Id.tick).Visibility = ViewStates.Invisible;
                    //}
                }

                //view.FindViewById<TextView>(Resource.Id.firstLine).Text = "دون قدما بتخصيص مليارات مع, يتمكن الأوروبية نفس كل, ٣٠ دار الدول ألمانيا. في عدد كانت الحكم, حتى سقطت انتهت مع. كل عدم وقوعها، العاصمة ارتكبها, دنو الأخذ الإطلاق م";
            }
        }

        bool dark = false;
        public RoleAdapter(List<Role> roles,bool dark)
        {
            allitems = roles;
            this.dark = dark;
        }


        List<Role> allitems = new List<Role>();

        public override int ItemCount
        {
            get
            {
                return allitems.Count;
            }
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            (holder as ViewHolder).SetItem(allitems[position]);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView;
            itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.roleitem, parent, false);
            ViewHolder vh = new ViewHolder(itemView, this,this.dark);
            vh.OnChosen += Vh_OnChosen;
            return vh;
        }

        public event Action<Role> OnChosen;

        private void Vh_OnChosen(Role obj)
        {
            OnChosen?.Invoke(obj);
        }
    }
}