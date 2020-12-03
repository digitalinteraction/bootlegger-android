using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using RestSharp;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

namespace IndabaTest
{
    [TestFixture]
    public class WorkflowTests
    {
        AndroidApp app;
        System.Net.CookieContainer cookies = new System.Net.CookieContainer();

        //[OneTimeSetUp]
        [SetUp]
        public void GetSession()
        {

            //code to get a test user session id:
            var client = new RestClient("https://app.indaba.dev");
            client.CookieContainer = cookies;

            client.FollowRedirects = false;
            //go to app.indaba.dev/auth/mobilelogin/local

            var request = new RestRequest("auth/mobilelogin/local");

            var response = client.Get(request);

            //get the redirect url, and post: firstName=

            var send = new RestRequest("auth/locallogin",Method.POST).AddParameter("firstName","TestFixture1");

            var result = client.Post(send);

            //var sessionid = result.ResponseUri;

            //Console.WriteLine(sessionid);
            //get the sessionid of the return url
        }

        [SetUp]
        public void BeforeEachTest()
        {
            // TODO: If the Android app being tested is included in the solution then open
            // the Unit Tests window, right click Test Apps, select Add App Project
            // and select the app projects that should be tested.
            app = ConfigureApp
                .Android
                //.Debug()
                .EnableLocalScreenshots()
                // TODO: Update this path to point to your Android app and uncomment the
                // code if the app is not included in the solution.
                .ApkFile("../../../Bootleg.Android/bin/Release/dev.indaba.apk")
                .StartApp();
        }

        [Test]
        public void A_SplashScreenChecks()
        {
            app.Screenshot("1");
        }

        [Test]
        public void B_Login()
        {
           app.Screenshot("Startup");
           DoLogin();
        }

        public void DoLogin()
        {
            //app.WaitForElement((e) => e.Id("seenearbybtn"));
            //app.Invoke("SetSession", "s%3A8WWjJQogaBNLRntT6iYXOupsprkb4pMw.kyELPs0yiHrXp7I3qFVRmLOFoS34i9gkv7O%2FnWMRQLs");
            var session = cookies.GetCookies(new Uri("https://app.indaba.dev"))[0].Value;
            app.Invoke("SetSession", session);
            //app.Repl();
            //app.Tap("")
            //text: "Enter a code…"

            app.Screenshot("Login Complete");
            //app.Repl();


            app.Screenshot("Logged In");
        }

        [Test]
        public void C_JoinShootAndCapture()
        {
            //app.Screenshot("First screen.");
            //app.WaitForElement((e) => e.Id("eventsView"));

            //Thread.Sleep(2000);
            
            //app.Tap(e => e.Text("Enter a code…"));
            //app.ScrollDownTo(e => e.All().Text("Enter a code…"));

            app.Query(e => e.Id("code").Invoke("setText", "3635"));

            //app.EnterText(e => e.Id("code"), "3635");

            app.Screenshot("Code Entered");

            //app.Repl();

            app.WaitForElement((arg) => arg.Id("image"));             

            app.WaitFor(() => app.Query(x => x.Id("image")).First().Rect.Height > 5) ;
            //var rect = app.Query(x => x.Id("image")).First().Rect;
            //Console.WriteLine(rect);

            //app.Repl();

            app.Screenshot("Roles Displayed");

            //NEED TO WAIT FOR IMAGE TO BE LOADED...

            app.Tap((arg) => arg.Id("image"));

            //app.Repl();

            app.Screenshot("Role Selected");

            app.WaitForElement((e) => e.Id("Play"));

            //app.Repl();

            SelectShotAndRecord();

            //app.SetOrientationLandscape();

            //app.Tap((arg) => arg.Id("image"));

            ////app.Repl();

            //SelectShotAndRecord();
            app.Back();
            app.Back();
        }

        [Test]
        public void D_Upload()
        {
            app.Repl();
        }

        [Test]
        public void E_ReviewAndTag()
        {

        }

        [Test]
        public void F_Edit()
        {

        }

        void SelectShotAndRecord()
        {
            app.WaitForElement(e => e.Id("im"));

            app.Screenshot("Shots Listed");

            app.Tap(e => e.Id("im"));

            app.Screenshot("Select Shot");

            app.WaitForNoElement(e => e.Id("shotselector"));

            //app.Repl();

            app.Tap((e) => e.Id("Play"));

            app.Screenshot("Record Started");

            Thread.Sleep(4000);

            app.Tap((e) => e.Id("Play"));

            app.Screenshot("Record Stopped");
        }
    }
}
