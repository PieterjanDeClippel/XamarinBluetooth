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
using Java.IO;
using Java.Lang;
using System.IO;
using Java.Lang.Reflect;

namespace XamarinBluetooth
{
	public class BluetoothConnector
	{

		private BluetoothSocketWrapper bluetoothSocket;
		private BluetoothDevice device;
		private bool secure;
		private BluetoothAdapter adapter;
		private List<UUID> uuidCandidates;
		private int candidate;
		
		/// <summary>
		/// Connect easily to a bluetooth device
		/// </summary>
		/// <param name="device">Device to connect to</param>
		/// <param name="secure">Whether or not the connection should be done via a secure socket</param>
		/// <param name="adapter">Android bluetooth adapter</param>
		/// <param name="uuidCandidates">List of UUID's. If null or empty, the serial PP is used</param>
		public BluetoothConnector(BluetoothDevice device, bool secure, BluetoothAdapter adapter, ParcelUuid[] uuidCandidates)
		{
			this.device = device;
			this.secure = secure;
			this.adapter = adapter;

			if (this.uuidCandidates == null)
			{
				this.uuidCandidates = new List<UUID>();
				this.uuidCandidates.Add(UUID.FromString("00001101-0000-1000-8000-00805F9B34FB"));
			}
			else
			{
				this.uuidCandidates = uuidCandidates.Select(T => T.Uuid).ToList();
			}
		}
		
		public BluetoothSocketWrapper Connect()
		{
			bool success = false;
			while (SelectSocket())
			{
				adapter.CancelDiscovery();

				try
				{
					bluetoothSocket.Connect();
					success = true;
					break;
				}
				catch (System.IO.IOException e)
				{
					//try the fallback
					try
					{
						bluetoothSocket = new FallbackBluetoothSocket(bluetoothSocket.GetUnderlyingSocket());
						Thread.Sleep(500);
						bluetoothSocket.Connect();
						success = true;
						break;
					}
					catch (FallbackException e1)
					{
						System.Console.WriteLine("BT", "Could not initialize FallbackBluetoothSocket classes.", e);
					}
					catch (InterruptedException e1)
					{
						System.Console.WriteLine("BT", e1.Message, e1);
					}
					catch (System.IO.IOException e1)
					{
						System.Console.WriteLine("BT", "Fallback failed. Cancelling.", e1);
					}
				}
			}

			if (!success)
			{
				throw new System.IO.IOException("Could not connect to device: " + device.Address);
			}

			return bluetoothSocket;
		}

		private bool SelectSocket()
		{
			if (candidate >= uuidCandidates.Count)
			{
				return false;
			}

			BluetoothSocket tmp;
			UUID uuid = uuidCandidates[candidate++];

			System.Console.WriteLine("BT", "Attempting to connect to Protocol: " + uuid);
			if (secure)
			{
				tmp = device.CreateRfcommSocketToServiceRecord(uuid);
			}
			else {
				tmp = device.CreateInsecureRfcommSocketToServiceRecord(uuid);
			}
			bluetoothSocket = new NativeBluetoothSocket(tmp);

			return true;
		}


		public interface BluetoothSocketWrapper
		{
			Stream GetInputStream();
			Stream GetOutputStream();
			string GetRemoteDeviceName();
			void Connect();
			string GetRemoteDeviceAddress();
			void Close();
			BluetoothSocket GetUnderlyingSocket();
		}
		
		public class NativeBluetoothSocket : BluetoothSocketWrapper
		{
			private BluetoothSocket socket;

			public NativeBluetoothSocket(BluetoothSocket tmp)
			{
				socket = tmp;
			}

			public Stream GetInputStream()
			{
				return socket.InputStream;
			}

			public Stream GetOutputStream()
			{
				return socket.OutputStream;
			}

			public string GetRemoteDeviceName()
			{
				return socket.RemoteDevice.Name;
			}

			public async void Connect()
			{
				await socket.ConnectAsync();
			}

			public string GetRemoteDeviceAddress()
			{
				return socket.RemoteDevice.Address;
			}

			public void Close()
			{
				socket.Close();
			}

			public BluetoothSocket GetUnderlyingSocket()
			{
				return socket;
			}
		}

		public class FallbackBluetoothSocket : NativeBluetoothSocket
		{
			private BluetoothSocket fallbackSocket;
			public FallbackBluetoothSocket(BluetoothSocket tmp) : base(tmp)
			{
				try
				{
					Class clazz = tmp.RemoteDevice.Class;
					Class[] paramTypes = new Class[] { Class.FromType(typeof(Integer)) };
					Method m = clazz.GetMethod("CreateRfcommSocket", paramTypes);
					Java.Lang.Object[] parms = new Java.Lang.Object[] { Integer.ValueOf(1) };
					fallbackSocket = (BluetoothSocket)m.Invoke(tmp.RemoteDevice, parms);
				}
				catch (Java.Lang.Exception e)
				{
					throw new FallbackException(e);
				}
			}

			public Stream GetInputStream()
			{
				return fallbackSocket.InputStream;
			}

			public Stream GetOutputStream()
			{
				return fallbackSocket.OutputStream;
			}

			public void Connect()
			{
				fallbackSocket.Connect();
			}
			
			public void Close()
			{
				fallbackSocket.Close();
			}
		}

		public class FallbackException : System.Exception
		{
			private long serialVersionUID = 1L;

			public FallbackException(System.Exception e) : base("FallBackException", e)
			{
			}
		}
	}
}