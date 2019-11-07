/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Linq;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Square.Picasso;
using Android.Graphics;
using Android.Support.V7.Widget;
using Bootleg.Droid.Adapters;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2.Trackselection;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2.Extractor;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Ext.Okhttp;
using Square.OkHttp3;
using System.Collections.Generic;
using Bootleg.API.Model;
using Bootleg.Droid.UI;

namespace Bootleg.Droid.Screens
{


    [Activity]
    public class Preview : Activity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //Window.SetStatusBarColor(Color.Transparent);
            SetTheme(Resource.Style.Theme_Normal);

            RequestWindowFeature(WindowFeatures.NoTitle);
            //Window.AddFlags(WindowManagerFlags.Fullscreen);
            ////Window.AddFlags(WindowManagerFlags.LayoutNoLimits); 
            //Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            //if ((int)Build.VERSION.SdkInt >= 21)
            //{
            //    Window.SetFlags(WindowManagerFlags.TranslucentStatus, WindowManagerFlags.TranslucentStatus);
            //}

            //Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.Fullscreen | SystemUiFlags.LayoutFullscreen | SystemUiFlags.HideNavigation | SystemUiFlags.LayoutStable);
            //Window.DecorView.SetOnSystemUiVisibilityChangeListener(this);
            SetContentView(Resource.Layout.VideoPreviewFullscreen);

            //FindViewById<ImageButton>(Resource.Id.sharebtn).Click += Preview_Click;
            //FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Gone;

            

        }

        public override bool DispatchKeyEvent(KeyEvent ev)
        {
            // If the event was not handled then see if the player view can handle it.
            return base.DispatchKeyEvent(ev) || _playerView.DispatchKeyEvent(ev);
        }

        private void _player_RenderedFirstFrame(object sender, EventArgs e)
        {
            AndroidHUD.AndHUD.Shared.Dismiss();
        }

        private void _playerView_ControllerVisibility(object sender, PlayerControlView.VisibilityEventArgs e)
        {
            if (e.Visibility != 0)
            {
                FindViewById(Resource.Id.videometadata).Visibility = ViewStates.Gone;
            }
            else
            {
                if (!Intent.GetBooleanExtra(Review.INGEST_MODE, false))
                    FindViewById(Resource.Id.videometadata).Visibility = ViewStates.Visible;
            }
        }



        protected override void OnResume()
        {
            base.OnResume();

            if ((int)Build.VERSION.SdkInt >= 19 &&(int)Build.VERSION.SdkInt < 21)
            {
                Window.AddFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
                //setWindowFlag(WindowManager.LayoutParams.FLAG_TRANSLUCENT_STATUS
                        //| WindowManager.LayoutParams.FLAG_TRANSLUCENT_NAVIGATION, true);
            }
            if ((int)Build.VERSION.SdkInt >= 19)
            {
                Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutStable);
                //View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                //                | View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                //                | View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                //);
            }
            if ((int)Build.VERSION.SdkInt >= 21)
            {
                Window.ClearFlags(WindowManagerFlags.TranslucentStatus | WindowManagerFlags.TranslucentNavigation);
                //WindowManager.LayoutParams.FLAG_TRANSLUCENT_STATUS
                //| WindowManager.LayoutParams.FLAG_TRANSLUCENT_NAVIGATION, false);
                Window.SetNavigationBarColor(Color.Transparent);
                Window.SetStatusBarColor(Color.Transparent);
            }



            //Window.DecorView.SystemUiVisibility = (StatusBarVisibility)(SystemUiFlags.LayoutFullscreen | SystemUiFlags.LayoutHideNavigation | SystemUiFlags.LayoutStable);
            Window.AddFlags(WindowManagerFlags.Fullscreen);
            //Window.AddFlags(WindowManagerFlags.TranslucentNavigation);
           


            _player = ExoPlayerFactory.NewSimpleInstance(this, new DefaultTrackSelector());
            _player.PlayWhenReady = true;
            _player.RepeatMode = Player.RepeatModeOne;

            //media_controller = new MyMediaController(this,false);
            _playerView = FindViewById<PlayerView>(Resource.Id.videoview);

            _playerView.Player = _player;

            _playerView.ControllerAutoShow = true;
            _playerView.ControllerHideOnTouch = true;
            _playerView.ControllerShowTimeoutMs = 3000;

            _playerView.ControllerVisibility += _playerView_ControllerVisibility;
            _player.RenderedFirstFrame += _player_RenderedFirstFrame;
            _playerView.RequestFocus();
            _playerView.SetFastForwardIncrementMs(-1);
            _playerView.SetRewindIncrementMs(-1);

            AndroidHUD.AndHUD.Shared.Show(this, null, -1, AndroidHUD.MaskType.None, null, null, true, () =>
            {
                Finish();
            });


            //if (_player == null)
            //    Finish();

            //get id and edit:
            string id = Intent.Extras.GetString(Review.PREVIEW_EDIT);
            if (!string.IsNullOrEmpty(id))
            {
                //Analytics.TrackEvent("PreviewScreen");
               //Insights.Track("PreviewScreen","edit",id);
                var alledits = Bootlegger.BootleggerClient.MyEdits.Values
                     .SelectMany(x => x)
                     .ToList();
                var edit = (from n in alledits where n.id == id select n).First();
                PlayEdit(edit);
            }
            else
            {   
                id = Intent.Extras.GetString(Review.PREVIEW);
                if (!string.IsNullOrEmpty(id))
                {
                    //Insights.Track("PreviewScreen", "video", id);
                    //Analytics.TrackEvent("PreviewScreen");
                   var mm = Bootlegger.BootleggerClient.GetMediaItem(id);
                    if (mm!=null)
                        PlayVideo(mm);
                }
            }

        }

        Edit shareedit;


        //MediaController media_controller;
        //VideoView videoview;
        private PlayerView _playerView;
        private SimpleExoPlayer _player;

        private async void PlayEdit(Edit edit)
        {
            var videofile = edit.shortlink;
            shareedit = edit;

            Bootlegger.BootleggerClient.LogUserAction("Preview",
                new KeyValuePair<string, string>("editid", edit.id));

            //AndHUD.Shared.Show(this, "Loading...", -1, MaskType.Black, null, null, true);
            //FindViewById<EditVideoView>(Resource.Id.videoplayer).ClearVideoSource();
            //FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Visible;
            //get video url:
            try {                
                //set other fields:
                FindViewById<TextView>(Resource.Id.metadata).Text = edit.title;
                FindViewById<TextView>(Resource.Id.timemeta).Text = edit.createdAt.LocalizeFormat("ha E d MMM yy");
                if (string.IsNullOrEmpty(edit.description))
                {
                    FindViewById<TextView>(Resource.Id.description).Visibility = ViewStates.Gone;
                }
                else
                {
                    FindViewById<TextView>(Resource.Id.description).Text = edit.description;
                    FindViewById<TextView>(Resource.Id.description).Visibility = ViewStates.Visible;
                }

                string url = await Bootlegger.BootleggerClient.GetEditUrl(edit);


                //DefaultHttpDataSourceFactory httpDataSourceFactory = new DefaultHttpDataSourceFactory("BootleggerPreview");

                ////var ok = new OkHttpClient();
                //int cacheSize = 300 * 1024 * 1024; // 300 MiB
                //Square.OkHttp3.Cache cache = new Square.OkHttp3.Cache(FilesDir, cacheSize);

                var client = new OkHttpClient.Builder()
                    //.Cache((Application as  BootleggerApp).FilesCache)
                    .Build();
                OkHttpDataSourceFactory httpDataSourceFactory = new OkHttpDataSourceFactory(client, "BootleggerPreview");
                
                var extractorsFactory = new DefaultExtractorsFactory();
                mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(url), httpDataSourceFactory, extractorsFactory, null, null);
                _player.Prepare(mediaSource);

            }
            catch (Exception e)
            {
                //adjust to what kind of exception it is:
                LoginFuncs.ShowToast(this, e);
            }
            finally
            {
                //AndHUD.Shared.Dismiss();
            }
        }

        IMediaSource mediaSource;

        //private void Preview_Click(object sender, EventArgs e)
        //{
        //    Intent sharingIntent = new Intent(Intent.ActionSend);
        //    sharingIntent.SetType("text/plain");
        //    sharingIntent.PutExtra(Intent.ExtraSubject, shareedit.title);
        //    sharingIntent.PutExtra(Intent.ExtraText, Bootlegger.BootleggerClient.server + "/v/" + shareedit.shortlink);
        //    StartActivity(Intent.CreateChooser(sharingIntent, new Java.Lang.String(Resources.GetString(Resource.String.sharevia))));
        //}

        public async void PlayVideo(MediaItem media)
        {
            Bootlegger.BootleggerClient.LogUserAction("Preview",
                new KeyValuePair<string, string>("mediaid", media.id));

            if (Intent.GetBooleanExtra(Review.INGEST_MODE,false))
            {
                var rv = FindViewById<RecyclerView>(Resource.Id.list);
                rv.SetLayoutManager(new LinearLayoutManager(this,LinearLayoutManager.Horizontal,false));

                var _readonly = Intent.GetBooleanExtra(Review.READ_ONLY, false);

                var chips = new ChipAdapter(this,_readonly);

                //var prev = Resources.GetStringArray(Resource.Array.default_edit_topics).ToList();
                //if (string.IsNullOrEmpty(Bootlegger.BootleggerClient.CurrentEvent.topics))
                //    Bootlegger.BootleggerClient.CurrentEvent.topics = string.Join(",", prev);
                //else
                    //prev = Bootlegger.BootleggerClient.CurrentEvent.topics.Split(',').ToList();

                if (_readonly)
                    chips.Update(null, media);
                else
                    chips.Update(Bootlegger.BootleggerClient.CurrentEvent.topics.ToList(), media);

                rv.SetAdapter(chips);
                FindViewById(Resource.Id.videometadata).Visibility = ViewStates.Gone;
            }

            FindViewById<ImageView>(Resource.Id.imageplayer).SetImageDrawable(null);

            try
            {
                FindViewById<TextView>(Resource.Id.metadata).Text = media.meta.shot_ex["name"].ToString() + " at " + media.meta.role_ex["name"].ToString() + " during " + media.meta.phase_ex["name"].ToString();
            }
            catch
            {
                FindViewById<TextView>(Resource.Id.metadata).Text = "";
            }
            try
            {
                FindViewById<TextView>(Resource.Id.timemeta).Text = media.CreatedAt.LocalizeFormat("ha E d MMM yy");
            }
            catch
            {
                FindViewById<TextView>(Resource.Id.timemeta).Text = media.Static_Meta["captured_at"].ToString();
            }

    
            FindViewById<TextView>(Resource.Id.description).Visibility = ViewStates.Gone;
            
            switch (media.MediaType)
            {
                case Shot.ShotTypes.VIDEO:
                case Shot.ShotTypes.AUDIO:
                    //FindViewById<View>(Resource.Id.videoplayer).Visibility = ViewStates.Visible;
                    try
                    {
                        //set other fields:
                        string url = await Bootlegger.BootleggerClient.GetVideoUrl(media);

                        if (url.StartsWith("file://"))
                        {
                            DefaultDataSourceFactory httpDataSourceFactory = new DefaultDataSourceFactory(this, "BootleggerPreview");

                            var extractorsFactory = new DefaultExtractorsFactory();
                            mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(url), httpDataSourceFactory, extractorsFactory, null, null);
                        }
                        else
                        {
                            var client = new OkHttpClient.Builder()
                            .Cache((Application as BootleggerApp).FilesCache)
                            .Build();

                            OkHttpDataSourceFactory httpDataSourceFactory = new OkHttpDataSourceFactory(client, "BootleggerPreview");

                            var extractorsFactory = new DefaultExtractorsFactory();
                            mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(url), httpDataSourceFactory, extractorsFactory, null, null);
                        }
                        
                        _player.Prepare(mediaSource);
                    }
                    catch (Exception e)
                    {
                        //Toast.MakeText(this, Resource.String.cannotloadvideo, ToastLength.Short).Show();
                        LoginFuncs.ShowToast(this, e);
                    }
                break;

                case Shot.ShotTypes.PHOTO:
                    //FindViewById<View>(Resource.Id.videoplayer).Visibility = ViewStates.Gone;
                    if (media.Status == MediaItem.MediaStatus.DONE && !string.IsNullOrEmpty(media.path))
                    {
                        //string url = await (Application as BootleggerApp).Comms.(videofile);
                        Picasso.With(this).Load(media.Thumb+"?s=300").Fit().CenterInside().Into(FindViewById<ImageView>(Resource.Id.imageplayer));
                    }
                    else
                        Picasso.With(this).Load("file://" + media.Filename).Fit().CenterInside().Into(FindViewById<ImageView>(Resource.Id.imageplayer));

                    break;
        }

        }

        protected override void OnPause()
        {
            base.OnPause();
            _player?.Release();
            _player?.Dispose();
            _player = null;
        }

        public override void OnBackPressed()
        {
            SetResult(Result.Ok);
            Finish();
        }
    }
}