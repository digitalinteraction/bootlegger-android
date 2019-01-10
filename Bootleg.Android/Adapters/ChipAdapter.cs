using System;
using System.Collections.Generic;
using System.Linq;

using Android.App;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;
using Bootleg.API.Model;
using static Android.Graphics.PorterDuff;
using static Android.Widget.CompoundButton;
using static Bootleg.API.Bootlegger;

namespace Bootleg.Droid.Adapters
{
    class ChipAdapter : RecyclerView.Adapter
    {
        public class ViewHolder : RecyclerView.ViewHolder
        {
            // each data item is just a string in this case
            private View view;
            ChipAdapter adpt;
            Topic currentitem;

            public ViewHolder(View itemView, ChipAdapter adpt) : base(itemView)
            {
                view = itemView;
                this.adpt = adpt;
            }

            private async void ViewHolder_Select(object sender, CheckedChangeEventArgs e)
            {


                if (adpt.media != null)
                {
                    try
                    {

                        var key = $"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}";
                        if (!adpt.media.Static_Meta.ContainsKey(key))
                            adpt.media.Static_Meta[key] = "";


                        var mt = adpt.media.Static_Meta[key].Split(',').ToList();
                        if (e.IsChecked)
                        {
                            mt.Add(currentitem.id);
                        }
                        else
                        {
                            mt.Remove(currentitem.id);
                        }

                        //remove blanks
                        mt.RemoveAll((n) => string.IsNullOrEmpty(n));
                        if (mt.Count > 0)
                            adpt.media.Static_Meta[$"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}"] = string.Join(",", mt);
                        else
                            adpt.media.Static_Meta.Remove($"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}");

                        BootleggerClient.SaveMedia(adpt.media);
                        await BootleggerClient.UpdateMeta(adpt.media);

                        if (e.IsChecked && !adpt._readonly)
                            view.FindViewById<ToggleButton>(Resource.Id.chip).SetCompoundDrawablesWithIntrinsicBounds(0, 0, Resource.Drawable.baseline_check_white_24, 0);
                        else
                            view.FindViewById<ToggleButton>(Resource.Id.chip).SetCompoundDrawablesWithIntrinsicBounds(0, 0, 0, 0);


                    }
                    catch (Exception)
                    {
                        //failed
                        Toast.MakeText(Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity, Resource.String.noconnectionshort, ToastLength.Short).Show();
                    }

                }
                else
                {
                    if (e.IsChecked && !adpt._readonly)
                        view.FindViewById<ToggleButton>(Resource.Id.chip).SetCompoundDrawablesWithIntrinsicBounds(0, 0, Resource.Drawable.baseline_check_white_24, 0);
                    else
                        view.FindViewById<ToggleButton>(Resource.Id.chip).SetCompoundDrawablesWithIntrinsicBounds(0, 0, 0, 0);
                    adpt.FireFilterUpdate(currentitem);
                }
            }

            internal void SetItem(Topic item)
            {
                currentitem = item;
                view.FindViewById<ToggleButton>(Resource.Id.chip).TextOn = item.GetLocalisedTagName(view.Resources.Configuration.Locale.Language);
                view.FindViewById<ToggleButton>(Resource.Id.chip).TextOff = item.GetLocalisedTagName(view.Resources.Configuration.Locale.Language);
                view.FindViewById<ToggleButton>(Resource.Id.chip).Text = item.GetLocalisedTagName(view.Resources.Configuration.Locale.Language);

                view.FindViewById<ToggleButton>(Resource.Id.chip).Enabled = !adpt._readonly;
                //view.FindViewById<ToggleButton>(Resource.Id.chip).Clickable = !adpt._readonly;

                if (adpt.media != null)
                {
                    view.FindViewById<ToggleButton>(Resource.Id.chip).CheckedChange -= ViewHolder_Select;

                    view.FindViewById<ToggleButton>(Resource.Id.chip).Checked = adpt.media.Static_Meta?[$"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}"].Split(',').Contains(item.id) ?? false;

                    view.FindViewById<ToggleButton>(Resource.Id.chip).CheckedChange += ViewHolder_Select;

                    if (view.FindViewById<ToggleButton>(Resource.Id.chip).Checked && !adpt._readonly)
                        view.FindViewById<ToggleButton>(Resource.Id.chip).SetCompoundDrawablesWithIntrinsicBounds(0, 0, Resource.Drawable.baseline_check_white_24, 0);
                }
                else
                {
                    view.FindViewById<ToggleButton>(Resource.Id.chip).CheckedChange -= ViewHolder_Select;
                    view.FindViewById<ToggleButton>(Resource.Id.chip).CheckedChange += ViewHolder_Select;
                }

                //var index = item.ToCharArray().Sum(x => x);
                //int index = Array.IndexOf(Bootlegger.BootleggerClient.CurrentEvent.topics?.Split(',') ?? new string[0], item);

                //Console.WriteLine(item + " : " + index);

                view.FindViewById<ToggleButton>(Resource.Id.chip).Background.SetColorFilter(Color.ParseColor(item.color), Mode.Multiply);

                //var index = BootleggerClient.CurrentEvent.topics.Find((t) => t.id == mtopics.First());
                //view.SetBackgroundColor(Color.ParseColor(index.color));
            }
        }

        List<Topic> filter = new List<Topic>();

        private void FireFilterUpdate(Topic item)
        {
            if (filter.Contains(item))
                filter.Remove(item);
            else
                filter.Add(item);

            OnTopicFilterChanged?.Invoke(filter);
        }

        MediaItem media;
        List<Topic> options;
        //List<string> selected;

        internal void Update(List<Topic> optionsIn, MediaItem mediaIn)
        {
            this.options.Clear();

            media = mediaIn;

            if (!mediaIn?.Static_Meta.ContainsKey($"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}") ?? false)
            {
                mediaIn.Static_Meta.Add($"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}", "");
            }

            if (optionsIn == null || optionsIn?.Count == 0)
            {
                //lookup and add topics from the metadata:

                var ids = mediaIn.Static_Meta[$"{((!WhiteLabelConfig.PUBLIC_TOPICS) ? BootleggerClient.CurrentUser?.id : "")}-{MetaDataFields.Topics}"].Split(',');
                //var topics = ids.Select(i => BootleggerClient.CurrentEvent.topics.Find((obj) => obj.id == i) ?? null);
                foreach (var t in ids)
                {
                    var tt = BootleggerClient.CurrentEvent.topics.Find((obj) => obj.id == t);
                    if (tt != null)
                        this.options.Add(tt);
                }
            }
            else
            {
                this.options.AddRange(optionsIn);
            }

            NotifyDataSetChanged();
        }

        private Activity context;
        bool _readonly = true;
        public ChipAdapter(Activity context, bool _readonly) : base()
        {
            this.context = context;
            this._readonly = _readonly;
            options = new List<Topic>();
        }

        public event Action<List<Topic>> OnTopicFilterChanged;

        public override int ItemCount => options.Count;

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            var item = options[position];
            ViewHolder view = holder as ViewHolder;
            view.SetItem(item);
        }

        public int GetIndexForItem(Topic item) => options.IndexOf(item);

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewtype)
        {
            View itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.topicchip, parent, false);
            ViewHolder vh = new ViewHolder(itemView, this);
            return vh;
        }
    }
}