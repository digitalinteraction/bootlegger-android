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

namespace Bootleg.Droid.UI.Dialogs
{
    internal class FullScreenVideoDialog:Dialog
    {
        public FullScreenVideoDialog(Context context) : base(context)
        {
        }

        public FullScreenVideoDialog(Context context, int themeResId) : base(context, themeResId)
        {
        }

        protected FullScreenVideoDialog(IntPtr javaReference, JniHandleOwnership transfer) : base(javaReference, transfer)
        {
        }

        protected FullScreenVideoDialog(Context context, bool cancelable, EventHandler cancelHandler) : base(context, cancelable, cancelHandler)
        {
        }

        protected FullScreenVideoDialog(Context context, bool cancelable, IDialogInterfaceOnCancelListener cancelListener) : base(context, cancelable, cancelListener)
        {
        }

        public event Action OnAboutToClose;

        public override void OnBackPressed()
        {
            OnAboutToClose?.Invoke();
            base.OnBackPressed();
        }
    }
}