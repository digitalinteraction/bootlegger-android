/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
 using System;
using Android.Content;
using Android.Views;

namespace Bootleg.Droid
{
    class ScreenOrientationEvent:OrientationEventListener
    {
        public event Action<bool> ShowWarning;

        public ScreenOrientationEvent(Context con)
            : base(con, Android.Hardware.SensorDelay.Normal)
        {

        }

        public override void OnOrientationChanged(int orientation)
        {

            //needs to be near 180
            if (orientation > 250 && orientation < 290)
            {
                if (ShowWarning != null)
                    ShowWarning(false);
            }
            else
            {
                if (ShowWarning != null)
                    ShowWarning(true);
            }
        }
    }
}