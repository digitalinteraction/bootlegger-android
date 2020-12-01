using System;
using System.IO;
using System.Linq;
using System.Threading;
using NUnit.Framework;
using Xamarin.UITest;
using Xamarin.UITest.Android;
using Xamarin.UITest.Queries;

namespace IndabaTest
{
    [TestFixture]
    public class Tests
    {
        AndroidApp app;

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
            //app.Repl();
        }

        [Test]
        public void B_LoginAndCapture()
        {
            //app.Screenshot("First screen.");
            //app.WaitForElement((e) => e.Id("eventsView"));

            //Thread.Sleep(2000);
            app.Screenshot("Startup");

            app.Invoke("SetSession", "s%3A8WWjJQogaBNLRntT6iYXOupsprkb4pMw.kyELPs0yiHrXp7I3qFVRmLOFoS34i9gkv7O%2FnWMRQLs");
            //app.Repl();
            //app.Tap("")
            //text: "Enter a code…"
            
            app.Screenshot("Login Complete");
            //app.Repl();

            app.WaitForElement((e) => e.Id("seenearbybtn"));

            app.Screenshot("Logged In");
            //app.Tap(e => e.Text("Enter a code…"));
            //app.ScrollDownTo(e => e.All().Text("Enter a code…"));

            app.Query(e => e.Id("code").Invoke("setText", "3635"));

            //app.EnterText(e => e.Id("code"), "3635");

            app.Screenshot("Code Entered");

            //app.Repl();

            app.WaitForElement((arg) => arg.Id("image"));             

            app.WaitFor(() => app.Query(x => x.Id("image")).First().Rect.Height > 5) ;
            var rect = app.Query(x => x.Id("image")).First().Rect;
            //Console.WriteLine(rect);

            //app.Repl();

            app.Screenshot("Roles Displayed");

            //NEED TO WAIT FOR IMAGE TO BE LOADED...

            app.Tap((arg) => arg.Id("image"));

            //app.Repl();

            app.Screenshot("Role Selected");

            app.WaitForElement((e) => e.Id("Play"),timeout:TimeSpan.FromMinutes(1));

            //app.Repl();

            SelectShotAndRecord();
            
            app.SetOrientationLandscape();

            SelectShotAndRecord();

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
