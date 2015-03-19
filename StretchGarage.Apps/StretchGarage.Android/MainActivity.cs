using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Locations;
using Android.Preferences;
using Android.Runtime;
using Android.Views;
using Android.Webkit;
using Android.Widget;
using Android.OS;
using Java.Lang;
using StretchGarage.Android.Views;
using StretchGarage.Android.Models;
using StretchGarage.Shared;
using String = System.String;
using StringBuilder = System.Text.StringBuilder;

namespace StretchGarage.Android
{
    [Activity(Label = "Stretch Garage", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, ILocationListener
    {
        Location _currentLocation; //Holder of lat/long (_currentLocation.Latitude/Longitude)
        LocationManager _locationManager; //Holder of accuracy and locationprovider
        String _locationProvider;
        private bool _gpsRunning = false;
        private int _id = -1;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide(); //Hides actionbar that is otherwise shown at the top of the application.
            SetContentView(Resource.Layout.Main); //Sets layout

            InitWebView(); //Inits webview

            if (!LoadId()) //Loads id value to _id
                ShowNewUserScreen();
            else
                InitializeLocationManager(); //inits gps loop
        }

        private class HybridWebViewClient : WebViewClient
        {
            public override bool ShouldOverrideUrlLoading(WebView webView, string url)
            {

                // If the URL is not our own custom scheme, just let the webView load the URL as usual
                var scheme = "hybrid:";

                if (!url.StartsWith(scheme))
                    return false;

                // This handler will treat everything between the protocol and "?"
                // as the method name.  The querystring has all of the parameters.
                var resources = url.Substring(scheme.Length).Split('?');
                var method = resources[0];
                var parameters = System.Web.HttpUtility.ParseQueryString(resources[1]);

                if (method == "UpdateLabel")
                {
                    var textbox = parameters["textbox"];

                    // Add some text to our string here so that we know something
                    // happened on the native part of the round trip.
                    var prepended = string.Format("C# says \"{0}\"", textbox);

                    // Build some javascript using the C#-modified result
                    var js = string.Format("SetLabelText('{0}');", prepended);

                    webView.LoadUrl("javascript:" + js);
                }

                return true;
            }
        }

        /// <summary>
        /// Initializes the webpage into the application
        /// </summary>
        private void InitWebView()
        {
            WebView localWebView = FindViewById<WebView>(Resource.Id.LocalWebView);
            localWebView.Settings.JavaScriptEnabled = true;
            localWebView.SetWebViewClient(new HybridWebViewClient());
            localWebView.LoadUrl("http://stretchgarageweb.azurewebsites.net/#/ParkingPlace/0");
            //localWebView.Settings.LoadWithOverviewMode = true;
            //localWebView.Settings.UseWideViewPort = true;
        }

        #region GPS Loop and Initialization
        /// <summary>
        /// Initializes gps function.
        /// Starts with getting provider for location
        /// and also sets accuracy of gps.
        /// When everything is ok it starts the loop.
        /// </summary>
        void InitializeLocationManager()
        {
            //http://developer.xamarin.com/recipes/android/os_device_resources/gps/get_current_device_location/

            _locationManager = (LocationManager)GetSystemService(LocationService);

            Criteria criteriaForLocationService = new Criteria { Accuracy = Accuracy.Fine };

            IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);

            if (acceptableLocationProviders.Any())
                _locationProvider = acceptableLocationProviders.First();
            else
                _locationProvider = String.Empty;

            Task loop = LoopGps(); //Start gps loop
        }

        /// <summary>
        /// async Loop that handles everything
        /// regarding updating the units location.
        /// Is intervall based from server response. 
        /// </summary>
        /// <returns></returns>
        async Task LoopGps()
        {
            int num = 20;
            while (num != 0)
            {
                _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this); //Start gps
                _gpsRunning = true;
                await Task.Delay(5000);
                _locationManager.RemoveUpdates(this); //Stop gps
                _gpsRunning = false;
                await ApiRequest.GetInterval(0, _currentLocation.Latitude, _currentLocation.Longitude);
                await Task.Delay(5000);
                num--;
            }
        }

        /// <summary>
        /// Updates global variable _currentLocation
        /// when method is called.
        /// Is automatically called onChange of value
        /// of lat/long when gps is on.
        /// </summary>
        /// <param name="location"></param>
        public void OnLocationChanged(Location location)
        {
            _currentLocation = location;
        }

        #region Override methods not used
        public void OnProviderDisabled(string provider) { }
        public void OnProviderEnabled(string provider) { }
        public void OnStatusChanged(string provider, Availability status, Bundle extras) { }
        #endregion 
        #endregion

        #region Id functions
        /// <summary>
        /// Creates user in db and saves id to shared pref.
        /// </summary>
        /// <param name="username"></param>
        private async Task<bool> SaveId(string username)
        {
            _id = await ApiRequest.GetUnitId(username); //Creates user id

            //TODO: Check that valid id was created

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = prefs.Edit();
            editor.PutInt("key_ID", _id);
            editor.Apply();

            return _id != -1; //returns true or false depending on if its -1
        }

        /// <summary>
        /// Loads shared preference for id.
        /// Sets _id to shared pref value
        /// </summary>
        /// <returns>Returns true/false(-1) depending on if id is -1</returns>
        private bool LoadId()
        {
            _id = -1;
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            _id = prefs.GetInt("key_ID", _id);

            return _id != -1; //returns true or false depending on if its -1
        }

        /// <summary>
        /// Creates and shows a dialog window to user
        /// for entering a username
        /// </summary>
        private void ShowNewUserScreen()
        {
            var customView = LayoutInflater.Inflate(Resource.Layout.CreateUserDialog, null);

            var builder = new AlertDialog.Builder(this);
            builder.SetView(customView);
            builder.SetPositiveButton(Resource.String.dialog_ok, OkClicked);
            builder.SetCancelable(false);

            builder.Create();
            builder.Show();
        }
        /// <summary>
        /// Handles dialog click event for ShowNewUserScreen dialog.
        /// If valid username is entered then it calls SaveId method.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void OkClicked(object sender, DialogClickEventArgs args)
        {
            var dialog = (AlertDialog)sender;
            var username = (EditText)dialog.FindViewById(Resource.Id.username);

            if (string.IsNullOrEmpty(username.Text))
            {
                Toast.MakeText(this, "Please fill in a valid username", ToastLength.Long).Show();
                ShowNewUserScreen();
                return;
            }

            if (await SaveId(username.Text)) //Starts location manager if save id is ok.
                InitializeLocationManager();
            else //Failed saving to server
                Toast.MakeText(this, "Failed to create user.", ToastLength.Long).Show();
        }
        #endregion
    }
}

