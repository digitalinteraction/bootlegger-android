/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System;
using Android.Content;
using Android.Widget;
using Android.Util;
using Android.Media;
using Bootleg.API;
using System.Timers;
using Android.Net;
using Android.Views;
using Android.Graphics;
using Android.Runtime;
using Com.Google.Android.Exoplayer2.UI;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Trackselection;

namespace Bootleg.Droid
{
    public class MyVideoView:FrameLayout
    {
        public MyVideoView(Context context):base(context)
        {
            Init();
        }

        public MyVideoView(Context context, IAttributeSet attrs):base(context,attrs,0)
        {
            Init();
        }

        public MyVideoView(Context context, IAttributeSet attrs, int defStyle):base(context, attrs, defStyle)
        {
            Init();
        }

        internal void SeekToStart()
        {
            mp.SeekTo(0);
        }

        //Timer tpos = new Timer(200);

        //protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        //{
        //    int newwidth = MeasureSpec.MakeMeasureSpec((int)((MeasureSpec.GetSize(heightMeasureSpec) / 9.0) * 16.0), Android.Views.MeasureSpecMode.Exactly);
        //    SetMeasuredDimension(newwidth, heightMeasureSpec);
        //}

        private PlayerView _playerView;
        private SimpleExoPlayer _player;

        private void InitializePlayer()
        {
            _player = ExoPlayerFactory.NewSimpleInstance(Context, new DefaultTrackSelector());
            _player.PlayWhenReady = true;

            _playerView = new PlayerView(Context);
            this.AddView(_playerView);
            _playerView.LayoutParameters = new FrameLayout.LayoutParams(LayoutParams.MatchParent, LayoutParams.MatchParent);
            
            _playerView.UseController = false;

            _playerView.Player = _player;
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();

            //mp.Info -= MyVideoView_Info;
            //mp.Completion -= MyVideoView_Completion;
        }

        void Init()
        {
            //Holder.SetKeepScreenOn(true);
            //Holder.SetType(SurfaceType.PushBuffers);
            //Holder.AddCallback(this);
            //mp = new MediaPlayer();
            //mp.Info += MyVideoView_Info;
            //mp.Prepared += Mp_Prepared;
            //mp.Completion += MyVideoView_Completion;
            
            poschecker.Elapsed += Poschecker_Elapsed;
            poschecker.Interval = 200;
            poschecker.Start();
        }

        private void Poschecker_Elapsed(object sender, ElapsedEventArgs e)
        {
            TitlePosition += 200;

            if (mediaisprepared && mp.IsPlaying && mp.CurrentPosition > OutPoint)
            {
                switch (playbackmode)
                {
                    case EditVideoView.PLAYBACK_MODE.TRIM_CLIP:

                        mp.Pause();
                        mp.SeekTo(InPoint);
                        mp.Start();
                        break;

                    case EditVideoView.PLAYBACK_MODE.PLAY_EDIT:
                        Post(() =>
                        {
                            mediaisprepared = false;
                            OnEndofVideo?.Invoke();
                        });
                        break;

                    case EditVideoView.PLAYBACK_MODE.PREVIEW:
                        mp.Pause();
                        mp.SeekTo(0);
                        mp.Start();
                        break;
                }
            }
            else if (IsTitle && TitlePosition > OutPoint && playbackmode == EditVideoView.PLAYBACK_MODE.PLAY_EDIT)
            {
                Post(() =>
                {
                    TitlePosition = 0;
                    mediaisprepared = false;
                    OnEndofVideo?.Invoke();
                });
            }
        }

        Timer poschecker = new Timer();
        int TitlePosition = 0;

        public void Pause()
        {
            try
            {
                mp.Pause();
            }
            catch { }
        }

        public bool IsPlaying
        {
            get
            {
                if (mediaisprepared)
                    return mp?.IsPlaying ?? false;
                else
                    return false;
            }
        }

        private void DoUpdate()
        {
            PostDelayed(() => {
                if (mediaisprepared && !IsTitle)
                {
                    try
                    {
                        //Console.WriteLine("Pos: " + mp.CurrentPosition);
                        OnPositionChange?.Invoke(mp.CurrentPosition, mp.Duration);
                    }
                    catch
                    {
                        //illagal state -- ignore
                    }
                }

                //check for outpoint
                try
                {
                    //keep on going...
                    if (mediaisprepared && mp.IsPlaying)
                        DoUpdate();
                    else if (IsTitle)
                    {
                        OnPositionChange?.Invoke(TitlePosition, OutPoint);
                        DoUpdate();
                    }

                }
                catch (Exception ed)
                {
                    //video error
                    Console.WriteLine(ed);
                }
            }, 200);
        }

        bool IsTitle = false;
        bool mediaisprepared = false;
        bool switchingmedia = false;

        private void MyVideoView_Completion(object sender, EventArgs e)
        {
            //if (!switchingmedia)
            //{
            try
            {
                switch (playbackmode)
                {
                    case EditVideoView.PLAYBACK_MODE.TRIM_CLIP:
                        mp?.SeekTo(InPoint);
                        break;

                    case EditVideoView.PLAYBACK_MODE.PLAY_EDIT:
                        mediaisprepared = false;
                        OnEndofVideo?.Invoke();
                        break;

                    case EditVideoView.PLAYBACK_MODE.PREVIEW:
                        mp?.SeekTo(0);
                        mp?.Start();
                        break;
                }
            }
            catch
            {
                //do nothing...
            }
            //}
        }

        MediaPlayer mp;

        public event Action<int,int> OnPositionChange;
        public event Action OnEndofVideo;

        public EditVideoView.PLAYBACK_MODE playbackmode
        {
            get; set;
        }

        public int InPoint { get; set; }
        public int OutPoint { get; set; }
        public Action<object, Android.Media.MediaPlayer.InfoEventArgs> OnInfo;

        private void MyVideoView_Info(object sender, Android.Media.MediaPlayer.InfoEventArgs e)
        {
            OnInfo?.Invoke(sender, e);
        }

        internal void Stop()
        {
            try
            {
                mp.Stop();
                mp.Reset();
            }
            catch { }
        }

        internal void Release()
        {
            mp.Release();
        }
        
        public void SetTitle()
        {
            mp.Reset();
            IsTitle = true;
            TitlePosition = 0;
            DoUpdate();
            OnInfo?.Invoke(null, new MediaPlayer.InfoEventArgs(true, null, MediaInfo.VideoRenderingStart, 0));
            OnPrepared?.Invoke();
        }

        public void SetAudioURI(Android.Net.Uri uri)
        {
            mediaisprepared = false;
            switchingmedia = true;
            IsTitle = false;
            mp.Reset();
            mp.SetAudioStreamType(Stream.Music);
            mp.SetDataSource(Context, uri);
            mp.PrepareAsync();
        }

        public int MyDuration { get; set; }

        private void Mp_Prepared(object sender, EventArgs e)
        {
            mediaisprepared = true;
            MyDuration = mp.Duration;
            DoUpdate();
            mp.SeekTo(InPoint);

            //OnInfo?.Invoke(sender, new MediaPlayer.InfoEventArgs(true,sender as MediaPlayer,MediaInfo.VideoRenderingStart,0));
            OnPrepared?.Invoke();

            mp.Start();
            switchingmedia = false;
        }

        public void SetVideoURI(Android.Net.Uri uri)
        {
            IsTitle = false;
            mp.Reset();
            mediaisprepared = false;
            switchingmedia = true;
            mp.SetDataSource(Context, uri);
            mp.PrepareAsync();
        }

        public event Action<int> OnBuffer;

        public event Action OnPrepared;

        private void Mp_BufferingUpdate(object sender, MediaPlayer.BufferingUpdateEventArgs e)
        {
            OnBuffer?.Invoke(e.Percent);
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            base.OnLayout(changed, left, top, right, bottom);
        }

        internal void SeekTo(int value)
        {
            mp.SeekTo(value);
        }

        internal void Start()
        {
            mp.Start();
            DoUpdate();
        }

        //public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        //{

        //}

        //public void SurfaceCreated(ISurfaceHolder holder)
        //{
        //    try
        //    {
        //        mp?.SetDisplay(holder);
        //    }
        //    catch
        //    {
        //        //holder / display not working
        //    }
        //}

        //public void SurfaceDestroyed(ISurfaceHolder holder)
        //{
        //    poschecker.Stop();
        //}
    }
}