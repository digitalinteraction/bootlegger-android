/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
using AndroidHUD;
using Android.Views;
using Android.Support.V7.App;
using Android.Support.V7.Widget;
using Bootleg.API;
using Android.Widget;
using System;
using Android.Support.Design.Widget;
using Android.Support.V7.Widget.Helper;
using System.Threading;
using Bootleg.Droid.UI;
using System.Linq;
using Newtonsoft.Json;
using Bootleg.Droid.Screens;
using static Bootleg.Droid.EditVideoView;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Android.Runtime;
using Bootleg.API.Model;
using System.ComponentModel;

namespace Bootleg.Droid
{
    [Activity(ScreenOrientation = ScreenOrientation.Landscape,ConfigurationChanges = ConfigChanges.KeyboardHidden|ConfigChanges.Keyboard, WindowSoftInputMode = (SoftInput.AdjustNothing))]
    public class Editor : AppCompatActivity
    {
        protected override void OnSaveInstanceState(Bundle outState)
        {
            outState.PutString("currentedit", JsonConvert.SerializeObject(CurrentEdit));
            base.OnSaveInstanceState(outState);
        }

        protected override void OnRestoreInstanceState(Bundle savedInstanceState)
        {
            base.OnRestoreInstanceState(savedInstanceState);
            var edit = savedInstanceState.GetString("currentedit");
            if (!string.IsNullOrEmpty(edit))
            {
                CurrentEdit = JsonConvert.DeserializeObject<Edit>(edit);
                CurrentEdit.media.RemoveAll(o => o.Status == MediaItem.MediaStatus.PLACEHOLDER);
                _adapter.UpdateData(CurrentEdit.media);
                _sliveradapter.UpdateData(CurrentEdit.media);
            }
        }

        protected override void OnStart()
        {
            base.OnStart();
            FindViewById(Resource.Id.edittimeline).PostDelayed(() =>
            {
                //adjust size of timelines
                FindViewById(Resource.Id.edittimeline).LayoutParameters.Height = preview.SeekerHeight;
            }, 300);
        }

        protected async override void OnResume()
        {
            if ((Application as BootleggerApp).TOTALFAIL == true)
            {
                Finish();
                System.Environment.Exit(0);
                return;
            }

            base.OnResume();

            if (Bootlegger.BootleggerClient.CurrentEvent == null)
            {
                Finish();
                return;
            }

            Window.AddFlags(WindowManagerFlags.Fullscreen);


            //Analytics.TrackEvent("Editor");

            preview.Startup();
            preview.OnPreview += Preview_OnPreview;
            preview.OnPlayerMoveNext += Preview_OnPlayerMoveNext;
            preview.OnUpdateClipDuration += Preview_OnUpdateClipDuration;

            Title = GetString(Resource.String.loading);

            FindViewById(Resource.Id.edittimeline).Visibility = ViewStates.Invisible;

            //load afresh?
            if (CurrentEdit == null)
            {

                string id = Intent.Extras.GetString(Review.EDIT);
                if (id == "")
                {
                    CurrentEdit = new Edit() { media = new System.Collections.Generic.List<MediaItem>() };
                    AndHUD.Shared.Dismiss();
                }
                else
                {
                    CancellationTokenSource cancel = new CancellationTokenSource();
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true, cancel.Cancel);

                    try
                    {
                        CurrentEdit = await Bootlegger.BootleggerClient.GetEdit(id, cancel.Token);
                        //SupportInvalidateOptionsMenu();
                        //CheckButtons();
                        AndHUD.Shared.Dismiss();
                    }
                    catch
                    {
                        Finish();
                        return;
                    }
                }

            }

            //update allclips with current edit info
            allclipsfragment.UpdateEdit(CurrentEdit.media);

            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("EditScreenOpen", new KeyValuePair<string, string>("editid", CurrentEdit.id), new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));

            Title = (!string.IsNullOrEmpty(CurrentEdit.title)) ? CurrentEdit.title : Resources.GetString(Resource.String.newedit);
            _adapter.UpdateData(CurrentEdit.media);
            _sliveradapter.UpdateData(CurrentEdit.media);
            UpdateTimings();
            //CheckButtons();
            if (CurrentEdit.media.Count > 0 && CurrentEdit.media.First().audio != null)
            {
                try
                {
                    var music = await Bootlegger.BootleggerClient.GetMusic();
                    var baseurl = new Uri(music.First().url);
                    CurrentMusic = new Music() { url = baseurl.ToString().Replace(baseurl.Segments.Last(), "") + CurrentEdit.media.First().audio, path = CurrentEdit.media.First().audio, caption = CurrentEdit.media.First().credits };
                    preview.SetAudio(CurrentMusic.url);
                    FindViewById<ImageButton>(Resource.Id.audiobtn).SetImageResource(Resource.Drawable.ic_music_edit_button);
                }
                catch
                {
                    //cannot load music...

                }
            }

            OriginalVersion = new Edit()
            {
                media = CurrentEdit.media.ToList(),
                title = CurrentEdit.title,
                user_id = CurrentEdit.user_id,
                id = CurrentEdit.id
            };

            ShouldAutoSave = true;
        }

        private Edit OriginalVersion;

        void Preview_OnUpdateClipDuration(int arg1, long arg2)
        {
            CurrentEdit.media[arg1].outpoint = CurrentEdit.media[arg1].inpoint + TimeSpan.FromMilliseconds(arg2);
            //TODO: update segment view:

            _sliveradapter.UpdateData(CurrentEdit.media);
            UpdateTimings();
        }


        private void Preview_OnPlayerMoveNext(int obj)
        {
            //update video view:
            _adapter.UpdatePlaying(CurrentEdit.media[obj]);
        }

        private void Preview_OnPreview(bool obj)
        {
            if (!TRIMMODE)
            {
                //if true, then starting preview
                if (obj)
                {
                    play_index = 0;
                    StartPlayback();
                }
                else
                {
                    preview.StopPlayback();
                    _adapter.UpdatePlaying(null);
                }
            }
            else
            {
                //do nothing if in trim mode
                TRIMMODE = false;
                _adapter.UpdatePlaying(null);
                _adapter.TrimMode(null, false);
                TrimOff();
            }
        }

        public override void FinishFromChild(Activity child)
        {
            base.FinishFromChild(child);
            if (!Bootlegger.BootleggerClient.Connected)
            {
                this.Finish();
                return;
            }
        }



        Edit CurrentEdit;
        FrameLayout bottompanel;

        //View videowrapper;

        //int default_editor_height = 0;
        //int small_editor_height = 0;

        TextView lefttimetotal;

        protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            //refresh list:
            if (requestCode == TAGGING)
                allclipsfragment.Refresh();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            autosaver?.CancelAsync();
        }

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Create your application here
            SetTheme(Resource.Style.Theme_Normal);

            SetContentView(Resource.Layout.Editor);

            bottompanel = FindViewById<FrameLayout>(Resource.Id.selector);

            FindViewById<FloatingActionButton>(Resource.Id.savebtn).Click += Editor_Click;
            FindViewById<ImageButton>(Resource.Id.audiobtn).Click += Editor_Click1;
            FindViewById<ImageButton>(Resource.Id.helpbtn).Click += Editor_Click2;

            //selector:
            var fragmentTransaction = SupportFragmentManager.BeginTransaction();
            allclipsfragment = new AllClipsFragment(AllClipsFragment.ClipViewMode.EDITING);
            fragmentTransaction.Add(Resource.Id.selector, allclipsfragment);
            fragmentTransaction.Commit();
            (allclipsfragment as IImagePausable).Pause();

            allclipsfragment.OnPreview += Fragment_OnPreview;
            allclipsfragment.OnChosen += Fragment_OnChosen;
            allclipsfragment.OnOpenIngest += IngestOpen;
            

            //recyclerview
            var listView = FindViewById<RecyclerView>(Resource.Id.editlist);
            var mLayoutManager = new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false);
            _adapter = new SingleEditAdapter(this);
            _adapter.OnChange += _adapter_OnChange;
            _adapter.OnPreview += _adapter_OnPreview;
            _adapter.OnTrim += _adapter_OnTrim;
            _adapter.OnDelete += _adapter_OnDelete;
            _adapter.HasStableIds = true;
            listView.SetLayoutManager(mLayoutManager);
            listView.SetAdapter(_adapter);

            timeline = FindViewById<RecyclerView>(Resource.Id.timeline);
            var mLayoutManager1 = new LinearLayoutManager(this, LinearLayoutManager.Horizontal, false);
            timeline.SetLayoutManager(mLayoutManager1);
            _sliveradapter = new SliverEditAdapter(this);
            timeline.SetAdapter(_sliveradapter);

            preview = FindViewById<EditVideoView>(Resource.Id.edit_preview);
            //preview.OnEndOfVideo += Preview_OnEndOfVideo;
            preview.OnInPointChanged += Preview_OnInPointChanged;
            preview.OnOutPointChanged += Preview_OnOutPointChanged;
            preview.OnPositionChange += Preview_OnPositionChange;
            preview.HideDetails();

            lefttimetotal = FindViewById<TextView>(Resource.Id.lefttimetotal);

            tracker = FindViewById<View>(Resource.Id.pos);

            ItemTouchHelper.Callback callback = new SwapCallback(this, _adapter, _sliveradapter);
            ItemTouchHelper touchHelper = new ItemTouchHelper(callback);
            touchHelper.AttachToRecyclerView(listView);

            EditorWizard.ShowWizard(this, false);

            //autosave function:
            autosaver = new BackgroundWorker();
            autosaver.WorkerSupportsCancellation = true;
            autosaver.DoWork += Autosaver_DoWork;
            autosaver.RunWorkerAsync();
        }

        bool ShouldAutoSave = false;

        async void Autosaver_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!autosaver.CancellationPending)
            {
                //HACK: For testing autosave

                Thread.Sleep(TimeSpan.FromSeconds(5));
                if (CurrentEdit != null && ShouldAutoSave && !autosaver.CancellationPending)
                {
                    //set title of video if there is one:
                    if (string.IsNullOrWhiteSpace(CurrentEdit.title))
                    {
                        if (CurrentEdit.media.First().MediaType == Shot.ShotTypes.TITLE)
                            CurrentEdit.title = CurrentEdit.media.First().titletext;
                        else
                            CurrentEdit.title = GetString(Resource.String.default_story_title);
                    }

                    if (CurrentMusic != null)
                    {
                        foreach (var m in CurrentEdit.media)
                        {
                            m.audio = null;
                            m.credits = null;
                        }

                        CurrentEdit.media.First().audio = CurrentMusic.path;
                        CurrentEdit.media.First().credits = CurrentMusic.caption;
                    }

                    if (CurrentEdit.media.Where(n => n.MediaType != Shot.ShotTypes.TITLE && n.Status != MediaItem.MediaStatus.PLACEHOLDER).Count() > 0)
                    {
                        try
                        {
                            await Bootlegger.BootleggerClient.SaveEdit(CurrentEdit);
                            if (CurrentEdit.media.Last().Status != MediaItem.MediaStatus.PLACEHOLDER)
                                CurrentEdit.media.Add(new MediaItem() { Status = MediaItem.MediaStatus.PLACEHOLDER });
                            Console.WriteLine($"Autosaved at {DateTime.Now.ToShortTimeString()} {DateTime.Now.Second} {DateTime.Now.Millisecond}");
                        }
                        catch
                        {
                            Console.WriteLine($"Failed to autosave at {DateTime.Now.ToShortTimeString()}");
                        }
                    }
                }
            }

        }

        BackgroundWorker autosaver;

        private void Editor_Click2(object sender, EventArgs e)
        {
            if (!TRIMMODE)
                EditorWizard.ShowWizard(this, true);
        }

        private Music CurrentMusic;

        private void Editor_Click1(object sender, EventArgs e)
        {
            if (!TRIMMODE)
            {
                //add open audio window:
                preview.StopPlayback();
                _adapter.UpdatePlaying(null);
                MusicDialog info = MusicDialog.NewInstance(CurrentMusic);
                info.OnPicked += (music) =>
                {
                    if (string.IsNullOrEmpty(music?.url))
                    {
                        FindViewById<ImageButton>(Resource.Id.audiobtn).SetImageResource(Resource.Drawable.ic_add_song);
                        CurrentMusic = null;
                        Bootlegger.BootleggerClient.LogUserAction("EditScreenClearMusic",
                                new KeyValuePair<string, string>("editid", CurrentEdit.id),
                                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
                    }
                    else
                    {
                        FindViewById<ImageButton>(Resource.Id.audiobtn).SetImageResource(Resource.Drawable.ic_music_edit_button);
                        CurrentMusic = music;
                        Bootlegger.BootleggerClient.LogUserAction("EditScreenSetMusic",
                                new KeyValuePair<string, string>("music", music.url),
                                new KeyValuePair<string, string>("editid", CurrentEdit.id),
                                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
                    }

                    preview.SetAudio(music?.url ?? null);
                };
                info.Show(SupportFragmentManager, "fragment_music_selector");
                Bootlegger.BootleggerClient.LogUserAction("EditScreenShowMusic",
                                new KeyValuePair<string, string>("editid", CurrentEdit.id),
                                new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
            }
        }

        const int TAGGING = 23345;
        private void IngestOpen()
        {
            if (!TRIMMODE)
            {
                Intent i = new Intent(this, typeof(Ingest));
                i.PutExtra(Review.EDIT, this.CurrentEdit.id);
                i.PutExtra(Review.FROMEDITOR, true);
                StartActivityForResult(i, TAGGING);
            }
        }

        private void BackClick(object sender, EventArgs e)
        {
            OnBackPressed();
        }

        //save btn
        private void Editor_Click(object sender, EventArgs e)
        {
            if (!TRIMMODE)
            {
                if (CurrentEdit.media.Count > 1)
                    ExitSave();
                else
                    Toast.MakeText(this, Resource.String.includevideo, ToastLength.Long).Show();
            }
        }

        private void _adapter_OnDelete(MediaItem m, int index)
        {
            preview.StopPlayback();
            _adapter.UpdatePlaying(null);
            _sliveradapter.UpdateData(CurrentEdit.media);

            Bootleg.API.Bootlegger.BootleggerClient.LogUserAction("EditScreenDelete", new KeyValuePair<string, string>("mediaid", m.id), new KeyValuePair<string, string>("index", index.ToString()), new KeyValuePair<string, string>("editid", CurrentEdit.id), new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        RecyclerView timeline;
        View tracker;

        private double countuptocurrent()
        {
            double total = 0;
            for (int i = 0; i < play_index; i++)
            {
                total += (((CurrentEdit.media[i].outpoint != TimeSpan.Zero && CurrentEdit.media[i].outpoint != TimeSpan.MaxValue) ? CurrentEdit.media[i].outpoint : CurrentEdit.media[i].ClipLength) - CurrentEdit.media[i].inpoint).TotalMilliseconds;
            }
            return total;
        }

        private double CountToIndex(int index)
        {
            double total = 0;
            for (int i = 0; i < index; i++)
            {
                total += (((CurrentEdit.media[i].outpoint != TimeSpan.Zero && CurrentEdit.media[i].outpoint != TimeSpan.MaxValue) ? CurrentEdit.media[i].outpoint : CurrentEdit.media[i].ClipLength) - CurrentEdit.media[i].inpoint).TotalMilliseconds;
            }
            return total;
        }

        public double TotalMilis
        {
            get
            {
                return CurrentEdit.media.Sum(o => (((o.outpoint != TimeSpan.Zero && o.outpoint != TimeSpan.MaxValue) ? o.outpoint : o.ClipLength) - o.inpoint).TotalMilliseconds);
            }
        }

        private void Preview_OnPositionChange(int arg1, int arg2, int window, PLAYBACK_MODE mode)
        {
            if (mode == PLAYBACK_MODE.PLAY_EDIT)
            {
                if (Resources.Configuration.LayoutDirection == LayoutDirection.Rtl)
                    tracker.TranslationX = timeline.MeasuredWidth + Utils.dp2px(this,34.5f) - Math.Min((float)(((CountToIndex(window) + arg1) / TotalMilis) * timeline.MeasuredWidth), timeline.MeasuredWidth);
                else
                    tracker.TranslationX = Math.Min((float)(((CountToIndex(window) + arg1) / TotalMilis) * timeline.MeasuredWidth), timeline.MeasuredWidth);


                lefttimetotal.Text = TimeSpan.FromMilliseconds(CountToIndex(window) + arg1).ToString(@"mm\:ss");
            }
            else
            {
                if (Resources.Configuration.LayoutDirection == LayoutDirection.Rtl)
                    tracker.TranslationX = timeline.MeasuredWidth + Utils.dp2px(this, 34.5f) - Math.Min((float)(((countuptocurrent() + arg1) / TotalMilis) * timeline.MeasuredWidth), timeline.MeasuredWidth);
                else
                    tracker.TranslationX = Math.Min((float)(((countuptocurrent() + arg1) / TotalMilis) * timeline.MeasuredWidth), timeline.MeasuredWidth);

                lefttimetotal.Text = TimeSpan.FromMilliseconds(Math.Min((float)(((countuptocurrent() + arg1) / TotalMilis) * timeline.MeasuredWidth), timeline.MeasuredWidth)).ToString(@"mm\:ss");

            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            preview.StopPlayback();
            preview.Release();
            //CurrentEdit = null;
        }

        SliverEditAdapter _sliveradapter;

        private class SwapCallback : ItemTouchHelper.SimpleCallback
        {
            SingleEditAdapter _adapter;
            SliverEditAdapter _timeline;
            Editor editor;
            public SwapCallback(Editor editor, SingleEditAdapter adapter, SliverEditAdapter timeline) : base(ItemTouchHelper.Left | ItemTouchHelper.Right, 0)
            {
                _adapter = adapter;
                _timeline = timeline;
                this.editor = editor;
            }

            public override void ClearView(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
            {
                base.ClearView(recyclerView, viewHolder);
                viewHolder.ItemView.ScaleX = 1;
                viewHolder.ItemView.ScaleY = 1;
            }

            public override void OnMoved(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder, int fromPos, RecyclerView.ViewHolder target, int toPos, int x, int y)
            {
                base.OnMoved(recyclerView, viewHolder, fromPos, target, toPos, x, y);
                Bootlegger.BootleggerClient.LogUserAction("EditScreenMove",
                    new KeyValuePair<string, string>("to", toPos.ToString()),
                    new KeyValuePair<string, string>("from", fromPos.ToString()),
                    new KeyValuePair<string, string>("mediaid", (viewHolder as SingleEditAdapter.ViewHolder).currentitem.id),
                    new KeyValuePair<string, string>("editid", editor.CurrentEdit.id),
                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
            }

            public override float GetSwipeEscapeVelocity(float defaultValue)
            {
                return float.MaxValue;
            }

            public override int GetMovementFlags(RecyclerView recyclerView, RecyclerView.ViewHolder viewHolder)
            {

                if (viewHolder.AdapterPosition == _adapter.ItemCount - 1)
                {
                    return MakeMovementFlags(0, 0);
                }
                else
                {
                    //stop playback
                    editor.preview.StopPlayback();

                    //make bigger:
                    viewHolder.ItemView.ScaleX = 1.05f;
                    viewHolder.ItemView.ScaleY = 1.05f;

                    if (viewHolder.AdapterPosition == _adapter.ItemCount - 2)
                    {
                        //int swipeFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;
                        int dragFlags = ItemTouchHelper.Left;
                        return MakeMovementFlags(dragFlags, 0);
                    }
                    else
                    {
                        //int swipeFlags = ItemTouchHelper.Up | ItemTouchHelper.Down;
                        int dragFlags = ItemTouchHelper.Left | ItemTouchHelper.Right;
                        return MakeMovementFlags(dragFlags, 0);
                    }


                }
            }

            public override bool CanDropOver(RecyclerView recyclerView, RecyclerView.ViewHolder current, RecyclerView.ViewHolder target)
            {
                return (target.AdapterPosition != _adapter.ItemCount - 1);
            }

            public override bool OnMove(RecyclerView p0, RecyclerView.ViewHolder p1, RecyclerView.ViewHolder p2)
            {
                _adapter.OnItemMove(p1.AdapterPosition, p2.AdapterPosition);
                _adapter.NotifyDataSetChanged();
                _timeline.NotifyDataSetChanged();
                //editor.preview.StopPlayback();
                _adapter.UpdatePlaying(null);



                return true;
            }

            public override void OnSwiped(RecyclerView.ViewHolder p0, int p1)
            {
                //do nothing...
                _adapter.OnItemDismiss(p0.AdapterPosition);

                editor.allclipsfragment.UpdateEdit(editor.CurrentEdit.media);

                _adapter.NotifyDataSetChanged();
                _timeline.NotifyDataSetChanged();

                editor.preview.StopPlayback();
                editor.TRIMMODE = false;
                _adapter.UpdatePlaying(null);
                _adapter.TrimMode(null, false);
                editor.TrimOff();
            }

            public override bool IsItemViewSwipeEnabled
            {
                get
                {
                    return false;
                }
            }

            public override bool IsLongPressDragEnabled
            {
                get
                {
                    return !editor.TRIMMODE;
                }
            }
        }

        private void Preview_OnOutPointChanged(TimeSpan obj)
        {


            if (current_media != null)
                current_media.outpoint = obj;

            _sliveradapter.NotifyChanges(CurrentEdit.media);
            _adapter.NotifyChanges(CurrentEdit.media);
            UpdateTimings();

            Bootlegger.BootleggerClient.LogUserAction("EditScreenOPChange",
                    new KeyValuePair<string, string>("op", obj.ToString()),
                    new KeyValuePair<string, string>("mediaid", current_media.id),
                    new KeyValuePair<string, string>("editid", CurrentEdit.id),
                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        private void Preview_OnInPointChanged(TimeSpan obj)
        {

            if (current_media != null)
                current_media.inpoint = obj;

            _sliveradapter.NotifyChanges(CurrentEdit.media);
            _adapter.NotifyChanges(CurrentEdit.media);
            UpdateTimings();

            Bootlegger.BootleggerClient.LogUserAction("EditScreenIPChange",
                    new KeyValuePair<string, string>("ip", obj.ToString()),
                    new KeyValuePair<string, string>("mediaid", current_media.id),
                    new KeyValuePair<string, string>("editid", CurrentEdit.id),
                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        bool TRIMMODE = false;

        void TrimOn()
        {
            FindViewById(Resource.Id.buttons).Alpha = 0.4f;
            FindViewById(Resource.Id.savebtn).Alpha = 0.4f;
        }

        void TrimOff()
        {
            FindViewById(Resource.Id.buttons).Alpha = 1f;
            FindViewById(Resource.Id.savebtn).Alpha = 1f;
        }

        private void _adapter_OnTrim(MediaItem obj)
        {
            if (!TRIMMODE)
            {
                TRIMMODE = true;
                _adapter.TrimMode(obj, true);
                _adapter.UpdatePlaying(obj);

                //open in trim mode
                current_media = obj;
                play_index = CurrentEdit.media.IndexOf(current_media);
                FindViewById(Resource.Id.edittimeline).Visibility = ViewStates.Invisible;
                TrimOn();

                try
                {
                    preview.StopPlayback();
                    _adapter.UpdatePlaying(null);
                    preview.TrimVideo(current_media);
                }
                catch (Exception e)
                {
                    //Toast.MakeText(this, e.Message, ToastLength.Short).Show();
                    LoginFuncs.ShowToast(this, e);

                }

                Bootlegger.BootleggerClient.LogUserAction("EditScreenTrim",
                    new KeyValuePair<string, string>("mediaid", current_media.id),
                    new KeyValuePair<string, string>("editid", CurrentEdit.id),
                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
            }
            else
            {
                preview.StopPlayback();
                TRIMMODE = false;
                _adapter.UpdatePlaying(null);
                _adapter.TrimMode(null, false);
                TrimOff();
            }
        }

        private int play_index = 0;
        private MediaItem current_media;
        private void StartPlayback()
        {
            TRIMMODE = false;
            _adapter.UpdatePlaying(null);
            _adapter.TrimMode(null, false);
            TrimOff();

            FindViewById(Resource.Id.edittimeline).Visibility = ViewStates.Visible;
            //FindViewById(Resource.Id.pausebtn).Visibility = ViewStates.Invisible;

            if (CurrentEdit.media.Count == 1)
                return;

            //play current index:
            if (play_index < 0)
                play_index = 0;

            if (play_index > CurrentEdit.media.Count - 2)
                play_index = CurrentEdit.media.Count - 2;

            current_media = CurrentEdit.media[play_index];

            try
            {
                preview.PlaySequence(CurrentEdit.media, current_media);
            }
            catch (Exception e)
            {
                //Toast.MakeText(this, e.Message, ToastLength.Short).Show();
                LoginFuncs.ShowToast(this, e);

            }

            Bootlegger.BootleggerClient.LogUserAction("EditScreenPlaySeq",
                    new KeyValuePair<string, string>("mediaid", current_media.id),
                    new KeyValuePair<string, string>("editid", CurrentEdit.id),
                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        private void _adapter_OnPreview(MediaItem item)
        {
            if (!TRIMMODE)
            {
                play_index = CurrentEdit.media.IndexOf(item);
                StartPlayback();
            }
        }

        AllClipsFragment allclipsfragment;

        private void Fragment_OnChosen(MediaItem obj)
        {
            var index = CurrentEdit.media.IndexOf(currentpick);

            try
            {
                CurrentEdit.media.RemoveAt(index);
            }
            catch
            {
                index = CurrentEdit.media.Count() - 1;
            }
            var newobj = obj.Copy();

            newobj.inpoint = TimeSpan.Zero;
            newobj.outpoint = newobj.ClipLength;

            CurrentEdit.media.Insert(index, newobj);

            allclipsfragment.UpdateEdit(CurrentEdit.media);

            currentpick = null;
            _adapter.UpdateData(CurrentEdit.media);
            _sliveradapter.UpdateData(CurrentEdit.media);
            UpdateTimings();
            CollapsePane();
            (allclipsfragment as IImagePausable).Pause();

            Bootlegger.BootleggerClient.LogUserAction("EditScreenAdd",
                    new KeyValuePair<string, string>("mediaid", obj.id),
                    new KeyValuePair<string, string>("index", index.ToString()),
                    new KeyValuePair<string, string>("editid", CurrentEdit.id),
                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));
        }

        //bool animating = false;

        void UpdateTimings()
        {
            //update in/out points of global timings:
            FindViewById<TextView>(Resource.Id.righttimetotal).Text = TimeSpan.FromMilliseconds(TotalMilis).ToString(@"mm\:ss");
        }

        void CollapsePane()
        {
            preview.StopPlayback();
            _adapter.UpdatePlaying(null);
            bottompanel.Visibility = ViewStates.Gone;

            IsExpanded = false;
        }

        void ExpandPane()
        {
            bottompanel.Visibility = ViewStates.Visible;
            (allclipsfragment as IImagePausable).Resume();

            IsExpanded = true;
            preview.StopPlayback();
            _adapter.UpdatePlaying(null);
        }

        bool IsExpanded = false;

        //string videofile = "";
        private void Fragment_OnPreview(MediaItem arg1, View arg2)
        {
            //pause stuff:
            preview.StopPlayback();
            _adapter.UpdatePlaying(null);

            Intent i = new Intent(this, typeof(Preview));
            i.PutExtra(Review.PREVIEW, arg1.id);
            i.PutExtra(Review.INGEST_MODE, true);
            i.PutExtra(Review.READ_ONLY, true);

            StartActivity(i);
        }

        public override void OnBackPressed()
        {
            try
            {
                if (TRIMMODE)
                {
                    TRIMMODE = false;
                    _adapter.TrimMode(null, false);
                    preview.StopPlayback();
                    _adapter.UpdatePlaying(null);
                    TrimOff();
                    return;
                }

                if (IsExpanded)
                {
                    currentpick = null;
                    CollapsePane();
                    (allclipsfragment as IImagePausable).Pause();
                }
                else
                {

                    Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
                    builder.SetMessage(Resource.String.savecancelmsg);
                    builder.SetPositiveButton(Resource.String.savebtnshort, (e, o) =>
                    {
                        ExitSave();
                    })
                    .SetNegativeButton(Resource.String.cancelbtn, async (e, o) =>
                     {
                         //Reset back to old version:
                         //CurrentEdit = OriginalVersion;
                         ShouldAutoSave = false;
                         await Bootlegger.BootleggerClient.SaveEdit(OriginalVersion);
                         autosaver.Dispose();
                         Finish();
                     });
                    builder.Create().Show();
                }
            }
            catch (Exception e)
            {
                LoginFuncs.ShowError(this, e);
            }
        }

        private void _adapter_OnChange(MediaItem obj, Shot.ShotTypes tp)
        {
            if (!TRIMMODE)
            {
                currentpick = obj;
                if (tp == Shot.ShotTypes.TITLE)
                {
                    //show dialog:
                    var builder = new Android.Support.V7.App.AlertDialog.Builder(this);

                    preview.StopPlayback();
                    _adapter.UpdatePlaying(null);

                    FrameLayout frameView = new FrameLayout(this);
                    var di = LayoutInflater.Inflate(Resource.Layout.textentrydialog, frameView);
                    di.FindViewById<EditText>(Resource.Id.text).Text = obj.titletext;
                    if (!string.IsNullOrEmpty(obj.titletext))
                    {
                        var lines = obj.titletext?.Split('\n');
                        di.FindViewById<TextView>(Resource.Id.counter).Text = GetString(Resource.String.linecounter, lines.Count());
                    }


                    builder.SetView(frameView);
                    builder.SetPositiveButton(Android.Resource.String.Ok, (o, e) => { });
                    builder.SetTitle(Resource.String.entertitledialog);
                    builder.SetCancelable(true);


                    var dialog = builder.Show();

                    var cantsubmit = false;


                    Button positiveButton = (Button)dialog.GetButton((int)DialogButtonType.Positive);
                    positiveButton.Click += delegate
                    {
                        if (cantsubmit)
                            return;

                        var lns = di.FindViewById<EditText>(Resource.Id.text).Text.ToString().Split('\n');
                        if (lns.Length > 8)
                        {
                            //LoginFuncs.ShowError(this, Resource.String.titlelengthwarning);
                            TextInputLayout lay = dialog.FindViewById<TextInputLayout>(Resource.Id.textlayout);
                            lay.ErrorEnabled = true;
                            lay.Error = GetString(Resource.String.titlelengthwarning);
                        }
                        else
                        {
                            var sanitisetext = Regex.Replace(di.FindViewById<TextView>(Resource.Id.text).Text, @"\p{Cs}", "").Replace('"', '\'').TrimStart('\n');

                            currentpick.titletext = sanitisetext;

                            if (!string.IsNullOrEmpty(currentpick.titletext))
                            {
                                var index = CurrentEdit.media.IndexOf(currentpick);
                                try
                                {
                                    CurrentEdit.media.RemoveAt(index);
                                }
                                catch
                                {
                                    index = CurrentEdit.media.Count() - 1;
                                }

                                var newobj = obj.Copy();
                                newobj.MediaType = Shot.ShotTypes.TITLE;
                                newobj.Status = MediaItem.MediaStatus.DONE;
                                newobj.inpoint = TimeSpan.Zero;
                                newobj.outpoint = TimeSpan.FromSeconds(3);
                                newobj.event_id = Bootlegger.BootleggerClient.CurrentEvent.id;
                                CurrentEdit.media.Insert(index, newobj);

                                allclipsfragment.UpdateEdit(CurrentEdit.media);


                                _adapter.UpdateData(CurrentEdit.media);
                                _sliveradapter.UpdateData(CurrentEdit.media);

                                UpdateTimings();
                                Bootlegger.BootleggerClient.LogUserAction("EditScreenAddTitle",
                                    new KeyValuePair<string, string>("title", currentpick.titletext),
                                    new KeyValuePair<string, string>("editid", CurrentEdit.id),
                                    new KeyValuePair<string, string>("eventid", Bootlegger.BootleggerClient.CurrentEvent.id));

                                currentpick = null;
                                dialog.Dismiss();
                            }
                            else
                            {
                                //dont insert blank title:
                                //currentpick = null;
                            }
                        }
                    };

                    dialog.FindViewById<EditText>(Resource.Id.text).RequestFocus();
                    dialog.FindViewById<EditText>(Resource.Id.text).TextChanged += (sender, e) =>
                    {
                        int linetoolong = -1;
                        var lns = e.Text.ToString().Split('\n');


                        for (int line = 0;line<lns.Length;line++)
                        {
                            if (lns[line].Length > 29)
                            {
                                linetoolong = line;
                                break;
                            }
                        }

                        if (linetoolong!=-1)
                        {
                            TextInputLayout titlelayout = dialog.FindViewById<TextInputLayout>(Resource.Id.textlayout);
                            titlelayout.ErrorEnabled = true;
                            titlelayout.Error = Resources.GetString(Resource.String.linetoolong,(linetoolong+1));
                            cantsubmit = true;
                        }
                        else
                        {
                            TextInputLayout titlelayout = dialog.FindViewById<TextInputLayout>(Resource.Id.textlayout);
                            titlelayout.ErrorEnabled = false;
                            cantsubmit = false;
                        }

                        dialog.FindViewById<TextView>(Resource.Id.counter).Text = GetString(Resource.String.linecounter, lns.Length);
                    };

                    //dialog.Show();
                }
                else
                {
                    ExpandPane();
                    (allclipsfragment as IImagePausable).Resume();
                }
            }
        }



        MediaItem currentpick;

        SingleEditAdapter _adapter;
        EditVideoView preview;


        public void ExitSave()
        {
            ShouldAutoSave = false;
            preview.StopPlayback();
            _adapter.UpdatePlaying(null);

            if (!CurrentEdit.media.Any(n => n.MediaType != Shot.ShotTypes.TITLE && n.Status != MediaItem.MediaStatus.PLACEHOLDER))
            {
                Toast.MakeText(this, Resource.String.includevideo, ToastLength.Long).Show();
                ShouldAutoSave = true;
                return;
            }

            Android.Support.V7.App.AlertDialog.Builder builder = new Android.Support.V7.App.AlertDialog.Builder(this);
            FrameLayout frameView = new FrameLayout(this);
            builder.SetView(frameView);
            var diag = builder.Create();
            diag.SetTitle(Resource.String.saveedit);

            LayoutInflater inflater = diag.LayoutInflater;
            View dialoglayout = inflater.Inflate(Resource.Layout.editsavedlg, frameView);

            //set title of video if there is one:
            if (string.IsNullOrWhiteSpace(CurrentEdit.title))
            {
                if (CurrentEdit.media.First().MediaType == Shot.ShotTypes.TITLE)
                    CurrentEdit.title = CurrentEdit.media.First().titletext;
            }

            //adjust for audio:
            if (CurrentMusic != null)
            {
                foreach (var m in CurrentEdit.media)
                {
                    m.audio = null;
                    m.credits = null;
                }

                CurrentEdit.media.First().audio = CurrentMusic.path;
                CurrentEdit.media.First().credits = CurrentMusic.caption;
            }

            dialoglayout.FindViewById<TextView>(Resource.Id.title).Text = CurrentEdit.title;

            if (WhiteLabelConfig.USE_EDIT_DESCRIPTION)
            {
                dialoglayout.FindViewById<TextView>(Resource.Id.description).Text = CurrentEdit.description;
            }
            else
            {
                dialoglayout.FindViewById<TextView>(Resource.Id.description).Visibility = ViewStates.Gone;
            }

            dialoglayout.FindViewById<Button>(Resource.Id.savebtn).Click += async (o, e) =>
            {
                bool doit = true;
                if (dialoglayout.FindViewById<TextView>(Resource.Id.title).Text.Length < 5)
                {
                    TextInputLayout titlelayout = dialoglayout.FindViewById<TextInputLayout>(Resource.Id.title_layout);
                    titlelayout.ErrorEnabled = true;
                    titlelayout.Error = Resources.GetString(Resource.String.entertitle);
                    doit = false;
                }

                if (WhiteLabelConfig.USE_EDIT_DESCRIPTION)
                {
                    if (dialoglayout.FindViewById<TextView>(Resource.Id.description).Text.Length < 5)
                    {
                        TextInputLayout titlelayout = dialoglayout.FindViewById<TextInputLayout>(Resource.Id.description_layout);
                        titlelayout.ErrorEnabled = true;
                        titlelayout.Error = Resources.GetString(Resource.String.enterdescription);
                        doit = false;
                    }
                }

                if (doit)
                {
                    CurrentEdit.title = dialoglayout.FindViewById<TextView>(Resource.Id.title).Text;
                    CurrentEdit.description = dialoglayout.FindViewById<TextView>(Resource.Id.description).Text;
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true);
                    try
                    {
                        await Bootlegger.BootleggerClient.SaveEdit(CurrentEdit);
                        Intent i = new Intent(this.ApplicationContext, typeof(Review));
                        i.PutExtra("processed", true);
                        StartActivity(i);
                        Finish();
                    }
                    catch (Exception ex)
                    {
                        _adapter.UpdateData(CurrentEdit.media);
                        _sliveradapter.UpdateData(CurrentEdit.media);
                        UpdateTimings();
                        //Toast.MakeText(this, Resources.GetString(Resource.String.editerror), ToastLength.Long).Show();
                        LoginFuncs.ShowToast(this, ex);
                    }
                    finally
                    {
                        AndHUD.Shared.Dismiss();
                        diag.Cancel();
                    }
                }
            };
            dialoglayout.FindViewById<Button>(Resource.Id.sharebtn).Click += async (o, e) =>
            {
                bool doit = true;

                if (dialoglayout.FindViewById<TextView>(Resource.Id.title).Text.Length < 5)
                {
                    TextInputLayout titlelayout = dialoglayout.FindViewById<TextInputLayout>(Resource.Id.title_layout);
                    titlelayout.ErrorEnabled = true;
                    titlelayout.Error = Resources.GetString(Resource.String.entertitle);
                    doit = false;
                }

                if (WhiteLabelConfig.USE_EDIT_DESCRIPTION)
                {
                    if (dialoglayout.FindViewById<TextView>(Resource.Id.description).Text.Length < 5)
                    {
                        TextInputLayout titlelayout = dialoglayout.FindViewById<TextInputLayout>(Resource.Id.description_layout);
                        titlelayout.ErrorEnabled = true;
                        titlelayout.Error = Resources.GetString(Resource.String.enterdescription);
                        doit = false;
                    }
                }

                if (doit)
                {
                    CurrentEdit.title = dialoglayout.FindViewById<TextView>(Resource.Id.title).Text;
                    CurrentEdit.description = dialoglayout.FindViewById<TextView>(Resource.Id.description).Text;
                    AndHUD.Shared.Show(this, Resources.GetString(Resource.String.loading), -1, MaskType.Black, null, null, true);
                    try
                    {
                        await Bootlegger.BootleggerClient.StartEdit(CurrentEdit);
                        Bundle conData = new Bundle();
                        conData.PutBoolean("processed", true);
                        Intent intent = new Intent();
                        intent.PutExtras(conData);
                        SetResult(Result.Ok, intent);
                        Intent i = new Intent(this.ApplicationContext, typeof(Review));
                        StartActivity(i);
                    }
                    catch (Exception ex)
                    {
                        _adapter.UpdateData(CurrentEdit.media);
                        _sliveradapter.UpdateData(CurrentEdit.media);
                        UpdateTimings();
                        LoginFuncs.ShowToast(this, ex);
                    }
                    finally
                    {
                        AndHUD.Shared.Dismiss();
                        diag.Cancel();
                    }
                }
            };
            diag.SetCancelable(true);
            diag.CancelEvent += (o,e) =>
            {
                ShouldAutoSave = true;
            };
            diag.Show();
            dialoglayout.Post(() =>
            {
                dialoglayout.FindViewById<TextView>(Resource.Id.title).RequestFocus();
            });
        }
    }
}