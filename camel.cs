using System.IO;
using System;
using System.Globalization;
using System.Text;

namespace Camel {
	public class Summary {
		public MBoxSummaryHeader header;
		public MessageInfo [] messages;
		
		public Summary (string file)
		{
			using (FileStream f = File.OpenRead (file)){
				header = new MBoxSummaryHeader (f);

				messages = new MessageInfo [header.count];
				
				for (int i = 0; i < header.count; i++){
					messages [i] = new MBoxMessageInfo (f);
				}
			}
		}
	}

	public class MessageInfo {
		public string uid, subject, from, to, cc, mlist;
		public uint size, flags;
		public DateTime sent, received;
		
		public MessageInfo (FileStream f)
		{
			uid = Decode.String (f);
			flags = Decode.UInt (f);
			size = Decode.UInt (f);
			sent = Decode.Time (f);
			received = Decode.Time (f);
			subject = Decode.String (f);
			from = Decode.String (f);
			to = Decode.String (f);
			cc = Decode.String (f);
			mlist = Decode.String (f);

			Decode.FixedInt (f);
			Decode.FixedInt (f);

			uint count;

			// references
			count = Decode.UInt (f);
			if (count > 0){
				for (int i = 0; i < count; i++){
					Decode.FixedInt (f);
					Decode.FixedInt (f);
				}
			}

			// user flags
			count = Decode.UInt (f);
			if (count > 0){
				for (int i = 0; i < count; i++){
					Decode.String (f);
				}
			}

			// user tags
			count = Decode.UInt (f);
			if (count > 0){
				for (int i = 0; i < count; i++){
					Decode.String (f);
				}
			}
		}

		public override string ToString ()
		{
			return String.Format ("From: {0}\nTo: {1}\nSubject: {2}", from, to, subject);
		}
	}

	public class MBoxMessageInfo : MessageInfo {
		uint from_pos;
		
		public MBoxMessageInfo (FileStream f) : base (f)
		{
			from_pos = Decode.Offset (f);
		}
	}
	
	public class SummaryHeader {
		public int version;
		public int flags;
		public int nextuid;
		public DateTime time;
		public int count;
		
		public SummaryHeader (FileStream f)
		{
			version = Decode.FixedInt (f);
			flags = Decode.FixedInt (f);
			nextuid = Decode.FixedInt (f);
			time = Decode.Time (f);
			count = Decode.FixedInt (f);

			Console.WriteLine ("V={0} time={1}, count={2}", version, time, count);
		}
	}

	public class MBoxSummaryHeader : SummaryHeader {
		public uint folder_size;
		
		public MBoxSummaryHeader (FileStream f) : base (f)
		{
			folder_size = Decode.UInt (f);
			Console.WriteLine ("Folder size:" + folder_size);
		}
	}
	
	public class Decode {
		static Encoding e = Encoding.UTF8;
		static long UnixBaseTicks;

		static Decode ()
		{
			UnixBaseTicks = new DateTime (1970, 1, 1, 0, 0, 0).Ticks;
		}
	       
		public static string String (FileStream f)
		{
			int len = (int) UInt (f);
			len--;

			if (len > 65535)
				throw new Exception ();
			byte [] buffer = new byte [len];
			f.Read (buffer, 0, (int) len);
			return new System.String (e.GetChars (buffer, 0, len));
		}

		public static uint UInt (FileStream f)
		{
			uint value = 0;
			int v;
			
			while (((v = f.ReadByte ()) & 0x80) == 0 && v != -1){
				value |= (byte) v;
				value <<= 7;
			}
			return value | ((byte)(v & 0x7f));
		}

		public static int FixedInt (FileStream f)
		{
			byte [] b = new byte [4];

			f.Read (b, 0, 4);

			return (b [0] << 24) | (b [1] << 16) | (b [2] << 8) | b [3];
		}

		public static DateTime Time (FileStream f)
		{
			byte [] b = new byte [4];

			f.Read (b, 0, 4);

			return new DateTime (UnixBaseTicks).AddSeconds ((b [0] << 24) | (b [1] << 16) | (b [2] << 8) | b [3]);
		}

		public static uint Offset (FileStream f)
		{
			byte [] b = new byte [4];

			f.Read (b, 0, 4);

			return (uint)((b [0] << 24) | (b [1] << 16) | (b [2] << 8) | b [3]);
		}
	}

	class Test {
		void Main (string [] args)
		{
			string file;
			
			if (args.Length == 0)
				file = "mbox.ev-summary";
			else
				file = args [0];
			
			Summary s = new Summary (file);
			
		}
		
	}
}
