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
using Android.Support.V7.Widget;
using Android.Graphics;
using Square.Picasso;
//using Com.Tonicartos.Superslim;
using Android.Support.V4.Content;
using Android.Graphics.Drawables;
using Android.Content;
using Bootleg.Droid.UI;
using Bootleg.API.Model;

namespace Bootleg.Droid
{
    public class EditAdapter : RecyclerView.Adapter
    {

        public enum EditTileType { VIEW_TYPE_CONTENT = 0x00, VIEW_TYPE_TITLE = 0x02 };


        //private static int LINEAR = 0;

        public class HeaderEditItem
        {
            public Edit Edit { get; set; }
            public EditTileType ItemViewType { get; set; }
            public string HeaderText { get; set; }
            public int SectionFirstPosition { get; set; }
            public int SectionManager { get; set; }
            public string SubText { get; set; }
            public int Icon { get; set; }
        }

        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            EditAdapter adpt;
            Edit currentedit;
        
            public ViewHolder(View itemView, EditAdapter adpt) : base(itemView)
            {
                view = itemView;
                this.adpt = adpt;
                //view.Click += View_Click;

                if (view.FindViewById<ImageButton>(Resource.Id.popup) != null)
                    view.FindViewById<ImageButton>(Resource.Id.popup).Click += ViewHolder_Click3;

                    //if (view.FindViewById<ImageButton>(Resource.Id.sharebtn)!=null)
                    //    view.FindViewById<ImageButton>(Resource.Id.sharebtn).Click += ViewHolder_Click;
                    //if (view.FindViewById<ImageButton>(Resource.Id.deletebtn) != null)
                    //    view.FindViewById<ImageButton>(Resource.Id.deletebtn).Click += ViewHolder_Click1;
                    //if (view.FindViewById<ImageButton>(Resource.Id.restartbtn) != null)
                    //    view.FindViewById<ImageButton>(Resource.Id.restartbtn).Click += ViewHolder_Click2;

                    view.Click += View_Click;
            }

            private void ViewHolder_Click3(object sender, EventArgs e)
            {
                Context wrapper = new ContextThemeWrapper(view.Context, Resource.Style.EditPopup);
                Android.Support.V7.Widget.PopupMenu popup = new Android.Support.V7.Widget.PopupMenu(wrapper, view.FindViewById<ImageButton>(Resource.Id.popup));
                popup.MenuInflater.Inflate(Resource.Menu.edit_menu, popup.Menu);
                popup.MenuItemClick += Popup_MenuItemClick;

                if (currentedit.fail)
                    popup.Menu.FindItem(Resource.Id.restart_menu_item).SetVisible(true);
                else
                    popup.Menu.FindItem(Resource.Id.restart_menu_item).SetVisible(false);

                if (currentedit.progress > 97 && !currentedit.fail)
                    popup.Menu.FindItem(Resource.Id.share_menu_item).SetVisible(true);
                else
                    popup.Menu.FindItem(Resource.Id.share_menu_item).SetVisible(false); 

                if (currentedit.code == null)
                    popup.Menu.FindItem(Resource.Id.delete_menu_item).SetVisible(true);
                else
                    popup.Menu.FindItem(Resource.Id.delete_menu_item).SetVisible(false);

                if (adpt.CurrentEvent == null)
                    popup.Menu.FindItem(Resource.Id.copy_menu_item).SetVisible(false);
                else
                    popup.Menu.FindItem(Resource.Id.copy_menu_item).SetVisible(true);


                popup.Show();
            }

            private async void Popup_MenuItemClick(object sender, Android.Support.V7.Widget.PopupMenu.MenuItemClickEventArgs e)
            {
                if (e.Item.ItemId == Resource.Id.share_menu_item)
                    ViewHolder_Click(null, null);

                if (e.Item.ItemId == Resource.Id.restart_menu_item)
                    ViewHolder_Click2(null, null);

                if (e.Item.ItemId == Resource.Id.copy_menu_item)
                {
                    try
                    {
                        var newedit = new Edit()
                        {
                            media = currentedit.media,
                            title = view.Context.GetString(Resource.String.copyof, currentedit.title),
                            user_id = currentedit.user_id
                        };
                        await Bootlegger.BootleggerClient.SaveEdit(newedit);
                        //refresh list:
                        adpt.OnRefresh?.Invoke();
                    }
                    catch
                    {
                        LoginFuncs.ShowError(view.Context,Resource.String.noconnectionshort);
                    }
                }

                if (e.Item.ItemId == Resource.Id.delete_menu_item)
                   ViewHolder_Click1(null, null);
            }

            private void View_Click(object sender, EventArgs e)
            {
                //if its a complete edit, then play it:
                if (currentedit != null && currentedit.path != "" && adpt.OnPreview!=null)
                    adpt.OnPreview(currentedit,view);
            }

            private void ViewHolder_Click2(object sender, EventArgs e)
            {
                //restart edit after fail...
                adpt.OnRestart?.Invoke(currentedit);
            }

            private void ViewHolder_Click1(object sender, EventArgs e)
            {
                //edit button
                adpt.OnDelete?.Invoke(currentedit);
            }

            internal void SetItem(HeaderEditItem item)
            {
                if (item.ItemViewType == EditTileType.VIEW_TYPE_TITLE)
                {
                    currentedit = null;
                    //view.FindViewById<TextView>(Resource.Id.header).Text = item.HeaderText;
                    //view.FindViewById<TextView>(Resource.Id.subheader).Text = item.SubText;
                    view.FindViewById<TextView>(Resource.Id.header).Text = item.HeaderText;
                    //view.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(item.Icon);
                }
                else if (item.ItemViewType == EditTileType.VIEW_TYPE_CONTENT)
                {
                    bool changed = false;
                    if (currentedit != item.Edit)
                        changed = true;
                    currentedit = item.Edit;
                    if (changed)
                    {

                        if (currentedit.media.Count > 0)
                        {
                            //get first media that has actual image:
                            try
                            {
                                var media = (from n in currentedit.media where n.MediaType != Shot.ShotTypes.TITLE select n).First();

                                if (media != null)
                                {
                                    Picasso.With(view.Context).
                                    Load(Bootlegger.BootleggerClient.LoginUrl.Scheme + "://" + Bootlegger.BootleggerClient.LoginUrl.Host + "/media/thumbnail/" + media.id + "?s=" + WhiteLabelConfig.THUMBNAIL_SIZE).
                                    Config(Bitmap.Config.Rgb565).
                                    Tag(adpt).
                                    Fit().
                                    CenterCrop().
                                    Into(view.FindViewById<ImageView>(Resource.Id.editimage));
                                }
                                else
                                {
                                    view.FindViewById<ImageView>(Resource.Id.editimage).SetImageDrawable(null);
                                }
                            }
                            catch(Exception)
                            {
                                //no valid media items in edit:
                                view.FindViewById<ImageView>(Resource.Id.editimage).SetImageDrawable(null);
                            }
                        }
                    }

                    view.FindViewById<TextView>(Resource.Id.title).Text = currentedit.title;
                    //Console.WriteLine(currentedit.title);
                    view.FindViewById<TextView>(Resource.Id.date).Text = currentedit.createdAt.LocalizeTimeDiff();
                    if (currentedit.code!=null) // still editing, or has been submitted
                    {
                        view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.progress).ProgressColor = new Color(ContextCompat.GetColor(view.Context,Resource.Color.blue));
                        
                        //finished
                        if (currentedit.progress > 97 && !currentedit.fail)
                        {
                            //view.FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Visible;
                            view.FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Gone;
                        }
                        else
                        {
                            //still going
                            //view.FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Invisible;
                            view.FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Visible;
                            view.FindViewById<RadialProgress.CleanRadialProgressView>(Resource.Id.progress).Value = (int)(currentedit.progress ?? 0);
                        }

                        //view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Gone;

                        if (currentedit.fail)
                        {
                            //view.FindViewById<ImageButton>(Resource.Id.restartbtn).Visibility = ViewStates.Visible;
                            view.FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Gone;
                            view.FindViewById<TextView>(Resource.Id.failreason).Visibility = ViewStates.Visible;
                            view.FindViewById<TextView>(Resource.Id.failreason).Text = currentedit.failreason;
                        }
                        else
                        {
                            //view.FindViewById<ImageButton>(Resource.Id.restartbtn).Visibility = ViewStates.Gone;
                            view.FindViewById<TextView>(Resource.Id.failreason).Visibility = ViewStates.Gone;
                        }
                    }
                    else
                    {
                        view.FindViewById<TextView>(Resource.Id.failreason).Visibility = ViewStates.Gone;
                        //view.FindViewById<ImageButton>(Resource.Id.sharebtn).Visibility = ViewStates.Invisible;
                        view.FindViewById<View>(Resource.Id.progress).Visibility = ViewStates.Gone;
                        //view.FindViewById<ImageButton>(Resource.Id.deletebtn).Visibility = ViewStates.Visible;
                    }
                }
            }

            private void ViewHolder_Click(object sender, EventArgs e)
            {
                adpt.OnShare.Invoke(currentedit);
            }
        }

        internal void UpdateEdit(Edit obj)
        {
            var e = allitems.FindIndex(o => { return o.Edit != null && o.Edit.id == obj.id; });
            allitems[e].Edit = obj;

            context.RunOnUiThread(() => {
                NotifyItemChanged(e);
            });
        }

        List<HeaderEditItem> allitems;
        Activity context;
        Shoot CurrentEvent = null;

        public EditAdapter(Activity context, Dictionary<Bootlegger.BootleggerEditStatus, List<Edit>> items,Shoot currentevent)
                : base()
        {
            this.context = context;
            this.CurrentEvent = currentevent;
            UpdateData(items,false);
        }


        public override long GetItemId(int position)
        {
            return allitems[position].Edit?.id.GetHashCode() ?? allitems[position].GetHashCode();
        }

        public event Action<Edit> OnShare;
        public event Action<Edit,View> OnPreview;
        public event Action<Edit> OnEdit;
        public event Action<Edit> OnDelete;
        public event Action<Edit> OnRestart;
        public event Action OnRefresh;


        public override int ItemCount
        {
            get
            {
                return allitems.Count();
            }
        }
     
        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = allitems[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
            //GridSLM.LayoutParams lp = GridSLM.LayoutParams.From(view.ItemView.LayoutParameters);

            //lp.SetSlm(GridSLM.Id);
            //lp.NumColumns = 1;
            ////lp.ColumnWidth = 300;
            //lp.FirstPosition = item.SectionFirstPosition;
            //view.ItemView.LayoutParameters = lp;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent,int viewtype)
        {
            View itemView;
            if (viewtype == (int)EditTileType.VIEW_TYPE_CONTENT)
            {
                itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.edititem, parent, false);
            }
            else if (viewtype == (int)EditTileType.VIEW_TYPE_TITLE)
            {
                itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.myeditstitle, parent, false);
            }
            else
            {
                itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.myeditstitle, parent, false);
            }

            ViewHolder vh = new ViewHolder(itemView,this);
            return vh;
        }

        //bool editing;
        internal void UpdateData(Dictionary<Bootlegger.BootleggerEditStatus, List<Edit>> myEdits,bool force)
        {
            if (!force && myEdits.Count == allitems?.Count)
                return;

            allitems = null;
            allitems = new List<HeaderEditItem>();

            int headerCount = 0;
            int sectionFirstPosition = 0;

            int i = 0;
            foreach (var keyval in myEdits)
            {
                if (CurrentEvent == null)
                {
                    //if (allitems.Count == 0)
                    //    allitems.Add(new HeaderEditItem() { HeaderText = context.GetString(Resource.String.finishedvideos), ItemViewType = EditTileType.VIEW_TYPE_TITLE, SectionFirstPosition = sectionFirstPosition });

                    if (keyval.Key == Bootlegger.BootleggerEditStatus.Complete)
                    {
                        foreach (var m in keyval.Value)
                        {
                            allitems.Add(new HeaderEditItem() { Edit = m, SectionFirstPosition = sectionFirstPosition });
                            i++;
                        }
                    }
                }
                else
                {
                    var filtered = from n in keyval.Value where n.media.FindAll((h)=>h.event_id==CurrentEvent.id).Count > 0 select n;
                    var count = filtered.Count();

                    if (count > 0)
                    {
                        sectionFirstPosition = i + headerCount;
                        headerCount++;
                        allitems.Add(new HeaderEditItem() { ItemViewType = EditTileType.VIEW_TYPE_TITLE, SectionFirstPosition = sectionFirstPosition, SubText = Java.Lang.String.Format("%d", count), HeaderText = GetHeader(keyval.Key) });
                    }


                    //filtered is wrong...
                    foreach (var m in filtered)
                    {
                        allitems.Add(new HeaderEditItem() { Edit = m, SectionFirstPosition = sectionFirstPosition });
                        i++;
                    }
                }
            }

            if (CurrentEvent == null)
            {
                //if (allitems.Count == 0)
                allitems.Insert(0,new HeaderEditItem() { HeaderText = context.GetString(Resource.String.finishedvideos), ItemViewType = EditTileType.VIEW_TYPE_TITLE, SectionFirstPosition = sectionFirstPosition });
            }

            if (CurrentEvent != null && allitems.Count == 0)
            {
                //if (allitems.Count == 0)
                allitems.Insert(0, new HeaderEditItem() { HeaderText = context.GetString(Resource.String.finishedvideos), ItemViewType = EditTileType.VIEW_TYPE_TITLE, SectionFirstPosition = sectionFirstPosition });
            }

            NotifyDataSetChanged();
        }

        string GetHeader(Bootlegger.BootleggerEditStatus status)
        {
            switch (status)
            {
                case Bootlegger.BootleggerEditStatus.Draft:
                    return context.GetString(Resource.String.unfinishededits);
                    break;
                case Bootlegger.BootleggerEditStatus.InProgress:
                    return context.GetString(Resource.String.inprogressedits);
                    break;
                case Bootlegger.BootleggerEditStatus.Complete:
                default:
                    return context.GetString(Resource.String.completededits);
                    break;
            }
            //(keyval.Key == Bootlegger.BootleggerEditStatus.InProgress ?  : )
        }

        public override int GetItemViewType(int position)
        {
            return (int)allitems[position].ItemViewType;
        }
    }
}