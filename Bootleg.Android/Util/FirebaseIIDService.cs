using Android.App;
using Android.Content;
using Firebase.Iid;
using System;

namespace Bootleg.Droid.Util
{
    [Service]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseIIDService : FirebaseInstanceIdService
    {
        public override void OnTokenRefresh()
        {
            var refreshedToken = FirebaseInstanceId.Instance.Token;
            Console.WriteLine("token: " + refreshedToken);
            Bootleg.API.Bootlegger.BootleggerClient.RegisterForPush(refreshedToken, API.Bootlegger.Platform.Android);
        }
    }
}