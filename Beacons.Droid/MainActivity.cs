using Android.App;
using Android.Widget;
using Android.OS;
using EstimoteSdk;
using Java.Util.Concurrent;
using Android.Content;
using System;
using System.Threading.Tasks;
using Android;
using Android.Content.PM;
using Android.Support.Design.Widget;
using Android.Views;
using Android.Graphics;
using System.Linq;

namespace Beacons.Droid
{
	[Activity(Label = "Beacons", MainLauncher = true, Icon = "@mipmap/icon")]
	public class MainActivity : Activity, BeaconManager.IServiceReadyCallback
	{
		const int RequestLocationId = 0;
		static readonly int NOTIFICATION_ID = 123321;

		View _layout;

		readonly string[] PermissionsLocation =
		   {
				Manifest.Permission.AccessCoarseLocation,
				Manifest.Permission.AccessFineLocation
			};

	

		BeaconManager _beaconManager;
		NotificationManager _notificationManager;
		EstimoteSdk.Region _region;

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);

			// Set our view from the "main" layout resource
			SetContentView(Resource.Layout.Main);

			_layout = FindViewById<LinearLayout>(Resource.Id.main_layout);

			_notificationManager = (NotificationManager)GetSystemService(NotificationService);


			if ((int)Build.VERSION.SdkInt < 23)
				InitMonitoring();
			else
				GetLocationPermission();

		}

		void InitMonitoring()
		{
			_beaconManager = new BeaconManager(this);

			// Default values are 5s of scanning and 25s of waiting time to save CPU cycles.
			// In order for this demo to be more responsive and immediate we lower down those values.
			_beaconManager.SetBackgroundScanPeriod(TimeUnit.Seconds.ToMillis(1), 0);

			_beaconManager.EnteredRegion += (sender, e) =>
			{
				PostNotification("Benvenuti alla sessione sui Beacon");
				_beaconManager.StartRanging(_region);
			};

			_beaconManager.ExitedRegion += (sender, e) =>
			{
				PostNotification("Non dimenticate di dare il vostro feedback");
				_beaconManager.StopRanging(_region);
				_layout.SetBackgroundColor(Color.Black);
			};

			_beaconManager.Ranging += (sender, e) =>
			 {
				 //Beacons in range
				var beacon = e.Beacons.FirstOrDefault();
				 if (beacon != null)
				 {
					 var accuracy=Utils.ComputeAccuracy(beacon);
					 _layout.SetBackgroundColor(ColorFromDistance(accuracy));
		
					PostNotification( $"Distance to beacon\n{accuracy:N1}m");

				 }
				 else
				 {
					PostNotification("Non dimenticate di dare il vostro feedback");
					_layout.SetBackgroundColor(Color.Black);
				 }
			 };

			_region = new EstimoteSdk.Region("community_days_id", "B9407F30-F5F8-466E-AFF9-25556B57FE6D", 24986);
		}

		protected override void OnResume()
		{
			base.OnResume();
			_notificationManager.Cancel(NOTIFICATION_ID);
			_beaconManager.Connect(this);
		}

		protected override void OnDestroy()
		{
			_notificationManager.Cancel(NOTIFICATION_ID);
			_beaconManager.Disconnect();

			base.OnDestroy();
		}


		void PostNotification(string message)
		{
			Intent notifyIntent = new Intent(this, GetType());
			notifyIntent.SetFlags(ActivityFlags.SingleTop);

			PendingIntent pendingIntent = PendingIntent.GetActivities(this, 0, new[] { notifyIntent }, PendingIntentFlags.UpdateCurrent);

			Notification notification = new Notification.Builder(this)
				//.SetSmallIcon(Resource.Drawable.beacon_gray)
				.SetContentTitle("Community Days")
				.SetContentText(message)
				.SetAutoCancel(true)
				.SetContentIntent(pendingIntent)
				.Build();

			notification.Defaults |= NotificationDefaults.Lights;
			//notification.Defaults |= NotificationDefaults.;

			_notificationManager.Notify(NOTIFICATION_ID, notification);
			TextView statusTextView = FindViewById<TextView>(Resource.Id.lblDistance);
			statusTextView.Text = message;
		}

		public void OnServiceReady()
		{
			_beaconManager.StartMonitoring(_region);
		}


		void GetLocationPermission()
		{
			//Check to see if any permission in our group is available, if one, then all are
			const string permission = Manifest.Permission.AccessFineLocation;
			if (CheckSelfPermission(permission) == (int)Permission.Granted)
			{
				InitMonitoring();
				return;
			}

			//need to request permission
			if (ShouldShowRequestPermissionRationale(permission))
			{
				//Explain to the user why we need to read the contacts
				Snackbar.Make(_layout, "Location access is required to scan for beacons", Snackbar.LengthIndefinite)
						.SetAction("OK", v => RequestPermissions(PermissionsLocation, RequestLocationId))
						.Show();
				return;
			}
			//Finally request permissions with the list of permissions and Id
			RequestPermissions(PermissionsLocation, RequestLocationId);
		}

		public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
		{
			switch (requestCode)
			{
				case RequestLocationId:
					{
						if (grantResults[0] == Permission.Granted)
						{
							//Permission granted
							var snack = Snackbar.Make(_layout, "Location permission is available, getting lat/long.", Snackbar.LengthShort);
							snack.Show();

							InitMonitoring();
						}
						else
						{
							//Permission Denied :(
							//Disabling location functionality
							var snack = Snackbar.Make(_layout, "Location permission is denied.", Snackbar.LengthShort);
							snack.Show();
						}
					}
					break;
			}
		}


		#region Color from distance
		private Color ColorFromDistance(double distance)
		{
			if (distance < 0.0d)
				return Color.Gray;
			else if (distance < 1.0d)
				return Color.Green;
			else if (distance < 5.0d)
				return Color.Orange;
			else
				return Color.Red;
		}
		#endregion
	}
}

