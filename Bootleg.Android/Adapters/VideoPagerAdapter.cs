/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.Views;
using Android.Support.V4.App;
using Bootleg.Droid.Fragments;
using System;
using Android.App;
using Android.Content;
using Bootleg.API;

namespace Bootleg.Droid.Adapters
{
    public class VideoPagerAdapter : FragmentPagerAdapter
    {
        public SelectRoleFrag myrolefrag;
        public AllShotsFrag allshotsfrag;
        public SettingsFrag settingsfrag;
        public Context context;

        public VideoPagerAdapter(Activity activity,
            Android.Support.V4.App.FragmentManager SupportFragmentManager)
            : base(SupportFragmentManager)
        {

            //if (allshotsfrag == null)
            //{ 
            //myrolefrag = new SelectRoleFrag(Bootlegger.BootleggerClient.CurrentEvent, true);
            //myrolefrag.OnRoleChanged += Myrolefrag_OnRoleChanged;
            allshotsfrag = new AllShotsFrag();
            settingsfrag = new SettingsFrag();
            //}
            this.context = activity;
        }

        public event Action OnRoleChanged;

        private void Myrolefrag_OnRoleChanged()
        {
            //rolechanged...
            OnRoleChanged?.Invoke();
        }

        //internal event Action OnRefresh;

        internal void RefreshShots()
        {
            allshotsfrag.RefreshShots();
            //OnRefresh?.Invoke();
        }

        //protected internal static readonly string[] Titles = { "Role", "AllShots", "Settings" };
        protected internal static readonly string[] Titles = {"AllShots", "Settings" };


        public override Android.Support.V4.App.Fragment GetItem(int position)
        {
            //Android.Util.Log.Info("MyPagerAdapter", string.Format("GetItem being called for position {0}", position));
            switch (position)
            {
                //case 0:
                //    return myrolefrag;
                case 0:
                    return allshotsfrag;
                case 1:
                    return settingsfrag;
                default:
                    return null;
            }
        }

        public override Java.Lang.Object InstantiateItem(ViewGroup container, int position)
        {
            //Android.Util.Log.Info("MyPagerAdapter", string.Format("InstantiateItem being called for position {0}", position));
            var result = base.InstantiateItem(container, position);
            return result;
        }

        public override int Count
        {
            get { return Titles.Length; }
        }

        public override Java.Lang.ICharSequence GetPageTitleFormatted(int position)
        {
            return new Java.Lang.String();
            //return new Java.Lang.String(Utils.GetStringByName(context,Titles[position]));
        }
    }
}