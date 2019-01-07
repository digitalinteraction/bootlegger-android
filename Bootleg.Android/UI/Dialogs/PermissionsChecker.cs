using System;
using System.Collections.Generic;
using Android;
using Android.App;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using System.Linq;
using Android.Support.Design.Widget;
using Android.Views;

namespace Bootleg.Droid.UI
{
    public class PermissionsChecker
    {
        public PermissionsChecker()
        {
        }

        public const int NON_OPTIONAL = 1;
        public const int OPTIONAL = 2;

        public static bool PermissionsCheck(Activity activity,View layout, bool optional, params string[] perms)
        {
            //TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
            {
                var isok = true;
                var isdenied = false;

                foreach (var perm in perms)
                {
                    if (ContextCompat.CheckSelfPermission(activity, (string)perm) != Android.Content.PM.Permission.Granted)
                        isok = false;

                    if (ContextCompat.CheckSelfPermission(activity, (string)perm) != Android.Content.PM.Permission.Denied)
                        isdenied = true;

                }

                if (isok)
                    return true;

                //if (ContextCompat.CheckSelfPermission(activity, Manifest.Permission.Camera) == Android.Content.PM.Permission.Granted && 
                //    ContextCompat.CheckSelfPermission(activity, Manifest.Permission.RecordAudio) == Android.Content.PM.Permission.Granted && 
                //    ContextCompat.CheckSelfPermission(activity, Manifest.Permission.AccessFineLocation) == Android.Content.PM.Permission.Granted && 
                //    ContextCompat.CheckSelfPermission(activity, Manifest.Permission.WriteExternalStorage) == Android.Content.PM.Permission.Granted)
                //{
                //    return true;
                //}

                var showrationale = false;

                foreach (var perm in perms)
                {
                    if (ActivityCompat.ShouldShowRequestPermissionRationale(activity, perm))
                        showrationale = true;
                }

                //if need to show rationale
                if (showrationale)
                {
                    Snackbar.Make(layout, "Location access is required to find shoots nearby.", Snackbar.LengthIndefinite)
                      .SetAction("OK", v => ActivityCompat.RequestPermissions(activity, perms, (optional) ? OPTIONAL : NON_OPTIONAL))
                      .Show();
                   
                    return false;
                }
                else
                {
                    //if dont neeed to show rationale
                    if ((optional && !isdenied) || !optional)
                        ActivityCompat.RequestPermissions(activity, perms, (optional) ? OPTIONAL : NON_OPTIONAL);

                    return false;
                }
            }
            else
            {
                return true;
                //tcs.SetResult(true);
            }
            //return tcs.Task;
        }
    }
}
