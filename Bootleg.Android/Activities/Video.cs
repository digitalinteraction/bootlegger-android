/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Android.Media;
using Bootleg.API;
using Android.Graphics;
using Android.Util;
using System.Threading.Tasks;
using Android.Views.Animations;
using Android.Content.Res;
using System.Timers;
using System.IO;
using Android.Support.V4.App;
using Android.Support.V4.View;
using ViewPagerIndicator;
using System.Text.RegularExpressions;
using Android.Content.PM;
using RadialProgress;
using System.Diagnostics;
using Bootleg.Droid.UI;
using static Bootleg.Droid.UI.PermissionsDialog;
using Square.Picasso;
using Bootleg.Droid.Fragments;
using Newtonsoft.Json;
using Android.Support.V4.Content;
using Android.Hardware.Camera2;
using Bootleg.Droid.Util;
using Plugin.Permissions;
using Android.Support.V7.Widget;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    [Activity(ConfigurationChanges = ConfigChanges.Orientation | ConfigChanges.ScreenSize, ScreenOrientation = ScreenOrientation.UserLandscape, LaunchMode = LaunchMode.SingleTask,ResizeableActivity = false)]
    public class Video : FragmentActivity, IFragmentController, Android.Widget.ViewSwitcher.IViewFactory
    {

        public override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
        }

        public override bool OnCreateOptionsMenu(IMenu menu)
        {
            return false;
        }

        public override bool OnPrepareOptionsMenu(IMenu menu)
        {
            return false;
        }

        MediaRecorder audioRecorder;
        bool recording;
        Java.IO.File path;

        Animation pulse;
        Timer shotlength;

        Stopwatch stopwatch;

        DateTime lastrecordtime = DateTime.Now;

        DateTime startedrecordingtime;

        string GetExternalPath()
        {
            var dirs = ContextCompat.GetExternalFilesDirs(this, null);
            if (dirs.Count() == 1)
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).Path;
            else if (dirs.Last() != null)
                return dirs.Last().AbsolutePath;
            else
                return Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryMovies).Path;
        }

        bool CheckSpace(string dir)
        {
            //var dir = Android.OS.Environment.(Android.OS.Environment.sec).Path;
            //Directory.CreateDirectory(dir);


            StatFs stat = new StatFs(dir);
            long bytesAvailable = stat.BlockSizeLong * stat.AvailableBlocksLong;
            long megAvailable = bytesAvailable / (1024*1024);
            if (megAvailable < 300)
            {
                //throw error:
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogStyle);
                builder.SetMessage(Resource.String.nospace);
                builder.SetNeutralButton(Android.Resource.String.Ok, (o, q) =>
                {
                    Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH, true);
                    Finish();
                    //Intent i = new Intent(this.ApplicationContext, typeof(Login));
                    //StartActivity(i);
                });
                builder.SetCancelable(false);
                try
                {
                    builder.Show();
                }
                catch
                {
                    //cannot do anything about this...
                }
                return false;
            }
            else
            {
                return true;
            }
        }


        void StartRecording()
        {
            
            var testfile = new Java.IO.File(GetExternalPath());
            if(CheckSpace(testfile.AbsolutePath))
            {
                progress_view.ProgressColor = Color.Tomato;
                FindViewById<View>(Resource.Id.recordlight).StartAnimation(pulse);
                FindViewById<View>(Resource.Id.recordlight).Visibility = ViewStates.Visible;

                //System.Console.WriteLine("starting record");
                stopwatch = new Stopwatch();
                stopwatch.Start();
                startedrecordingtime = DateTime.Now;
                recording = true;
                //recordlength = 0;
                FindViewById<ToggleButton>(Resource.Id.Play).Checked = true;
                //if (camera == null)
                //    camera = Android.Hardware.Camera.Open();

                if (Android.OS.Environment.ExternalStorageState != Android.OS.Environment.MediaMounted)
                {
                    Toast.MakeText(this, Resource.String.nostorage, ToastLength.Short);
                }
                testfile = new Java.IO.File(testfile, "bootlegger");

                if (!testfile.Exists())
                {
                    if (!testfile.Mkdirs())
                    {
                        //major error!!
                        Log.Error("BL", "Directory not created (might not be needed)");
                    }
                }

                //new filename:
                Java.IO.File file;

                //fix for if this phone has taken over someone else's account and thus does not have a client or server side shot allocation:
                Shot.ShotTypes shotype;

                try
                {
                    shotype = (Bootlegger.BootleggerClient.CurrentClientShotType != null)
                        ? Bootlegger.BootleggerClient.CurrentClientShotType.shot_type
                        : Bootlegger.BootleggerClient.CurrentServerShotType.shot_type;
                }
                catch
                {
                    shotype = Shot.ShotTypes.VIDEO;
                }

                switch (shotype)
                {

                    case Shot.ShotTypes.PHOTO:
                        file = new Java.IO.File(testfile, DateTime.Now.Ticks + ".jpg");
                        path = file;
                        thumbnailfilename = file.ToString();
                        progress_view.Visibility = ViewStates.Gone;
                        cameraDriver.TakePhoto(thumbnailfilename);
                        break;

                    case Shot.ShotTypes.AUDIO:
                        file = new Java.IO.File(testfile, DateTime.Now.Ticks + ".aac");
                        path = file;
                        file.CreateNewFile();
                        if (audioRecorder != null)
                        {
                            audioRecorder.Reset();
                        }
                        else
                        {
                            audioRecorder = new MediaRecorder();
                        }
                        //recorder.SetVideoSource(null);
                        audioRecorder.Reset();
                        audioRecorder.SetAudioSource(AudioSource.Mic);
                        audioRecorder.SetOutputFormat(OutputFormat.AacAdts);
                        audioRecorder.SetOutputFile(file.AbsolutePath);
                        audioRecorder.SetAudioEncoder(AudioEncoder.Aac);
                        audioRecorder.SetMaxDuration(1000*60*5);
                        audioRecorder.SetPreviewDisplay(null);

                        StartLocationTrack();

                        try
                        {
                            audioRecorder.Prepare();
                        }
                        catch (Exception)
                        {

                        }
                        try
                        {
                            audioRecorder.Start();
                            pulse = AnimationUtils.LoadAnimation(this, Resource.Animation.pulse);
                            FindViewById<View>(Resource.Id.recordlight).StartAnimation(pulse);
                            FindViewById<View>(Resource.Id.recordlight).Visibility = ViewStates.Visible;
                            if (!WhiteLabelConfig.REDUCE_BANDWIDTH)
                            {
                                Bootlegger.BootleggerClient.RecordingStarted();
                            }

                            progress_view.Value = 0;
                            FindViewById<TextView>(Resource.Id.timestamp).Text = "00:00";
                        }
                        catch (Exception)
                        {
                            //recording failed:
                            recording = false;
                            FindViewById<ToggleButton>(Resource.Id.Play).Checked = false;
                        }
                        break;

                    case Shot.ShotTypes.VIDEO:
                        file = new Java.IO.File(testfile, DateTime.Now.Ticks + ".mp4");

                        file.CreateNewFile();
                        path = file;

                        StartLocationTrack();

                        try
                        {
                            cameraDriver.StartRecord(path.AbsolutePath);

                            Bootlegger.BootleggerClient.RecordingStarted();


                            progress_view.Value = 0;
                            FindViewById<TextView>(Resource.Id.timestamp).Text = "00:00";
                        }
                        catch (Exception)
                        {
                            //recording failed:
                            recording = false;
                            stopwatch.Stop();
                            FindViewById<ToggleButton>(Resource.Id.Play).Checked = false;
                        }
                        break;
                } //end switch
            }//end space check
        }

        private void StartLocationTrack()
        {
            if (!disablelocation)
            {
                if (WhiteLabelConfig.GPS_RECORD_INTERVAL_SECS > TimeSpan.Zero)
                {
                    Plugin.Geolocator.CrossGeolocator.Current.StopListeningAsync();
                    Plugin.Geolocator.CrossGeolocator.Current.StartListeningAsync(WhiteLabelConfig.GPS_RECORD_INTERVAL_SECS, 10, true);
                }
            }
        }

        private void StopLocationTrack()
        {
            Plugin.Geolocator.CrossGeolocator.Current.StopListeningAsync();
            //Plugin.Geolocator.CrossGeolocator.Current.StartListeningAsync(5000, 10);
        }

        async Task StopRecording()
        {
            Bitmap bitmap = null;
            Shot.ShotTypes shotype;
            try
            {
                shotype = (Bootlegger.BootleggerClient.CurrentClientShotType != null) ? Bootlegger.BootleggerClient.CurrentClientShotType.shot_type : Bootlegger.BootleggerClient.CurrentServerShotType.shot_type;
            }
            catch
            {
                shotype = Shot.ShotTypes.VIDEO;
            }
            //var shotype = ((Application as BootleggerApp).Comms.CurrentClientShotType != null) ? (Application as BootleggerApp).Comms.CurrentClientShotType.shot_type : (Application as BootleggerApp).Comms.CurrentServerShotType.shot_type;

            switch (shotype)
            {
                case Shot.ShotTypes.AUDIO:
                    //stop audio recorder:
                    try
                    {
                        //crash here...
                        audioRecorder.Stop();
                        audioRecorder.Reset();
                    }
                    catch (Exception)
                    {

                    }

                    break;

                case Shot.ShotTypes.PHOTO:
                    //do nothing...
                    //bitmap = await BitmapFactory.DecodeFileAsync(thumbnailfilename);

                    //resize bitmap:
                    //create thumbnailed version of the jpg

                    break;

                default:
                    try
                    {
                        //crash here...
                        cameraDriver.StopRecord();
                    }
                    catch (Exception)
                    {

                    }

                    //add thumb
                    bitmap = await ThumbnailUtils.CreateVideoThumbnailAsync(path.AbsolutePath, Android.Provider.ThumbnailKind.MiniKind);
                    break;
            }

            //display the shot selection screen if needed:
            FindViewById<ToggleButton>(Resource.Id.Play).Checked = false;
            recording = false;
            progress_view.Value = 0;
            if (LIVEMODE)
                progress_view.ProgressColor = Color.DodgerBlue;

            FindViewById<TextView>(Resource.Id.timestamp).Text = "00:00";
            progress_view.Visibility = ViewStates.Visible;
            stopwatch.Stop();

            StopLocationTrack();

            if (!LIVEMODE)
            {
                if (!WhiteLabelConfig.SHOW_ALL_SHOTS)
                {
                    //return to role selection:
                    FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                    
                    FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Visible;
                    //set role selection tab:
                }
                else
                {
                    FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                }
            }

            if (!WhiteLabelConfig.REDUCE_BANDWIDTH)
            {
                Bootlegger.BootleggerClient.RecordingStopped();
            }

            if (shotype != Shot.ShotTypes.PHOTO)
            {
                pulse.AnimationEnd += pulse_AnimationEnd;
                pulse.Cancel();
                FindViewById<View>(Resource.Id.recordlight).ClearAnimation();
                FindViewById<View>(Resource.Id.recordlight).Post(new Action(() =>
                {
                    FindViewById<View>(Resource.Id.recordlight).Visibility = ViewStates.Invisible;
                }));
            }


            progress_view.Value = 0;
            FindViewById<TextView>(Resource.Id.timestamp).Text = "00:00";

            //create new media item
            MediaItem newm = new MediaItem();
            newm.Filename = path.AbsolutePath;
            if (Bootlegger.BootleggerClient.CurrentClientShotType != null)
                newm.ShortName = Bootlegger.BootleggerClient.CurrentClientShotType.name;
            else
                newm.ShortName = newm.Filename.Split('/').Last().Split('.').Reverse().Skip(1).First();
            //add geo data
            var mygeo = new Dictionary<string, string>();
            if (Plugin.Geolocator.CrossGeolocator.Current.IsGeolocationAvailable && Plugin.Geolocator.CrossGeolocator.Current.IsGeolocationEnabled && currentposition != null)
            {
                mygeo.Add("gps_lat", currentposition.Latitude.ToString());
                mygeo.Add("gps_lng", currentposition.Longitude.ToString());
                mygeo.Add("gps_head", currentposition.Heading.ToString());
                mygeo.Add("gps_acc", currentposition.Accuracy.ToString());
                mygeo.Add("gps_alt", currentposition.Altitude.ToString());
                mygeo.Add("gps_spd", currentposition.Speed.ToString());
            }

            mygeo.Add("clip_length", stopwatch.Elapsed.ToString());
            mygeo.Add("camera", (CURRENTCAMERA == 0) ? "rear" : "front");

            mygeo.Add("device_os", Build.VERSION.SdkInt.ToString());
            mygeo.Add("device_hardware", Build.Manufacturer);
            mygeo.Add("device_hardware_ver", Build.Model);



            if (cameraDriver.ZoomLevels.Count < FindViewById<SeekBar>(Resource.Id.zoom).Progress)
                mygeo.Add("zoom", (cameraDriver.ZoomLevels[FindViewById<SeekBar>(Resource.Id.zoom).Progress].IntValue()/100.0).ToString());

            mygeo.Add("captured_at", startedrecordingtime.ToString("dd/MM/yyyy H:mm:ss.ff tt zz"));

            string filename = path.AbsolutePath + ".jpg";

            if (shotype == Shot.ShotTypes.PHOTO)
            {
                filename = thumbnailfilename;
            }

            FileStream outt;

            if (bitmap != null)
            {
                bool dothumb = false;
                try
                {
                    outt = new FileStream(filename, FileMode.CreateNew);
                    await bitmap.CompressAsync(Android.Graphics.Bitmap.CompressFormat.Jpeg, 90, outt);
                    outt.Close();
                    dothumb = true;
                }
                catch (Exception)
                {

                }
                if (!dothumb)
                    filename = null;
            }

            var m = Bootlegger.BootleggerClient.CreateMediaMeta(newm, mygeo, null, filename);


            //HACK: does this stop the server getting the information it needs??
            if (!LIVEMODE && !WhiteLabelConfig.REDUCE_BANDWIDTH)
            {
                try
                {
                    await Bootlegger.BootleggerClient.UploadMedia(m);
                }
                catch
                {
                    //if no internet etc, then cant do this right now...
                }
            }

            //update upload gui
            hasrecorded = true;
            recording = false;
            progress_view.Value = 0;
        }

        async Task record()
        {
          
            if (!recording && canrecord)
            {
                //cant record within 4 seconds of last record time...
                if ((DateTime.Now - lastrecordtime).TotalMilliseconds < 4000)
                {
                    FindViewById<ToggleButton>(Resource.Id.Play).Checked = false;
                    return;
                }
                else
                {
                    lastrecordtime = DateTime.Now;
                    StartRecording();

                    //DISABLE ROLE SELECTION
                    //_adapter.myrolefrag.View.Enabled = false;
                }   
            }
            else if (recording)
            {
                //disable the button:
                FindViewById<ToggleButton>(Resource.Id.Play).Enabled = false;
                //stop recording

                await StopRecording();
                
                FindViewById<ToggleButton>(Resource.Id.Play).Enabled = true;
            }
        }

        //long recordlength;
        long waitinglength;
        bool hasrecorded = false;

        void shotlength_Elapsed(object sender, ElapsedEventArgs e)
        {
            //stop recording now max_length reached.
            if (!LIVEMODE && recording && (Bootlegger.BootleggerClient.CurrentClientShotType != null && Bootlegger.BootleggerClient.CurrentClientShotType.max_length != 0))
            {
                if (stopwatch.Elapsed.TotalSeconds >= Bootlegger.BootleggerClient.CurrentClientShotType.max_length)
                {
                    RunOnUiThread(() =>
                    {
                        record();
                    });
                }
            }

            //STOP RECORDING IF THE SERVER FOR SOME REASON HAS NOT STOPPED YOU ALREADY IN LIVE MODE AFTER 2 MINS5
            if (recording && LIVEMODE && stopwatch.Elapsed.TotalSeconds > 60*2)
            {
                RunOnUiThread(() =>
                {
                    record();
                });
            }

            RunOnUiThread(() =>
            {
                //if recording...
                if (recording)
                {
                    waitinglength = 0;
                    if (expectedrecordlength > 0)
                    {
                        progress_view.Visibility = ViewStates.Visible;
                        progress_view.Value = (int)(((stopwatch.Elapsed.TotalSeconds) / (double)expectedrecordlength) * 100);
                    }
                    else
                    {
                        if (Bootlegger.BootleggerClient.CurrentClientShotType != null && Bootlegger.BootleggerClient.CurrentClientShotType.max_length != 0)
                        {
                            //if it has a max length
                            progress_view.Visibility = ViewStates.Visible;
                            progress_view.Value = (int)(((stopwatch.Elapsed.TotalSeconds) / (double)Bootlegger.BootleggerClient.CurrentClientShotType.max_length) * 100);

                        }
                    }

                    //TODO: i18n timestamp
                    FindViewById<TextView>(Resource.Id.timestamp).Text = stopwatch.Elapsed.Minutes.ToString().PadLeft(2, '0') + ":" + stopwatch.Elapsed.Seconds.ToString().PadLeft(2, '0');
                    //recordlength += 1000;
                }
                else
                {
                    try
                    {
                        if (Bootlegger.BootleggerClient.CurrentEvent!=null && Bootlegger.BootleggerClient.CurrentEvent.HasStarted && hasrecorded && !Bootlegger.BootleggerClient.CurrentEvent.offline && !LIVEMODE)
                        {
                            FindViewById<View>(Resource.Id.recordlight).Visibility = ViewStates.Invisible;
                            progress_view.Visibility = ViewStates.Visible;
                            progress_view.Value = (int)((((double)waitinglength) / ((double)Bootlegger.BootleggerClient.CurrentUser.CycleLength)) * 100);
                            waitinglength += 1;
                        }
                    }
                    catch
                    {

                    }
                    finally
                    {
                        FindViewById<View>(Resource.Id.recordlight).Visibility = ViewStates.Invisible;
                    }
                }
            });
        }

        void pulse_AnimationEnd(object sender, Animation.AnimationEndEventArgs e)
        {
            FindViewById<View>(Resource.Id.recordlight).Post(new Action(() => { 
                FindViewById<View>(Resource.Id.recordlight).Visibility = ViewStates.Invisible; 
            }));
        }

        //SurfaceView video;
        bool canrecord = false;

        void start()
        {
            var play = FindViewById<ToggleButton>(Resource.Id.Play);           

            play.Click += delegate
            {
                record();
            };
            canrecord = true;

            progress_view.Value = 0;
            FindViewById<TextView>(Resource.Id.timestamp).Text = "00:00";

            // setup timer for countdown display
            if (shotlength != null)
                shotlength.Stop();
            shotlength = new Timer(1000);
            shotlength.Elapsed += shotlength_Elapsed;
            shotlength.Start();
        }

        //DrawerLayout tabHost;
        private TextSwitcher mSwitcher;
        ShotPageAdapter mAdapter;
        //ShotAdapter allshots;
        ViewPager _pager;
        bool firstrun;
        bool PermissionsGranted = false;

        protected async override void OnStart()
        {
            base.OnStart();

            Bootlegger.BootleggerClient.OnRoleChanged += Comms_OnRoleChanged;
            Bootlegger.BootleggerClient.OnMessage += Comms_OnMessage;
            Bootlegger.BootleggerClient.OnPhaseChange += Comms_OnPhaseChange;
            Bootlegger.BootleggerClient.OnEventUpdated += Comms_OnEventUpdated;
            Bootlegger.BootleggerClient.OnImagesUpdated += Comms_OnImagesUpdated;
            Bootlegger.BootleggerClient.OnPermissionsChanged += Comms_OnPermissionsChanged;
            Bootlegger.BootleggerClient.OnLoginElsewhere += Comms_OnLoginElsewhere;

            //download image files for the event:
            if (!firstrun)
            {
                FindViewById<FrameLayout>(Resource.Id.initprogress).Visibility = ViewStates.Visible;
                if ((Application as BootleggerApp).IsReallyConnected)
                {
                    try
                    {
                        //throw new Exception("test");
                        await Bootlegger.BootleggerClient.GetImages();
                    }
                    catch (Exception e)
                    {
                        //cant do this -- so close application!
                        Log.Error("Bootlegger", e.Message);
                        if (e.InnerException != null)
                            Log.Error("Bootlegger", e.InnerException.Message);

                        RunOnUiThread(new Action(() =>
                        {
                            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogStyle);
                            builder.SetMessage(Resources.GetString(Resource.String.tryagain, e.Message));
                            builder.SetNeutralButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>((o, q) =>
                            {
                                //(Application as BootleggerApp).TOTALFAIL = true;
                                Finish();
                                return;
                            }));
                            builder.SetCancelable(false);
                            try
                            {
                                builder.Show();
                            }
                            catch
                            {
                                //cannot do anything about this...
                            }

                        }));
                    }
                    finally
                    {
                        firstrun = true;
                    }
                }
            }

            _pager = null;

            try
            {
                if (shotselector == null)
                {
                    //add shot selector to panel:
                    var role = Bootlegger.BootleggerClient.CurrentClientRole;
                    shotselector = new ShotSelectAdapter(this);
                    shotselector.OnShotSelected += SelectorClick;


                    FindViewById<RecyclerView>(Resource.Id.shotselectorlist).SetLayoutManager(new GridLayoutManager(this, 3));
                    FindViewById<RecyclerView>(Resource.Id.shotselectorlist).SetAdapter(shotselector);

                    shotselector.UpdateData(Bootlegger.BootleggerClient.CurrentClientRole.Shots);

                    //add role fragment to shot panel:
                    if (!WhiteLabelConfig.SHOW_ALL_SHOTS)
                    {

                        FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Gone;
                    }
                }
            }
            catch { }


            //LIVE MODE
            if (LIVEMODE)
            {
                try
                {
                    if (mAdapter == null)
                    {
                        //setup pager for demo images:
                        mAdapter = new ShotPageAdapter(SupportFragmentManager, Bootlegger.BootleggerClient.CurrentClientRole.Shots);
                        ViewPager mPager = FindViewById<ViewPager>(Resource.Id.pager);
                        mPager.Adapter = mAdapter;
                        var indicator = FindViewById<CirclePageIndicator>(Resource.Id.indicator);
                        indicator.SetViewPager(mPager);
                        indicator.SetSnap(true);
                    }
                }
                catch { }
            }

            //hide progress            
            FindViewById<FrameLayout>(Resource.Id.initprogress).Visibility = ViewStates.Invisible;

            if (!ViewConfiguration.Get(this).HasPermanentMenuKey && !KeyCharacterMap.DeviceHasKey(Keycode.Back))
            {
                var padding_right = 0;
                var navbardim = Resources.GetIdentifier("navigation_bar_height", "dimen", "android");
                if (navbardim > 0)
                {
                    padding_right = Resources.GetDimensionPixelSize(navbardim);
                    //var param = FindViewById(Resource.Id.allbuttons).LayoutParameters.DeepCopy();

                    (FindViewById<LinearLayout>(Resource.Id.allbuttons).LayoutParameters as LinearLayout.MarginLayoutParams).RightMargin = padding_right;
                    (FindViewById<FrameLayout>(Resource.Id.recordbtnwrapper).LayoutParameters as LinearLayout.MarginLayoutParams).RightMargin = padding_right;
                }

                FindViewById<TextView>(Resource.Id.overlaytext).SetPadding(0, Utils.dp2px(this, 6), Utils.dp2px(this, 80), 0);
            }

            Bootlegger.BootleggerClient.OnModeChange += Comms_OnModeChange;

            //fix for camera 1 api:
            try
            {
                FindViewById<View>(Resource.Id.camera_preview).Post(() =>
                {
                    FindViewById<View>(Resource.Id.camera_preview).RequestLayout();
                });
            }
            catch
            {
                //handle camera not created yet (permissions not met)
            }

            //if its in role select mode and coming from the roles selection screen, show the shots rather than the roles:

            if (from_roles ?? false && !WhiteLabelConfig.SHOW_ALL_SHOTS)
            {
                FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Gone;
            }

        }

        private void _adapter_OnRoleChanged()
        {
            //can only do this if not recording:
            if (!recording)
            {
                shotselector.UpdateData(Bootlegger.BootleggerClient.CurrentClientRole.Shots);
                if (!LIVEMODE)
                    FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
            }
            else
            {
                //Toast.MakeText(this,Resource.String.norolechange,ToastLength.Short).Show();
            }
        }

        ShotSelectAdapter shotselector;
        //VideoPagerAdapter _adapter;
        CleanRadialProgressView progress_view;

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Android.Content.PM.Permission[] grantResults)
        {
            Plugin.Permissions.PermissionsImplementation.Current.OnRequestPermissionsResult(requestCode, permissions, grantResults);
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            RequestedOrientation = ScreenOrientation.Landscape;

            Bootlegger.BootleggerClient.InBackground = false;

            SetTheme(Resource.Style.Theme_Normal);

            Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.Fullscreen | SystemUiFlags.LayoutStable);

            RequestWindowFeature(WindowFeatures.NoTitle);
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            Window.AddFlags(WindowManagerFlags.KeepScreenOn);
            Window.AddFlags(WindowManagerFlags.LayoutNoLimits);

            SetContentView(Resource.Layout.Video_Main);

            progress_view = FindViewById<CleanRadialProgressView>(Resource.Id.progress);

            progress_view.ProgressColor = Color.Tomato;


            start();

            //show progress
            Bootlegger.BootleggerClient.OnImagesDownloading += (o) =>
            {
                RunOnUiThread(new Action(() =>
                {
                    FindViewById<ProgressBar>(Resource.Id.initprogressbar).Progress = o;
                })); ;
            };

            FindViewById<ImageButton>(Resource.Id.goback).Click += Fin_Click;

            FindViewById<ImageButton>(Resource.Id.switchcam).Click += Cam_Switch;


            FindViewById<ImageView>(Resource.Id.overlayimg).SetAlpha(120);


            Bootlegger.BootleggerClient.OnNotification += Comms_OnNotification;


            if (ApplicationContext.PackageManager.HasSystemFeature(PackageManager.FeatureCameraFlash) && WhiteLabelConfig.USE_FLASH)
            {
                FindViewById<ToggleButton>(Resource.Id.flash).Click += FlashToggle_Click;
            }
            else
            {
                FindViewById<ToggleButton>(Resource.Id.flash).Visibility = ViewStates.Gone;
            }

            FindViewById<Button>(Resource.Id.openhelp).Click += HelpOpen_Click;

            if (!WhiteLabelConfig.EXTERNAL_LINKS)
                FindViewById(Resource.Id.openhelp).Visibility = ViewStates.Gone;

            FindViewById<ImageButton>(Resource.Id.Help).Click += Help_Click;

            FindViewById(Resource.Id.helpoverlay).Click += HelpOverlay_Click;

            FindViewById<ToggleButton>(Resource.Id.showoverlay).CheckedChange += Video_CheckedChange;

            if (!WhiteLabelConfig.SHOW_ALL_SHOTS)
            {
                FindViewById<ImageButton>(Resource.Id.switchrole).Visibility = ViewStates.Gone;
            }
            FindViewById<ImageButton>(Resource.Id.switchrole).Click += Video_Click;


            mSwitcher = FindViewById<TextSwitcher>(Resource.Id.message);

            // Set the factory used to create TextViews to switch between.
            mSwitcher.SetFactory(this);

            mSwitcher.SetInAnimation(this, Android.Resource.Animation.FadeIn);
            mSwitcher.SetOutAnimation(this, Android.Resource.Animation.FadeOut);

            FindViewById(Resource.Id.messageholder).Visibility = ViewStates.Invisible;
            var msg = FindViewById<TextSwitcher>(Resource.Id.message);


            FindViewById<SeekBar>(Resource.Id.zoom).ProgressChanged += Video_ProgressChanged;

            Bootlegger.BootleggerClient.CanUpload = false;

            Plugin.Geolocator.CrossGeolocator.Current.DesiredAccuracy = 10;
            Plugin.Geolocator.CrossGeolocator.Current.PositionChanged += geo_PositionChanged;


            ScreenOrientationEvent orientation = new ScreenOrientationEvent(this);
            orientation.ShowWarning += orientation_ShowWarning;
            orientation.Enable();

            FindViewById<ImageButton>(Resource.Id.closeshots).Click += CloseShots_Click;

            //White Label Text Size
            if (WhiteLabelConfig.LARGE_SHOT_FONT)
                FindViewById<TextView>(Resource.Id.overlaytext).SetTextSize(ComplexUnitType.Sp, 40);

            if (savedInstanceState == null)
            {
                selectrolefrag = new SelectRoleFrag(Bootlegger.BootleggerClient.CurrentEvent, true);
                Android.Support.V4.App.FragmentTransaction ft = SupportFragmentManager.BeginTransaction();
                ft.Add(Resource.Id.roleselector, selectrolefrag, "arolefragment").Commit();
            }
            else
            {
                selectrolefrag = SupportFragmentManager.FindFragmentByTag("arolefragment") as SelectRoleFrag;
            }

            selectrolefrag.OnRoleChanged += () =>
            {
                FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Gone;
                _adapter_OnRoleChanged();
            };
        }

        private void Video_Click(object sender, EventArgs e)
        {
            //OPEN CHANGE ROLE PANEL:
            _pager.SetCurrentItem(0, false);
        }

        private void Comms_OnLoginElsewhere()
        {
            //logged in elsewhere...
            RunOnUiThread(new Action(() =>
            {
                if (recording)
                {
                    record();
                }

                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogStyle);
                builder.SetMessage(Resource.String.loggedinelsewhere);
                builder.SetNeutralButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>((o, q) =>
                {
                    Finish();
                    Bootlegger.BootleggerClient.CurrentClientRole = null;
                }));
                builder.SetCancelable(false);
                try
                {
                    builder.Show();
                }
                catch
                {
                    //cannot do anything about this...
                }

            }));

        }

        private void Comms_OnPermissionsChanged()
        {
            RunOnUiThread(async () =>
            {
            try
            {
                await AskPermissions(this, Bootlegger.BootleggerClient.CurrentEvent, true);
            }
            catch (NotGivenPermissionException)
            {
                //not accepted the permissions, so remove from the event:
                new Android.Support.V7.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogStyle).SetMessage(Resource.String.finishshooting)
                .SetCancelable(false)
                .SetTitle(Resource.String.permschanged)
                .SetMessage(Resource.String.notacceptperms)
                .SetPositiveButton(Resource.String.iunderstand, (o,e) => {
                    ShowLeaveDialog(true);
                })
                .Show();
                }
            });
        }

        private void Comms_OnImagesUpdated()
        {
            //do a notify
            RunOnUiThread(() =>
            {
                shotselector?.UpdateData(Bootlegger.BootleggerClient.CurrentClientRole.Shots);
            });
        }

        private void Video_Touch(object sender, View.TouchEventArgs e)
        {
            e.Handled = true;
        }

        private void Leave_Click(object sender, EventArgs e)
        {
            ShowLeaveDialog(false);
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            //change page:
            _pager.CurrentItem = 2;
        }

        private void AllShots_Click(object sender, EventArgs e)
        {
            //change page:
            _pager.CurrentItem = 1;
        }

        private void ChangeRole_Click(object sender, EventArgs e)
        {
            //change page:
            _pager.CurrentItem = 0;
        }

        private void HelpOpen_Click(object sender, EventArgs e)
        {
            LoginFuncs.ShowHelp(this,"#video");
        }

        private void CloseShots_Click(object sender, EventArgs e)
        {
            if (Bootlegger.BootleggerClient.CurrentClientShotType==null)
                Bootlegger.BootleggerClient.SetShot(Bootlegger.BootleggerClient.CurrentEvent.shottypes.First());
            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
            //CloseButtons();
        }


        void Comms_OnEventUpdated()
        {
            //update adapters:
            try
            {
                FindViewById(Resource.Id.shotselector).Post(() =>
                {
                    //already disconnected
                    if (Bootlegger.BootleggerClient.CurrentClientRole != null)
                        shotselector.UpdateData(Bootlegger.BootleggerClient.CurrentClientRole.Shots);
                });
            }
            catch (Exception)
            {

            }
        }

        void Comms_OnPhaseChange(MetaPhase obj)
        {
            //if there are no roles associated with this phase:
            if (obj.roles == null || obj.roles.Contains(Bootlegger.BootleggerClient.CurrentClientRole.id))
            {
                Comms_OnMessage(Bootlegger.BootleggerNotificationType.PhaseChanged,obj.name, false, false, true);
            }
            else
            {
                Comms_OnMessage(Bootlegger.BootleggerNotificationType.RoleUpdated,obj.name,true,false,true);
            }

        }

        void Comms_OnNotification(Bootlegger.BootleggerNotificationType ntype,string arg1)
        {
            Intent resultIntent = new Intent(this, typeof(SplashActivity));
            Android.Support.V4.App.TaskStackBuilder stackBuilder = Android.Support.V4.App.TaskStackBuilder.Create(this);
            stackBuilder.AddParentStack(Java.Lang.Class.FromType(typeof(Video)));
            stackBuilder.AddNextIntent(resultIntent);

            PendingIntent resultPendingIntent = stackBuilder.GetPendingIntent(0, (int)PendingIntentFlags.UpdateCurrent);
            string title = "";
            string content = "";
            switch (ntype)
            {
                case Bootlegger.BootleggerNotificationType.CrewReminder:
                    title = Resources.GetString(Resource.String.crewreminder_title);
                    if (string.IsNullOrEmpty(arg1))
                        content = Resources.GetString(Resource.String.crewreminder_content);
                    else
                        content = arg1;
                    break;
                case Bootlegger.BootleggerNotificationType.PhaseChanged:
                    title = Resources.GetString(Resource.String.phasechange_title);
                    content = Resources.GetString(Resource.String.phasechange,arg1);
                    break;
                case Bootlegger.BootleggerNotificationType.RoleUpdated:
                case Bootlegger.BootleggerNotificationType.ShootUpdated:
                    title = Resources.GetString(Resource.String.phasechange_title);
                    content = Resources.GetString(Resource.String.phasechangerole,arg1);
                    break;
            }

            NotificationCompat.Builder builder = new NotificationCompat.Builder(this)
                .SetContentTitle(title)

                .SetStyle(new NotificationCompat.BigTextStyle().BigText(content))
                .SetContentIntent(resultPendingIntent)
                .SetSmallIcon(Resource.Drawable.ic_notification);
                //.SetContentText(content);
            builder.SetAutoCancel(true);

            // Obtain a reference to the NotificationManager
            NotificationManager notificationManager = (NotificationManager)GetSystemService(Context.NotificationService);
            notificationManager.Notify(0, builder.Build());
        }

        void SelectorClick(Shot item)
        {
            ShowShotRelease(item);

            Picasso.With(this).Load("file://" + item.image).Into(FindViewById<ImageView>(Resource.Id.overlayimg));

            if (item.coverage_class!=null)
            {
                if (Bootlegger.BootleggerClient.CurrentEvent.coverage_classes.Count > item.coverage_class)
                {
                    FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace((!string.IsNullOrEmpty(item.description))?item.description : "", "%%(.*?)%%", Bootlegger.BootleggerClient.CurrentEvent.coverage_classes[int.Parse(item.coverage_class.ToString())].name);
                }
            }
            else
            {
                FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace((!string.IsNullOrEmpty(item.description)) ? item.description : "", "%%(.*?)%%", Resources.GetString(Resource.String.them));
            }


            FindViewById(Resource.Id.overlay).Visibility = ViewStates.Visible;
            FindViewById<ToggleButton>(Resource.Id.showoverlay).Checked = true;

            Bootlegger.BootleggerClient.AcceptShot(item);
            Bootlegger.BootleggerClient.SetShot(item);
            if (recording && stopwatch != null)
                Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "shot", item.id.ToString());

            switch (item.shot_type)
            {
                case Shot.ShotTypes.PHOTO:
                    FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Resource.Drawable.ic_photo_camera_white_24dp);
                    FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                    break;
                case Shot.ShotTypes.AUDIO:
                    FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Resource.Drawable.ic_mic_white_48dp);
                    FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                    break;
                default:
                    FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Gone;
                    break;
            }

            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;

        }

        bool flashon = false;

        private void FlashToggle_Click(object sender, EventArgs e)
        {
            if (FindViewById(Resource.Id.helpoverlay).Visibility == ViewStates.Visible)
                return;

           

            if (cameraDriver.HasFlash)
            {
                if (!flashon)
                {
                    cameraDriver.FlashOn();
                    flashon = true;
                    if (recording)
                        Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "flash", "on");
                }
                else
                {
                    cameraDriver.FlashOff();
                    flashon = false;
                    if (recording)
                        Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "flash", "off");
                }
            }
        }

        void Fin_Click(object sender, EventArgs e)
        {
            //show uploads screen after the event has finished
            Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH, true);
            Finish();
            return;
        }

        private void orientation_ShowWarning(bool obj)
        {
            if (obj)
            {
                if (stopwatch != null && recording && FindViewById<ImageView>(Resource.Id.rotatewarning).Visibility == ViewStates.Invisible)
                    Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "rotwarn", "on");
                FindViewById<ImageView>(Resource.Id.rotatewarning).Visibility = ViewStates.Visible;
            }
            else
            {
                if (stopwatch != null && recording && FindViewById<ImageView>(Resource.Id.rotatewarning).Visibility == ViewStates.Visible)
                    Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "rotwarn", "off");
                FindViewById<ImageView>(Resource.Id.rotatewarning).Visibility = ViewStates.Invisible;
            }
        }

        TimeSpan lastzoomreport = TimeSpan.Zero;

        void Video_ProgressChanged(object sender, SeekBar.ProgressChangedEventArgs e)
        {
            //adjust camera zoom

            cameraDriver.Zoom(e.Progress);

            if (recording && lastzoomreport < stopwatch.Elapsed - TimeSpan.FromMilliseconds(200))
            {
                lastzoomreport = stopwatch.Elapsed;
                Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "zoom", (cameraDriver.ZoomLevels[FindViewById<SeekBar>(Resource.Id.zoom).Progress].IntValue()/100.0).ToString());
            }
        }

        void Comms_OnServerDied()
        {
            
            RunOnUiThread(new Action(() =>
            {
                if (recording)
                    record();
                Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogStyle);

                builder.SetNeutralButton(Android.Resource.String.Ok,new EventHandler<DialogClickEventArgs>((o,q) =>
                {
                    //(Application as BootleggerApp).TOTALFAIL = true;
                    Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH, true);
                    Finish();
                    //Intent i = new Intent(this.ApplicationContext, typeof(Login));
                    //StartActivity(i);
                }));
                builder.SetCancelable(false);
                try
                {
                    builder.Show();
                }
                catch
                {
                    //cannot do anything about this...
                }
                
            }));
        }

        void HelpOverlay_Click(object sender, EventArgs e)
        {
            FindViewById(Resource.Id.helpoverlay).Visibility = ViewStates.Invisible;
        }

        void Help_Click(object sender, EventArgs e)
        {
            //show tooltips for help (or help overlay)
            FindViewById(Resource.Id.helpoverlay).Visibility = ViewStates.Visible;
        }

        void Start_Click(object sender, EventArgs e)
        {
            Bootlegger.BootleggerClient.EventStarted();
        }

        void Hold_Click(object sender, EventArgs e)
        {
            Bootlegger.BootleggerClient.HoldShot();
        }

        void Skip_Click(object sender, EventArgs e)
        {
            Bootlegger.BootleggerClient.SkipShot();
        }

        Plugin.Geolocator.Abstractions.Position currentposition;

        void geo_PositionChanged(object sender, Plugin.Geolocator.Abstractions.PositionEventArgs e)
        {
            currentposition = e.Position;
            if (recording && WhiteLabelConfig.GPS_RECORD_INTERVAL_SECS > TimeSpan.Zero)
            {
                Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "geo", JsonConvert.SerializeObject(e.Position));
            }
        }

        void Video_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            if (e.IsChecked)
            {
                FindViewById(Resource.Id.overlay).Visibility = ViewStates.Visible;
            }
            else
            {
                FindViewById(Resource.Id.overlay).Visibility = ViewStates.Invisible;
            }
        }

        Android.Support.V7.App.AlertDialog currentshotchoicedialog;

        void Comms_OnRoleChanged(Role obj)
        {
            //RunOnUiThread(new Action(() =>
            //{
            //    Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this, Resource.Style.MyAlertDialogStyle);
            //    builder.SetMessage(Resources.GetString(Resource.String.wouldyoudorole,obj.name)).SetPositiveButton(Android.Resource.String.Yes, new EventHandler<DialogClickEventArgs>((o, e) =>
            //    {
            //        Bootlegger.BootleggerClient.AcceptRole(obj);
            //    })).SetNegativeButton(Android.Resource.String.No, new EventHandler<DialogClickEventArgs>((o, e) =>
            //    {
            //        Bootlegger.BootleggerClient.RejectRole(obj);
            //    })).SetCancelable(false).Show();
            //}));
        }

        int expectedrecordlength = 0;

        Timer timer;
        int timercount = 0;

        void Comms_OnCountdown(int obj)
        {
            RunOnUiThread(new Action(() =>
            {
                //close shot selection dialog to stop confusion
                if (currentshotchoicedialog != null && currentshotchoicedialog.IsShowing)
                {
                    currentshotchoicedialog.Dismiss();
                    if (Bootlegger.BootleggerClient.CurrentServerShotType!=null)
                    {
                        Picasso.With(this).Load("file://" + Bootlegger.BootleggerClient.CurrentServerShotType.image).Into(FindViewById<ImageView>(Resource.Id.overlayimg));

                        if (Bootlegger.BootleggerClient.CurrentServerShotType.coverage_class != null)
                        {
                            if (Bootlegger.BootleggerClient.CurrentEvent.coverage_classes.Count > Bootlegger.BootleggerClient.CurrentServerShotType.coverage_class)
                            {
                                FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace(Bootlegger.BootleggerClient.CurrentServerShotType.description, "%%(.*?)%%", Bootlegger.BootleggerClient.CurrentEvent.coverage_classes[int.Parse(Bootlegger.BootleggerClient.CurrentServerShotType.coverage_class.ToString())].name);
                            }
                        }
                        else
                        {
                            FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace(Bootlegger.BootleggerClient.CurrentServerShotType.description, "%%(.*?)%%", "them");
                        }

                        //FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace(item.description, "%%(.*?)%%", "");
                        FindViewById(Resource.Id.overlay).Visibility = ViewStates.Visible;
                        FindViewById<ToggleButton>(Resource.Id.showoverlay).Checked = true;
                    }
                    //show the overlay for the current server based shot...


                    Comms_OnMessage(Bootlegger.BootleggerNotificationType.GoingLive,"",false,false,false);
                }

                //FindViewById<LinearLayout>(Resource.Id.countdown).Visibility = ViewStates.Visible;

                timercount = obj;
                //FindViewById<TextView>(Resource.Id.livecount).Text = Java.Lang.String.Format("%d", obj);

                if (timer != null)
                    timer.Stop();

                timer = new Timer(1000);
                timer.Elapsed += (o, e) =>
                {
                    timercount--;
                    if (timercount <= 0)
                    {
                        (o as Timer).Stop();
                    }
                    else
                    {
                        RunOnUiThread(new Action(() => {
                            //bounce animation:
                            ScaleAnimation animation = new ScaleAnimation(1,1.5f,1,1.5f,Dimension.RelativeToSelf, 0.5f, Dimension.RelativeToSelf, 0.5f);
                            animation.RepeatCount = 0;
                            animation.RepeatMode = RepeatMode.Reverse;
                            animation.Interpolator = new DecelerateInterpolator();
                            animation.Duration = 100;

                        }));
                    }
                };
                timer.Start();
            }));
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        public override void OnBackPressed()
        {
            if (FindViewById(Resource.Id.helpoverlay).Visibility == ViewStates.Visible)
            {
                FindViewById(Resource.Id.helpoverlay).Visibility = ViewStates.Gone;
            }
            else
            {
             
                    if (!WhiteLabelConfig.SHOW_ALL_SHOTS)
                    {
                        if (FindViewById(Resource.Id.shotselector).Visibility == ViewStates.Visible)
                        {
                            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                            FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Visible;
                        }
                        else
                        {


                            if (FindViewById(Resource.Id.shotselector).Visibility == ViewStates.Gone && !recording && FindViewById(Resource.Id.roleselector).Visibility == ViewStates.Gone)
                            {
                                FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                            }
                            else if (FindViewById(Resource.Id.roleselector).Visibility == ViewStates.Visible)
                            {
                                ShowLeaveDialog(false);
                            }
                        }
                    }
                    else
                    {
                        if (FindViewById(Resource.Id.shotselector).Visibility == ViewStates.Visible || !LIVEMODE)
                        {
                            ShowLeaveDialog(false);
                        }
                        else
                        {
                            if (FindViewById(Resource.Id.shotselector).Visibility == ViewStates.Gone && !recording)
                            {
                                FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                            }
                        }
                    }

                    
                //}
            }
        }

        async void ShowLeaveDialog(bool cancelable, bool perms = false)
        {

            if (recording)
                await StopRecording();
            Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH, true);
            Intent intent = new Intent();
            intent.PutExtra("videocap", true);
            if (perms)
                intent.PutExtra("needsperms", true);

            intent.PutExtra("eventid", Bootlegger.BootleggerClient.CurrentEvent.id);
            SetResult(Result.Ok, intent);
            Finish();
        }

        private void ShowShotRelease(Shot item)
        {
            if (item.release && Bootlegger.BootleggerClient.CurrentEvent.shotrelease != null && Bootlegger.BootleggerClient.CurrentEvent.shotrelease != "")
            {
                //show release dialog:
                var dialog = new Android.Support.V7.App.AlertDialog.Builder(this,Resource.Style.MyAlertDialogStyle);

                View di = LayoutInflater.Inflate(Resource.Layout.shot_release_dialog, null);
                //if ((Application as BootleggerApp).Comms.CurrentEvent.shotrelease != null && (Application as BootleggerApp).Comms.CurrentEvent.shotrelease != "")
                di.FindViewById<TextView>(Resource.Id.text).TextFormatted = (Android.Text.Html.FromHtml(Bootlegger.BootleggerClient.CurrentEvent.shotrelease));

                dialog.SetView(di);

                dialog.SetNegativeButton(Android.Resource.String.No, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                {
                    //return to the shot selection screen
                    FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                }))
                .SetPositiveButton(Resource.String.agree, new EventHandler<DialogClickEventArgs>((oe, eo) =>
                {
                    //continue:
                }))
                .SetCancelable(false)
                .Show();
            }
        }

        public void Video_ItemSelected(Shot item)
        {
            //Shot item;

            ShowShotRelease(item);

            //sets the overlay:

            Picasso.With(this).Load("file://" + item.image).Into(FindViewById<ImageView>(Resource.Id.overlayimg));

            if (item.coverage_class != null)
            {
                if (Bootlegger.BootleggerClient.CurrentEvent.coverage_classes.Count > item.coverage_class)
                {
                    FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace(item.description, "%%(.*?)%%", Bootlegger.BootleggerClient.CurrentEvent.coverage_classes[int.Parse(item.coverage_class.ToString())].name);
                }
            }
            else
            {
                FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace(item.description, "%%(.*?)%%", Resources.GetString(Resource.String.them));
            }

            //FindViewById<TextView>(Resource.Id.overlaytext).Text = Regex.Replace(item.description, "%%(.*?)%%", "");
            FindViewById(Resource.Id.overlay).Visibility = ViewStates.Visible;
            FindViewById<ToggleButton>(Resource.Id.showoverlay).Checked = true;

            if (!LIVEMODE)
                Bootlegger.BootleggerClient.AcceptShot(item);

            Bootlegger.BootleggerClient.SetShot(item);

            if (recording && stopwatch != null)
                Bootlegger.BootleggerClient.AddTimedMeta(stopwatch.Elapsed, "shot", item.id.ToString());

            switch (item.shot_type)
            {
                case Shot.ShotTypes.PHOTO:
                    FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Android.Resource.Drawable.IcMenuGallery);
                    FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                    break;
                case Shot.ShotTypes.AUDIO:
                    FindViewById<ImageView>(Resource.Id.shottype).SetImageResource(Android.Resource.Drawable.IcButtonSpeakNow);
                    FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Visible;
                    break;

                default:
                    FindViewById<ImageView>(Resource.Id.shottype).Visibility = ViewStates.Gone;
                    break;
            }


            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
        }



        void Comms_OnMessage(Bootlegger.BootleggerNotificationType ntype, string obj,bool dialog,bool shots,bool vibrate)
        {
            
            string content = "";
            switch (ntype)
            {
                case Bootlegger.BootleggerNotificationType.CrewReminder:
                    if (string.IsNullOrEmpty(obj))
                        content = Resources.GetString(Resource.String.crewreminder_content);
                    else
                        content = obj;
                    break;
                case Bootlegger.BootleggerNotificationType.PhaseChanged:
                    content = Resources.GetString(Resource.String.phasechange, obj);
                    break;
                case Bootlegger.BootleggerNotificationType.RoleUpdated:
                    content = Resources.GetString(Resource.String.phasechangerole, obj);
                    break;
                case Bootlegger.BootleggerNotificationType.ShootUpdated:
                    content = Resources.GetString(Resource.String.shootupdatedlong, obj);
                    break;
            }

            if (!dialog)
            {
                RunOnUiThread(new Action(() =>
                {
                    var msg = FindViewById<TextSwitcher>(Resource.Id.message);
                    if (msg != null)
                    {
                        FindViewById(Resource.Id.messageholder).PostDelayed(() =>
                        {
                            msg.SetText("");
                            FindViewById(Resource.Id.messageholder).Visibility = ViewStates.Invisible;
                        }, 6000);
                        FindViewById(Resource.Id.messageholder).Visibility = ViewStates.Visible;
                        msg.SetText(content);
                    }
                }));
            }
            else
            {
               
                    //generic message with dialog
                    RunOnUiThread(new Action(() =>
                    {
                        Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this,Resource.Style.MyAlertDialogStyle);
                        builder
                        .SetMessage(content)
                        .SetPositiveButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>((o, e) =>
                        {
                            
                        }))
                        .SetCancelable(false).Show();
                    }));
                //}
            }
            if (vibrate)
            {
                Vibrator vi;
                vi = (Vibrator)GetSystemService(Context.VibratorService);

                if (vi.HasVibrator)
                {
                    vi.Vibrate(100);
                }

            }
        }

        bool LIVEMODE;


        bool shownonce;

        //doing this on load...
        void Comms_OnModeChange(string obj)
        {
                switch (obj)
                {
                    case "manual":
                        RunOnUiThread(new Action(() =>
                        {
                            //show play button
                            FindViewById(Resource.Id.Play).Visibility = ViewStates.Visible;
                            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                        }));
                        break;
                    case "stopped":
                        //event is closed:
                        RunOnUiThread(new Action(() =>
                        {
                            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                              FindViewById(Resource.Id.Closed).Visibility = ViewStates.Visible;
                        }));
                        break;
                    case "timed":
                        //show hold / skip button
                        RunOnUiThread(new Action(() =>
                        {
                            FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                            FindViewById(Resource.Id.Play).Visibility = ViewStates.Gone;
                        }));
                        LIVEMODE = true;
                        break;
                    case "selection":
                    default:
                        RunOnUiThread(new Action(() =>
                        {
                            if (!shownonce)
                            {
                                FindViewById(Resource.Id.Play).Visibility = ViewStates.Visible;
                                FindViewById<ToggleButton>(Resource.Id.Play).TextOn = "";
                                FindViewById<ToggleButton>(Resource.Id.Play).TextOff = "";
                               
                                if (WhiteLabelConfig.SHOW_ALL_SHOTS)
                                {
                                    FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                                    FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Gone;
                                }
                                else
                                {
                                    FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Gone;
                                    FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Visible;
                                }
                                shownonce = true;
                            }
                        }));
                        //}));
                        LIVEMODE = false;
                        
                        break;
                }
        }



        public View MakeView()
        {
            // Create a new TextView

            TextView t = new TextView(this)
            {
                Gravity = GravityFlags.Top | GravityFlags.CenterHorizontal
            };
            if (t.LayoutDirection == Android.Views.LayoutDirection.Ltr)
            {
                t.SetPadding(30, 5, 5, 15);
                t.SetCompoundDrawablesWithIntrinsicBounds(Resource.Drawable.ic_headset_mic_white_48dp, 0, 0, 0);
            }
            else
            {
                t.SetPadding(5, 5, 30, 15);
                t.SetCompoundDrawablesWithIntrinsicBounds(0, 0, Resource.Drawable.ic_headset_mic_white_48dp, 0);
            }
            t.TextSize = 20;
            t.SetTextColor(Color.Black);
            return t;
        }

        //bool started = false;

        protected async override void OnPause()
        {
            base.OnPause();
            //background beacon
            //Beacons.BeaconInstance.InBackground = true;


            //tell client that not in shooting mode
            if (!WhiteLabelConfig.REDUCE_BANDWIDTH)
                Bootlegger.BootleggerClient.NotReadyToShoot();

            //pause any location services:
            await Plugin.Geolocator.CrossGeolocator.Current.StopListeningAsync();
            //geo.StopListening();

            //if (recorder != null)
            //{
            if (recording)
            {
                await record();
            }

            Bootlegger.BootleggerClient.InBackground = true;



            lock (cameraDriverLock)
            {
                if (cameraDriver != null)
                    cameraDriver.CloseCamera();
            }
            //kill camera??
        }

        object cameraDriverLock = new object();
        bool disablelocation = false;

        bool? from_roles;

        //bool waitingonperms = false;
        
        protected async override void OnResume()
        {
            base.OnResume();
            //Analytics.TrackEvent("VideoScreen");
            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("Video", 
                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));

            //FindViewById<FrameLayout>(Resource.Id.rolesdemo).Visibility = ViewStates.Gone;

            //set var to remember if we came from the roles screen:
            if (!from_roles.HasValue)
                from_roles = Intent.GetBooleanExtra(Roles.FROM_ROLE, false);

            var permstatus = new List<Plugin.Permissions.Abstractions.PermissionStatus>
            {
                await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Camera),
                await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Microphone),
                await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Storage)
            };



            if (!permstatus.All((p) => p == Plugin.Permissions.Abstractions.PermissionStatus.Granted))
            {
                var permsToAskFor = new[] { Plugin.Permissions.Abstractions.Permission.Camera, Plugin.Permissions.Abstractions.Permission.Microphone, Plugin.Permissions.Abstractions.Permission.Storage };

                if (WhiteLabelConfig.USE_GPS)
                {
                    permsToAskFor.Append(Plugin.Permissions.Abstractions.Permission.Location);
                }


                var hasperms = await Plugin.Permissions.CrossPermissions.Current.RequestPermissionsAsync(permsToAskFor);

                if (hasperms.All((p) => (p.Key == Plugin.Permissions.Abstractions.Permission.Location)? true : p.Value == Plugin.Permissions.Abstractions.PermissionStatus.Granted))
                    PermissionsGranted = true;
                else
                {

                    ShowLeaveDialog(false,true);
                }
            }
            else
            {
                PermissionsGranted = true;
            }


            if (!PermissionsGranted)
            {
                Finish();
                return;
            }

            if (WhiteLabelConfig.ALLOW_BLE)
            {
                //start broadcasting beacon
                //await Beacons.BeaconInstance.StartBroadcastingBle(this, Bootlegger.BootleggerClient.CurrentEvent);
                //Beacons.BeaconInstance.InBackground = false;
            }

            //Insights.Track("VideoScreen");
            
            Bootlegger.BootleggerClient.InBackground = false;
            FindViewById(Resource.Id.helpoverlay).Visibility = ViewStates.Invisible;
            if (Bootlegger.BootleggerClient.CurrentEvent == null)
            {
                Finish();
                return;
            }

            Comms_OnModeChange(Bootlegger.BootleggerClient.CurrentEvent.CurrentMode);

            pulse = AnimationUtils.LoadAnimation(this, Resource.Animation.pulse);

            lock (cameraDriverLock)
            {
                try
                {
                    ReStartCamera(null);
                }
                catch
                {

                }
            }


            //ask for location perms:
            if (WhiteLabelConfig.USE_GPS)
            {
                var status = await CrossPermissions.Current.CheckPermissionStatusAsync(Plugin.Permissions.Abstractions.Permission.Location);
                
                if (status == Plugin.Permissions.Abstractions.PermissionStatus.Granted)
                {
                    if (!Plugin.Geolocator.CrossGeolocator.Current.IsListening)
                        await Plugin.Geolocator.CrossGeolocator.Current.StartListeningAsync(WhiteLabelConfig.GPS_RECORD_INTERVAL_SECS, 10, true);
                   
                }
                else
                {
                    disablelocation = true;
                }

                //if (!disablelocation)
                //{

                //}
            }

            if (from_roles.HasValue && from_roles==true && !WhiteLabelConfig.SHOW_ALL_SHOTS)
            {
                FindViewById(Resource.Id.shotselector).Visibility = ViewStates.Visible;
                FindViewById(Resource.Id.roleselector).Visibility = ViewStates.Gone;
            }

        }

        SelectRoleFrag selectrolefrag;

        protected override void OnStop()
        {
            base.OnStop();
            //started = false;

            //stop ble beacon
            //Beacons.BeaconInstance.StopBroadcastingBle();

            Bootlegger.BootleggerClient.OnCountdown -= Comms_OnCountdown;
            Bootlegger.BootleggerClient.OnRoleChanged -= Comms_OnRoleChanged;
            Bootlegger.BootleggerClient.OnServerDied -= Comms_OnServerDied;
            Bootlegger.BootleggerClient.OnMessage -= Comms_OnMessage;
            Bootlegger.BootleggerClient.OnPhaseChange -= Comms_OnPhaseChange;
            Bootlegger.BootleggerClient.OnEventUpdated -= Comms_OnEventUpdated;
            Bootlegger.BootleggerClient.OnImagesUpdated -= Comms_OnImagesUpdated;
            Bootlegger.BootleggerClient.OnPermissionsChanged -= Comms_OnPermissionsChanged;
        }

        string thumbnailfilename = "";

        #region CAMERA

        public enum CAMERA_POSITION { REAR=0, FRONT=1};
        CAMERA_POSITION CURRENTCAMERA;

        private void Cam_Switch(object sender, EventArgs e)
        {
            if (!recording)
            {
                if (CURRENTCAMERA == CAMERA_POSITION.REAR && cameraDriver.NumCameras > 1)
                {
                    CURRENTCAMERA = CAMERA_POSITION.FRONT;
                    ReStartCamera(null);
                }
                else if (CURRENTCAMERA != CAMERA_POSITION.REAR)
                {
                    CURRENTCAMERA = CAMERA_POSITION.REAR;
                    ReStartCamera(null);
                }
                else
                {
                    ReStartCamera(null);
                }
            }
        }

        ICameraDriver cameraDriver;

        private void ReStartCamera(Bundle savedInstanceState)
        {
            //stop old camera
            if (PermissionsGranted)
            {
                try
                {
                    if (cameraDriver != null)
                        cameraDriver.CloseCamera();
                }
                catch
                {

                }

                if (Build.VERSION.SdkInt >= BuildVersionCodes.Lollipop)
                {
                    CameraManager manager = (CameraManager)GetSystemService(Context.CameraService);

                    var cameraId = Camera2Fragment.GetCam(manager, CURRENTCAMERA);

                    CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                    if ((int)characteristics.Get(CameraCharacteristics.InfoSupportedHardwareLevel) != (int)InfoSupportedHardwareLevel.Full)
                        cameraDriver = Camera1Fragment.NewInstance((int)CURRENTCAMERA);
                    else
                        cameraDriver = Camera2Fragment.newInstance(CURRENTCAMERA);
                }
                else
                {
                    cameraDriver = Camera1Fragment.NewInstance((int)CURRENTCAMERA);
                }

                SupportFragmentManager.BeginTransaction().Replace(Resource.Id.VideoPreview, cameraDriver as Android.Support.V4.App.Fragment).Commit();

                cameraDriver.OnPictureTaken += CameraDriver_OnPictureTaken;
                cameraDriver.OnSetupComplete += CameraDriver_OnSetupComplete;
                cameraDriver.OnError += CameraDriver_OnError;
            }
        }

        private void CameraDriver_OnError(string obj)
        {
            //Toast.MakeText(Application.Context, new Exce, ToastLength.Short).Show();
            LoginFuncs.ShowToast(this, new Exception("Camera Problem"));
            Bootlegger.BootleggerClient.UnSelectRole(!WhiteLabelConfig.REDUCE_BANDWIDTH, true);
            Finish();
            return;
        }

        private void CameraDriver_OnSetupComplete()
        {
            if (!cameraDriver.HasZoom)
                FindViewById<SeekBar>(Resource.Id.zoom).Visibility = ViewStates.Gone;
            else
            {
                try
                {
                    var levels = cameraDriver.ZoomLevels;
                    var p = from n in levels where n.IntValue() >= 200 orderby n.IntValue() ascending select n;
                    var max = levels.IndexOf(p.First());
                    FindViewById<SeekBar>(Resource.Id.zoom).Max = max;
                }
                catch
                {
                    FindViewById<SeekBar>(Resource.Id.zoom).Visibility = ViewStates.Gone;
                }
            }

            try
            {
                FindViewById<View>(Resource.Id.camera_preview).Post(() =>
                {
                    FindViewById<View>(Resource.Id.camera_preview).RequestLayout();
                });
            }
            catch
            {
                //handle camera not created yet (permissions not met)
            }
        }

        private void CameraDriver_OnPictureTaken()
        {
            RunOnUiThread(async () =>
            {
                if (recording)
                {
                    await record();
                }
            });
        }

        #endregion
    }

}