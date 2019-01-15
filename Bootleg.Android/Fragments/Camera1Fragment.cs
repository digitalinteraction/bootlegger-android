/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using Android.Content;
using Android.Hardware;
using Android.Media;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Bootleg.Droid.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#pragma warning disable CS0618 // Camera 1 API is obsolete, use only when Camera2 isn't available

namespace Bootleg.Droid.Fragments
{
    public class Camera1Fragment : Android.Support.V4.App.Fragment, Android.Hardware.Camera.IPictureCallback,ICameraDriver
    {
        private Android.Hardware.Camera mCamera;
        private CameraPreview mPreview;
        private View mCameraView;

        public event Action<string> OnError;

        public Camera1Fragment()
        {

        }

        public void CloseCamera()
        {

        }

        public IList<Java.Lang.Integer> ZoomLevels
        {
            get
            {
                return mCamera.GetParameters().ZoomRatios;
            }
        }

        public bool HasFlash
        {
            get
            {
                var para = mCamera.GetParameters();
                if (para.SupportedFlashModes != null)
                    return (para.SupportedFlashModes != null && para.SupportedFlashModes.Contains(Android.Hardware.Camera.Parameters.FlashModeTorch));
                else
                    return false;
            }
        }

        public bool HasZoom
        {
            get
            {
                return mCamera?.GetParameters().IsZoomSupported ?? false;
            }
        }

        public int NumCameras
        {
            get
            {
                return Android.Hardware.Camera.NumberOfCameras;
            }
        }

        event Action SetupComplete;

        public event Action OnSetupComplete
        {
            add
            {
                lock (objectLock)
                {
                    SetupComplete += value;
                }
            }

            remove
            {
                lock (objectLock)
                {
                    SetupComplete -= value;
                }
            }
        }

        event Action PictureTaken;

        object objectLock = new Object();
        event Action ICameraDriver.OnPictureTaken
        {
            add
            {
                lock (objectLock)
                {
                    PictureTaken += value;
                }
            }

            remove
            {
                lock (objectLock)
                {
                    PictureTaken -= value;
                }
            }
        }

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
        }

        public static Camera1Fragment NewInstance(int camera)
        {
            Camera1Fragment fragment = new Camera1Fragment(camera);
            return fragment;
        }

        public Camera1Fragment(int camera)
        {
            CURRENTCAMERA = camera;
        }

        Camera1Controls controls;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.Camera1Fragment, container, false);

            bool opened = safeCameraOpenInView(view);

            
            controls = new Camera1Controls(mCamera);
            ChildFragmentManager.BeginTransaction().Replace(Resource.Id.camera_controls, controls).Commit();

            if (!opened)
            {
                Console.WriteLine("Camera failed to open");
                return view;
            }

            SetupComplete?.Invoke();

            //view.Post(() => view.RequestLayout());

            return view;
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            base.OnViewCreated(view, savedInstanceState);
            //view.Invalidate();
        }

        private bool safeCameraOpenInView(View view)
        {
            bool opened = false;
            releaseCameraAndPreview();
            mCamera = getCameraInstance(CURRENTCAMERA);
            mCameraView = view;
            opened = mCamera != null;

            if (opened)
            {
                mPreview = new CameraPreview(Activity.BaseContext, mCamera, view);
                FrameLayout preview = view.FindViewById<FrameLayout>(Resource.Id.camera_preview);
                preview.AddView(mPreview, 0);
                mPreview.StartCameraPreview();
                try
                {
                    mCamera.EnableShutterSound(false);
                }
                catch
                {

                }
            }

            return opened;
        }

        public static Camera getCameraInstance(int camera)
        {
            Camera c = null;
            try
            {
                c = Camera.Open(camera);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return c;
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            releaseCameraAndPreview();
        }

        private void releaseCameraAndPreview()
        {
            if (mCamera != null)
            {
                mCamera.StopPreview();
                mCamera.Release();
                mCamera = null;
            }
            if (mPreview != null)
            {
                mPreview.DestroyDrawingCache();
                mPreview.mCamera = null;
            }
        }

        string thumbnailfilename;

        public void OnPictureTaken(byte[] data, Camera camera)
        { 
            FileStream outt;
            try
            {
                outt = new FileStream(thumbnailfilename, FileMode.CreateNew);
                outt.Write(data, 0, data.Length);
                outt.Flush(true);
                outt.Close();
                PictureTaken?.Invoke();
            }
            catch (Exception)
            {

            }
            finally
            {
                mPreview.StartCameraPreview();
            }
        }

        public void TakePhoto(string filename)
        {
            thumbnailfilename = filename;
            mCamera.TakePicture(null, null, this);
        }

        MediaRecorder recorder;

        int CURRENTCAMERA = 0;

        public void StartRecord(string filename)
        {
            if (recorder != null)
            {
                recorder.Reset();
            }
            else
            {
                recorder = new MediaRecorder();
            }
            mCamera.Lock();
            mCamera.Unlock();

            recorder.SetCamera(mCamera);
            recorder.SetVideoSource(VideoSource.Camera);
            recorder.SetAudioSource(AudioSource.Camcorder);

            try
            {
                //try for full hd
                recorder.SetProfile(CamcorderProfile.Get(CURRENTCAMERA, CamcorderQuality.Q1080p));
            }
            catch
            {
                try
                {
                    recorder.SetProfile(CamcorderProfile.Get(CURRENTCAMERA, CamcorderQuality.Q720p));
                }
                catch
                {

                    try
                    {
                        //if not hd, try for highest phone can give
                        recorder.SetProfile(CamcorderProfile.Get(CURRENTCAMERA, CamcorderQuality.High));
                    }
                    catch
                    {
                        //if not, then set to low
                        recorder.SetProfile(CamcorderProfile.Get(CURRENTCAMERA, CamcorderQuality.Q480p));
                    }
                }
            }

            //recorder.SetAudioEncoder(AudioEncoder.Aac);
            //recorder.SetVideoEncoder(VideoEncoder.H264);
            recorder.SetPreviewDisplay(mPreview.Holder.Surface);
            recorder.SetOutputFile(filename);
            recorder.SetMaxDuration(1000 * 60 * 5);

            try
            {
                recorder.Prepare();
                recorder.Start();
            }
            catch (Exception e)
            {
                OnError?.Invoke(e.ToString());
            }
        }

        public void StopRecord()
        {
            recorder.Stop();
            recorder.Reset();
            if (mCamera != null)
                mCamera.Lock();
        }

        public void FlashOn()
        {
            var para = mCamera.GetParameters();
            para.FlashMode = Android.Hardware.Camera.Parameters.FlashModeTorch;
            mCamera.SetParameters(para);
        }

        public void FlashOff()
        {
            var para = mCamera.GetParameters();
            para.FlashMode = Android.Hardware.Camera.Parameters.FlashModeOff;
            mCamera.SetParameters(para);
        }

        public void Zoom(int level)
        {
            if (mCamera != null)
            {
                var param = mCamera.GetParameters();
                param.Zoom = level;
                mCamera.SetParameters(param);
            }
        }

    }

    public class CameraPreview : SurfaceView, ISurfaceHolderCallback
    {
        public Camera mCamera;
        private Context mContext;
        private Camera.Size mPreviewSize;
        private IEnumerable<Camera.Size> mSupportedPreviewSizes;
        private View mCameraView;

        public CameraPreview(Context context, Camera camera, View cameraView) : base(context)
        {
            mCameraView = cameraView;
            mContext = context;
            setCamera(camera);
            Holder.AddCallback(this);
            Holder.SetKeepScreenOn(true);
        }

        public void StartCameraPreview()
        {
            try
            {
                mCamera.SetPreviewDisplay(Holder);
                
                mCamera.StartPreview();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        private void setCamera(Camera camera)
        {
            mCamera = camera;
            mSupportedPreviewSizes = mCamera.GetParameters().SupportedPreviewSizes;
            RequestLayout();
        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {
            try
            {
                mCamera.SetPreviewDisplay(holder);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            FrameLayout.LayoutParams lp = this.LayoutParameters as FrameLayout.LayoutParams;

            Display display = mContext.GetSystemService(Context.WindowService).JavaCast<IWindowManager>().DefaultDisplay;
            Android.Graphics.Point rect = new Android.Graphics.Point();
            display.GetRealSize(rect);
            display.GetRealSize(rect);

            
            lp.Gravity = GravityFlags.CenterVertical;
            lp.Width = rect.X; // required width
            lp.Height = (int)(rect.X * (9/16f)); // required height
            this.LayoutParameters = lp;
        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {
            if (mCamera != null)
            {
                mCamera.StopPreview();
            }
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Android.Graphics.Format format, int width, int height)
        {
            if (Holder.Surface == null) return;

            try
            {
                Camera.Parameters parameters = mCamera.GetParameters();
                //parameters.FocusMode = Camera.Parameters.FocusModeInfinity;

                if (parameters.IsVideoStabilizationSupported)
                    parameters.VideoStabilization = true;

                if (mPreviewSize != null)
                {
                    Camera.Size previewSize = mPreviewSize;
                    parameters.SetPreviewSize(previewSize.Width, previewSize.Height);
                }

                mCamera.SetParameters(parameters);
                mCamera.StartPreview();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            int width = ResolveSize(SuggestedMinimumWidth, widthMeasureSpec);
            int height = ResolveSize(SuggestedMinimumHeight, heightMeasureSpec);

            SetMeasuredDimension(width, height);

            //Console.WriteLine(width);

            //force to widescreen aspect:

            height = (int)(width * (1080f / 1920));

            if (mSupportedPreviewSizes != null)
            {
                mPreviewSize = getOptimalPreviewSize(mSupportedPreviewSizes, width, height);
            }
        }

        protected override void OnLayout(bool changed, int left, int top, int right, int bottom)
        {
            if (!changed) return;

            int width = right - left;
            int height = bottom - top;
            int previewWidth = width;
            int previewHeight = height;
            int degrees = 0;

            if (mPreviewSize != null)
            {
                Display display = mContext.GetSystemService(Context.WindowService).JavaCast<IWindowManager>().DefaultDisplay;

                switch (display.Rotation)
                {
                    case SurfaceOrientation.Rotation0:
                        previewWidth = mPreviewSize.Height;
                        previewHeight = mPreviewSize.Width;
                        mCamera.SetDisplayOrientation(90);
                        degrees = 0;
                        break;
                    case SurfaceOrientation.Rotation90:
                        previewWidth = mPreviewSize.Width;
                        previewHeight = mPreviewSize.Height;
                        degrees = 90;
                        break;
                    case SurfaceOrientation.Rotation180:
                        previewWidth = mPreviewSize.Height;
                        previewHeight = mPreviewSize.Width;
                        degrees = 180;
                        break;
                    case SurfaceOrientation.Rotation270:
                        previewWidth = mPreviewSize.Width;
                        previewHeight = mPreviewSize.Height;
                        mCamera.SetDisplayOrientation(180);
                        degrees = 270;
                        break;
                }
            }

            int scaledChildHeight = previewHeight * width / previewWidth;

            //adjust to make explicitally 16:9
            //height = (int)(width * (1080f / 1920));

            //Console.WriteLine("Scaled to " + width + " : " + scaledChildHeight);

            mCameraView.Layout(0, (height - scaledChildHeight)/2, width, scaledChildHeight + ((height - scaledChildHeight) / 2));

            Camera.CameraInfo info = new Camera.CameraInfo();
            Camera.GetCameraInfo(0, info);

            int rotate = (info.Orientation - degrees + 360) % 360;
            Camera.Parameters camParams = mCamera.GetParameters();
            camParams.SetRotation(rotate);
            mCamera.SetParameters(camParams);
        }

        private Camera.Size getOptimalPreviewSize(IEnumerable<Camera.Size> sizes, int width, int height)
        {
            Camera.Size optimalSize = null;
            double ASPECT_TOLERANCE = 0.1;
            double targetRatio = (double)width / height;

            // Try to find a size match which suits the whole screen minus the menu on the left.
            foreach (Camera.Size size in sizes.OrderBy(o=>o.Width))
            {
                double ratio = (double)size.Width / size.Height;

                if (ratio <= targetRatio + ASPECT_TOLERANCE && ratio >= targetRatio - ASPECT_TOLERANCE)
                {
                    optimalSize = size;
                }
            }

            // If we cannot find the one that matches the aspect ratio, ignore the requirement.
            if (optimalSize == null)
            {
                // TODO : Backup in case we don't get a size.
            }
            return optimalSize;
        }
    }
}
#pragma warning restore CS0618 // Type or member is obsolete