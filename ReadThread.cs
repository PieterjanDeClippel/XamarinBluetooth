using Java.Lang;
using System;
using System.IO;
using System.Text;

namespace XamarinBluetooth
{
	public class ReadThread : Thread
	{
		private Stream input;

		public ReadThread(Stream input)
		{
			this.input = input;
		}

		#region Event
		public class StreamReadEventArgs : EventArgs
		{
			public string Data { get; private set; }
			public StreamReadEventArgs(string Data)
			{
				this.Data = Data;
			}
		}
		public delegate void StreamReadEventHandler(object sender, StreamReadEventArgs e);
		/// <summary>
		/// Fired when the InputStream received data. Not ThreadSafe.
		/// </summary>
		public event StreamReadEventHandler StreamRead;
		protected void OnStreamRead(StreamReadEventArgs e)
		{
			if (StreamRead != null)
				StreamRead(this, e);
		}
		#endregion

		public override void Run()
		{
			base.Run();
			byte[] buffer = new byte[1024];
			int byte_count;

			while(true)
			{
				try
				{
					byte_count = input.Read(buffer, 0, buffer.Length);
					string msg = Encoding.UTF8.GetString(buffer, 0, byte_count);
					OnStreamRead(new StreamReadEventArgs(msg));
				}
				catch (Java.IO.IOException)
				{
					break;
				}
			}
		}
	}
}