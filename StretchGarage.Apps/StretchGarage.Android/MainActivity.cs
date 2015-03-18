﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Locations;
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

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            ActionBar.Hide(); //Hides actionbar that is otherwise shown at the top of the application.
            SetContentView(Resource.Layout.Main); //Sets layout

            InitWebView(); //Inits webview

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
    }
}

