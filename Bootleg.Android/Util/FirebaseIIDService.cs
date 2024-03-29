using Android.App;
using Android.Content;
using Bootleg.API;
using Firebase.Iid;
using System;

namespace Bootleg.Droid.Util
{
    [Service(Exported = true)]
    [IntentFilter(new[] { "com.google.firebase.INSTANCE_ID_EVENT" })]
    public class FirebaseIIDService : FirebaseInstanceIdService
    {
        public override void OnTokenRefresh()
        {
            try
            {
                var refreshedToken = FirebaseInstanceId.Instance.Token;
                Console.WriteLine("token: " + refreshedToken);
                Bootlegger.BootleggerClient.RegisterForPush(refreshedToken, API.Bootlegger.Platform.Android);
            }
            catch
            {
                Console.WriteLine("Cant set Firebase Token - API not initialised.");
            }
        }
    }
}