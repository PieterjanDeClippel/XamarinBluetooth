using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Bluetooth;
using Java.Util;

namespace XamarinBluetooth
{
	[Activity(Label = "DiscoverActivity")]
	public class DiscoverActivity : Activity
	{
		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.DiscoverLayout);

			_adapter = BluetoothAdapter.DefaultAdapter;

			Button btnDiscover = FindViewById<Button>(Resource.Id.btnDiscover);
			btnDiscover.Click += BtnDiscover_Click;

			rec = new BluetoothReceiver();

			rec.DeviceFound += Rec_DeviceFound;
			RegisterReceiver(rec, new IntentFilter(BluetoothDevice.ActionFound));
			rec.DiscoveryStarted += Rec_DiscoveryStarted;
			RegisterReceiver(rec, new IntentFilter(BluetoothAdapter.ActionDiscoveryStarted));
			rec.DiscoveryFinished += Rec_DiscoveryFinished;
			RegisterReceiver(rec, new IntentFilter(BluetoothAdapter.ActionDiscoveryFinished));
			rec.BondStateChanged += Rec_BondStateChanged;
			RegisterReceiver(rec, new IntentFilter(BluetoothDevice.ActionBondStateChanged));
			//rec.UUIDFetched += Rec_UUIDFetched;
			//RegisterReceiver(rec, new IntentFilter(BluetoothDevice.ActionUuid));

			ListView list = FindViewById<ListView>(Resource.Id.listView1);
			ListAdapter = new ArrayAdapter<string>(this, Android.Resource.Layout.SimpleListItem1);
			list.Adapter = ListAdapter;
			list.ItemClick += List_ItemClick;
		}
		
		private void List_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
		{
			FoundDevices[e.Position].CreateBond();
		}

		private void Rec_DiscoveryStarted(object sender, EventArgs e)
		{
			Button btnDiscover = FindViewById<Button>(Resource.Id.btnDiscover);
			btnDiscover.Text = "Stop Discovery";
		}

		private void Rec_DiscoveryFinished(object sender, EventArgs e)
		{
			Button btnDiscover = FindViewById<Button>(Resource.Id.btnDiscover);
			btnDiscover.Text = "Restart Discovery";
		}

		private void Rec_DeviceFound(object sender, DeviceFoundEventArgs e)
		{
			if (e.Device == null) return;
			if (e.Device.Name == null) return;
			if (e.Device.BondState == Bond.Bonded | e.Device.BondState == Bond.Bonding) return;
			ListAdapter.Add(e.Device.Name);
			FoundDevices.Add(e.Device);
		}

		private void Rec_BondStateChanged(object sender, BondStateChangedEventArgs e)
		{
			if(e.NewState == Bond.Bonded)
			{
				ListAdapter.Remove(e.Device.Name);
				FoundDevices.Remove(e.Device);
				Toast.MakeText(this, "Gekoppeld met " + e.Device.Name, ToastLength.Short);
			}
			else
			{
				Toast.MakeText(this, "Bezig met koppelen met " + e.Device.Name, ToastLength.Long);
			}
		}

		BluetoothAdapter _adapter;
		BluetoothReceiver rec;
		ArrayAdapter ListAdapter;
		List<BluetoothDevice> FoundDevices = new List<BluetoothDevice>();

		private void BtnDiscover_Click(object sender, EventArgs e)
		{
			if (_adapter.IsDiscovering)
				_adapter.CancelDiscovery();
			else
			{
				ListAdapter.Clear();
				FoundDevices.Clear();
				_adapter.StartDiscovery();
			}
		}
	}

	public class BluetoothReceiver : BroadcastReceiver
	{
		public override void OnReceive(Context context, Intent intent)
		{
			switch (intent.Action)
			{
				case BluetoothDevice.ActionFound:
					BluetoothDevice device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
					OnDeviceFound(new DeviceFoundEventArgs(device));
					break;
				case BluetoothAdapter.ActionDiscoveryStarted:
					OnDiscoveryStarted(EventArgs.Empty);
					break;
				case BluetoothAdapter.ActionDiscoveryFinished:
					OnDiscoveryFinished(EventArgs.Empty);
					break;
				case BluetoothDevice.ActionBondStateChanged:
					BluetoothDevice device2 = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
					Bond oldState = (Bond)(int)intent.GetParcelableExtra(BluetoothDevice.ExtraPreviousBondState);
					Bond newState = (Bond)(int)intent.GetParcelableExtra(BluetoothDevice.ExtraBondState);
					OnBondStateChanged(new BondStateChangedEventArgs(device2, oldState, newState));
					break;
				case BluetoothDevice.ActionUuid:
					UUID uuid = (UUID)intent.GetParcelableExtra(BluetoothDevice.ExtraUuid);
					OnUUIDFetched(new UUIDFetchedEventArgs(uuid));
					break;
			}
		}

		public delegate void DeviceFoundEventHandler(object sender, DeviceFoundEventArgs e);
		public event DeviceFoundEventHandler DeviceFound;
		protected void OnDeviceFound(DeviceFoundEventArgs e)
		{
			if (DeviceFound != null)
				DeviceFound(this, e);
		}

		public event EventHandler DiscoveryStarted;
		protected void OnDiscoveryStarted(EventArgs e)
		{
			if (DiscoveryStarted != null)
				DiscoveryStarted(this, e);
		}

		public event EventHandler DiscoveryFinished;
		protected void OnDiscoveryFinished(EventArgs e)
		{
			if (DiscoveryFinished != null)
				DiscoveryFinished(this, e);
		}

		public delegate void BondStateChangedEventHandler(object sender, BondStateChangedEventArgs e);
		public event BondStateChangedEventHandler BondStateChanged;
		protected void OnBondStateChanged(BondStateChangedEventArgs e)
		{
			if (BondStateChanged != null)
				BondStateChanged(this, e);
		}

		public delegate void UUIDFetchedEventHandler(object sender, UUIDFetchedEventArgs e);
		public event UUIDFetchedEventHandler UUIDFetched;
		protected void OnUUIDFetched(UUIDFetchedEventArgs e)
		{
			if (UUIDFetched != null)
				UUIDFetched(this, e);
		}
	}

	public class DeviceFoundEventArgs : EventArgs
	{
		public DeviceFoundEventArgs(BluetoothDevice Device)
		{
			device = Device;
		}

		private BluetoothDevice device;
		public BluetoothDevice Device
		{
			get { return device; }
		}
	}
	public class BondStateChangedEventArgs : EventArgs
	{
		public BondStateChangedEventArgs(BluetoothDevice Device, Bond OldState, Bond NewState)
		{
			device = Device;
			oldState = OldState;
			newState = NewState;
		}

		private BluetoothDevice device;
		public BluetoothDevice Device
		{
			get { return device; }
		}
		private Bond oldState;
		public Bond OldState
		{
			get { return oldState; }
		}
		private Bond newState;
		public Bond NewState
		{
			get { return newState; }
		}
	}
	public class UUIDFetchedEventArgs : EventArgs
	{
		public UUIDFetchedEventArgs(UUID uuid)
		{
			this.uuid = uuid;
		}

		private UUID uuid;
		public UUID UUID
		{
			get { return uuid; }
		}
	}
}