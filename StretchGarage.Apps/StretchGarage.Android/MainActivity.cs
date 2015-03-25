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
        //Location _currentLocation; //Holder of lat/long (_currentLocation.Latitude/Longitude)
        LocationManager _locationManager; //Holder of accuracy and locationprovider
        String _locationProvider;
        private int _id = -1;
        private WebView _webView;
        private bool GpsRunning = false;
        private DateTime CheckSpeedTime = DateTime.MinValue;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide(); //Hides actionbar that is otherwise shown at the top of the application.
            SetContentView(Resource.Layout.Main); //Sets layout

            InitWebView(); //Inits webview

            _id = 0; //TEST!!!
            /*if (!LoadId()) //Loads id value to _id
                ShowNewUserScreen();
            else*/
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
            _webView = FindViewById<WebView>(Resource.Id.LocalWebView);
            _webView.Settings.JavaScriptEnabled = true;
            _webView.SetWebViewClient(new HybridWebViewClient());
            _webView.LoadUrl("http://stretchgarageweb.azurewebsites.net/#/");
            //localWebView.Settings.LoadWithOverviewMode = true;
            //localWebView.Settings.UseWideViewPort = true;
        }

        /// <summary>
        /// Override method for handling back press.
        /// Changes back button to handle webview instead
        /// for activity window
        /// </summary>
        public override void OnBackPressed()
        {
            if (_webView.CanGoBack())
            {
                _webView.GoBack();
            }
            else
            {
                //super.OnBackPressed();
            }
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
            _locationManager = (LocationManager)GetSystemService(LocationService);

            if (SetBestProvider(Accuracy.Medium))
                StartGps();
        }

        /// <summary>
        /// Sets _locationProvider to BestProvider
        /// for given accuracy
        /// if no provider found it returns false
        /// </summary>
        /// <returns></returns>
        private bool SetBestProvider(Accuracy accuracy)
        {
            Criteria criteriaForLocationService = new Criteria { Accuracy = accuracy };
            _locationProvider = _locationManager.GetBestProvider(criteriaForLocationService, true);

            return _locationProvider != null;

            #region Old code
            IList<string> acceptableLocationProviders = _locationManager.GetProviders(criteriaForLocationService, true);
            if (acceptableLocationProviders.Any())
                _locationProvider = acceptableLocationProviders.First();
            else
                _locationProvider = String.Empty; //TODO: FIX TO TRY AGAIN AFTER A WHILE 
            #endregion
        }
        
        /// <summary>
        /// Starts Gps with global
        /// provider
        /// </summary>
        /// <returns></returns>
        async Task StartGps()
        {
            GpsRunning = true;
            _locationManager.RequestLocationUpdates(_locationProvider, 0, 0, this); //Start gps
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
            if (CheckSpeedTime != null && CheckSpeedTime < DateTime.Now)
            {
                Task getInterval = GetInterval(location);
            }
            else if (location != null && GpsRunning)
            {
                Task getInterval = GetInterval(location);
            }
        }

        async Task GetInterval(Location location)
        {
            if (CheckSpeedTime == DateTime.MinValue)
            {
                GpsRunning = false;
                _locationManager.RemoveUpdates(this);
            }

            var checkLocation = await GetServerResult(location);

            string toastMessage = string.Format("Location(lat:{0} long:{1}) IsParked:{2}, CheckSpeed:{3}", location.Latitude, location.Longitude, checkLocation.IsParked, checkLocation.CheckSpeed);
            Toast.MakeText(this, toastMessage, ToastLength.Long).Show();

            await Task.Delay(checkLocation.Interval);
            if (SetBestProvider(Accuracy.Medium))
                StartGps();
        }

        async Task<CheckLocation> GetServerResult(Location location)
        {
            WebApiResponse response = await ApiRequestManager.GetInterval(_id, location.Latitude, location.Longitude);

            CheckLocation result = !response.Success ? (CheckLocation) response.Content : new CheckLocation(20000, false, false);

            CheckSpeedTime = result.CheckSpeed ? DateTime.UtcNow.AddMilliseconds(result.Interval) : DateTime.MinValue;

            return result;
        }

        #region Override methods not used

        public void OnProviderDisabled(string provider){}

        public void OnProviderEnabled(string provider){}

        public void OnStatusChanged(string provider, Availability status, Bundle extras){}
        #endregion 
        #endregion

        #region Id functions
        /// <summary>
        /// Creates user in db and saves id to shared pref.
        /// </summary>
        /// <param name="username"></param>
        private async Task<bool> SaveId(string username)
        {
            WebApiResponse response = await ApiRequestManager.GetUnitId(username); //Creates user id

            if (!response.Success)
            {
                Toast.MakeText(this, response.Message, ToastLength.Long).Show();
                return false;
                //TODO: Check that valid id was created

            }

            _id = (int)response.Content;

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

