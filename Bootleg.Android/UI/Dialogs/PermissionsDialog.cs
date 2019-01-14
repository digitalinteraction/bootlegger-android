/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using Bootleg.API;
using Bootleg.API.Model;

namespace Bootleg.Droid.UI
{
    public static class PermissionsDialog
    {
        public static Task AskPermissions(Activity context,Shoot eventid,bool retry=false)
        {
            //if (WhiteLabelConfig.USE_RELEASE_DIALOG)
            //SHOW DIALOG IF THE EVENT IS SET TO PUBLIC!
            if (eventid.ispublic)
            {
                if (retry || !Bootlegger.BootleggerClient.CurrentUser.permissions.ContainsKey(eventid.id))
                {
                    TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

                    var builder = new Android.Support.V7.App.AlertDialog.Builder(context);
                    builder.SetTitle(Resource.String.sharename);

                    View di = context.LayoutInflater.Inflate(Resource.Layout.release_dialog, null);
                    if (eventid.release != null && eventid.release != "")
                        di.FindViewById<TextView>(Resource.Id.text).TextFormatted = (Android.Text.Html.FromHtml(eventid.release));

                    if (eventid.publicview)
                        di.FindViewById<TextView>(Resource.Id.publicview).Text = context.Resources.GetString(Resource.String.publicview);
                    else
                        di.FindViewById<TextView>(Resource.Id.publicview).Text = context.Resources.GetString(Resource.String.notpublicview);

                    if (eventid.publicshare)
                        di.FindViewById<TextView>(Resource.Id.publicshare).Text = context.Resources.GetString(Resource.String.publicshare);
                    else
                        di.FindViewById<TextView>(Resource.Id.publicshare).Text = context.Resources.GetString(Resource.String.nopublicshare);

                    builder.SetView(di);

                    builder.SetNegativeButton(Resource.String.yes_share, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                    {
                        try
                        {
                            Bootlegger.BootleggerClient.SetUserPrivacy(false, eventid.id);
                            tcs.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            LoginFuncs.ShowError(context, ex);
                            tcs.SetResult(false);
                        }

                    }))
                    .SetPositiveButton(Resource.String.no_share, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                    {
                        try
                        {
                            Bootlegger.BootleggerClient.SetUserPrivacy(true, eventid.id);
                            tcs.SetResult(true);
                        }
                        catch (Exception ex)
                        {
                            LoginFuncs.ShowError(context, ex);
                            tcs.SetResult(false);
                        }
                    }))
                    .SetOnCancelListener(new OnDismissListener(() =>
                    {
                        tcs.SetException(new NotGivenPermissionException(context.Resources.GetString(Resource.String.notacceptperms)));
                    }))
                    .SetCancelable(true);
                    var dialog = builder.Create();
                    dialog.Show();

                    dialog.GetButton((int)AlertDialog.InterfaceConsts.ButtonNegative).Enabled = false;
                    dialog.GetButton((int)AlertDialog.InterfaceConsts.ButtonPositive).Enabled = false;

                    //when scrolled to near bottom (or doesnt need scroll):
                    di.FindViewById<ScrollView>(Resource.Id.scroller).ScrollChange+= (object sender, View.ScrollChangeEventArgs e) =>
                    {
                        var scroller = sender as ScrollView;
                        View view = (View)scroller.GetChildAt(0);

                        // Calculate the scrolldiff
                        int diff = (view.Bottom - (scroller.Height + scroller.ScrollY));

                        //Console.WriteLine(diff);
                        //Console.WriteLine(scroller);
                        //Console.WriteLine(scroller.ScrollY);



                        // if diff is zero, then the bottom has been reached
                        if (diff <= 0)
                        {
                            // notify that we have reached the bottom
                            //Log.d(ScrollTest.LOG_TAG, "MyScrollView: Bottom has been reached");
                        //}


                        //if (scroller.ScrollY > scroller.MaxScrollAmount - di.Height)
                        //{
                            dialog.GetButton((int)AlertDialog.InterfaceConsts.ButtonNegative).Enabled = true;
                            dialog.GetButton((int)AlertDialog.InterfaceConsts.ButtonPositive).Enabled = true;
                        }
                    };

                    //continue
                    return tcs.Task;
                }
                else
                {
                   return Task.FromResult(true);
                }
            }
            else
            {
                return Task.FromResult(true);
            }
        }

        private sealed class OnDismissListener : Java.Lang.Object, IDialogInterfaceOnCancelListener
        {
            private readonly Action action;

            public OnDismissListener(Action action)
            {
                this.action = action;
            }

            public void OnCancel(IDialogInterface dialog)
            {
                this.action();
            }
        }

        [Serializable]
        public class NotGivenPermissionException : Exception
        {
            public NotGivenPermissionException()
            {
            }

            public NotGivenPermissionException(string message) : base(message)
            {
            }

            public NotGivenPermissionException(string message, Exception innerException) : base(message, innerException)
            {
            }

            protected NotGivenPermissionException(SerializationInfo info, StreamingContext context) : base(info, context)
            {
            }
        }
    }
}