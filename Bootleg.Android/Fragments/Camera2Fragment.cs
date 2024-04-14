/* Copyright (C) 2014 Newcastle University
 *
 * This software may be modified and distributed under the terms
 * of the MIT license. See the LICENSE file for details.
 */
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.OS;
using Android.Util;
using Android.Views;
using Android.Widget;
using Android.Graphics;
using Android.Hardware.Camera2;
using Android.Hardware.Camera2.Params;
using Android.Media;

using Java.Lang;
using Java.Util;
using Java.IO;
using Java.Util.Concurrent;
using Bootleg.Droid.UI;
using System;
using Java.Nio;

namespace Bootleg.Droid.Fragments
{
    public class Camera2Fragment : Android.Support.V4.App.Fragment, ICameraDriver
    {
        public Camera2Fragment()
        {
            //empty constructor for Java mapping
        }

        private const string TAG = "Camera2VideoFragment";
        private SparseIntArray ORIENTATIONS = new SparseIntArray();

        // AutoFitTextureView for camera preview
        public AutoFitTextureView textureView;

        public CameraDevice cameraDevice;
        public CameraCaptureSession previewSession;
        public MediaRecorder mediaRecorder;

        //private bool isRecordingVideo;
        public Semaphore cameraOpenCloseLock = new Semaphore(1);

        internal void MakeError(CameraError error)
        {
            OnError?.Invoke(error.ToString());
        }

        public event Action<string> OnError;

        // Called when the CameraDevice changes state
        private MyCameraStateCallback stateListener;
        // Handles several lifecycle events of a TextureView
        private MySurfaceTextureListener surfaceTextureListener;

        public CaptureRequest.Builder builder;
        private CaptureRequest.Builder previewBuilder;

        private Size videoSize;
        private Size previewSize;


        private HandlerThread backgroundThread;
        private Handler backgroundHandler;

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

        object objectLock = new System.Object();
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

        public static string GetCam(CameraManager _manager, Video.CAMERA_POSITION pos)
        {
            string desiredCameraId = null;
            foreach (string cameraId in _manager.GetCameraIdList())
            {
                CameraCharacteristics chars = _manager.GetCameraCharacteristics(cameraId);
                //List < CameraCharacteristics.Key <?>> keys = chars.getKeys();
                try
                {
                    if ((int)chars.Get(CameraCharacteristics.LensFacing) == ((pos==Video.CAMERA_POSITION.REAR)?(int)LensFacing.Back:(int)LensFacing.Front))
                    {
                        // This is the one we want.
                        desiredCameraId = cameraId;
                        return desiredCameraId;
                    }
                }
                catch (IllegalArgumentException)
                {
                    // This key not implemented, which is a bit of a pain. Either guess - assume the first one
                    // is rear, second one is front, or give up.
                    
                }
            }
            return _manager.GetCameraIdList()[0];
        }

        public IList<Integer> ZoomLevels
        {
            get
            {
                string cameraId = GetCam(manager, CURRENTCAMERA);
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                var max = (int)characteristics.Get(CameraCharacteristics.ScalerAvailableMaxDigitalZoom);
                var list = new List<Integer>();
                for (int i=0; i<max*100;i++)
                {
                    list.Add(new Integer(i));
                }
                return list;
            }
        }

        public bool HasFlash
        {
            get
            {
                string cameraId = GetCam(manager, CURRENTCAMERA);
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                var yes = (bool)characteristics.Get(CameraCharacteristics.FlashInfoAvailable);
                return yes;
            }
        }

        public bool HasZoom
        {
            get
            {
                if (manager != null)
                {
                    string cameraId = GetCam(manager, CURRENTCAMERA);
                    CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);
                    var _max = (int)characteristics.Get(CameraCharacteristics.ScalerAvailableMaxDigitalZoom);
                    return _max > 0;
                }
                else
                {
                    return false;
                }
            }
        }

        public int NumCameras
        {
            get
            {
                return manager.GetCameraIdList().Length;
            }
        }

        public Camera2Fragment(Video.CAMERA_POSITION camera)
        {
            CURRENTCAMERA = camera;
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation0, 90);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation90, 0);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation180, 270);
            ORIENTATIONS.Append((int)SurfaceOrientation.Rotation270, 180);
            surfaceTextureListener = new MySurfaceTextureListener(this);
            stateListener = new MyCameraStateCallback(this);
        }

        public static Camera2Fragment newInstance(Video.CAMERA_POSITION camera)
        {
            var fragment = new Camera2Fragment(camera);
            return fragment;
        }

        private Size ChooseVideoSize(Size[] choices)
        {
            foreach (Size _size in choices)
            {
                if (_size.Width == _size.Height * 16 / 9 && _size.Width <= 1920)
                    return _size;
            }
            Log.Error(TAG, "Couldn't find any suitable video size");
            return choices[choices.Length - 1];
        }

        private Size ChooseOptimalSize(Size[] choices, int width, int height, Size aspectRatio)
        {
            var bigEnough = new List<Size>();
            int w = aspectRatio.Width;
            int h = aspectRatio.Height;
            foreach (Size option in choices)
            {
                if (option.Height == option.Width * h / w &&
                    option.Width >= width && option.Height >= height)
                    bigEnough.Add(option);
            }

            if (bigEnough.Count > 0)
                return (Size)Collections.Min(bigEnough, new CompareSizesByArea());
            else
            {
                Log.Error(TAG, "Couldn't find any suitable preview size");
                return choices[0];
            }
        }

        Bundle laststate = null;

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            laststate = savedInstanceState;
            return inflater.Inflate(Resource.Layout.Camera2Fragment, container, false);
        }

        public override void OnViewCreated(View view, Bundle savedInstanceState)
        {
            textureView = (AutoFitTextureView)view.FindViewById(Resource.Id.texture);
        }

        public override void OnStart()
        {
            base.OnStart();
        }

        public override void OnStop()
        {
            base.OnStop();
        }

        public override void OnResume()
        {
            base.OnResume();
            StartBackgroundThread();
            if (textureView.IsAvailable)
                openCamera(textureView.Width, textureView.Height,laststate);
            else
                textureView.SurfaceTextureListener = surfaceTextureListener;
        }

        public override void OnPause()
        {
            base.OnPause();
            //if (!started)
            //{
            //    closePreviewSession();
            //    CloseCamera();
            //    StopBackgroundThread();
            //}
        }

        private void StartBackgroundThread()
        {
            if (backgroundThread != null)
            {
                StopBackgroundThread();
            }
            backgroundThread = new HandlerThread("CameraBackground");
            backgroundThread.Start();
            backgroundHandler = new Handler(backgroundThread.Looper);
        }

        private void StopBackgroundThread()
        {
            backgroundThread.QuitSafely();
            try
            {
                backgroundThread.Join();
                backgroundThread = null;
                backgroundHandler = null;
            }
            catch (InterruptedException e)
            {
                e.PrintStackTrace();
            }
        }

        CameraManager manager;
        Rect max;
        Size size;

        //Tries to open a CameraDevice
        public void openCamera(int width, int height, Bundle savedInstanceState)
        {
            if (null == Activity || Activity.IsFinishing)
                return;

            manager = (CameraManager)Activity.GetSystemService(Context.CameraService);
            try
            {
                if (!cameraOpenCloseLock.TryAcquire(2500, TimeUnit.Milliseconds))
                    throw new RuntimeException("Time out waiting to lock camera opening.");

                string cameraId = GetCam(manager, CURRENTCAMERA);
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraId);

                StreamConfigurationMap map = (StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap);

                videoSize = ChooseVideoSize(map.GetOutputSizes(Class.FromType(typeof(MediaRecorder))));

                previewSize = ChooseOptimalSize(map.GetOutputSizes(Class.FromType(typeof(MediaRecorder))), width, height, videoSize);

                int orientation = (int)Resources.Configuration.Orientation;
                if (orientation == (int)Android.Content.Res.Orientation.Landscape)
                {
                    textureView.SetAspectRatio(previewSize.Width, previewSize.Height);
                }
                else
                {
                    textureView.SetAspectRatio(previewSize.Height, previewSize.Width);
                }
                configureTransform(width, height);
                mediaRecorder = new MediaRecorder();
                if (stateListener==null)
                    stateListener = new MyCameraStateCallback(this);

                manager.OpenCamera(cameraId, stateListener, null);
                SetupComplete?.Invoke();
                max = (Rect)characteristics.Get(CameraCharacteristics.SensorInfoActiveArraySize);
                size = (Size)characteristics.Get(CameraCharacteristics.SensorInfoPixelArraySize);

                controls = new Camera2Controls(cameraDevice,size,max);
                controls.OnFocus += DoFocus;
                try
                {
                    ChildFragmentManager.BeginTransaction().Replace(Resource.Id.camera_controls, controls).Commit();
                }
                catch (System.Exception)
                {

                }
            }
            catch (CameraAccessException)
            {
                Toast.MakeText(Activity, "Cannot access the camera.", ToastLength.Short).Show();
                Activity.Finish();
            }
            catch (NullPointerException)
            {
                //var dialog = new ErrorDialog();
                //dialog.Show(ChildFragmentManager, "dialog");
            }
            catch (InterruptedException)
            {
                throw new RuntimeException("Interrupted while trying to lock camera opening.");
            }
        }

        Camera2Controls controls;

        List<Surface> surfaces;
        //Start the camera preview
        public void startPreview()
        {
            if (null == cameraDevice || !textureView.IsAvailable || null == previewSize)
                return;

            try
            {
                closePreviewSession();
                SurfaceTexture texture = textureView.SurfaceTexture;
                //Assert.IsNotNull(texture);

                texture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);

                previewBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
                surfaces = new List<Surface>();
                var previewSurface = new Surface(texture);
                surfaces.Add(previewSurface);
                previewBuilder.AddTarget(previewSurface);

                if (previewBuilder != null)
                {
                    if (flashon)
                        previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Torch);

                    Rect sensor_rect = max;
                    int left = sensor_rect.Width() / 2;
                    int right = left;
                    int top = sensor_rect.Height() / 2;
                    int bottom = top;
                    int hwidth = (int)(sensor_rect.Width() / (2.0 * zoomlev));
                    int hheight = (int)(sensor_rect.Height() / (2.0 * zoomlev));
                    left -= hwidth;
                    right += hwidth;
                    top -= hheight;
                    bottom += hheight;
                    previewBuilder.Set(CaptureRequest.ScalerCropRegion, new Rect(left, top, right, bottom));
                }

                previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Off);
                previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);

                //var recorderSurface = mediaRecorder.Surface;
                //surfaces.Add(recorderSurface);
                //previewBuilder.AddTarget(recorderSurface);
                mPreviewSession = new PreviewCaptureStateCallback(this,false);
                cameraDevice.CreateCaptureSession(surfaces, mPreviewSession, backgroundHandler);

            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
            catch (IOException e)
            {
                e.PrintStackTrace();
            }
        }

        PreviewCaptureStateCallback mPreviewSession;

        public void CloseCamera()
        {
            try
            {
                cameraOpenCloseLock.Acquire();
                closePreviewSession();
                if (null != cameraDevice)
                {
                    cameraDevice.Close();
                    cameraDevice = null;
                }
                if (null != mediaRecorder)
                {
                    mediaRecorder.Release();
                    mediaRecorder = null;
                }
            }
            catch (InterruptedException)
            {
                throw new RuntimeException("Interrupted while trying to lock camera closing.");
            }
            catch (System.Exception)
            {

            }
            finally
            {
                cameraOpenCloseLock.Release();
            }
        }

        //Update the preview
        public void updatePreview()
        {
            if (null == cameraDevice)
                return;

            try
            {
                //SetUpCaptureRequestBuilder(previewBuilder);
                //HandlerThread thread = new HandlerThread("CameraPreview");
                //thread.Start();
                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
            }
            catch (CameraAccessException e)
            {
                e.PrintStackTrace();
            }
        }

        //private void setUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        //{
        //    builder.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);
        //    builder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Auto);

        //}

        //Configures the neccesary matrix transformation to apply to the textureView
        public void configureTransform(int viewWidth, int viewHeight)
        {
             if (null == Activity || null == previewSize || null == textureView)
                return;

            int rotation = (int)Activity.WindowManager.DefaultDisplay.Rotation;
            var matrix = new Matrix();
            var viewRect = new RectF(0, 0, viewWidth, viewHeight);
            var bufferRect = new RectF(0, 0, previewSize.Height, previewSize.Width);
            float centerX = viewRect.CenterX();
            float centerY = viewRect.CenterY();
            if ((int)SurfaceOrientation.Rotation90 == rotation || (int)SurfaceOrientation.Rotation270 == rotation)
            {
                bufferRect.Offset((centerX - bufferRect.CenterX()), (centerY - bufferRect.CenterY()));
                matrix.SetRectToRect(viewRect, bufferRect, Matrix.ScaleToFit.Fill);
                float scale = System.Math.Max(
                    (float)viewHeight / previewSize.Height,
                    (float)viewHeight / previewSize.Width);
                matrix.PostScale(scale, scale, centerX, centerY);
                matrix.PostRotate(90 * (rotation - 2), centerX, centerY);
            }

            textureView.SetTransform(matrix);
            View.RequestLayout();
        }

        string thumbnailfilename = "";

        private void SetUpMediaRecorder()
        {
            if (null == Activity)
                return;
            mediaRecorder.SetAudioSource(AudioSource.Mic);
            mediaRecorder.SetVideoSource(VideoSource.Surface);
            mediaRecorder.SetOutputFormat(OutputFormat.Mpeg4);
            mediaRecorder.SetOutputFile(thumbnailfilename);
            //CamcorderProfile profile;
            //if (CamcorderProfile.HasProfile(CamcorderQuality.Q1080p))
            //    profile = CamcorderProfile.Get(CURRENTCAMERA, CamcorderQuality.Q1080p);
            //else
            //    profile = CamcorderProfile.Get(CURRENTCAMERA, CamcorderQuality.High);
            //mediaRecorder.SetProfile(profile);
            mediaRecorder.SetVideoEncodingBitRate(10000000);
            mediaRecorder.SetVideoFrameRate(30);
            mediaRecorder.SetVideoSize(videoSize.Width, videoSize.Height);
            mediaRecorder.SetVideoEncoder(VideoEncoder.H264);
            mediaRecorder.SetAudioEncoder(AudioEncoder.Aac);


            //int rotation = (int)Activity.WindowManager.DefaultDisplay.Rotation;
            //int orientation = ORIENTATIONS.Get(rotation);

            SurfaceOrientation rotation = Activity.WindowManager.DefaultDisplay.Rotation;
            CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraDevice.Id);
            var mSensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
            int finalOrientation = (ORIENTATIONS.Get((int)rotation) + mSensorOrientation + 270) % 360;

            ////captureBuilder.Set(CaptureRequest.JpegOrientation, new Java.Lang.Integer(finalOrientation));

            if (CURRENTCAMERA == Video.CAMERA_POSITION.FRONT)
                mediaRecorder.SetOrientationHint(0);
            else
                mediaRecorder.SetOrientationHint(finalOrientation);

            mediaRecorder.Prepare();
        }

        private void closePreviewSession()
        {
            if (previewSession != null)
            {
                previewSession.Close();
                previewSession = null;
            }
        }

        private void StartRecordingVideo()
        {
            try
            {
                closePreviewSession();
                SetUpMediaRecorder();
                SurfaceTexture texture = textureView.SurfaceTexture;
                //Assert.IsNotNull(texture);
                //texture.SetDefaultBufferSize(previewSize.Width, previewSize.Height);
                previewBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.Record);
                var surfaces = new List<Surface>();
                var previewSurface = new Surface(texture);
                surfaces.Add(previewSurface);
                previewBuilder.AddTarget(previewSurface);

                if (flashon)
                    previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Torch);

                Rect sensor_rect = max;
                int left = sensor_rect.Width() / 2;
                int right = left;
                int top = sensor_rect.Height() / 2;
                int bottom = top;
                int hwidth = (int)(sensor_rect.Width() / (2.0 * zoomlev));
                int hheight = (int)(sensor_rect.Height() / (2.0 * zoomlev));
                left -= hwidth;
                right += hwidth;
                top -= hheight;
                bottom += hheight;
                previewBuilder.Set(CaptureRequest.ScalerCropRegion, new Rect(left, top, right, bottom));

                var recorderSurface = mediaRecorder.Surface;
                surfaces.Add(recorderSurface);
                previewBuilder.AddTarget(recorderSurface);
                mPreviewSession = new PreviewCaptureStateCallback(this,true);
                cameraDevice.CreateCaptureSession(surfaces, mPreviewSession, backgroundHandler);

            }
            catch (IllegalStateException e)
            {
                e.PrintStackTrace();
            }
        }

        private void stopRecordingVideo()
        {
            //UI
            //isRecordingVideo = false;
            //Stop recording

            //try
            //{
            //    previewSession.StopRepeating();
            //    previewSession.AbortCaptures();
            //}
            //catch
            //{

            //}

            try
            {
                mediaRecorder.Stop();
                mediaRecorder.Reset();
            }
            catch
            {
                //usually if this is happening when the app is getting put in the background
            }

            try
            {
                startPreview();
            
            }catch{

            }
			
            // Workaround for https://github.com/googlesamples/android-Camera2Video/issues/2
            //CloseCamera();
            //openCamera(textureView.Width, textureView.Height);
        }

        /// <summary>
        /// Sets up capture request builder.
        /// </summary>
        /// <param name="builder">Builder.</param>
        //private void SetUpCaptureRequestBuilder(CaptureRequest.Builder builder)
        //{
        //    // In this sample, w just let the camera device pick the automatic settings
        //    builder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Off);
        //    builder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);
        //    previewSession.Capture(builder.Build(), null, backgroundHandler);
        //}

        private class CameraCaptureListener : CameraCaptureSession.CaptureCallback
        {
            public Camera2Fragment Fragment;
            public File File;
            public override void OnCaptureCompleted(CameraCaptureSession session, CaptureRequest request, TotalCaptureResult result)
            {
                if (Fragment != null && File != null)
                {
                    Fragment.startPreview();
                }
            }
        }


        // This CameraCaptureSession.StateListener uses Action delegates to allow the methods to be defined inline, as they are defined more than once
        private class CameraCaptureStateListener : CameraCaptureSession.StateCallback
        {
            public Action<CameraCaptureSession> OnConfigureFailedAction;
            public override void OnConfigureFailed(CameraCaptureSession session)
            {
                OnConfigureFailedAction?.Invoke(session);
            }

            public Action<CameraCaptureSession> OnConfiguredAction;
            public override void OnConfigured(CameraCaptureSession session)
            {
                OnConfiguredAction?.Invoke(session);
            }

        }

        private class ImageAvailableListener : Java.Lang.Object, ImageReader.IOnImageAvailableListener
        {
            public File File;
            public Camera2Fragment fragment;
            public void OnImageAvailable(ImageReader reader)
            {
                Image image = null;
                try
                {
                    image = reader.AcquireNextImage();
                    ByteBuffer buffer = image.GetPlanes()[0].Buffer;
                    byte[] bytes = new byte[buffer.Capacity()];
                    buffer.Get(bytes);
                    Save(bytes);
                    fragment.PictureTaken?.Invoke();
                }
                catch (FileNotFoundException ex)
                {
                    Log.WriteLine(LogPriority.Info, "Camera capture session", ex.StackTrace);
                }
                catch (IOException ex)
                {
                    Log.WriteLine(LogPriority.Info, "Camera capture session", ex.StackTrace);
                }
                finally
                {
                    if (image != null)
                        image.Close();
                }
            }

            private void Save(byte[] bytes)
            {
                OutputStream output = null;
                try
                {
                    if (File != null)
                    {
                        output = new FileOutputStream(File);
                        output.Write(bytes);
                    }
                }
                finally
                {
                    if (output != null)
                        output.Close();
                }
            }
        }

        public void TakePhoto(string filename)
        {
            thumbnailfilename = filename;
            try
            {
                Activity activity = Activity;
                if (activity == null || cameraDevice == null)
                {
                    return;
                }

                // Pick the best JPEG size that can be captures with this CameraDevice
                CameraCharacteristics characteristics = manager.GetCameraCharacteristics(cameraDevice.Id);
                Size[] jpegSizes = null;
                if (characteristics != null)
                {
                    jpegSizes = ((StreamConfigurationMap)characteristics.Get(CameraCharacteristics.ScalerStreamConfigurationMap)).GetOutputSizes((int)ImageFormatType.Jpeg);
                }
                int width = 640;
                int height = 480;
                if (jpegSizes != null && jpegSizes.Length > 0)
                {
                    width = jpegSizes[0].Width;
                    height = jpegSizes[0].Height;
                }

                // We use an ImageReader to get a JPEG from CameraDevice
                // Here, we create a new ImageReader and prepare its Surface as an output from the camera
                ImageReader reader = ImageReader.NewInstance(width, height, ImageFormatType.Jpeg, 1);
                List<Surface> outputSurfaces = new List<Surface>
                {
                    reader.Surface
                };
                //outputSurfaces.Add(new Surface(mTextureView.SurfaceTexture));

                CaptureRequest.Builder captureBuilder = cameraDevice.CreateCaptureRequest(CameraTemplate.StillCapture);
                captureBuilder.AddTarget(reader.Surface);
                //SetUpCaptureRequestBuilder(captureBuilder);
                // Orientation
                SurfaceOrientation rotation = activity.WindowManager.DefaultDisplay.Rotation;
                var mSensorOrientation = (int)characteristics.Get(CameraCharacteristics.SensorOrientation);
                int finalOrientation = (ORIENTATIONS.Get((int)rotation) + mSensorOrientation + 270) % 360;

                captureBuilder.Set(CaptureRequest.JpegOrientation, new Java.Lang.Integer(finalOrientation));

                if (flashon)
                    captureBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Torch);

                Rect sensor_rect = max;
                int left = sensor_rect.Width() / 2;
                int right = left;
                int top = sensor_rect.Height() / 2;
                int bottom = top;
                int hwidth = (int)(sensor_rect.Width() / (2.0 * zoomlev));
                int hheight = (int)(sensor_rect.Height() / (2.0 * zoomlev));
                left -= hwidth;
                right += hwidth;
                top -= hheight;
                bottom += hheight;
                captureBuilder.Set(CaptureRequest.ScalerCropRegion, new Rect(left, top, right, bottom));
                // Output file
                //File file = new File(activity.GetExternalFilesDir(null), DateTime.Now.ToString("MM-dd-yyyy-HH-mm-ss-fff", CultureInfo.InvariantCulture) + ".jpg"); //((CameraActivity)activity).learningTask.Id + ".jpg");

                // This listener is called when an image is ready in ImageReader 
                // Right click on ImageAvailableListener in your IDE and go to its definition
                ImageAvailableListener readerListener = new ImageAvailableListener() { File = new File(thumbnailfilename),fragment = this };

                // We create a Handler since we want to handle the resulting JPEG in a background thread
                HandlerThread thread = new HandlerThread("CameraPicture");
                thread.Start();
                Handler backgroundHandler = new Handler(thread.Looper);
                reader.SetOnImageAvailableListener(readerListener, backgroundHandler);

                //This listener is called when the capture is completed
                // Note that the JPEG data is not available in this listener, but in the ImageAvailableListener we created above
                // Right click on CameraCaptureListener in your IDE and go to its definition
                CameraCaptureListener captureListener = new CameraCaptureListener() { Fragment = this, File = new File(thumbnailfilename) };

                cameraDevice.CreateCaptureSession(outputSurfaces, new CameraCaptureStateListener()
                {
                    OnConfiguredAction = (CameraCaptureSession session) => {
                        try
                        {
                            session.Capture(captureBuilder.Build(), captureListener, backgroundHandler);
                        }
                        catch (CameraAccessException ex)
                        {
                            Log.WriteLine(LogPriority.Info, "Capture Session error: ", ex.ToString());
                        }
                    }
                }, backgroundHandler);
            }
            catch (CameraAccessException ex)
            {
                Log.WriteLine(LogPriority.Info, "Taking picture error: ", ex.StackTrace);
            }
        }

        public void StartRecord(string filename)
        {
            thumbnailfilename = filename;
            //SetUpMediaRecorder();
            StartRecordingVideo();
        }

        public void StopRecord()
        {
            stopRecordingVideo();
        }

        Video.CAMERA_POSITION CURRENTCAMERA = 0;

        internal void FocusComplete()
        {

            previewBuilder.Set(CaptureRequest.ControlAfTrigger, null);
            //previewBuilder.SetTag(null);
            previewSession.SetRepeatingRequest(previewBuilder.Build(), null, null);
        }

        internal void DoFocus(Rect r)
        {
            var handler = new FocusCallback(this);

            //CANCEL PREVIOUS AUTOFOCUS
            previewSession.StopRepeating();

            previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Off);
            previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);
            previewSession.Capture(previewBuilder.Build(), handler, backgroundHandler);

            previewBuilder.Set(CaptureRequest.ControlMode, (int)ControlMode.Auto);
            previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Auto);
            previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Start);

            MeteringRectangle rect = new MeteringRectangle(r, MeteringRectangle.MeteringWeightMax);

            previewBuilder.SetTag("FOCUS");

            previewBuilder.Set(CaptureRequest.ControlAfRegions, new MeteringRectangle[] { rect });
            previewSession.Capture(previewBuilder.Build(), handler, backgroundHandler);
        }

        bool flashon = false;

        public void FlashOn()
        {
            previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Torch);
            previewSession.SetRepeatingRequest(previewBuilder.Build(),null, backgroundHandler);
            flashon = true;
        }

        public void FlashOff()
        {
            previewBuilder.Set(CaptureRequest.FlashMode, (int)FlashMode.Off);
            previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);
            flashon = false;
        }

        private double zoomlev = 1;

        public void Zoom(int level)
        {
            var lev = ((level) / 100.0) + 1.0;
            zoomlev = lev;
            if (previewBuilder != null)
            {
                Rect sensor_rect = max;
                int left = sensor_rect.Width() / 2;
                int right = left;
                int top = sensor_rect.Height() / 2;
                int bottom = top;
                int hwidth = (int)(sensor_rect.Width() / (2.0 * lev));
                int hheight = (int)(sensor_rect.Height() / (2.0 * lev));
                left -= hwidth;
                right += hwidth;
                top -= hheight;
                bottom += hheight;

                previewBuilder.Set(CaptureRequest.ScalerCropRegion, new Rect(left,top,right,bottom));
                previewSession.SetRepeatingRequest(previewBuilder.Build(), null, backgroundHandler);

                previewBuilder.Set(CaptureRequest.ControlAfMode, (int)ControlAFMode.Off);
                previewBuilder.Set(CaptureRequest.ControlAfTrigger, (int)ControlAFTrigger.Cancel);
                previewSession.Capture(previewBuilder.Build(), null, backgroundHandler);
            }
        }

        public int GetAudioLevel()
        {
            if (mediaRecorder != null)
                return mediaRecorder.MaxAmplitude;
            else
                return 0;
        }

        // Compare two Sizes based on their areas
        private class CompareSizesByArea : Java.Lang.Object, Java.Util.IComparator
        {
            public int Compare(Java.Lang.Object lhs, Java.Lang.Object rhs)
            {
                // We cast here to ensure the multiplications won't overflow
                if (lhs is Size && rhs is Size)
                {
                    var right = (Size)rhs;
                    var left = (Size)lhs;
                    return Long.Signum((long)left.Width * left.Height -
                        (long)right.Width * right.Height);
                }
                else
                    return 0;

            }
        }
    }
}
