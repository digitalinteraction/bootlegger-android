/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;
using Android.Content;
using Android.Util;
using System;
using Java.Text;
using Android.Text.Format;
using Android.App;
using Android.Views;
using AndroidHUD;

namespace Bootleg.Droid
{
    public static class Utils
    {

        public static void DissmissHud()
        {
            try
            {
                AndHUD.Shared.Dismiss();
            }
            catch
            {
                //cant dismiss hud
            }
        }

        public static Activity GetActivity(this View view)
        {
            Context context = view.Context;

            while (context is ContextWrapper)
            {
                if (context is Activity)
                {
                    return (Activity)context;
                }
                context = ((ContextWrapper)context).BaseContext;
            }
            return null;
        }

        public static string GetStringByName(Context context,string aString)
        {
            string packageName = context.PackageName;
            int resId = context.Resources.GetIdentifier(aString, "string", packageName);
            return context.GetString(resId);
        }

        public static string LocalizeTimeDiff(this DateTime input)
        {
            DateTime sTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            long milis = (long)(input.ToUniversalTime() - sTime).TotalMilliseconds;
            return DateUtils.GetRelativeTimeSpanStringFormatted(milis,Java.Lang.JavaSystem.CurrentTimeMillis(),DateUtils.DayInMillis).ToString();
        }

        public static string LocalizeFormat(this DateTime input, string format)
        {
            TimeSpan t = input - new DateTime(1970, 1, 1);
            long secondsSinceEpoch = (long)t.TotalSeconds;


            var ff = new SimpleDateFormat(format);
            var output = ff.Format(new Java.Util.Date(secondsSinceEpoch * 1000));
           // var s = DateFormat.Format(, secondsSinceEpoch);
            return output;
        }


        public static List<T> Swap<T>(this List<T> list, int indexA, int indexB)
        {
            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
            return list;
        }

        public static int sp2px(Context activity, float spValue)
        {
            int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Sp, spValue, activity.Resources.DisplayMetrics);
            return px;
        }

        public static int dp2px(Context activity, float spValue)
        {
            int px = (int)TypedValue.ApplyDimension(ComplexUnitType.Dip, spValue, activity.Resources.DisplayMetrics);
            return px;
        }
    }
}