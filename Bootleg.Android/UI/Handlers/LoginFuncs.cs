/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;

using Android.Content;
using System.Threading.Tasks;
using System.Net;
using System.Threading;
using Bootleg.API.Exceptions;
using Android.Graphics;
using Android.Support.CustomTabs;
using Android.App;
using Android.Support.V4.View;
using Android.Views;
using Bootleg.API;
using Android.Support.V4.Content;
using Firebase.Iid;
using Android.Support.Design.Widget;
using Bootleg.Droid.Screens;
using Android.Widget;
using static Bootleg.Droid.UI.PermissionsDialog;

namespace Bootleg.Droid.UI
{
    public static class LoginFuncs
    {
        public const string LOGIN_PROVIDER = "login_provider";
        public const string WINDOW_TYPE = "window_type";
        public const string HELP_LINK = "help_link";
        public const string LOGIN_PROVIDER_GOOGLE = "google";
        public const string LOGIN_PROVIDER_FACEBOOK = "facebook";
        public const string LOGIN_PROVIDER_LOCAL = "local";
        public const int LOGIN_RESPONSE = 11;
        public const int HELP_RESPONSE = 12;
        public const int NEW_SHOOT = 13;

        public static Task ShowSnackbar(Context context, View root, string text)
        {
            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
            Snackbar.Make(root, text, Snackbar.LengthIndefinite)
                     .SetAction(Android.Resource.String.Ok, v => tcs.SetResult(true))
                     .Show();

            return tcs.Task;
        }


        public static void ShowMessage(Context context, int res)
        {
            Activity activity = (context ?? Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity) as Activity;

            try
            {
                Snackbar.Make(activity.FindViewById(Resource.Id.main_content), activity.GetString(res), Snackbar.LengthShort).Show();
            }
            catch
            {
                Console.WriteLine("Crashed trying to show error");
            }
        }

        public static void ShowError(Context context, Exception e)
        {
            //detect type of exception and show corrent 

            //NeedsPermissionsException - acceptperms

            //RoleNotSelectedException - norolechosen

            //NoNetworkException - no ip based off the back of the IsReallyConnected function

            //UnknownNetworkException - not successful network operation, find out why and report

            //var message = context.GetString(Resource.String.noconnectionshort);
            var message = e.Message;


            switch (e)
            {

                case ApiKeyException ex:
                    message = context.GetString(Resource.String.apikeynovalid);
                    break;

                case NeedsPermissionsException ex:
                case NotGivenPermissionException exx:
                    message = context.GetString(Resource.String.acceptperms);
                    break;

                case RoleNotSelectedException ex:
                    message = context.GetString(Resource.String.norolechosen);
                    break;

                case NoNetworkException ex:
                    message = context.GetString(Resource.String.errornonetwork);
                    break;

                case UnknownNetworkException ex:
                    message = context.GetString(Resource.String.errorunknown);
                    break;

                case NeedsUpdateException ex:
                    message = context.GetString(Resource.String.errornonetwork);
                    break;

                case SessionLostException ex:
                    message = context.GetString(Resource.String.loginagain);
                    break;

                case ServerErrorException ex:
                    message = context.GetString(Resource.String.errorserver);
                    break;

                case TaskCanceledException ex:
                    message = context.GetString(Resource.String.errorcanceled);
                    break;

                case StoriesDisabledException ex:
                    message = context.GetString(Resource.String.storiesdisabled);
                    break;
            }

            try
            {
                Snackbar.Make((context as Activity).FindViewById(Resource.Id.main_content), message, Snackbar.LengthShort).Show();
            }
            catch
            {
                Console.WriteLine("Crashed trying to show error");
            }
        }




        public static void ShowHelp(Android.App.Activity context,string link)
        {
           var builder = new CustomTabsIntent.Builder()
          .SetToolbarColor(ContextCompat.GetColor(context,Resource.Color.blue))
          .SetSecondaryToolbarColor(Android.Resource.Color.White)
          .SetShowTitle(true);
            Bitmap icon;
            if (ViewCompat.GetLayoutDirection(context.FindViewById<ViewGroup>(Android.Resource.Id.Content).GetChildAt(0)) != ViewCompat.LayoutDirectionRtl)
                icon = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.ic_arrow_back_white_24dp);
            else
                icon = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.ic_arrow_forward_white_24dp);

            builder.SetCloseButtonIcon(icon);
            var intent = builder.Build();
            intent.Intent.PutExtra(Intent.ExtraReferrer, Android.Net.Uri.Parse("app://" + context.PackageName));
            intent.LaunchUrl(context, Android.Net.Uri.Parse(context.Resources.GetString(Resource.String.HelpLink) + link));
        }

        public const int NEW_SHOOT_REQUEST = 34;

        //internal static void NewShoot(Android.App.Activity context)
        //{
        //    if ((context.Application as BootleggerApp).IsReallyConnected)
        //    {
        //        context.StartActivityForResult(typeof(NewShoot), NEW_SHOOT_REQUEST);
        //    }
        //    else
        //    {
        //        ShowError(context, new Exception(context.GetString(Resource.String.noconnectionshort)));
        //        //Toast.MakeText(context, Resource.String.checknetwork, ToastLength.Long).Show();
        //    }
        //}

        public static async Task TryLogin(Activity context,CancellationToken cancel)
        {
            if (Bootlegger.BootleggerClient.Connected)
                return;

            var allprefs = context.GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
            if (Bootlegger.BootleggerClient.SessionCookie != null)
            {
                try
                {
                    //try connecting
                    try
                    {

                        if (!(context.Application as BootleggerApp).IsReallyConnected)
                        {
                            throw new NoNetworkException();
                        }
                        else
                        {

                            await Bootlegger.BootleggerClient.Connect(Bootlegger.BootleggerClient.SessionCookie, cancel);

                            var edit = allprefs.Edit();
                            edit.PutBoolean("firstrun", true);
                            edit.Apply();

                            if (!WhiteLabelConfig.LOCAL_SERVER)
                            {
                                try
                                {
                                    //FirebaseApp.InitializeApp(context);
                                    var refreshedToken = FirebaseInstanceId.Instance.Token;
                                    //Console.WriteLine("token: " + refreshedToken);
                                    Bootleg.API.Bootlegger.BootleggerClient.RegisterForPush(refreshedToken, API.Bootlegger.Platform.Android);
                                }
                                catch (Exception e)
                                {
                                    ShowError(context, new Exception("Firebase Error"));
                                }
                            }
                        }
                    }
                    catch (TaskCanceledException e)
                    {
                        throw e;
                    }
                    catch (NotSupportedException e)
                    {
                        if ((context.Application as BootleggerApp).IsReallyConnected)
                        {
                            throw new NeedsUpdateException();
                            //ShowError(context,new NeedsUpdateException());
                        }
                        else
                        {
                            throw new NoNetworkException();
                            //ShowError(context, new NoNetworkException());
                        }
                        throw e;
                    }
                    catch (ApiKeyException e)
                    {
                        if ((context.Application as BootleggerApp).IsReallyConnected)
                        {
                            throw new ApiKeyException();
                            //ShowError(context, new ApiKeyException());
                        }
                        else
                        {
                            throw new NoNetworkException();
                            //ShowError(context, new NoNetworkException());
                        }

                        throw e;
                    }
                    catch (Exception e)
                    {
                        if ((context.Application as BootleggerApp).IsReallyConnected)
                        {
                            throw new SessionLostException();
                        }
                        else
                        {   
                            throw new NoNetworkException();
                        }
                    }
                }
                catch (TaskCanceledException e)
                {
                    throw e;
                }
                catch (Exception e)
                {
                    throw e;
                }
            }
            else
            {
                //no session stored:
                //if its the first time using the app...
                var firstrun = allprefs.GetBoolean("firstrun", false);
                if (!firstrun)
                    throw new WebException(context.Resources.GetString(Resource.String.loginagain));
            }
        }

        internal static void ShowToast(Context activity, Exception e)
        {
            //default: Resource.String.cannotloadvideo
            var message = activity.GetString(Resource.String.cannotloadvideo);
            switch (e)
            {
                case StoriesDisabledException ex:
                    message = activity.GetString(Resource.String.storiesdisabled);
                    break;
                case ApiKeyException ex:
                    message = activity.GetString(Resource.String.apikeynovalid);
                    break;

                case NeedsPermissionsException ex:
                case NotGivenPermissionException exx:
                    message = activity.GetString(Resource.String.acceptperms);
                    break;

                case RoleNotSelectedException ex:
                    message = activity.GetString(Resource.String.norolechosen);
                    break;

                case NoNetworkException ex:
                    message = activity.GetString(Resource.String.errornonetwork);
                    break;

                case UnknownNetworkException ex:
                    message = activity.GetString(Resource.String.errorunknown);
                    break;

                case NeedsUpdateException ex:
                    message = activity.GetString(Resource.String.errornonetwork);
                    break;

                case SessionLostException ex:
                    message = activity.GetString(Resource.String.loginagain);
                    break;

                case ServerErrorException ex:
                    message = activity.GetString(Resource.String.errorserver);
                    break;

                case TaskCanceledException ex:
                    message = activity.GetString(Resource.String.errorcanceled);
                    break;
                default:
                    break;
            }

            Toast.MakeText(activity, message, ToastLength.Short).Show();
        }

        internal static void OpenLogin(Activity context,string provider)
        {
            if (string.IsNullOrEmpty(provider) && Bootlegger.BootleggerClient.CurrentUser!=null)
            {
                provider = Bootlegger.BootleggerClient.CurrentUser.profile["provider"].ToString();
            }

            var builder = new CustomTabsIntent.Builder()
           .SetToolbarColor(ContextCompat.GetColor(context,Resource.Color.blue))
           .SetSecondaryToolbarColor(Android.Resource.Color.White)
           .SetShowTitle(true);
            Bitmap icon;
            if (ViewCompat.GetLayoutDirection(context.FindViewById<ViewGroup>(Android.Resource.Id.Content).GetChildAt(0)) != ViewCompat.LayoutDirectionRtl)
                icon = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.ic_arrow_back_white_24dp);
            else
                icon = BitmapFactory.DecodeResource(context.Resources, Resource.Drawable.ic_arrow_forward_white_24dp);

            builder.SetCloseButtonIcon(icon);
            var intent = builder.Build();
            intent.Intent.PutExtra(Intent.ExtraReferrer, Android.Net.Uri.Parse("app://" + context.PackageName));
            intent.LaunchUrl(context, Android.Net.Uri.Parse(Bootlegger.BootleggerClient.LoginUrl.ToString() + "/" + provider + "?cbid=" + WhiteLabelConfig.DATASCHEME));
        }
    }
}