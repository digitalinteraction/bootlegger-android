using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.Content;
using Android.Views;
using Android.Widget;
using Bootleg.API;
using Java.Lang;
using static Android.Graphics.PorterDuff;
using static Bootleg.Droid.AllClipsFragment;

namespace Bootleg.Droid.Adapters
{
    class IconOrderSpinnerAdapter : ArrayAdapter<FilterTuple<Bootlegger.MediaItemFilterDirection, string>>
    {

        Context context;

        public IconOrderSpinnerAdapter(Context context):base(context, Android.Resource.Layout.ActivityListItem)
        {
            this.context = context;
        }

        public override View GetDropDownView(int position, View convertView, ViewGroup parent)
        {
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

            switch (GetItem(position).Item1)
            {
                case Bootlegger.MediaItemFilterDirection.ASCENDING:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_action_sort_by_attributes_interface_button_option_1);
                    holder.Text.Text = context.GetString(Resource.String.orderup);
                    break;

                case Bootlegger.MediaItemFilterDirection.DESCENDING:
                    holder.Icon.SetImageResource(Resource.Drawable.ic_action_sort_by_attributes);
                    holder.Text.Text = context.GetString(Resource.String.orderdown);
                    break;

            }

            return view;
        }

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            return GetColoredView(position, convertView, parent, false);
        }

    }
}