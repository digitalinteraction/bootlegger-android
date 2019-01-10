using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using Firebase.Messaging;

namespace Bootleg.Droid.Util
{
    [Service, IntentFilter(new[] { "com.google.firebase.MESSAGING_EVENT" })]
    public class MyFirebaseMessageService : FirebaseMessagingService
    {
        public override void OnMessageReceived(RemoteMessage message)
        {
            var notification = message.GetNotification();
            var title = notification.Title;
            var body = notification.Body;
            SendNotification(title, body);
        }

        private void SendNotification(string title, string body)
        {
            var intent = new Intent(this, typeof(SplashActivity));
            intent.AddFlags(ActivityFlags.ClearTop);
            var pendingIntent = PendingIntent.GetActivity(this, 0, intent, PendingIntentFlags.OneShot);

            var notificationBuilder = new NotificationCompat.Builder(this)
                .SetSmallIcon(Resource.Drawable.ic_notification)
                .SetContentTitle(title)
                .SetContentText(body)
                .SetAutoCancel(true)
                .SetChannelId(BootleggerApp.CHANNEL_ID)
                .SetContentIntent(pendingIntent);

            var notificationManager = NotificationManager.FromContext(this);
            notificationManager.Notify(0, notificationBuilder.Build());
        }
    }
}