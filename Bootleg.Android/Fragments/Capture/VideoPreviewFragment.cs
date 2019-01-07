/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
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
using Bootleg.API;
using Java.Lang;

namespace Bootleg.Droid.Fragments.Capture
{
    public class VideoPreviewFragment: Android.Support.V4.App.Fragment
    {
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.VideoPreview, container, false);
            return view;
        }

        private string currentvideo;
        //private bool playeropen = false;


        public async void PlayEdit(View view, Edit edit)
        {
            currentitemview = view;
            //put it in the same place as the thing to animate:
            //View.FindViewById<View>(Resource.Id.clipdetails).PivotX = 0f;
            //View.FindViewById<View>(Resource.Id.clipdetails).PivotY = 0f;

            //View.FindViewById<View>(Resource.Id.clipdetails).ScaleX = 0.33f;
            //View.FindViewById<View>(Resource.Id.clipdetails).ScaleY = 0.15f;
            int[] pos = new int[2];
            int[] offset = new int[2];
            currentitemview.GetLocationOnScreen(pos);
            currentitemview.Visibility = ViewStates.Visible;

            //View.FindViewById<View>(Resource.Id.clipdetails).SetX(pos[0]);
            //View.FindViewById<View>(Resource.Id.clipdetails).SetY(pos[1] - View.FindViewById<View>(Resource.Id.tabpager).Top);

            //View.FindViewById<View>(Resource.Id.clipdetails).Visibility = ViewStates.Visible;

            PlayerOpen = true;

            //then animate it:
            //var animator = View.FindViewById<View>(Resource.Id.clipdetails).Animate();
            //animator.X(0);
            //animator.Y(0);
            //animator.ScaleX(1);
            //animator.ScaleY(1);
            //animator.SetDuration(150);

            var videofile = edit.shortlink;
            if (currentvideo != videofile)
            {
                //play video...
                currentvideo = videofile;
                //Console.WriteLine(videofile);

                //get video url:
                string url = await Bootlegger.BootleggerClient.GetEditUrl(edit);

                //FindViewById<VideoView>(Resource.Id.videoplayer).SetVideoURI(Android.Net.Uri.Parse(videofile.Replace("https://","http://"),new Dictionary<string, string>() {
                // {(Application as BootleggerApp).Comms.SessionCookie.Name,(Application as BootleggerApp).Comms.SessionCookie.Value}
                //});
                View.FindViewById<EditVideoView>(Resource.Id.videoplayer).SetVideoSource(url,EditVideoView.PLAYBACK_MODE.PREVIEW,null);


                //set other fields:
                View.FindViewById<TextView>(Resource.Id.metadata).Text = edit.title;
                View.FindViewById<TextView>(Resource.Id.time).Text = edit.createdAt.ToShortDateString();
                View.FindViewById<TextView>(Resource.Id.username).Text = edit.description;
            }
            else
            {
                View.FindViewById<VideoView>(Resource.Id.videoplayer).SeekTo(0);
            }
        }

        public async void PlayVideo(View view,MediaItem media)
        {
            currentitemview = view;
            //put it in the same place as the thing to animate:
            //View.FindViewById<View>(Resource.Id.clipdetails).PivotX = 0f;
            //View.FindViewById<View>(Resource.Id.clipdetails).PivotY = 0f;

            //View.FindViewById<View>(Resource.Id.clipdetails).ScaleX = 0.33f;
            //View.FindViewById<View>(Resource.Id.clipdetails).ScaleY = 0.15f;
            int[] pos = new int[2];
            int[] offset = new int[2];
            currentitemview.GetLocationOnScreen(pos);
            currentitemview.Visibility = ViewStates.Visible;

            //View.FindViewById<View>(Resource.Id.clipdetails).SetX(pos[0]);
            //View.FindViewById<View>(Resource.Id.clipdetails).SetY(pos[1]);

            //View.FindViewById<View>(Resource.Id.clipdetails).Visibility = ViewStates.Visible;

            PlayerOpen = true;

            //then animate it:
            //var animator = View.FindViewById<View>(Resource.Id.clipdetails).Animate();
            //animator.X(0);
            //animator.Y(0);
            //animator.ScaleX(1);
            //animator.ScaleY(1);
            //animator.SetDuration(150);

            //var videofile = (media.lowres != "") ? media.lowres : media.Filename;
            string videofile = await Bootlegger.BootleggerClient.GetVideoUrl(media);

            if (currentvideo != videofile)
            {
                //play video...
                currentvideo = videofile;
                //Console.WriteLine(videofile);

                //get video url:

                View.FindViewById<EditVideoView>(Resource.Id.videoplayer).SetVideoSource(videofile, EditVideoView.PLAYBACK_MODE.PREVIEW,media);
                //set other fields:
                View.FindViewById<TextView>(Resource.Id.metadata).Text = media.id;
                try
                {
                    View.FindViewById<TextView>(Resource.Id.time).Text = DateTime.Parse(media.Static_Meta["captured_at"]).ToShortDateString();
                }
                catch
                {
                    View.FindViewById<TextView>(Resource.Id.time).Text = media.Static_Meta["captured_at"].ToString();
                }
                View.FindViewById<TextView>(Resource.Id.username).Text = (media.created_by == Bootlegger.BootleggerClient.CurrentUser.id) ? Bootlegger.BootleggerClient.CurrentUser.displayName : media.created_by;
            }
            else
            {
                View.FindViewById<EditVideoView>(Resource.Id.videoplayer).Restart();
            }
        }

        View currentitemview;

        public bool PlayerOpen { get; set; }

        public void Close()
        {
            if (PlayerOpen)
            {
                View.FindViewById<EditVideoView>(Resource.Id.videoplayer).StopPlayback();
                //hide the player...
                //View.FindViewById<View>(Resource.Id.clipdetails).ScaleX = 1;
                //View.FindViewById<View>(Resource.Id.clipdetails).ScaleY = 1;
                //View.FindViewById<View>(Resource.Id.clipdetails).Visibility = ViewStates.Visible;

                //then animate it:
                int[] pos = new int[2];
                currentitemview.GetLocationOnScreen(pos);

                //var animator = View.FindViewById<View>(Resource.Id.clipdetails).Animate();
                //animator.X(pos[0]);
                //animator.Y(pos[1]);
                //animator.ScaleX(0.33f);
                //animator.ScaleY(0.15f);
                //animator.SetDuration(150);
                //animator.WithEndAction(new Runnable(() => {
                //    View.FindViewById<View>(Resource.Id.clipdetails).Visibility = ViewStates.Invisible;
                //}));
                PlayerOpen = false;
            }
        }
    }
}