/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */

using Android.App;
using Android.Content;
using Android.Net.Wifi;
using Android.OS;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Bootleg.Droid.UI;
using BranchXamarinSDK;
using RestSharp.Extensions.MonoHttp;
using System;
using System.Linq;
using static Android.Net.Wifi.WifiManager;

namespace Bootleg.Droid
{
    [Activity(Theme = "@style/Theme.Splash", ScreenOrientation = Android.Content.PM.ScreenOrientation.Portrait, MainLauncher = true, NoHistory = true)]
    [IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault }, DataScheme = "offline")]
    //[IntentFilter(new[] { Intent.ActionView }, Categories = new[] { Intent.CategoryBrowsable, Intent.CategoryDefault }, DataScheme = "https", DataHost = "",DataPathPrefix = "/watch/view/")]
    public partial class SplashActivity : FragmentActivity, IBranchSessionInterface
    {
		
		
       
    }
}