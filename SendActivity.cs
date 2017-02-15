using System;
using System.Collections.Generic;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Hardware;
using Android.Content.PM;

namespace XamarinBluetooth
{
	[Activity(Label = "SendActivity", ScreenOrientation = ScreenOrientation.Portrait)]
	public class SendActivity : Activity, ISensorEventListener
	{
		const int buf_size = 1024;
		byte[] inputbuffer = new byte[buf_size];

		protected override void OnCreate(Bundle savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			SetContentView(Resource.Layout.SendLayout);

			Button btnSend = FindViewById<Button>(Resource.Id.btnSend);
			EditText txtInput = FindViewById<EditText>(Resource.Id.textinput);
			Button btnDisconnect = FindViewById<Button>(Resource.Id.btnDisconnect);

			btnSend.Click += BtnSend_Click;
			btnDisconnect.Click += BtnDisconnect_Click;
			txtInput.Enabled = false;
			btnSend.Enabled = false;

			//ReadThread reading = new ReadThread(MainActivity.MySocket.InputStream);
			//reading.StreamRead += Reading_StreamRead;
			//reading.Start();

			_sm = (SensorManager)GetSystemService(Context.SensorService);
		}

		bool use_accelero = true;
		public override bool OnPrepareOptionsMenu(IMenu menu)
		{
			menu.Clear();
			if(use_accelero)
			{
				menu.Add(Menu.None, 1, Menu.None, "Text Input");
			}
			else
			{
				menu.Add(Menu.None, 2, Menu.None, "Accelerometer");
			}
			return base.OnPrepareOptionsMenu(menu);
		}
		public override bool OnOptionsItemSelected(IMenuItem item)
		{
			Button btnSend = FindViewById<Button>(Resource.Id.btnSend);
			EditText txtInput = FindViewById<EditText>(Resource.Id.textinput);
			if (item.ItemId == 1)
			{
				use_accelero = false;
				txtInput.Enabled = true;
				btnSend.Enabled = true;
			}
			else
			{
				use_accelero = true;
				txtInput.Enabled = false;
				btnSend.Enabled = false;
			}
			return base.OnOptionsItemSelected(item);
		}

		protected override void OnResume()
		{
			base.OnResume();
			_sm.RegisterListener(this, _sm.GetDefaultSensor(SensorType.Accelerometer), SensorDelay.Ui);
		}
		protected override void OnPause()
		{
			base.OnPause();
			_sm.UnregisterListener(this);
		}

		//private void Reading_StreamRead(object sender, ReadThread.StreamReadEventArgs e)
		//{
		//	RunOnUiThread(delegate {
		//		TextView view = FindViewById<TextView>(Resource.Id.txtReceived);

		//		string txt = "Received data: " + e.Data + "\r\n";
		//		MainActivity.MySocket.OutputStream.Write(Encoding.UTF8.GetBytes(txt), 0, txt.Length);
		//		view.Append(e.Data);
		//	});
		//}

		public override void OnBackPressed()
		{
			_sm.UnregisterListener(this);
			MainActivity.MySocket.Close();
			MainActivity.MySocket.Dispose();
			base.OnBackPressed();
		}

		private void BtnDisconnect_Click(object sender, EventArgs e)
		{
			_sm.UnregisterListener(this);
			MainActivity.MySocket.Close();
			MainActivity.MySocket.Dispose();
			Finish();
		}

		private void BtnSend_Click(object sender, EventArgs e)
		{
			EditText txt = FindViewById<EditText>(Resource.Id.textinput);

			byte[] data = Encoding.UTF8.GetBytes(txt.Text);
			MainActivity.MySocket.OutputStream.WriteAsync(data, 0, data.Length);
		}

		static readonly object syncLock = new object();
		SensorManager _sm;

		public void OnAccuracyChanged(Sensor sensor, [GeneratedEnum] SensorStatus accuracy)
		{
		}

		byte SentByte = 0;
		public void OnSensorChanged(SensorEvent e)
		{
			TextView view = FindViewById<TextView>(Resource.Id.txtReceived);

			lock (syncLock)
			{
				view.Text = string.Format("x={0:f}\r\ny={1:f}\r\nz={2:f}", e.Values[0], e.Values[1], e.Values[2]);
				byte send = ConvertDirection(e.Values);
				if (send != SentByte & use_accelero)
					MainActivity.MySocket.OutputStream.Write(new byte[] { SentByte = send }, 0, 1);
			}
		}

		public byte ConvertDirection(IList<float> accel)
		{
			// http://cdn.sparkfun.com/datasheets/Robotics/DaguCarCommands.pdf
			//
			// Y	->		-8:+8		=		vooruit:achteruit
			// X	->		>3:<-3		=		links:rechts

			float x = accel[0];
			float y = accel[1];

			// speed =   0 -> 15
			int speed = System.Math.Min(Convert.ToInt32(System.Math.Abs(y) * 2), 15);
			// forw =	true:vooruit	false:achteruit
			bool forw = y <= 0;
			// dir =	0:links	-1:rechtdoor	1:rechts
			int dir = x >= 3 ? 0 : x <= -3 ? 1 : -1;

			int MSB_nibble = 0;
			if(dir == -1)
			{
				MSB_nibble = forw ? 1 : 2;
			}
			else
			{
				MSB_nibble = forw ? 5 : 7;
				MSB_nibble += dir;
			}

			return Convert.ToByte((MSB_nibble << 4) + speed);
		}
	}
}