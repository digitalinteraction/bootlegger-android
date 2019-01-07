using System;
using System.Collections.Generic;
using Android.App;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Bootleg.API.Model;

namespace Bootleg.Droid.Adapters
{
    class MusicAdapter : RecyclerView.Adapter
    {
        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            Music currentitem;
            public event Action<Music> OnPreview;
            public event Action<Music> OnSelect;
            MusicAdapter adpt;

            public ViewHolder(View itemView, MusicAdapter adpt) : base(itemView)
            {
                view = itemView;
                view.Click += ViewHolder_Click1;
                this.adpt = adpt;
                view.FindViewById<ImageButton>(Resource.Id.previewbtn).Click += ViewHolder_Click;
                view.FindViewById<ProgressBar>(Resource.Id.progress).Indeterminate = true;

            }

            private void ViewHolder_Click1(object sender, EventArgs e)
            {
                OnSelect?.Invoke(currentitem);
            }

            internal void SetItem(Music item,int position)
            {
                if (position == 0)
                    view.FindViewById<ImageButton>(Resource.Id.previewbtn).Visibility = ViewStates.Invisible;
                else
                    view.FindViewById<ImageButton>(Resource.Id.previewbtn).Visibility = ViewStates.Visible;

                //if (item.IsPlaying)
                //    view.FindViewById<ProgressBar>(Resource.Id.progress).Visibility = ViewStates.Visible;
                //else
                //    view.FindViewById<ProgressBar>(Resource.Id.progress).Visibility = ViewStates.Invisible;


                currentitem = item;

                view.FindViewById<TextView>(Resource.Id.firstLine).Text = item.title;

                if(currentitem.IsPlaying)
                    view.FindViewById<ImageButton>(Resource.Id.previewbtn).SetImageResource(Resource.Drawable.ic_action_pause);
                else
                    view.FindViewById<ImageButton>(Resource.Id.previewbtn).SetImageResource(Resource.Drawable.ic_play_arrow_black_24dp);

                if (item.Equals(adpt.current) || (string.IsNullOrEmpty(item.url)) && string.IsNullOrEmpty(adpt.current?.url))
                {
                    view.FindViewById<ImageView>(Resource.Id.icon).SetImageResource(Resource.Drawable.ic_done_black_24dp);
                }
                else
                {
                    view.FindViewById<ImageView>(Resource.Id.icon).SetImageDrawable(null);
                }
            }

            private void ViewHolder_Click(object sender, EventArgs e)
            {
                //view.FindViewById<ProgressBar>(Resource.Id.progress).Visibility = ViewStates.Visible;
                OnPreview?.Invoke(currentitem);
            }
        }

        List<Music> options = new List<Music>();

        internal void Update(List<Music> options)
        {
            this.options.Clear();

            this.options.Add(new Music() { title =  Application.Context.GetString(Resource.String.nomusic)  });

            this.options.AddRange(options);
            
            NotifyDataSetChanged();
        }

        Music current;
        public MusicAdapter(Music current) : base()
        {
            this.current = current;
        }

        public event Action<Music> OnPreview;
        public event Action<Music> OnSelected;

        public override int ItemCount => options.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = options[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item,position);
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewtype)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.musicitem, parent, false);
            ViewHolder vh = new ViewHolder(itemView,this);
            vh.OnPreview += Vh_OnPreview;
            vh.OnSelect += Vh_OnSelect;
            return vh;
        }

        private void Vh_OnSelect(Music obj)
        {
            current = obj;
            OnSelected?.Invoke(obj);
            NotifyDataSetChanged();
        }

        private void Vh_OnPreview(Music obj)
        {
            OnPreview?.Invoke(obj);
        }

        internal void UpdateBuffered(Music obj)
        {
            foreach (var o in options)
            {
                o.IsBuffered = false;
            }

            obj.IsBuffered = true;

            NotifyDataSetChanged();
        }

        internal void UpdatePlaying(Music obj)
        {
            foreach(var o in options)
            {
                o.IsPlaying = false;
            }

            obj.IsPlaying = true;

            NotifyDataSetChanged();
        }
    }
}