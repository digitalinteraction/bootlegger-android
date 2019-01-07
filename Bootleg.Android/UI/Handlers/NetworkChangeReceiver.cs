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
using Android.Net;

namespace Bootleg.Droid.UI
{
    public class NetworkChangeReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            IsNetworkAvailable(context);
        }

        public event Action LostWifi;
        public event Action GotWifi;

        private bool IsNetworkAvailable(Context context)
        {
            ConnectivityManager connectivity = (ConnectivityManager)
              context.GetSystemService(Context.ConnectivityService);
            if (connectivity != null)
            {
                NetworkInfo mWifi = connectivity.GetNetworkInfo(ConnectivityType.Wifi);
                if (!mWifi.IsConnected)
                {
                    //turn off wifi...
                    if (LostWifi != null)
                        LostWifi();
                }
                else
                {
                    if (GotWifi != null)
                        GotWifi();
                }
            }
            return false;
        }
    }
}