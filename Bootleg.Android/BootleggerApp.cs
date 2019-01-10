/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Bootleg.API;
using Square.Picasso;
using Java.Net;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using static Android.App.Application;
using Android.OS;
using System.Linq;
using static Square.Picasso.Picasso;
using BranchXamarinSDK;
using Plugin.CurrentActivity;
using Square.OkHttp;
using Android.Icu.Util;
using System.Net;
using Plugin.Connectivity;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Crashes;
using Microsoft.AppCenter.Analytics;

namespace Bootleg.Droid
{
#if DEBUG
    [Application(Debuggable = true)]
#else
    [Application(Debuggable = false)]
#endif
    [MetaData("io.branch.sdk.auto_link_disable", Value = "false")]
	[MetaData("io.branch.sdk.TestMode", Value = "true")]
	[MetaData("io.branch.sdk.BranchKey", Value = "@string/BRANCHKEY")]
    [MetaData("com.google.android.nearby.messages.API_KEY",Value = WhiteLabelConfig.GOOGLE_NEARBY_KEY)]
    public class BootleggerApp : Android.App.Application, IActivityLifecycleCallbacks, IListener
    {
        public static string LOG_TAG = WhiteLabelConfig.BUILD_VARIANT;

        public BootleggerApp(IntPtr handle, JniHandleOwnership transfer)
                        : base(handle, transfer)
        {
            //ConnectEventId = "";
            //ConnectEditId = "";

            RegisterActivityLifecycleCallbacks(this);
            ResetReturnState();
        }


        public ApplicationReturnState ReturnState { get; set; }

        public enum ReturnType {SIGN_IN_ONLY, CREATE_SHOOT, JOIN_CODE, OPEN_EDIT, OPEN_SHOOT, OPEN_UPLOAD}

        public void ResetReturnState()
        {
            ReturnState = new ApplicationReturnState()
            {
                ReturnsTo = ReturnType.SIGN_IN_ONLY
            };
        }

        public string eventtoconnectoafterloggingin = "";

        public class ApplicationReturnState
        {
            public ReturnType ReturnsTo { get; set; }
            public string Session { get; set; }
            public string Payload { get; set; }
        }


        public bool TOTALFAIL { get; set; }

        string GetExternalPath()
        {
            var dirs = ContextCompat.GetExternalFilesDirs(ApplicationContext, null);
            if (dirs.Count() == 1)
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).Path;
            else if (dirs.Last() != null)
                return dirs.Last().AbsolutePath;
            else
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).Path;
        }

        private void InitComms()
        {

            //_comms = new Bootlegger(WhiteLabelConfig.SERVER, WhiteLabelConfig.PORT, this.FilesDir.AbsolutePath, WhiteLabelConfig.APIKEY, WhiteLabelConfig.CBID);
            Bootlegger.Create(WhiteLabelConfig.SERVER, WhiteLabelConfig.PORT, this.FilesDir.AbsolutePath, WhiteLabelConfig.APIKEY, WhiteLabelConfig.CBID, GetExternalPath(), WhiteLabelConfig.LOCAL_LOGIN);
            Bootlegger.BootleggerClient.Language = Resources.Configuration.Locale.Language;
            _cache = new Square.OkHttp3.Cache(FilesDir, 300 * 1024 * 1024);
        }

        Square.OkHttp3.Cache _cache;

        public Square.OkHttp3.Cache FilesCache
        {
            get => _cache;
        }

        public void Start()
        {
            InitComms();

            //.IndicatorsEnabled(true).
            Picasso picasso = new Builder(this).
                //Downloader(new CookieImageDownloader(this, Bootlegger.BootleggerClient)).
                Downloader(new OkBlClient(this)).
                //IndicatorsEnabled(true).
                //LoggingEnabled(truer
                Listener(this).
                Build();

            try
            {
                SetSingletonInstance(picasso);
            }
            catch
            {
                //already singleton
            }

            Bootlegger.BootleggerClient.OnReportError += Comms_OnReportError;
            Bootlegger.BootleggerClient.OnSavePreferences += Comms_OnSavePreferences;
            Bootlegger.BootleggerClient.OnGlobalUploadProgress += Comms_OnGlobalUploadProgress;
            Bootlegger.BootleggerClient.OnCurrentUploadsComplete += Comms_OnCurrentUploadsComplete;
            Bootlegger.BootleggerClient.OnCurrentUploadsFailed += Comms_OnCurrentUploadsFailed;
        }

        class OkBlClient : OkHttpDownloader
        {
            CookieManager cm;
            public OkBlClient(Context context):base(context)
            {
                cm = new CookieManager();
                this.Client.SetCookieHandler(cm);
            }

            public override DownloaderResponse Load(Android.Net.Uri p0, int p1)
            {
                if (Bootlegger.BootleggerClient.SessionCookie != null)
                {
                    string cookieName = Bootlegger.BootleggerClient.SessionCookie.Name;
                    string cookieValue = Bootlegger.BootleggerClient.SessionCookie.Value;
                    cm.CookieStore.Add(new URI(Bootlegger.BootleggerClient.LoginUrl.ToString()), new HttpCookie(cookieName, cookieValue) { Domain = Bootlegger.BootleggerClient.LoginUrl.Host });
                    
                }

                try
                {
                    Console.WriteLine(p0);
                    return base.Load(p0, p1);
                }
                catch (System.Exception e)
                {
                    Console.WriteLine(e);
                    return null;
                }
            }

        }

        private void Comms_OnCurrentUploadsFailed()
        {
            if (uploadnotification != null)
            {
                NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                Intent resultIntent = new Intent(this, typeof(SplashActivity));
                Android.Support.V4.App.TaskStackBuilder stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
                stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(Video)));
                stackBuilder.AddNextIntent(resultIntent);

                PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

                uploadbuilder = new NotificationCompat.Builder(this)
                    .SetContentTitle(GetString(Resource.String.failedtoupload))
                    .SetContentIntent(resultPendingIntent)
                    .SetSmallIcon(Resource.Drawable.ic_notification)
                    .SetContentText(GetString(Resource.String.uploadfailshort)); // "Could not upload videos"
                uploadbuilder.SetChannelId(CHANNEL_ID);
                uploadnotification = uploadbuilder.Build();
                notificationManager.Notify(1, uploadnotification);
            }
            else
            {
                uploadnotification = null;
            }
        }

        public void ClearNotifications()
        {
            if (uploadnotification != null)
            {
                NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                notificationManager.CancelAll();
            }
        }

        private void Comms_OnReportError(System.Exception obj)
        {
            //Insights.Report(obj, Insights.Severity.Error);
        }

        void Comms_OnSavePreferences(string arg1, string arg2)
        {
            var allprefs = GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
            //var prefs = PreferenceManager.GetDefaultSharedPreferences(loginscreen).Edit();
            var prefs = allprefs.Edit();
            prefs.PutString(arg1, arg2);
            prefs.Apply();
        }

        public Java.Net.CookieManager cookieManager { get; set; }

        public class CookieImageDownloader : UrlConnectionDownloader
        {
            Bootlegger comms;
            public CookieImageDownloader(Context p0, Bootlegger comms) : base(p0)
            {
                this.comms = comms;
            }

            protected override HttpURLConnection OpenConnection(Android.Net.Uri p0)
            {
                HttpURLConnection conn = base.OpenConnection(p0);
                if (comms.SessionCookie != null)
                {
                    string cookieName = comms.SessionCookie.Name;
                    string cookieValue = comms.SessionCookie.Value;
                    conn.SetRequestProperty("Cookie", cookieName + "=" + cookieValue);
                }
                return conn;
            }

        }

        public override void OnCreate ()
        {
            base.OnCreate();
            //Firebase.FirebaseApp.InitializeApp(this);
            CrossCurrentActivity.Current.Init(this);

            if (!string.IsNullOrWhiteSpace(WhiteLabelConfig.BRANCHKEY))
                BranchAndroid.GetAutoInstance(ApplicationContext);

            //CrashManager.Register(ApplicationContext, WhiteLabelConfig.HOCKEYAPPID);
            //MetricsManager.Register(this,WhiteLabelConfig.HOCKEYAPPID);
            //FeedbackManager.Register(ApplicationContext, WhiteLabelConfig.HOCKEYAPPID);
            //FeedbackManager.CheckForAnswersAndNotify(ApplicationContext);
            AppCenter.Start(WhiteLabelConfig.HOCKEYAPPID, typeof(Analytics), typeof(Crashes));
            

            RegisterActivityLifecycleCallbacks(this);

            if ((int)Build.VERSION.SdkInt > 25)
            {

                NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                NotificationChannel channel = new NotificationChannel(CHANNEL_ID, "Our Story", NotificationImportance.Default);
                channel.Description = "Our Story";
                notificationManager.CreateNotificationChannel(channel);
            }

        }

        public override void OnTerminate()
        {
            base.OnTerminate();
            UnregisterActivityLifecycleCallbacks(this);
        }


        private void Comms_OnCurrentUploadsComplete()
        {
            if (uploadnotification != null)
            {
                NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                Intent resultIntent = new Intent(this, typeof(SplashActivity));
                Android.Support.V4.App.TaskStackBuilder stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
                stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(Video)));
                stackBuilder.AddNextIntent(resultIntent);

                PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

                uploadbuilder = new NotificationCompat.Builder(this)
                    .SetContentTitle(Resources.GetString(Resource.String.uploadcomplete))
                    .SetContentIntent(resultPendingIntent)
                    .SetSmallIcon(Resource.Drawable.ic_notification)
                    .SetContentText(Resources.GetString(Resource.String.alluploadsdone));
                uploadbuilder.SetChannelId(CHANNEL_ID);
                uploadnotification = uploadbuilder.Build();
                notificationManager.Notify(1, uploadnotification);
            }
            else
            {
                uploadnotification = null;
            }
        }

        public bool IsReallyConnected
        {
            get
            {
                if (WhiteLabelConfig.LOCAL_SERVER)
                {
                    return CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi);
                }
                else
                    return CrossConnectivity.Current.IsConnected;
            }
        }

        Notification uploadnotification;
        public const string CHANNEL_ID = "bootlegger_channel_1";
        NotificationCompat.Builder uploadbuilder;
        private void Comms_OnGlobalUploadProgress(double arg1, int arg2, int arg3)
        {
            if (Bootlegger.BootleggerClient.CanUpload)
            {
                if (uploadnotification == null)
                {
                    Intent resultIntent = new Intent(this, typeof(SplashActivity));
                    Android.Support.V4.App.TaskStackBuilder stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
                    stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(Uploads)));
                    stackBuilder.AddNextIntent(resultIntent);

                    PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);

                    uploadbuilder = new NotificationCompat.Builder(this)
                        .SetContentTitle(Resources.GetString(Resource.String.uploadingclips))
                        .SetContentIntent(resultPendingIntent)
                        .SetSmallIcon(Resource.Drawable.ic_notification)
                        .SetProgress(100, 0, false)
                        .SetContentText(Resources.GetString(Resource.String.uploadprogress, arg2, arg3));
                    uploadbuilder.SetOngoing(true);
                    uploadbuilder.SetAutoCancel(false);
                    uploadbuilder.SetChannelId(CHANNEL_ID);
                }
                else
                {
                    uploadbuilder.SetContentText(Resources.GetString(Resource.String.uploadprogress, arg2, arg3));
                    uploadbuilder.SetProgress(100, (int)(arg1 * 100), false);
                    uploadbuilder.SetChannelId(CHANNEL_ID);
                }

                NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
                uploadnotification = uploadbuilder.Build();
                notificationManager.Notify(1, uploadnotification);

                //NotificationChannel mChannel = new NotificationChannel(id, name, importance);
            }
        }

        public void OnActivityCreated(Activity activity, Bundle savedInstanceState)
        {
            //CrossCurrentActivity.Current.Activity = activity;
            try
            {
                Bootlegger.BootleggerClient.OnResume();
            }
            catch
            {
                InitComms();
                Bootlegger.BootleggerClient.OnResume();
            }
        }

        public void OnActivityDestroyed(Activity activity)
        {
        }

        public void OnActivityPaused(Activity activity)
        {
            Bootlegger.BootleggerClient.OnPause();
            
        }

        public void OnActivityResumed(Activity activity)
        {
            //Bootlegger.TESTKILL();
            //CrossCurrentActivity.Current.Activity = activity;

        }

        public void OnActivitySaveInstanceState(Activity activity, Bundle outState)
        {
        }

        public void OnActivityStarted(Activity activity)
        {
            //CrossCurrentActivity.Current.Activity = activity;
        }

        public void OnActivityStopped(Activity activity)
        {
            
        }

        public void OnImageLoadFailed(Picasso p0, Android.Net.Uri p1, Java.Lang.Exception p2)
        {
            //Console.WriteLine(p2);
        }

        //public string loginsession { get; set; }

        //public bool OPENUPLOAD { get; set; }

        //public string UploadEventId { get; set; }

        public string ADVERT { get; set; }

        //public string ConnectEventId { get; internal set; }
        //public string ConnectEditId { get; internal set; }
        //public string JoinCode { get; internal set; }
    }
}