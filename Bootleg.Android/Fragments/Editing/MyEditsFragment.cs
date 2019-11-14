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
using Android.Support.V4.Widget;
using Android.Widget;
using Android.Content;
using Square.Picasso;
using System.Threading;
using Bootleg.Droid.UI;
using AndroidHUD;
using System.Threading.Tasks;
using static Android.Support.V7.Widget.GridLayoutManager;
using Android.App;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class MyEditsFragment : Android.Support.V4.App.Fragment, IImagePausable
    {

        public MyEditsFragment()
		{
			
		}

        //Shoot CurrentEvent = null;

        //public MyEditsFragment()
        //{
        //    //this.CurrentEvent = currentevent;
        //}

        //private bool editing;
    
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public View theview;
        //private Review review;

        //bool loaded = false;

        public override void OnDestroy()
        {
            base.OnDestroy();
            Picasso.With(Context).CancelTag(Context);
        }

        bool alreadyloaded = false;

        public override void OnResume()
        {
            base.OnResume();
            if (!alreadyloaded)
            {
                alreadyloaded = true;
                theview.Post(() =>
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Visible;
                });
                theview.PostDelayed(() => { Refresh(true); }, 1000);
                
            }
            else
            {
                Refresh(false);
                theview.Post(() =>
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Gone;
                });
            }
            Reattach();
        }

        public void Reattach()
        {
            Bootlegger.BootleggerClient.OnEditUpdated += Comms_OnEditUpdated;
        }

        private void Comms_OnEditUpdated(Edit obj)
        {
            //find the edit in the list, and update...
            _adapter.UpdateEdit(obj);
            if (obj.progress > 97)
            {
                theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Post(() =>
                {
                    var b = Bootlegger.BootleggerClient.MyEdits;
                    _adapter.UpdateData(Bootlegger.BootleggerClient.MyEdits, true);
                });
            }
            
        }

        EditAdapter _adapter;

        public void Pause()
        {
            Picasso picasso = Picasso.With(Context);
            picasso.PauseTag(_adapter);
        }

        public void Resume()
        {
            Picasso picasso = Picasso.With(Context);
            picasso.ResumeTag(_adapter);
        }

        private class MySpanSizeLookup : SpanSizeLookup
        {
            EditAdapter adapter;
            private Activity Activity;
            int collumns = 1;

            public MySpanSizeLookup(EditAdapter adapter, int collumns)
            {
                this.adapter = adapter;
                this.collumns = collumns;
            }

            public override int GetSpanSize(int position)
            {
                if (adapter.GetItemViewType(position) == (int)EditAdapter.EditTileType.VIEW_TYPE_CONTENT)
                    return 1;
                else
                    return collumns;
            }
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.edits_list, container, false);

            //create adapter for edits:

            _adapter = new EditAdapter(Activity, Bootlegger.BootleggerClient.MyEdits,Bootlegger.BootleggerClient.CurrentEvent);
            _adapter.HasStableIds = true;
            _adapter.OnShare += _adapter_OnShare;
            _adapter.OnEdit += _adapter_OnEdit;
            _adapter.OnPreview += _adapter_OnPreview;
            _adapter.OnDelete += _adapter_OnDelete;
            //_adapter.OnRestart += _adapter_OnRestart;
            _adapter.OnRefresh += _adapter_OnRefresh;

            view.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refresh += MyEditsFragment_Refresh;

            

            var listView = view.FindViewById<RecyclerView>(Resource.Id.alledits);
            int cols = Activity.Resources.Configuration.Orientation == Android.Content.Res.Orientation.Landscape ? 2 : 1;
            var mLayoutManager = new GridLayoutManager(Activity, cols);
            mLayoutManager.SetSpanSizeLookup(new MySpanSizeLookup(_adapter, cols));
            //var mLayoutManager = new GridLayoutManager(container.Context,2);
            listView.SetLayoutManager(mLayoutManager);
            listView.SetAdapter(_adapter);
            theview = view;

            //listView.ScrollChange += ListView_ScrollChange;
            //RecyclerView.ItemAnimator animator = listView.GetItemAnimator();

            //if (animator is SimpleItemAnimator)
            //{
            //    ((SimpleItemAnimator)animator).SupportsChangeAnimations = false;
            //}

            listView.SetItemAnimator(null);

            listView.AddOnScrollListener(new PausableScrollListener(Context,_adapter));
            return view;
        }

        private void _adapter_OnRefresh()
        {
            Refresh();
        }

        //private void ListView_ScrollChange(object sender, View.ScrollChangeEventArgs e)
        //{
        //    OnScrollChange?.Invoke(e);
        //}

        //public event Action<View.ScrollChangeEventArgs> OnScrollChange;

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;
            if (_adapter.ItemCount == 1)
            {
                theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
            }

            //if (Bootlegger.BootleggerClient.CurrentEvent != null && _adapter.ItemCount == 0)
            //{
            //    theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
            //}
        }

        public override void OnPause()
        {
            cancel.Cancel();
            Bootlegger.BootleggerClient.OnEditUpdated -= Comms_OnEditUpdated;
            base.OnPause();
        }

        private async void _adapter_OnRestart(Edit obj)
        {
            AndHUD.Shared.Show(Activity, Resources.GetString(Resource.String.connecting), -1, MaskType.Black);
            try
            {
                cancel = new CancellationTokenSource();
                await LoginFuncs.TryLogin(Activity, cancel.Token);
                //await Bootlegger.BootleggerClient.RestartEdit(obj);
                _adapter.UpdateData(Bootlegger.BootleggerClient.MyEdits,true);
                _adapter.NotifyDataSetChanged();
            }
            catch (TaskCanceledException)
            {
                //do nothing
            }
            catch (Exception e)
            {
                LoginFuncs.ShowError(Context, e);
            }
            finally
            {
                AndHUD.Shared.Dismiss();
            }
        }

        private void _adapter_OnDelete(Edit obj)
        {
            //show delete dialog:
            new Android.Support.V7.App.AlertDialog.Builder(Activity).SetMessage(Resource.String.deleteedit)
            .SetPositiveButton(Android.Resource.String.Ok, new EventHandler<DialogClickEventArgs>(async (oe, eo) =>
            {
                await Bootlegger.BootleggerClient.DeleteEdit(obj);
                _adapter.UpdateData(Bootlegger.BootleggerClient.MyEdits,false);
                if (_adapter.ItemCount == 1)
                {
                    theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
                }
            }))
            .SetNegativeButton(Android.Resource.String.Cancel, new EventHandler<DialogClickEventArgs>((oe, eo) =>
            {
            }))
            .SetTitle(Resource.String.removeedit)
            .Show();
        }

        private void _adapter_OnPreview(Edit obj,View v)
        {
            //open preview:
            if (!string.IsNullOrEmpty(obj.code) && obj.progress>97 && !obj.failed)
            {
                OnPreview?.Invoke(obj, v);
            }
            else
            {
                _adapter_OnEdit(obj);
            }
        }

        public event Action<Edit, View> OnPreview;

        public event Action<Edit> OnOpenEdit;

        private void _adapter_OnEdit(Edit obj)
        {
            if (string.IsNullOrEmpty(obj.path) && obj.progress==null)
            {
                OnOpenEdit?.Invoke(obj);
            }
            else
            {
                LoginFuncs.ShowMessage(Activity, Resource.String.editready);
            }
        }

        private void _adapter_OnShare(Edit obj)
        {
            //do share...

            Intent sharingIntent = new Intent(Intent.ActionSend);
            sharingIntent.SetType("text/plain");
            sharingIntent.PutExtra(Intent.ExtraSubject, obj.title);
            sharingIntent.PutExtra(Intent.ExtraText, Bootlegger.BootleggerClient.server + "/v/" + obj.shortlink);
            //StartActivity(Intent.CreateChooser(sharingIntent, Resources.GetString(Resource.String.sharevia)));
        }

        CancellationTokenSource cancel = new CancellationTokenSource();

        private async void Refresh(bool manually)
        {
            cancel = new CancellationTokenSource();

            try
            {
                if (manually)
                {
                    if (!Bootlegger.BootleggerClient.Connected && (Context.ApplicationContext as BootleggerApp).IsReallyConnected)
                    {
                        //AndHUD.Shared.Show(Activity, Resources.GetString(Resource.String.connecting), -1, MaskType.Black);
                        try
                        {
                            await LoginFuncs.TryLogin(Activity, cancel.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            //do nothing
                        }
                        catch (Exception)
                        {
                            //LoginFuncs.ShowError(Context, Resource.String.cantconnect);
                            throw new Exception();
                            //return;
                        }
                    }

                    if ((Context.ApplicationContext as BootleggerApp).IsReallyConnected)
                    {
                        await Bootlegger.BootleggerClient.GetMyEdits(cancel.Token, false);
                        //if we are in the review screen
                        if (Bootlegger.BootleggerClient.CurrentEvent != null)
                        {
                            try
                            {
                                Bootlegger.BootleggerClient.RegisterForEditUpdates();
                            }
                            catch
                            {
                                //not online, so dont register for updates
                            }
                        }
                    }
                }

                 _adapter.UpdateData(Bootlegger.BootleggerClient.MyEdits, true);

                theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Gone;
                if (_adapter.ItemCount == 1)
                {
                    theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
                }

                //if (Bootlegger.BootleggerClient.CurrentEvent != null && _adapter.ItemCount == 0)
                //{
                //    theview.FindViewById<View>(Resource.Id.emptytext).Visibility = ViewStates.Visible;
                //}
            }
            catch(TaskCanceledException)
            {
                //user cancelled
            }
            catch (Exception e)
            {
                if (Activity != null)
                    LoginFuncs.ShowError(Activity,e);
            }
            finally
            {
                theview.Post(() => { theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Gone; });
                AndHUD.Shared.Dismiss();
            }
            //}
            //else
            //{
            //    theview.Post(() => { theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Gone; });
            //}
        }

        private void MyEditsFragment_Refresh(object sender, EventArgs e)
        {
            theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Post(() =>
            {
                theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refreshing =true;
            });
            Refresh(true);
            theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Post(() =>
            {
                theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Refreshing = false;
            });
        }

        internal void Refresh()
        {
            try {
                theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Post(() =>
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Visible;
                });
                Refresh(true);
                theview.FindViewById<SwipeRefreshLayout>(Resource.Id.swiperefresh).Post(() =>
                {
                    theview.FindViewById<ProgressBar>(Resource.Id.progressBar).Visibility = ViewStates.Gone;
                });
            }
            catch
            {
                //view not started...
            }
        }
    }
}