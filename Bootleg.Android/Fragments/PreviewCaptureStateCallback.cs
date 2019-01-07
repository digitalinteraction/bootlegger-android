/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.Hardware.Camera2;
using Android.Widget;

namespace Bootleg.Droid.Fragments
{
    public class PreviewCaptureStateCallback : CameraCaptureSession.StateCallback
    {
        Camera2Fragment fragment;
        bool recorder = false;
        public PreviewCaptureStateCallback(Camera2Fragment frag,bool recorder)
        {
            this.recorder = recorder;
            fragment = frag;
        }
        public override void OnConfigured(CameraCaptureSession session)
        {
            fragment.previewSession = session;
            fragment.updatePreview();
            fragment.Activity.RunOnUiThread(() =>
            {
                fragment.textureView.RequestLayout();
            });
            if (recorder)
            {
                fragment.mediaRecorder.Start();
            }
        }

        public override void OnConfigureFailed(CameraCaptureSession session)
        {
            if (null != fragment.Activity)
                Toast.MakeText(fragment.Activity, "Failed", ToastLength.Short).Show();
        }
    }
}
