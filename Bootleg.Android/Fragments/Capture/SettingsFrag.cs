/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 ﻿using Android.OS;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Bootleg.Droid.UI;
using System;

namespace Bootleg.Droid
{
    public class SettingsFrag : Android.Support.V4.App.Fragment
    {
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public override void OnResume()
        {
            base.OnResume();
            if (Bootlegger.BootleggerClient.CurrentUser.permissions.ContainsKey(Bootlegger.BootleggerClient.CurrentEvent.id))
                base.Activity.FindViewById<SwitchCompat>(Resource.Id.hideidentity).Checked = Bootlegger.BootleggerClient.CurrentUser.permissions[Bootlegger.BootleggerClient.CurrentEvent.id];
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.Settings, container, false);
            view.FindViewById<SwitchCompat>(Resource.Id.hideidentity).CheckedChange += SettingsFrag_CheckedChange;
            return view;

        }

        void SettingsFrag_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            try
            {
                Bootlegger.BootleggerClient.SetUserPrivacy(e.IsChecked, Bootlegger.BootleggerClient.CurrentEvent.id);
            }
            catch (Exception ex)
            {
                LoginFuncs.ShowError(Context,ex);
            }
        }
    }
}
