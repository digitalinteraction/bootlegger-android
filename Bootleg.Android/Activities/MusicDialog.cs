using System;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Views;
using Android.Widget;
using System.Threading;
using Bootleg.API;
using Bootleg.Droid.UI;
using Android.Support.V7.Widget;
using Bootleg.Droid.Adapters;
using System.Collections.Generic;
using Android.Media;
using Com.Google.Android.Exoplayer2.Extractor;
using Com.Google.Android.Exoplayer2.Ext.Okhttp;
using Com.Google.Android.Exoplayer2.Source;
using Com.Google.Android.Exoplayer2;
using Com.Google.Android.Exoplayer2.Trackselection;
using Bootleg.API.Model;
using Square.OkHttp3;

namespace Bootleg.Droid.Screens
{
    [Activity(Label = "MusicDialog")]
    public class MusicDialog : Android.Support.V4.App.DialogFragment
    {
        OkHttpClient webclient;
        DefaultExtractorsFactory extractorsFactory;
        OkHttpDataSourceFactory httpDataSourceFactory;
        IMediaSource mediaSource;
        SimpleExoPlayer _audioPlayer;
        Music CurrentMusic;

        public MusicDialog(Music current)
        {
            CurrentMusic = current;
        }

        public static MusicDialog NewInstance(Music current)
        {
            MusicDialog frag = new MusicDialog(current);
            return frag;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetStyle((int)DialogFragmentStyle.Normal, Resource.Style.ShootDialog);
            webclient = new OkHttpClient.Builder()
               .Cache((Activity.Application as BootleggerApp).FilesCache)
               .Build();
            httpDataSourceFactory = new OkHttpDataSourceFactory(webclient, "BootleggerEditor");
            extractorsFactory = new DefaultExtractorsFactory();
            _audioPlayer = ExoPlayerFactory.NewSimpleInstance(Context, new DefaultTrackSelector());
            _audioPlayer.RenderedFirstFrame += _audioPlayer_RenderedFirstFrame;
        }

        private void _audioPlayer_RenderedFirstFrame(object sender, EventArgs e)
        {
            listAdapter.UpdateBuffered(currentPreview);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            return inflater.Inflate(Resource.Layout.musicdialog, container);
        }

        MusicAdapter listAdapter;

        public override async void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);

            view.FindViewById<ImageButton>(Resource.Id.closebtn).Click += MusicDialog_Click;

            try
            {
                List<Music> music = await Bootlegger.BootleggerClient.GetMusic();

                listAdapter = new MusicAdapter(CurrentMusic);
                listAdapter.OnPreview += ListAdapter_OnPreview;
                listAdapter.OnSelected += ListAdapter_OnSelected;
               
                var listview = view.FindViewById<RecyclerView>(Resource.Id.listview);
                listview.SetAdapter(listAdapter);
                listview.SetLayoutManager(new LinearLayoutManager(Activity));
                listview.AddItemDecoration(new Android.Support.V7.Widget.DividerItemDecoration(Activity, Android.Support.V7.Widget.DividerItemDecoration.Vertical));
                listAdapter.Update(music);
            }
            catch (Exception e)
            {
                //dismiss dialog
                LoginFuncs.ShowError(Activity, e);
                Dismiss();
            }
        }

        private void MusicDialog_Click(object sender, EventArgs e)
        {
            OnPicked?.Invoke(null);
            _audioPlayer.Stop();
            _audioPlayer.Release();
            Dismiss();
        }

        private void ListAdapter_OnSelected(Music obj)
        {
            OnPicked?.Invoke(obj);
            _audioPlayer.Stop();
            _audioPlayer.Release();
            Dismiss();
        }

        protected MediaPlayer player;

        Music currentPreview;

        private void ListAdapter_OnPreview(Music obj)
        {
            listAdapter.UpdatePlaying(obj);
            currentPreview = obj;
           
            _audioPlayer.PlayWhenReady = false;
            mediaSource = new ExtractorMediaSource(Android.Net.Uri.Parse(obj.url), httpDataSourceFactory, extractorsFactory, null, null);
            try
            {
                _audioPlayer.Prepare(mediaSource);
                _audioPlayer.PlayWhenReady = true;
            }
            catch (Exception e)
            {
                LoginFuncs.ShowError(Context.ApplicationContext, e);
            }

            Bootlegger.BootleggerClient.LogUserAction("MusicPreview",
                new KeyValuePair<string, string>("music", obj.url),
                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        public override void OnDismiss(IDialogInterface dialog)
        {
            base.OnDismiss(dialog);
            _audioPlayer.Stop();
            _audioPlayer.Release();
        }

        public event Action<Music> OnPicked;

        CancellationTokenSource cancel = new CancellationTokenSource();
    }
}