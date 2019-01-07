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
using Com.Google.Android.Exoplayer2;
using Java.Lang;

namespace Bootleg.Droid.UI
{
    public class ProgressTracker:Java.Lang.Object, IRunnable
    {

        private SimpleExoPlayer player;
        private Handler handler;
        public event Action<int,int,int> OnPositionChange;
        private int DELAY_MS = 200;

        public ProgressTracker(SimpleExoPlayer player,int interval = 200)
        {
            DELAY_MS = interval;
            this.player = player;
            handler = new Handler();
            handler.Post(this);
        }

        public void Run()
        {
            //int position = (int)player.CurrentPosition;
            OnPositionChange?.Invoke((int)player.CurrentPosition, (int)player.Duration,player.CurrentWindowIndex);
            handler.PostDelayed(this, DELAY_MS);
        }

        public void Dispose()
        {
            handler.RemoveCallbacks(this);
        }
    }
}