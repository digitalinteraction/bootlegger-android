/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.Hardware.Camera2;
using Android.Widget;

namespace Bootleg.Droid.Fragments
{
    public class FocusCallback : CameraCaptureSession.CaptureCallback
    {
        Camera2Fragment fragment;
        public FocusCallback(Camera2Fragment frag)
        {
            fragment = frag;
        }

        public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
        {
            base.OnCaptureCompleted(session, request, result);
            if (request.Tag?.ToString() == "FOCUS")
            {
                fragment.FocusComplete();
                //the focus trigger is complete -
                //resume repeating (preview surface will get frames), clear AF trigger
            }
        }

    }
}
