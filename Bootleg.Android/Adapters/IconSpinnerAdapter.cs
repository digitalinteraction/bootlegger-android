using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Support.V4.Widget;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Java.Lang;
using static Android.Graphics.PorterDuff;
using static Bootleg.Droid.AllClipsFragment;

namespace Bootleg.Droid.Adapters
{
    class IconSpinnerAdapter : ArrayAdapter<FilterTuple<Bootlegger.MediaItemFilterType, string>>
    {

        Context context;

        //private List<FilterTuple<Bootlegger.MediaItemFilterType, string>> objects;


        public IconSpinnerAdapter(Context context):base(context, Android.Resource.Layout.ActivityListItem)
        {
            this.context = context;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
            //blue icons
            return GetColoredView(position, convertView, parent, true);
        }

        View GetColoredView(int position, View convertView, ViewGroup parent, bool color)
        {
            var view = convertView;
            IconSpinnerAdapterViewHolder holder = null;

            if (view != null)
                holder = view.Tag as IconSpinnerAdapterViewHolder;

            if (holder == null)
            {
                holder = new IconSpinnerAdapterViewHolder();
                var inflater = context.GetSystemService(Context.LayoutInflaterService).JavaCast<LayoutInflater>();
                view = inflater.Inflate(Resource.Layout.icon_list_item, parent, false);
                holder.Icon = view.FindViewById<ImageView>(Resource.Id.icon);
                holder.Text = view.FindViewById<TextView>(Resource.Id.text);
                view.Tag = holder;
            }

            if (color)
            {
                holder.Icon.SetColorFilter(new Color(ContextCompat.GetColor(context, Resource.Color.blue)), Mode.Multiply);
                holder.Text.SetTextColor(new Color(ContextCompat.GetColor(context, Resource.Color.blue)));
            }
            else
            {
                holder.Icon.SetColorFilter(Color.White);
                holder.Text.SetTextColor(Color.White);
            }

            //ImageViewCompat.SetImageTintList(holder.Icon, ColorStateList.ValueOf(ContextCompat.GetColor(context, Resource.Color.blue)));

            switch (GetItem(position).Item1)
            {
                case Bootlegger.MediaItemFilterType.CONTRIBUTOR:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_people_white_48dp);
                    holder.Text.Text = context.GetString(Resource.String.bywho);
                    break;

                case Bootlegger.MediaItemFilterType.DATE:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_event_white_48dp);
                    holder.Text.Text = context.GetString(Resource.String.bywhen);
                    ;
                    break;

                case Bootlegger.MediaItemFilterType.LENGTH:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_action_timer);
                    holder.Text.Text = context.GetString(Resource.String.bylength);
                    break;

                case Bootlegger.MediaItemFilterType.PHASE:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_access_time_white_24dp);
                    holder.Text.Text = context.GetString(Resource.String.byphase);
                    break;

                case Bootlegger.MediaItemFilterType.ROLE:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_videocam_white_24dp);
                    holder.Text.Text = context.GetString(Resource.String.bycamera);
                    break;

                case Bootlegger.MediaItemFilterType.SHOT:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_photo_white_48dp);
                    holder.Text.Text = context.GetString(Resource.String.bywhat);
                    break;

                case Bootlegger.MediaItemFilterType.TOPIC:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_tag_white_24dp);
                    holder.Text.Text = context.GetString(Resource.String.topic);
                    break;
            }

            return view;
        }
       
        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            //white icons
            return GetColoredView(position, convertView, parent, false);
        }

    }

    class IconSpinnerAdapterViewHolder : Java.Lang.Object
    {
        public ImageView Icon{ get; set; }
        public TextView Text { get; set; }

    }
}