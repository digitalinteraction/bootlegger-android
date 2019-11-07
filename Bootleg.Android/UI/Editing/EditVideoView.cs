/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.Content;
using Android.Views;
using Android.Widget;
using Android.Util;
using Bootleg.API;
using Xamarin.RangeSlider;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Trackselection;
using Bootleg.Droid.UI.Dialogs;
using Square.OkHttp3;
using Com.Google.Android.Exoplayer2.Ext.Okhttp;
using Com.Google.Android.Exoplayer2.Extractor;
using Com.Google.Android.Exoplayer2.Source;
using System.Collections.Generic;
using Bootleg.Droid.UI;
using Com.Google.Android.Exoplayer2.Util;
using Com.Google.Android.Exoplayer2.Upstream;
using Com.Google.Android.Exoplayer2.Text;
using Bootleg.API.Model;
using Android.Support.V4.Content.Res;
using Java.Net;
using System.Threading.Tasks;
using System.Text;

namespace Bootleg.Droid
{
    public class EditVideoView : FrameLayout,IPlayerEventListener
    {

        public void Startup()
        {
            /*** NEW PLAYER ***/
            _player = ExoPlayerFactory.NewSimpleInstance(Context, new DefaultTrackSelector());
            _player.PlayWhenReady = true;
            _player.AddListener(this);

            progress?.Dispose();
            clipper?.Dispose();

            progress = new ProgressTracker(_player);
            progress.OnPositionChange += Progress_OnPositionChange;
            clipper = new ProgressTracker(_player, 50);
            clipper.OnPositionChange += Clipper_OnPositionChange;

            _playerView = FindViewById<PlayerView>(Resource.Id.videoview);

            //Android.Graphics.Typeface subtitleTypeface = ResourcesCompat.GetFont(Context, Resource.Font.montserratregular);
            Android.Graphics.Typeface subtitleTypeface = ResourcesCompat.GetFont(Context, Resource.Font.montserratregular);

            var captionStyleCompat = new CaptionStyleCompat(Android.Graphics.Color.White, Android.Graphics.Color.Transparent, Android.Graphics.Color.Transparent, CaptionStyleCompat.EdgeTypeNone, Android.Graphics.Color.Transparent, subtitleTypeface);

            _playerView.SubtitleView.SetStyle(captionStyleCompat);
            _playerView.SubtitleView.SetFractionalTextSize(0.06f);
            //_playerView.SubtitleView.SetFixedTextSize((int)ComplexUnitType.Sp, 10);

            _playerView.SubtitleView.SetBottomPaddingFraction(0.4f);
            _playerView.SubtitleView.TextAlignment = TextAlignment.Center;


            _playerView.Player = _player;
            _playerView.UseController = true;

            webclient = new OkHttpClient.Builder()
                .Cache((Context.ApplicationContext as BootleggerApp).FilesCache)
                .Build();
            httpDataSourceFactory = new OkHttpDataSourceFactory(webclient, "BootleggerEditor");
            extractorsFactory = new DefaultExtractorsFactory();
            defaultDataSourceFactory = new DefaultDataSourceFactory(Context, "BootleggerEditor");
            /*************/

            _audioPlayer = ExoPlayerFactory.NewSimpleInstance(Context, new DefaultTrackSelector());
            _audioPlayer.Volume = 0.4f;
            _audioPlayer.RepeatMode = Player.RepeatModeOne;

            cursor = FindViewById<View>(Resource.Id.trackposition);
            seeker = FindViewById<RangeSliderControl>(Resource.Id.seeker);
            trackcontrols = FindViewById<View>(Resource.Id.trackcontrols);
            seeker.LowerValueChanged += Seeker_LeftValueChanged;
            seeker.UpperValueChanged += Seeker_RightValueChanged;
            seeker.StepValueContinuously = true;
            track = FindViewById<View>(Resource.Id.track);

            title = FindViewById<TextView>(Resource.Id.title);

            FindViewById<ImageButton>(Resource.Id.fullscreenbtn).Click += Fullscreen_Click;

            videoWrapper = FindViewById(Resource.Id.videoWrapper);

            mFullScreenDialog = new FullScreenVideoDialog(Context, Android.Resource.Style.ThemeBlackNoTitleBarFullScreen);
            mFullScreenDialog.OnAboutToClose += MFullScreenDialog_OnAboutToClose;

            seeker.Visibility = ViewStates.Invisible;
        }

        public int SeekerHeight
        {
            get  {
                try
                {
                    return FindViewById(Resource.Id.seeker).MeasuredHeight;
                }
                catch
                {
                    return 0;
                }
            }
        }

        public EditVideoView(Context context) : base(context)
        {

        }

        PlayerView _playerView;
        SimpleExoPlayer _player;
        View cursor;
        RangeSliderControl seeker;
        View trackcontrols;
        View track;

        public void Restart()
        {
            _player.SeekTo(0);
        }

        TextView title;
        OkHttpClient webclient;
        DefaultExtractorsFactory extractorsFactory;
        OkHttpDataSourceFactory httpDataSourceFactory;
        //TitleDataSourceFactory titleDataSourceFactory;
        DefaultDataSourceFactory defaultDataSourceFactory;
        IMediaSource mediaSource;

        ProgressTracker progress;
        ProgressTracker clipper;

        SimpleExoPlayer _audioPlayer;
        IMediaSource audioSource;

        public EditVideoView(Context context, IAttributeSet attrs) : base(context, attrs)
        {
            LayoutInflater inflater = (LayoutInflater)context.GetSystemService(Context.LayoutInflaterService);
            inflater.Inflate(Resource.Layout.EditVideo, this);

            videocontainer = FindViewById<View>(Resource.Id.video);
            //videocontainer.Click += Video_Click;

            Startup();
        }


        private void Clipper_OnPositionChange(int arg1, int arg2, int arg3)
        {
            //for checking in/out points:
            if (currentmode == PLAYBACK_MODE.TRIM_CLIP)
            {
                if (arg1 > currentitem.outpoint.TotalMilliseconds)
                {
                    _player.PlayWhenReady = false;
                    _player.SeekTo((int)currentitem.inpoint.TotalMilliseconds);
                    _player.PlayWhenReady = true;
                }
            }
        }

        private void Progress_OnPositionChange(int pos,int dur,int window)
        {
            if (mediaSource != null)
            {
                //current media cursor
                if (Resources.Configuration.LayoutDirection == Android.Views.LayoutDirection.Rtl)
                    cursor.TranslationX = track.MeasuredWidth + Utils.dp2px(Context, 34.5f) - (float)(((double)pos / dur) * track.MeasuredWidth);
                else
                    cursor.TranslationX = (float)(((double)pos / dur) * track.MeasuredWidth);

                FindViewById<TextView>(Resource.Id.lefttime).Text = TimeSpan.FromMilliseconds(pos).ToString(@"mm\:ss");
                if (_player.Duration > 0)
                    FindViewById<TextView>(Resource.Id.righttime).Text = TimeSpan.FromMilliseconds(_player.Duration).ToString(@"mm\:ss");

                //Console.WriteLine(window);

                if (currentmode == PLAYBACK_MODE.TRIM_CLIP)
                {
                    OnPositionChange?.Invoke((int)Math.Min(pos - InPoint.TotalMilliseconds, OutPoint.TotalMilliseconds), dur, window, currentmode);
                }
                else
                {
                    OnPositionChange?.Invoke(pos, dur,window, currentmode);
                }
            }

        }

        View videoWrapper;

        private void MFullScreenDialog_OnAboutToClose()
        {
            ((ViewGroup)videoWrapper.Parent).RemoveView(videoWrapper);
            ((RelativeLayout)FindViewById(Resource.Id.video)).AddView(videoWrapper, 0);
            fullscreen = false;
            OnPreview?.Invoke(false);
            mFullScreenDialog.Dismiss();
        }

        bool fullscreen = false;

        FullScreenVideoDialog mFullScreenDialog;

        public event Action<bool> OnPreview;

        private void Fullscreen_Click(object sender, EventArgs e)
        {
            if (!fullscreen)
            {
                ((ViewGroup)videoWrapper.Parent).RemoveView(videoWrapper);
                mFullScreenDialog.AddContentView(videoWrapper, new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, ViewGroup.LayoutParams.MatchParent));
                mFullScreenDialog.Show();
                fullscreen = true;
                //OnPreview?.Invoke(true);
            }
        }

        //View inoutrect;
        View videocontainer;

        TimeSpan ip;
        TimeSpan op;

        string source;

        internal void ClearVideoSource()
        {
            //video?.Pause();

            //_player.Stop();
            //_player.Release();
            _player.PlayWhenReady = false;
            //_player.ClearVideoSurface();

            if (title!=null)
            {
                title.Visibility = ViewStates.Invisible;
            }
            FindViewById<View>(Resource.Id.progress).Post(() =>
            {
                //inoutrect.Visibility = ViewStates.Gone;
                seeker.Visibility = ViewStates.Invisible;
                FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Invisible;
            });
        }

        public enum PLAYBACK_MODE { PLAY_EDIT, TRIM_CLIP};

        public void HideDetails()
        {
            FindViewById<View>(Resource.Id.infopanel).Visibility = ViewStates.Gone;
        }

        public void ShowDetails()
        {
            FindViewById<View>(Resource.Id.infopanel).Visibility = ViewStates.Visible;
        }

        private PLAYBACK_MODE currentmode;
        MediaItem currentitem;

        private void PrepareSingleSource(string uri, MediaItem media)
        {
            durationSet = false;

            if (uri.StartsWith("file://"))
            {
                mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(uri), defaultDataSourceFactory, extractorsFactory, null, null);
            }
            else
            {
                mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(uri), httpDataSourceFactory, extractorsFactory, null, null);
            }

            //mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(uri), httpDataSourceFactory, extractorsFactory, null, null);
            try
            {
                _player.Prepare(mediaSource);
            }
            catch (Exception e)
            {
                Toast.MakeText(Context, Resource.String.noconnectionshort, ToastLength.Short).Show();

                //LoginFuncs.ShowError(Context, new Exception(e.Message));
            }
        }

        internal void SetAudio(string v)
        {
            //set audio:
            if (v != null)
            {
                audioSource = new ExtractorMediaSource(Android.Net.Uri.Parse(v), httpDataSourceFactory, extractorsFactory, null, null);
                _audioPlayer.Prepare(audioSource);
                _audioPlayer.PlayWhenReady = false;
            }
            else
            {
                audioSource = null;
                _audioPlayer.Stop();
            }
        }

        public double CountToIndex(List<MediaItem> meta, MediaItem startingat)
        {
            int index = meta.IndexOf(startingat);
            double total = 0;
            for (int i = 0; i < index; i++)
            {
                total += (((meta[i].outpoint != TimeSpan.Zero && meta[i].outpoint != TimeSpan.MaxValue) ? meta[i].outpoint : meta[i].ClipLength) - meta[i].inpoint).TotalMilliseconds;
            }
            return total;
        }

        List<MediaItem> currentsequence;

        public async Task PlaySequence(List<MediaItem> meta, MediaItem startingat)
        {
            currentsequence = meta;
            currentmode = PLAYBACK_MODE.PLAY_EDIT;
            _playerView.Visibility = ViewStates.Visible;

            FindViewById(Resource.Id.trackcontrols).Visibility = ViewStates.Invisible;
            FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Visible;

            //build media source:

            DynamicConcatenatingMediaSource source = new DynamicConcatenatingMediaSource();
            foreach (var m in meta.GetRange(0,meta.Count-1))
            {
                if (m.MediaType == Shot.ShotTypes.TITLE)
                {
                    var titletext = m.titletext.TrimStart('\n');

                    //titletext = titletext.Replace(",", "%2C");

                    string text = $"WEBVTT\n\n00:00.000 --> 00:{m.outpoint.Seconds.ToString("D2")}.000\n{titletext}";

                    Format textFormat = Format.CreateTextSampleFormat(null, MimeTypes.TextVtt, Format.NoValue, "en");

                    var uri = Android.Net.Uri.Parse("data:text/vtt;base64," + Base64.EncodeToString(Encoding.UTF8.GetBytes(text),Base64Flags.Default));

                    var mm = new MergingMediaSource(
                        new ExtractorMediaSource(Android.Net.Uri.Parse("rawresource:///" + Resource.Raw.blackvideo), defaultDataSourceFactory, extractorsFactory, null, null),
                        new SingleSampleMediaSource(uri, defaultDataSourceFactory, textFormat, (long)m.outpoint.TotalMilliseconds)
                        );
                    
                    source.AddMediaSource(mm);
                }
                else
                {
                    try
                    {
                        var uri = await Bootlegger.BootleggerClient.GetVideoUrl(m);
                        ExtractorMediaSource ss;
                        if (uri.StartsWith("file://",StringComparison.InvariantCulture))
                        {
                            ss = new ExtractorMediaSource(Android.Net.Uri.Parse(uri), defaultDataSourceFactory, extractorsFactory, null, null);
                        }
                        else
                        {
                            ss = new ExtractorMediaSource(Android.Net.Uri.Parse(uri), httpDataSourceFactory, extractorsFactory, null, null);
                        }

                        var mm = new ClippingMediaSource(ss, (long)m.inpoint.TotalMilliseconds * 1000, (long)m.outpoint.TotalMilliseconds * 1000);
                        source.AddMediaSource(mm);
                    }
                    catch (Exception e)
                    {
                        LoginFuncs.ShowToast(Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity, e);
                        //Toast.MakeText(Context, Resource.String.noconnectionshort, ToastLength.Short).Show();
                    }
                }
            }

            mediaSource = source;
            try
            {
                _player.Prepare(mediaSource);
                _player.PlayWhenReady = true;
                _player.RepeatMode = Player.RepeatModeAll;

                //seek to start of the chosen clip:
                _player.SeekTo(meta.IndexOf(startingat), 0);


                if (audioSource != null)
                    //seek audio:
                    _audioPlayer.SeekTo((long)CountToIndex(meta, startingat));

                //throw new Exception("THIS BREAKS DATABASE");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);

                //Toast.MakeText(Context, Resource.String.noconnectionshort, ToastLength.Short).Show();
                LoginFuncs.ShowToast(Context, e);
                //LoginFuncs.ShowError(Context, e);
            }
        }

        public async void TrimVideo(MediaItem meta)
        {
            //fade out current...
            FindViewById(Resource.Id.trackcontrols).Visibility = ViewStates.Visible;
            FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Visible;

            _playerView.Visibility = ViewStates.Visible;
            //_audioPlayer.Volume = 0;
            _audioPlayer.PlayWhenReady = false;

            try
            {
                var uri = await Bootlegger.BootleggerClient.GetVideoUrl(meta);
                currentitem = meta;
                //Console.WriteLine("ci ip: " + meta.inpoint);
                currentmode = PLAYBACK_MODE.TRIM_CLIP;

                switch (currentitem?.MediaType ?? Shot.ShotTypes.VIDEO)
                {
                    case Shot.ShotTypes.VIDEO:
                    case Shot.ShotTypes.AUDIO:
                        PrepareSingleSource(uri, meta);
                        _player.PlayWhenReady = false;
                        _player.SeekTo((long)currentitem.inpoint.TotalMilliseconds);
                        _player.PlayWhenReady = true;
                        _player.RepeatMode = Player.RepeatModeOne;
                        break;
                    case Shot.ShotTypes.TITLE:




                        try
                        {
                            _player.Prepare(mediaSource);
                        }
                        catch (Exception e)
                        {
                            //Toast.MakeText(Context, Resource.String.noconnectionshort, ToastLength.Short).Show();
                            LoginFuncs.ShowToast(Context, e);

                            //LoginFuncs.ShowError(Context, new Exception(e.Message));
                        }

                        break;
                }

                seeker.Visibility = ViewStates.Invisible;

                //Console.WriteLine(uri);
                _playerView.Post(() =>
                {

                    //video.Start();
                    //FindViewById<ImageView>(Resource.Id.pausebtn).Visibility = ViewStates.Gone;
                    try
                    {
                        if (WhiteLabelConfig.SHOW_META_EDITING)
                        {
                            if (meta != null)
                            {
                                FindViewById<TextView>(Resource.Id.username).Text = meta.Contributor.ToString();
                                FindViewById<TextView>(Resource.Id.role).Text = meta.meta.role_ex["name"].ToString();
                                FindViewById<TextView>(Resource.Id.phase).Text = meta.meta.phase_ex["name"].ToString();
                                FindViewById<TextView>(Resource.Id.shot).Text = meta.meta.shot_ex["name"].ToString();
                            }
                            else
                            {
                                FindViewById<TextView>(Resource.Id.username).Text = "unknown";
                                FindViewById<TextView>(Resource.Id.role).Text = "unknown";
                                FindViewById<TextView>(Resource.Id.phase).Text = "unknown";
                                FindViewById<TextView>(Resource.Id.shot).Text = "unknown";
                            }
                        }
                    }
                    catch { }
                });
                source = uri;
            }
            catch (Exception e)
            {
                LoginFuncs.ShowError(Context, e);
            }
        }

        public TimeSpan InPoint { get { return ip; } set {
                ip = value;
                //mediaSource.InPoint = (int)ip.TotalMilliseconds;
                OnInPointChanged?.Invoke(value);
            }
        }

        public TimeSpan OutPoint { get { return op; } set {
                op = value;
                //Console.WriteLine("Setting Out: " + op);
                //video.OutPoint = (int)op.TotalMilliseconds;
                OnOutPointChanged?.Invoke(value);
            } }

        public event Action<TimeSpan> OnInPointChanged;
        public event Action<TimeSpan> OnOutPointChanged;

        private void Seeker_RightValueChanged(object sender, EventArgs value)
        {
            _player.PlayWhenReady = false;

            long seektoval = 0;

            if (Resources.Configuration.LayoutDirection == Android.Views.LayoutDirection.Rtl)
            {
                InPoint = TimeSpan.FromMilliseconds(_player.Duration +2   - seeker.GetSelectedMaxValue());
                _player.SeekTo((int)InPoint.TotalMilliseconds);
            }
            else
            {
                OutPoint = TimeSpan.FromMilliseconds(seeker.GetSelectedMaxValue());
                seektoval = (long)Math.Max(InPoint.TotalMilliseconds, (OutPoint - TimeSpan.FromSeconds(2)).TotalMilliseconds);
                _player.SeekTo(seektoval);
            }


            _player.PlayWhenReady = true;
        }

        private void Seeker_LeftValueChanged(object sender, EventArgs value)
        {
            //seek to
            _player.PlayWhenReady = false;

            long seektoval = 0;

            if (Resources.Configuration.LayoutDirection == Android.Views.LayoutDirection.Rtl)
            {
                OutPoint = TimeSpan.FromMilliseconds(_player.Duration + 2 - seeker.GetSelectedMinValue());
                seektoval = (long)Math.Max(InPoint.TotalMilliseconds, (OutPoint - TimeSpan.FromSeconds(2)).TotalMilliseconds);
                _player.SeekTo(seektoval);
            }
            else
            {
                InPoint = TimeSpan.FromMilliseconds(seeker.GetSelectedMinValue());
                _player.SeekTo((int)seeker.GetSelectedMinValue());
                Console.WriteLine("left changed");
            }

            _player.PlayWhenReady = true;
        }

        public event Action<int, int, int, PLAYBACK_MODE> OnPositionChange;

        internal void StopPlayback()
        {
            _player.Stop();

            _audioPlayer.PlayWhenReady = false;
            _player.PlayWhenReady = false;
            //FindViewById<ImageView>(Resource.Id.pausebtn).Visibility = ViewStates.Gone;
            mediaSource = null;
            seeker.Visibility = ViewStates.Invisible;
            _playerView.Visibility = ViewStates.Invisible;
            //show play button
            //FindViewById<ImageView>(Resource.Id.pausebtn).SetImageResource(Resource.Drawable.baseline_play_circle_outline_white_48);
            //FindViewById(Resource.Id.pausebtn).Visibility = ViewStates.Visible;
        }

        internal void Release()
        {
            _player.Release();
            _audioPlayer.Release();
            //video.Release();
        }

        void IPlayerEventListener.OnLoadingChanged(bool p0)
        {

        }

        void IPlayerEventListener.OnPlaybackParametersChanged(PlaybackParameters p0)
        {

        }

        void IPlayerEventListener.OnPlayerError(ExoPlaybackException p0)
        {
            LoginFuncs.ShowToast(Context, p0);
            //Toast.MakeText(Context, Resource.String.noconnectionshort, ToastLength.Short).Show();
        }

        bool durationSet = false;

        void IPlayerEventListener.OnPlayerStateChanged(bool p0, int p1)
        {

            //just been paused from playing:
            //if (p1 == Player.StateIdle && currentmode == PLAYBACK_MODE.PLAY_EDIT)
            //{
            //    _audioPlayer.PlayWhenReady = false;
            //}

            if (p1 == Player.StateReady && !_player.PlayWhenReady && currentmode == PLAYBACK_MODE.PLAY_EDIT)
            {
                //actually paused
                _audioPlayer.PlayWhenReady = false;
            }

            if (p1 == Player.StateReady && _player.PlayWhenReady && currentmode == PLAYBACK_MODE.PLAY_EDIT)
            {
                //actually playing
                _audioPlayer.PlayWhenReady = true;
            }

            if (p1 == Player.StateReady)
            {
                FindViewById<View>(Resource.Id.progress).Post(() =>
                {
                    FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Gone;
                });

                if (currentmode != PLAYBACK_MODE.TRIM_CLIP)
                {
                    if (audioSource!=null && _player.PlayWhenReady)
                        _audioPlayer.PlayWhenReady = true;

                    seeker.Post(() =>
                    {
                        seeker.Visibility = ViewStates.Invisible;
                        //inoutrect.Visibility = ViewStates.Gone;
                    });
                }

                FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Invisible;
            }

            

            if (p1==Player.StateReady && !durationSet)
            {
                durationSet = true;


                Console.WriteLine("File:" + _player.CurrentPeriodIndex);
                Console.WriteLine("File Outpoint: " + OutPoint.TotalMilliseconds);
                Console.WriteLine("File Duration:" + _player.Duration);





                if (currentmode == PLAYBACK_MODE.TRIM_CLIP)
                { 
                    InPoint = currentitem.inpoint;
                    OutPoint = currentitem.outpoint;

                   

                    if (OutPoint == InPoint || OutPoint == TimeSpan.Zero)
                    {
                        //op = TimeSpan.FromMilliseconds(video.OutPoint);
                        //Console.WriteLine("111 duration: " + TimeSpan.FromMilliseconds(_player.Duration));
                        OutPoint = TimeSpan.FromMilliseconds(_player.Duration);
                        //OnOutPointChanged?.Invoke(OutPoint);

                    }

                    //reset duration to the duration of the file
                    if (currentitem != null)
                    {
                        if (currentitem.MediaType != Shot.ShotTypes.TITLE && OutPoint.TotalMilliseconds > _player.Duration)
                        {
                            OutPoint = TimeSpan.FromMilliseconds(_player.Duration);
                            //OnOutPointChanged?.Invoke(OutPoint);
                        }
                    }

                    if (currentitem != null)
                    {
                        if (currentitem.MediaType != Shot.ShotTypes.TITLE)
                        {
                            InPoint = currentitem.inpoint;
                            _player.PlayWhenReady = false;
                            _player.SeekTo((int)ip.TotalMilliseconds);
                            _player.PlayWhenReady = true;
                        }
                    }

                    seeker.Post(() =>
                    {
                        seeker.SetRangeValues(0, _player.Duration + 2);

                        //seeker.SetSelectedMinValue(0);
                        //seeker.SetSelectedMaxValue(_player.Duration + 2);
                        //seeker.Visibility = ViewStates.Visible;
                        //return;

                        if (Resources.Configuration.LayoutDirection == Android.Views.LayoutDirection.Rtl)
                        {
                            seeker.SetSelectedMinValue(_player.Duration - (float)OutPoint.TotalMilliseconds);
                            seeker.SetSelectedMaxValue(_player.Duration - (float)InPoint.TotalMilliseconds);
                        }
                        else
                        {
                            seeker.SetSelectedMinValue((float)InPoint.TotalMilliseconds);
                            seeker.SetSelectedMaxValue((float)OutPoint.TotalMilliseconds);

                        }
                        seeker.Visibility = ViewStates.Visible;
                    });
            }

            FindViewById<TextView>(Resource.Id.righttime).Text = TimeSpan.FromMilliseconds(_player.Duration).ToString(@"mm\:ss");

            }
        }

        public event Action<int> OnPlayerMoveNext;
        public event Action<int,long> OnUpdateClipDuration;


        void IPlayerEventListener.OnPositionDiscontinuity(int p0)
        {
            //start audio again at the start:
            if (_player.CurrentWindowIndex == 0)
            {
                if (currentmode != PLAYBACK_MODE.TRIM_CLIP)
                {
                    //skip audio to start:
                    _audioPlayer.PlayWhenReady = false;
                    _audioPlayer.SeekTo(0);
                    _audioPlayer.PlayWhenReady = true;
                }
            }

            //Console.WriteLine(_player.Duration);

            //FIX FOR SETTING CORRECT DURATION ON A CLIP EVEN IF IT HAS NOT BEEN PUT INTO TRIM MODE
            if (currentmode == PLAYBACK_MODE.PLAY_EDIT)
            {
                if (currentsequence != null && _player.Duration > 0)
                    if (currentsequence[_player.CurrentWindowIndex].outpoint.TotalMilliseconds > _player.Duration)
                    {
                        OnUpdateClipDuration?.Invoke(_player.CurrentWindowIndex, _player.Duration);
                    }
            }

            OnPlayerMoveNext?.Invoke(_player.CurrentWindowIndex);
        }

        void IPlayerEventListener.OnRepeatModeChanged(int p0)
        {
            //throw new NotImplementedException();
        }

        void IPlayerEventListener.OnSeekProcessed()
        {
            //throw new NotImplementedException();

        }

        void IPlayerEventListener.OnShuffleModeEnabledChanged(bool p0)
        {
            //throw new NotImplementedException();
        }

        void IPlayerEventListener.OnTimelineChanged(Timeline p0, Java.Lang.Object p1, int p2)
        {
            //throw new NotImplementedException();
        }

        void IPlayerEventListener.OnTracksChanged(TrackGroupArray p0, TrackSelectionArray p1)
        {
            //throw new NotImplementedException();
        }
    }
}