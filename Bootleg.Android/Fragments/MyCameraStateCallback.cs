/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.Hardware.Camera2;
using Android.Widget;

namespace Bootleg.Droid.Fragments
{
    public class MyCameraStateCallback : CameraDevice.StateCallback
    {
        Camera2Fragment fragment;
        public MyCameraStateCallback(Camera2Fragment frag)
        {
            fragment = frag;
        }

        public override void OnOpened(CameraDevice camera)
        {
            fragment.cameraDevice = camera;
            fragment.startPreview();
            fragment.cameraOpenCloseLock.Release();
            if (null != fragment.textureView)
                fragment.configureTransform(fragment.textureView.Width, fragment.textureView.Height);
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            fragment.cameraOpenCloseLock.Release();
            camera.Close();
            fragment.cameraDevice = null;
        }

        public override void OnError(CameraDevice camera, CameraError error)
        {
            fragment.cameraOpenCloseLock.Release();
            camera.Close();
            fragment.cameraDevice = null;
            if (null != fragment.Activity)
            {
                fragment.MakeError(error);
                //fragment.Activity.Finish();
            }
        }


    }
}
