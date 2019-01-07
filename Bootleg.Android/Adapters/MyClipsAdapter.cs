/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Square.Picasso;
using Android.Support.V7.Widget;
//using Com.Tonicartos.Superslim;
using Android.Graphics;
using Android.Support.V4.Content;
using static Bootleg.Droid.MyClipsAdapter.ViewHolder;
using Bootleg.API.Model;
//using Android.Content.Res;

namespace Bootleg.Droid
{
    public class MyClipsAdapter : RecyclerView.Adapter
    {
        //private bool doing_work;

        public enum TileType:int { MEDIAITEM=1, UPLOADER=2, SYNC=3};

        public class HeaderMediaItem
        {
            public MediaItem MediaItem { get; set; }
            public TileType ViewType { get; set; }
            public string HeaderText { get; set; }
            public string SubText { get; set; }
            public int SectionFirstPosition { get; set; }
            public int SectionManager { get; set; }
            public int Icon { get; set; }
            public bool NoHeaders { get; internal set; }
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            MyClipsAdapter adpt;
            public event Action<MediaItem> OnDelete;
            public event Action<MediaItem,View> OnPreview;
            HeaderMediaItem media;

            public ViewHolder(View itemView, MyClipsAdapter adpt) : base(itemView)
            {
                view = itemView;
                this.adpt = adpt;

                if (view.FindViewById<ImageButton>(Resource.Id.deletebtn) != null)
                {
                    view.FindViewById<ImageButton>(Resource.Id.deletebtn).Click += ViewHolder_Click;
                    view.Click += ViewHolder_Click1;
                }

                if (view.FindViewById(Resource.Id.syncnow) != null)
                {
                    adpt.OnSyncStatusChanged += Adpt_OnSyncStatusChanged;
                    view.FindViewById<Button>(Resource.Id.syncnow).Click += (obj, args) =>
                    {
                    //initiate sync:
                    adpt.FireRefreshClips();
                    };
                }

                if (view.FindViewById(Resource.Id.uploadbtn) != null)
                {
                    view.FindViewById<Button>(Resource.Id.uploadbtn).Click += (o, e) => { adpt.FireUpload(); };
                }
            }

            private void BootleggerClient_OnGlobalUploadProgress(double arg1, int arg2, int arg3)
            {
                view.FindViewById<TextView>(Resource.Id.uploadcount).Post(() =>
                {
                    view.FindViewById<TextView>(Resource.Id.uploadcount).Text = view.Context.GetString(Resource.String.waitingtoupload, arg3 - arg2);
                });
            }

          
            internal void SetItem(HeaderMediaItem item)
            {
                if (item.ViewType == TileType.SYNC)
                {
                    media = null;
                    view.FindViewById<TextView>(Resource.Id.subtitle).Text = view.Context.GetString(Resource.String.updatingvideos);

                    if (adpt.CurrentStatus != null)
                        Adpt_OnSyncStatusChanged(adpt.CurrentStatus.Item1, adpt.CurrentStatus.Item2);
                    else
                        Adpt_OnSyncStatusChanged(SyncStatus.OK, 0);


                    //if there is media waiting for upload:
                    if (Bootlegger.BootleggerClient.MyMediaEditing.Count == 0 && Bootlegger.BootleggerClient.UploadQueueEditing.Count == 0)
                    {
                        //if none of my media, and none pending upload
                        if ((Bootlegger.BootleggerClient.CurrentEvent.publicedit || Bootlegger.BootleggerClient.CurrentEvent.publicview) && Bootlegger.BootleggerClient.CurrentEvent.numberofclips > 0)
                        {
                            //if public edit
                            //view.FindViewById<TextView>(Resource.Id.nofootage).Text = adpt.context.Resources.GetString(Resource.String.contributedfootage, Bootlegger.BootleggerClient.CurrentEvent.numberofclips);
                        }
                        else
                        {
                            //if not public edit
                            //view.FindViewById<TextView>(Resource.Id.nofootage).Text = adpt.context.Resources.GetString(Resource.String.nofootage);
                        }

                        //show message
                        view.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
                    }
                    else
                    {
                        view.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;
                    }

                }
                else if (item.ViewType == TileType.UPLOADER)
                {
                    media = null;

                    
                    //Bootlegger.BootleggerClient.OnGlobalUploadProgress += BootleggerClient_OnGlobalUploadProgress;
                    //Bootlegger.BootleggerClient.OnCurrentUploadsComplete += Comms_OnCurrentUploadsComplete;

                    //link upload info:
                    //if (Bootlegger.BootleggerClient.UploadQueueEditing.Count > 0)
                    //{
                        //view.FindViewById<View>(Resource.Id.uploadtile).Visibility = ViewStates.Visible;

                    if (Bootlegger.BootleggerClient.CanUpload)
                    {
                        view.FindViewById<Button>(Resource.Id.uploadbtn).Text = view.Resources.GetString(Resource.String.pause);
                    }
                    else
                    {
                        view.FindViewById<Button>(Resource.Id.uploadbtn).Text = view.Resources.GetString(Resource.String.upload);
                    }

                    view.FindViewById<TextView>(Resource.Id.subheader).Text = view.Context.GetString(Resource.String.waitingtoupload, Bootlegger.BootleggerClient.UploadQueueEditing.Count);
                    //}
                    //else
                    //{
                    //    view.FindViewById<View>(Resource.Id.uploadtile).Visibility = ViewStates.Gone;
                    //}
                }
                else
                {
                    media = item;

                    if (media.MediaItem.Static_Meta.ContainsKey("captured_at"))
                    {

                        //DateTime thedate;
                        try
                        {
                            //21 / 01 / 2016 20:31:18.41 pm + 00
                            if (item.MediaItem.CreatedAt != null)
                            {
                                view.FindViewById<TextView>(Resource.Id.uploadsubtitle).Text = item.MediaItem.CreatedAt.LocalizeTimeDiff();
                            }

                            //view.FindViewById<TextView>(Resource.Id.uploadsubtitle).Text = DateTime.ParseExact(media.Static_Meta["captured_at"].ToString(), "dd/MM/yyyy H:mm:ss.ff ", CultureInfo.InvariantCulture).ToString("hhtt ddd dd MMM");
                        }
                        catch
                        {
                            view.FindViewById<TextView>(Resource.Id.uploadsubtitle).Text = media.MediaItem.Static_Meta["captured_at"].ToString();
                        }
                    }
                    else
                    {
                        try
                        {
                            view.FindViewById<TextView>(Resource.Id.uploadsubtitle).Text = media.MediaItem.Static_Meta["captured_at"].ToString();
                        }
                        catch
                        {
                            view.FindViewById<TextView>(Resource.Id.uploadsubtitle).Text = "?";
                        }
                    }

                    view.FindViewById<ImageView>(Resource.Id.pendingimg).Visibility = ViewStates.Gone;

                    switch (media.MediaItem.MediaType)
                    {
                        case Shot.ShotTypes.VIDEO:
                            view.FindViewById<ImageView>(Resource.Id.mediatype).SetImageDrawable(null);
                            break;

                        case Shot.ShotTypes.PHOTO:
                            view.FindViewById<ImageView>(Resource.Id.mediatype).SetImageResource(Resource.Drawable.ic_photo_camera_white_24dp);

                            break;

                        case Shot.ShotTypes.AUDIO:
                            view.FindViewById<ImageView>(Resource.Id.mediatype).SetImageResource(Resource.Drawable.ic_mic_white_48dp);
                            break;
                    }
                    //if (changed)
                    //{
                    switch (media.MediaItem.MediaType)
                    {
                        case Shot.ShotTypes.VIDEO:
                        case Shot.ShotTypes.PHOTO:
                            if (media.MediaItem.Thumb != null && !media.MediaItem.Thumb.Contains("http"))
                                Picasso.With(view.Context).Load("file://" + media.MediaItem.Thumb).
                                    Tag(adpt).
                                    MemoryPolicy(MemoryPolicy.NoCache).
                                    Fit().
                                    NoFade().
                                    //NoPlaceholder().
                                    Config(Bitmap.Config.Rgb565).
                                    CenterCrop().
                                    Into(view.FindViewById<ImageView>(Resource.Id.image));
                            else
                                Picasso.With(view.Context).Load(media.MediaItem.Thumb).
                                    Fit().
                                    //NoPlaceholder().
                                    Tag(adpt).
                                    NoFade().
                                    Config(Bitmap.Config.Rgb565).
                                    MemoryPolicy(MemoryPolicy.NoCache).
                                    CenterCrop().
                                    Into(view.FindViewById<ImageView>(Resource.Id.image));
                            break;

                        case Shot.ShotTypes.AUDIO:
                            Picasso.With(view.Context).Load(Resource.Drawable.ic_audiotrack_black_48dp).
                                Tag(adpt).
                                Config(Bitmap.Config.Rgb565).
                                MemoryPolicy(MemoryPolicy.NoCache).
                                Into(view.FindViewById<ImageView>(Resource.Id.image));
                            break;
                    }
                    //}

                    var col = new Color(ContextCompat.GetColor(view.Context, Resource.Color.blue));
                    view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).ProgressColor = col;


                    UpdateProgress();

                    media.MediaItem.OnChanged += (sender) =>
                    {
                        view.Post(() =>
                        {
                            UpdateProgress();   
                            //adpt.NotifyItemChanged(AdapterPosition);
                        });
                    };
                }
            }

            public enum SyncStatus { DOWNLOADING, MISSING, OK, SYNCING };

            private void Adpt_OnSyncStatusChanged(SyncStatus status, int obj)
            {
                view.FindViewById(Resource.Id.syncnow).Visibility = ViewStates.Gone;
                switch (status)
                {
                    case SyncStatus.SYNCING:
                        view.FindViewById<TextView>(Resource.Id.subtitle).Text = view.Context.GetString(Resource.String.lookingfornew);
                        view.FindViewById<TextView>(Resource.Id.subtitle).Visibility = ViewStates.Visible;
                        view.FindViewById(Resource.Id.progressBar).Visibility = ViewStates.Visible;
                        break;
                    case SyncStatus.MISSING:
                        view.FindViewById<TextView>(Resource.Id.subtitle).Text = view.Context.GetString(Resource.String.newvideos, obj);
                        view.FindViewById(Resource.Id.syncnow).Visibility = ViewStates.Visible;
                        view.FindViewById<TextView>(Resource.Id.subtitle).Visibility = ViewStates.Visible;
                        view.FindViewById(Resource.Id.progressBar).Visibility = ViewStates.Gone;
                        break;
                    case SyncStatus.DOWNLOADING:
                        view.FindViewById<TextView>(Resource.Id.subtitle).Text = view.Context.GetString(Resource.String.updatingvideos);
                        view.FindViewById<TextView>(Resource.Id.subtitle).Visibility = ViewStates.Visible;
                        view.FindViewById(Resource.Id.progressBar).Visibility = ViewStates.Visible;
                        break;
                    case SyncStatus.OK:
                        view.FindViewById(Resource.Id.progressBar).Visibility = ViewStates.Gone;
                        view.FindViewById<TextView>(Resource.Id.subtitle).Visibility = ViewStates.Gone;
                        break;

                }
            }

            private void UpdateProgress()
            {
                if (media.MediaItem.Status != MediaItem.MediaStatus.DONE)
                {
                    view.FindViewById<TextView>(Resource.Id.filesize).Text = media.MediaItem.FileSize;
                    view.FindViewById<ImageView>(Resource.Id.pendingimg).Visibility = ViewStates.Gone;
                }
                else
                {
                    //if its in the uploading section:
                    if (Bootlegger.BootleggerClient.CanUpload && media.SectionFirstPosition == 0 && !media.NoHeaders)
                        view.FindViewById<ImageView>(Resource.Id.pendingimg).Visibility = ViewStates.Visible;
                    else
                        view.FindViewById<ImageView>(Resource.Id.pendingimg).Visibility = ViewStates.Gone;


                    try
                    {
                        view.FindViewById<TextView>(Resource.Id.filesize).Text = (media.MediaItem.created_by == Bootlegger.BootleggerClient.CurrentUser.id) ? view.Context.GetString(Resource.String.me) : media.MediaItem.Contributor;

                        //view.FindViewById<TextView>(Resource.Id.filesize).Text = media.MediaItem.Contributor;
                    }
                    catch
                    {
                        view.FindViewById<TextView>(Resource.Id.filesize).Text = media.MediaItem.FileSize;
                    }
                }

                if (media.MediaItem.Status == MediaItem.MediaStatus.UPLOADING)
                {
                    view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).Value = (int)media.MediaItem.Progress;
                    view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).Visibility = ViewStates.Visible;
                    view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Gone;
                }
                else
                {
                    view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.uploadprogress).Visibility = ViewStates.Gone;

                    if (media.MediaItem.Status == MediaItem.MediaStatus.DONE)
                    {
                        view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Gone;
                    }
                    else
                    {
                        if (!Bootlegger.BootleggerClient.CanUpload)
                            view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Visible;
                        else
                            view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Gone;
                    }
                }
            }

            private void ViewHolder_Click1(object sender, EventArgs e)
            {
                OnPreview?.Invoke(media.MediaItem,view);
            }

            private void ViewHolder_Click(object sender, EventArgs e)
            {
                OnDelete?.Invoke(media.MediaItem);
            }
        }


        public event Action OnRefreshClips;
        public event Action OnUpload;


        private void FireRefreshClips()
        {
            OnRefreshClips?.Invoke();
        }

        private void FireUpload()
        {
            OnUpload?.Invoke();
        }

        internal void UpdateData(List<MediaItem> notuploaded, List<MediaItem> uploaded)
        {
            int count = 0;
            count += (notuploaded.Count > 0) ? notuploaded.Count + 2 : 0;
            count += uploaded.Count;
            //if (count == items.Count)
            //    return;
            items = null;
            items = new List<HeaderMediaItem>();

            //if (WhiteLabelConfig.REDUCE_BANDWIDTH)
            //{
            //    items.Add(new HeaderMediaItem() { ViewType = TileType.LOADER, SectionFirstPosition = 0 });
            //}

            int headerCount = 0;

            if (notuploaded.Count > 0)
            {
                items.Add(new HeaderMediaItem() { ViewType = TileType.UPLOADER, SectionFirstPosition = 0, SubText = Java.Lang.String.Format("%d", notuploaded.Count), Icon = Resource.Drawable.ic_timelapse_white_24dp, HeaderText = context.GetString(Resource.String.uploadswaiting) });
                headerCount++;

                foreach (var i in notuploaded)
                {
                    items.Add(new HeaderMediaItem() { ViewType = TileType.MEDIAITEM, MediaItem = i, SectionFirstPosition = 0 });
                }


                
            }

            items.Add(new HeaderMediaItem() { ViewType = TileType.SYNC, SectionFirstPosition = Math.Max(items.Count - 1 + headerCount,0), SubText = Java.Lang.String.Format("%d", uploaded.Count), Icon = Resource.Drawable.ic_film_play_button, HeaderText = context.GetString(Resource.String.allvideos) });
            headerCount++;
            foreach (var i in uploaded)
            {
                items.Add(new HeaderMediaItem() { ViewType = TileType.MEDIAITEM, NoHeaders = headerCount==0,   MediaItem = i, SectionFirstPosition = (notuploaded.Count>0) ? (notuploaded.Count - 1 + headerCount):0 });
            }
            NotifyDataSetChanged();
        }

        List<HeaderMediaItem> items = new List<HeaderMediaItem>();
        Activity context;

        public override int ItemCount
        {
            get
            {
                return items.Count();
            }
        }

        public event Action<SyncStatus, int> OnSyncStatusChanged;

        public Tuple<SyncStatus, int> CurrentStatus;

        public void FireSyncStatusChanged(SyncStatus status, int media)
        {
            CurrentStatus = new Tuple<SyncStatus, int>(status, media);
            OnSyncStatusChanged?.Invoke(status, media);
        }

        public MyClipsAdapter(Activity context, List<MediaItem> notuploaded,List<MediaItem> uploaded)
        {
            this.context = context;
            UpdateData(notuploaded, uploaded);
           
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = items[position];

            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
            //GridSLM.LayoutParams lp = GridSLM.LayoutParams.From(view.ItemView.LayoutParameters);
            
            //lp.SetSlm(GridSLM.Id);
            //lp.NumColumns = 3;
            ////lp.ColumnWidth = 300;
            //lp.FirstPosition = item.SectionFirstPosition;
            //view.ItemView.LayoutParameters = lp;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = null;
            switch (viewType)
            {
                case (int)TileType.MEDIAITEM:
                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.uploadlistitem, parent, false);
                    break;

                case (int)TileType.UPLOADER:

                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.uploadlistupload, parent, false);
                    break;

                case (int)TileType.SYNC:

                    itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.uploadlistsync, parent, false);
                    break;
            }
            
            ViewHolder vh = new ViewHolder(itemView, this);
            vh.OnDelete += Vh_OnDelete;
            vh.OnPreview += Vh_OnPreview;
            return vh;   
        }

        private void Vh_OnPreview(MediaItem obj,View v)
        {
            OnPreview?.Invoke(obj,v);
        }

        public event Action<MediaItem> OnDelete;
        public event Action<MediaItem,View> OnPreview;

        private void Vh_OnDelete(MediaItem obj)
        {
            OnDelete?.Invoke(obj);
        }

        public override long GetItemId(int position)
        {
            return items[position].MediaItem?.id.GetHashCode() ?? items[position].GetHashCode();
        }

        public override int GetItemViewType(int position)
        {
            return (int)items[position].ViewType;
        }

       
    }

        
    }
