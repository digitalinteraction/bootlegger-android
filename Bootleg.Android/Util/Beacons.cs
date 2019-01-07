using System;
using System.Collections.Generic;
using Android.Content;
using Android.Runtime;
using AltBeaconOrg.BoundBeacon.Powersave;
using AltBeaconOrg.BoundBeacon;
using Android.Util;
using Java.Net;
using Android.Bluetooth.LE;
using System.Text;
using Android.Bluetooth;
using System.Threading.Tasks;
using System.Threading;
using Org.Altbeacon.Beacon.Utils;
using System.Linq;
using Android.Gms.Common.Apis;
using Android.Gms.Nearby;
using Android.Support.V4.App;
using static Android.Gms.Common.Apis.GoogleApiClient;
using Android.Gms.Common;
using Bootleg.API;
using Android.Gms.Nearby.Messages;
using Bootleg.API.Model;

namespace Bootleg.Droid.Util
{
    public class Beacons : AdvertiseCallback, IBeaconConsumer, IRangeNotifier, IOnConnectionFailedListener
    {
        private static Beacons _beaconInst;
        public static Beacons BeaconInstance {
            get
            {
                if (_beaconInst == null)
                    _beaconInst = new Beacons();
                return _beaconInst;
            } }

        private Beacons()
        {

        }

        public override void OnStartFailure([GeneratedEnum] AdvertiseFailure errorCode)
        {
            base.OnStartFailure(errorCode);
        }

        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            base.OnStartSuccess(settingsInEffect);
        }

        private BackgroundPowerSaver backgroundPowerSaver;

        public void Start(Context context)
        {
            this.ApplicationContext = context;
        }

        public void OnBeaconServiceConnect()
        {
            _beaconManager.SetForegroundBetweenScanPeriod(5000); // 5000 milliseconds
            _beaconManager.StartRangingBeaconsInRegion(new Region("all-beacons-region",null,null,null));
        }

        private BeaconManager _beaconManager;

        bool VerifyBluetooth()
        {
            try
            {
                return BeaconManager.GetInstanceForApplication(ApplicationContext).CheckAvailability();
            }
            catch (BleNotAvailableException)
            {
                return false;
            }
        }

        bool _inBackground = false;
        public bool InBackground{
            get{
                return _inBackground;
            }
            set
            {
                _inBackground = value;
                if (true)
                {
                    if (_beaconManager?.IsBound(this) ?? false)
                    {
                        _beaconManager.SetBackgroundMode(false);
                    }
                }
                else
                {
                    if (_beaconManager.IsBound(this))
                    {
                        _beaconManager.SetBackgroundMode(true);
                    }
                }
            }
        }

        public Context ApplicationContext { get; private set; }

        public IntPtr Handle => base.Handle;

        Beacon beacon;
        BeaconTransmitter beaconTransmitter;

        public async Task<bool> StartBroadcastingBle(Context context, Shoot ev)
        {
            //ask to turn on bluetooth:
            BluetoothAdapter mBluetoothAdapter = BluetoothAdapter.DefaultAdapter;

            var allprefs = context.GetSharedPreferences(WhiteLabelConfig.BUILD_VARIANT.ToLower(), FileCreationMode.Private);
            //var prefs = allprefs.Edit();
            var useble = allprefs.GetBoolean("ble-" + ev.id, true);
            //prefs.PutInt("ble_enable", currentversion);
            //prefs.Apply();
            if (!useble)
                return false;

            if (mBluetoothAdapter.State == State.Off)
            {
                //ask to turn on:
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                var dialog = new Android.Support.V7.App.AlertDialog.Builder(context);
                dialog.SetTitle(Resource.String.enableble);
                dialog.SetMessage(Resource.String.blemessage);
                dialog.SetPositiveButton(Android.Resource.String.Yes, (o,e) =>
                {
                    tcs.SetResult(true);
                });

                dialog.SetNegativeButton(Android.Resource.String.No, (o,e) =>
                {
                    tcs.SetResult(false);
                });

                dialog.Show();

                var result = await tcs.Task;
                if (result)
                {
                    allprefs.Edit().PutBoolean("ble-" + ev.id, true).Apply();

                    //turn on ble
                    mBluetoothAdapter.Enable();
                    //need to wait for ble event saying its started here:
                    await Task.WhenAny(Task.Delay(3000), Task.Factory.StartNew(() =>
                     {
                         while (mBluetoothAdapter.State != State.On)
                         {
                             Task.Yield();
                         }
                     }));
                }
                else
                {
                    allprefs.Edit().PutBoolean("ble-" + ev.id, false).Apply();
                    return false;
                }
            }

            var compat = BeaconTransmitter.CheckTransmissionSupported(context);
            if (compat == BeaconTransmitter.Supported)
            {
                try
                {
                    byte[] urlBytes = UrlBeaconUrlCompressor.Compress("https://"+ WhiteLabelConfig.BEACONHOST + "/b/");// 91071");//+ev.offlinecode);
                    //HACK: remove line!
                    //ev.offlinecode = "91071";
                    List<byte> bytes = new List<byte>(urlBytes);
                    bytes.AddRange(Encoding.ASCII.GetBytes(ev.offlinecode));

                    urlBytes = bytes.ToArray();

                    Identifier encodedUrlIdentifier = Identifier.FromBytes(urlBytes, 0, urlBytes.Length, false);
                    List<Identifier> identifiers = new List<Identifier>();
                    identifiers.Add(encodedUrlIdentifier);
                    beacon = new Beacon.Builder()
                            .SetIdentifiers(identifiers)
                            .SetServiceUuid(0xfeaa)
                            .SetManufacturer(0x0118)
                            .SetBeaconTypeCode(0x10)
                            .SetTxPower(-59)
                            .Build();
                    BeaconParser beaconParser = new BeaconParser().SetBeaconLayout(BeaconParser.EddystoneUrlLayout);

                    beaconTransmitter = new BeaconTransmitter(context, beaconParser);
                    
                    beaconTransmitter.StartAdvertising(beacon,this);
                }
                catch (MalformedURLException)
                {
                    Log.Debug("BOOTLEGGER", "That URL cannot be parsed");
                }
                return true;
            }
            else
            {
                //not supported
                return false;
            }
        }

        public void StopBroadcastingBle()
        {
            beaconTransmitter?.StopAdvertising();
        }

        GoogleApiClient mGoogleApiClient;

        Message mActiveMessage;

        public void StartPublishingNearby(FragmentActivity context, Shoot shoot)
        {
            if (mGoogleApiClient == null)
            {
                mGoogleApiClient = new GoogleApiClient.Builder(context)
               .AddApi(NearbyClass.MessagesApi)
               .AddConnectionCallbacks(async (bundle) =>
               {
                   string urlBytes = "https://" + WhiteLabelConfig.BEACONHOST + "/b/" + shoot.offlinecode;
                   mActiveMessage = new Message(Encoding.ASCII.GetBytes(urlBytes));
                   await Android.Gms.Nearby.NearbyClass.Messages.Publish(mGoogleApiClient, mActiveMessage);
               }, (bundle) =>
               {
                   //failed to publish
               })
               .EnableAutoManage(context, this)
               .Build();
            }
        }

        public async Task StopPublishingNearby()
        {
            if (mActiveMessage != null)
            {
                await Android.Gms.Nearby.NearbyClass.Messages.Unpublish(mGoogleApiClient, mActiveMessage);
                mActiveMessage = null;
            }

            //await Android.Gms.Nearby.NearbyClass.Messages.Unsubscribe(mGoogleApiClient);

            //unpublish();
            //unsubscribe();
            if (mGoogleApiClient.IsConnected)
            {
                mGoogleApiClient.Disconnect();
            }
        }

        //async Task StartNearbyDiscovery()
        //{
        //    if (CrossConnectivity.Current.ConnectionTypes.Contains(Plugin.Connectivity.Abstractions.ConnectionType.WiFi))
        //    {
        //        var status = await NearbyClass.Connections.StartDiscovery(mGoogleApiClient, serviceId, TIMEOUT_DISCOVER, new MyConnectionsEndpointDiscoveryListener(this));

        //        if (status.IsSuccess)
        //        {
        //            //DebugLog("startDiscovery:onResult: SUCCESS");

        //            //UpdateViewVisibility(NearbyConnectionState.Discovering);
        //        }
        //        else
        //        {
        //            //DebugLog("startDiscovery:onResult: FAILURE");

        //            int statusCode = status.StatusCode;
        //            if (statusCode == ConnectionsStatusCodes.StatusAlreadyDiscovering)
        //            {
        //                //DebugLog("STATUS_ALREADY_DISCOVERING");
        //            }
        //            else
        //            {
        //                //UpdateViewVisibility(NearbyConnectionState.Ready);
        //            }
        //        }
        //    }
        //}

        public bool StartScanningBle()
        {
            NearbyShoots = new HashSet<Shoot>();
            _shortcodes = new HashSet<string>();

            if (!VerifyBluetooth())
                return false;

            _beaconManager = BeaconManager.GetInstanceForApplication(ApplicationContext);
            
            ////  Estimote > 2013
            _beaconManager.BeaconParsers.Add(new BeaconParser().SetBeaconLayout(BeaconParser.EddystoneUrlLayout));

            _beaconManager.Bind(this);
            _beaconManager.SetRangeNotifier(this);

            backgroundPowerSaver = new BackgroundPowerSaver(ApplicationContext);

            return true;
        }

        public bool BindService(Intent intent, IServiceConnection serviceConnection, [GeneratedEnum] Bind flags)
        {
            return ApplicationContext.BindService(intent, serviceConnection, flags);
        }

        public void UnbindService(IServiceConnection serviceConnection)
        {
            ApplicationContext.UnbindService(serviceConnection);
        }

        public void Dispose()
        {
            if (_beaconManager.IsBound(this)) _beaconManager.Unbind(this);
        }

        public void DidDetermineStateForRegion(int state, Region region)
        {

        }

        public async void DidRangeBeaconsInRegion(ICollection<Beacon> beacons, Region region)
        {
            foreach (var beacon in beacons)
            {
                if (beacon.ServiceUuid == 0xfeaa && beacon.BeaconTypeCode == 0x10)
                {
                    // This is a Eddystone-URL frame
                    Uri url = new Uri(UrlBeaconUrlCompressor.Uncompress(beacon.Id1.ToByteArray()));
                    //lookup beacons:

                    if (url.Host == WhiteLabelConfig.BEACONHOST && url.PathAndQuery.StartsWith("/b/"))
                    {
                        //Console.WriteLine(url);
                        var shortcode = url.Segments.Last();

                        if (!_shortcodes.Contains(shortcode))
                        {
                            try
                            {
                                var shootid = await Bootlegger.BootleggerClient.GetEventFromShortcode(shortcode);
                                var shoot = await Bootlegger.BootleggerClient.GetEventInfo(shootid, new CancellationTokenSource().Token);
                                _shortcodes.Add(shortcode);
                                NearbyShoots.Add(shoot);
                                OnEventsFound?.Invoke();
                            }
                            catch
                            {
                                Console.WriteLine("Cant find shoot from shortcode");
                            }
                        }
                    }
                }
            }
        }

        public void OnConnectionFailed(ConnectionResult result)
        {
            //connection failed:
        }

        ~Beacons()
        {
            if (_beaconManager?.IsBound(this)??false) _beaconManager.Unbind(this);
        }

        public event Action OnEventsFound;
        private HashSet<string> _shortcodes;
        public HashSet<Shoot> NearbyShoots { get; private set; }
       

    }
}