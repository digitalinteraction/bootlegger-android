using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Bootleg.Droid.UI
{
    interface ICameraDriver
    {
        void TakePhoto(string filename);
        void StartRecord(string filename);
        void StopRecord();
        void FlashOn();
        void FlashOff();
        void Zoom(int level);
        IList<Java.Lang.Integer> ZoomLevels { get; }
        bool HasFlash { get; }
        bool HasZoom { get; }
        int NumCameras { get; }
        event Action OnPictureTaken;
        event Action OnSetupComplete;
        event Action<string> OnError;
        void CloseCamera();
    }
}