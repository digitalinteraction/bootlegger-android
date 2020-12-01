/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.OS;
using Android.Views;
using Bootleg.API;
using Android.Support.V7.Widget;
using Android.Content;
using Android.Widget;
using Android.Support.V4.Widget;
using Square.Picasso;
using System.Threading;
using Bootleg.Droid.UI;
using System.Threading.Tasks;
using AndroidHUD;
using static Android.Support.V7.Widget.GridLayoutManager;
using Android.Content.Res;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class MyClipsFragment : Android.Support.V4.App.Fragment, IImagePausable
    {


        public MyClipsFragment()
        {

        }

        private Review review;

        public override void OnDestroy()
        {
            base.OnDestroy();
            Picasso.With(Context).CancelTag(Context);
        }

        public MyClipsFragment(Review review)
        {
            this.review = review;
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

        }


        View theview;
        MyClipsAdapter _adapter;

        bool firstload = true;

        CancellationTokenSource cancel = new CancellationTokenSource();

        MySpanSizeLookup spanLookup;

        public void Redraw()
        {
            _adapter.NotifyDataSetChanged();
        }


        internal void RefreshUploads()
        {
            _adapter.UpdateData(Bootlegger.BootleggerClient.UploadQueueEditing, Bootlegger.BootleggerClient.MyMediaEditing);
        }

        internal async void Refresh()
        {

            _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.DOWNLOADING, 0);


            try
            {
                cancel = new CancellationTokenSource();

                var info = await Bootlegger.BootleggerClient.GetEventInfo(Bootlegger.BootleggerClient.CurrentEvent.id, new System.Threading.CancellationToken());
                OnEventInfoUpdate?.Invoke(info);


                await Bootlegger.BootleggerClient.GetMyMedia(cancel.Token);

                //if I can edit everyones media:
                if (Bootlegger.BootleggerClient.CurrentEvent.publicedit)
                {
                    //load everyones media:
                    Bootlegger.BootleggerClient.GetEveryonesMedia(cancel.Token);
                }

                _adapter.UpdateData(Bootlegger.BootleggerClient.UploadQueueEditing, Bootlegger.BootleggerClient.MyMediaEditing);
                _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);

                OnRefresh?.Invoke();
            }
            catch (TaskCanceledException)
            {
                //do nothing, moving screens
            }
            catch (Exception e)
            {
                try
                {
                    LoginFuncs.ShowError(Activity, e);
                    _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);
                }
                catch
                {
                    //fails as the fragment is lost.
                }
            }
            finally
            {
                //if not waiting for everyones media to download:
                //_adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK,0);
                
            }
        }

        private void BootleggerClient_OnMediaLoadingComplete(int obj)
        {
            if (Activity != null)
            {
                //refresh the view:
                try
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Post(() =>
                    {
                        _adapter.UpdateData(Bootlegger.BootleggerClient.UploadQueueEditing, Bootlegger.BootleggerClient.MyMediaEditing);
                    });
                }
                catch
                {
                    //screen disappeard
                }
                finally
                {
                    _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);
                }
            }
        }

        bool doing_work = false;

        public override void OnResume()
        {
            base.OnResume();

            Bootlegger.BootleggerClient.OnMediaLoadingComplete += BootleggerClient_OnMediaLoadingComplete;

            RefreshUploads();
            //Bootlegger.BootleggerClient.OnMoreMediaLoaded += BootleggerClient_OnMoreMediaLoaded;

            var listView = theview.FindViewById<RecyclerView>(Resource.Id.myclips);

            if (firstload)
            {
                firstload = false;
                //show summary of the data:
                if (!doing_work)
                {
                    doing_work = true;

                    _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.SYNCING, 0);

                    theview.Post(async () =>
                    {
                        try
                        {
                            var info = await Bootlegger.BootleggerClient.GetEventInfo(Bootlegger.BootleggerClient.CurrentEvent.id, new System.Threading.CancellationToken());
                            OnEventInfoUpdate?.Invoke(info);
                            var mediahave = Bootlegger.BootleggerClient.MyMediaEditing.Count;
                            //update ui:

                            if (info.numberofclips > mediahave)
                            {
                                //if can see the clips:
                                if (Bootlegger.BootleggerClient.CurrentEvent.publicedit)
                                //if (Bootlegger.publicedit || )
                                    _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.MISSING, (info.numberofclips - mediahave));
                                else
                                    _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);
                            }
                            else
                            {
                                _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);
                            }

                            doing_work = false;
                        }
                        catch (Exception e)
                        {
                            try
                            {
                                if ((Context.ApplicationContext as BootleggerApp).IsReallyConnected)
                                    LoginFuncs.ShowError(Context, e);

                                _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);
                            }
                            catch
                            {
                                //unknown error...
                            }
                        }
                    });
                }
            }

        }

        public event Action OnStartUpload;
        public event Action<Shoot> OnEventInfoUpdate;

        public void Pause()
        {
            Picasso picasso = Picasso.With(Context);
            picasso.PauseTag(Context);
        }

        public void Resume()
        {
            Picasso picasso = Picasso.With(Context);
            picasso.ResumeTag(Context);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            theview = inflater.Inflate(Resource.Layout.myclips, container, false);

            _adapter = new MyClipsAdapter(Activity, Bootlegger.BootleggerClient.UploadQueueEditing, Bootlegger.BootleggerClient.MyMediaEditing);
            _adapter.OnRefreshClips += _adapter_OnRefreshClips;
            _adapter.OnUpload += _adapter_OnUpload;

            _adapter.HasStableIds = true;

            var listView = theview.FindViewById<RecyclerView>(Resource.Id.myclips);

            var cols = Activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? 3 : 2;

            var mLayoutManager = new GridLayoutManager(Activity,cols);
            spanLookup = new MySpanSizeLookup(_adapter, cols);
            mLayoutManager.SetSpanSizeLookup(spanLookup);

            listView.SetLayoutManager(mLayoutManager);
            listView.SetAdapter(_adapter);
            listView.SetItemAnimator(null);

            theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refresh += MyClipsFragment_Refresh;

            _adapter.OnDelete += _adapter_OnDelete;
            _adapter.OnPreview += _adapter_OnPreview;

            listView.AddOnScrollListener(new PausableScrollListener(Context, _adapter));
            return theview;
        }

        private class MySpanSizeLookup : SpanSizeLookup
        {
            MyClipsAdapter adapter;
            int collumns = 1;

            public MySpanSizeLookup(MyClipsAdapter adapter, int collumns)
            {
                this.adapter = adapter;
                this.collumns = collumns;
            }

            public override int GetSpanSize(int position)
            {
                if (adapter.GetItemViewType(position) == (int)MyClipsAdapter.TileType.MEDIAITEM)
                    return 1;
                else
                    return collumns;
            }
        }


        private void _adapter_OnUpload()
        {
            OnStartUpload?.Invoke();
        }

        private void _adapter_OnRefreshClips()
        {
            Refresh();
        }

        public override void OnPause()
        {
            base.OnPause();
            try
            {
                cancel.Cancel();
            }
            catch
            {

            }
        }


        private void MyClipsFragment_Refresh(object sender, EventArgs e)
        {
            theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refreshing = false;
            Refresh();
        }

        public event Action<MediaItem, View> OnPreview;
        public event Action OnRefresh;

        private void _adapter_OnPreview(MediaItem obj, View v)
        {
            //start preview:
            OnPreview?.Invoke(obj, v);
        }

        private void _adapter_OnDelete(MediaItem obj)
        {
            new Android.Support.V7.App.AlertDialog.Builder(Context).
                SetTitle(Resource.String.areyousure).
                SetMessage(Resource.String.removewarn).
                SetPositiveButton(Resource.String.continuebtn, async (o, e) =>
                {
                    try
                    {
                        AndHUD.Shared.Show(Context, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true);
                        await Bootlegger.BootleggerClient.RemoveLocalFile(obj);
                    }
                    catch (Exception)
                    {
                            //silent catch -- file will reappear if it fails anyway...
                            //LoginFuncs.ShowError(Context, ex);
                        }
                    finally
                    {
                        Utils.DissmissHud();
                        _adapter.UpdateData(Bootlegger.BootleggerClient.UploadQueueEditing, Bootlegger.BootleggerClient.MyMediaEditing);
                        _adapter.FireSyncStatusChanged(MyClipsAdapter.ViewHolder.SyncStatus.OK, 0);
                    }
                }).
                SetNegativeButton(Android.Resource.String.Cancel, (o, e) =>
                {
                        //do nothing
                    }).
                SetCancelable(false).
                Show();
        }
    }
}