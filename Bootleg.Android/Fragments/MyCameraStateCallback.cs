/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System;
using Android.Hardware.Camera2;
using Android.Widget;
using Microsoft.AppCenter.Crashes;

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

            try
            {
                fragment.cameraDevice = camera;
                fragment.startPreview();
                fragment.cameraOpenCloseLock.Release();
                if (null != fragment.textureView)
                    fragment.configureTransform(fragment.textureView.Width, fragment.textureView.Height);
            }
            catch (Exception e)
            {
                Crashes.TrackError(e);
            }
        }

        public override void OnDisconnected(CameraDevice camera)
        {
            try
            {
                fragment.cameraOpenCloseLock.Release();
            camera.Close();
            fragment.cameraDevice = null;
            }
            catch (Exception e)
            {
                Crashes.TrackError(e);
            }
        }

        public override void OnError(CameraDevice camera, CameraError error)
        {
            fragment.cameraOpenCloseLock.Release();
            camera.Close();
            fragment.cameraDevice = null;
            if (null != fragment.Activity)
            {
                fragment.MakeError(error);
            }
        }


    }
}
