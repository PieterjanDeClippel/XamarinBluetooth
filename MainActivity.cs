//#define functie

using Android.App;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;
using Java.Lang;
using System.Linq;
using Java.Util;
using Android.Content;
using System;
using System.Collections.Generic;
using Java.Lang.Reflect;
using System.Diagnostics;
using Android.Runtime;

namespace XamarinBluetooth
{
	[Activity(Label = "XamarinBluetooth", MainLauncher = true, Icon = "@drawable/icon")]
	public class MainActivity : Activity
	{
		private BluetoothAdapter _adapter;
		public static BluetoothSocket MySocket;
		private List<BluetoothDevice> bonded = new List<BluetoothDevice>();

		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);
			SetContentView(Resource.Layout.Main);

			ListView list = FindViewById<ListView>(Resource.Id.listGekoppeld);
			Button btnZoek = FindViewById<Button>(Resource.Id.btnZoek);

			btnZoek.Click += ZoekApparaten;
			list.ItemClick += List_ItemClick;
		}
		protected override void OnStart()
		{
			base.OnStart();
			InitializeBluetooth();
		}
		
		void InitializeBluetooth()
		{
			_adapter = BluetoothAdapter.DefaultAdapter;
			if (_adapter == null)
			{
				Toast.MakeText(this, "Bluetooth is not available", ToastLength.Long).Show();
				Finish();
				return;
			}

			if (!_adapter.IsEnabled)
			{
				const int REQUEST_ENABLE_BT = 2;
				var enableBtIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
				StartActivityForResult(enableBtIntent, REQUEST_ENABLE_BT);
			}

			ListView list = FindViewById<ListView>(Resource.Id.listGekoppeld);
			ArrayAdapter ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1);
			list.Adapter = ListAdapter;
			ListAdapter.Clear();
			foreach (BluetoothDevice dev in _adapter.BondedDevices)
				ListAdapter.Add(dev.Name);
			bonded.Clear();
			bonded.AddRange(_adapter.BondedDevices);
		}

		protected override void OnActivityResult(int requestCode, [GeneratedEnum] Result resultCode, Intent data)
		{
			const int REQUEST_ENABLE_BT = 2;
			if (requestCode != REQUEST_ENABLE_BT)
			{
				base.OnActivityResult(requestCode, resultCode, data);
			}
			else if (resultCode == Result.Canceled)
			{
				Toast.MakeText(this, "This app requires bluetooth", ToastLength.Long).Show();
				Finish();
			}
		}

		private void List_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			BluetoothDevice dev = bonded[e.Position];
			_adapter.CancelDiscovery();
			UUID uuid = UUID.FromString("00001101-0000-1000-8000-00805F9B34FB");
			BluetoothSocket sock = dev.CreateRfcommSocketToServiceRecord(uuid);
			sock.Connect();

			MySocket = sock;
			StartActivity(typeof(SendActivity));
		}

		private void ZoekApparaten(object sender, EventArgs e)
		{
			StartActivity(typeof(DiscoverActivity));
		}
	}
}

