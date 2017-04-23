//
// test.cs: A test program for the mlist.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//
// (C) 2003 Ximian, Inc.
//
using System;
using System.Globalization;
using Gtk;
using Camel;

class SimpleProvider : IDataProvider {
	Summary summary;

	DateTime today_start, yesterday_start, week_ago;
	DateTimeFormatInfo date_format_info;
	string [] day_names, month_names;
	
	public SimpleProvider (string fname)
	{
		summary = new Summary (fname);

		DateTime now = DateTime.Now;
		today_start = new DateTime (now.Year, now.Month, now.Day);
		yesterday_start = today_start - new TimeSpan (24, 0, 0);
		week_ago = now;
		week_ago = now - new TimeSpan (7, 0, 0, 0);

		day_names = CultureInfo.CurrentCulture.DateTimeFormat.DayNames;
		month_names = CultureInfo.CurrentCulture.DateTimeFormat.MonthNames;
	}

	public object GetData (int row, Datum col)
	{
		MessageInfo mi = summary.messages [row];
		
		switch (col){
		case Datum.Sender:
			string f = mi.from;

			if (f.StartsWith ("<"))
				return f;
			
			int pos = f.IndexOf ("<");
			if (pos > -1){
				return f.Substring (0, pos-1);
			}

			return f;
			
		case Datum.TimeString:
			DateTime t = mi.received;

			if (t > today_start)
				return String.Format ("{0}:{1:D2} {2}",
						      t.Hour > 12 ? t.Hour-12 : t.Hour, t.Minute, t.Hour > 12 ? "PM" : "AM");
			if (t > week_ago)
				return String.Format ("{0} {1}/{2}",
						      t.DayOfWeek, t.Month, t.Day);

			return String.Format ("{0} {1}", month_names [t.Month-1], t.Day);

		case Datum.Time:
			return mi.received;
			
		case Datum.Subject:
			return mi.subject;

			//
			// Read/unread
			//
		case Datum.ReadStatus:
			return ((mi.flags & 16) != 0);

		case Datum.IsDeleted:
			return ((mi.flags & 2) != 0);
			
		case Datum.GroupTime:
			DateTime rec = mi.received;
			if (rec > today_start)
				return today_start;

			return new DateTime (rec.Year, rec.Month, rec.Day);

		case Datum.Label_GroupTime:
			DateTime dt = mi.received;

			if (dt > today_start)
				return "Today";

			if (dt > yesterday_start)
				return "Yesterday";
			
			if (dt < week_ago)
				return "This week";

			return String.Format ("{0} {1}, {2}", month_names [dt.Month-1], dt.Day, dt.Year);
			
		default:
			return null;
		}
	}

	public string GetCaption (int row, Datum col)
	{
		return "Caption";
	}
	
	public int Rows {
		get {
			return summary.messages.Length;
		}
	}
}

class Boot {
	
	static void Main (string [] args)
	{
		Application.Init ();

		Console.WriteLine ("{0}", GLib.Markup.EscapeText ("Martin MOKREJ"));
		
		Window win = new Window ("Main Window");
		HBox box = new HBox (false, 0);
		SimpleProvider p = new SimpleProvider ("mbox.ev-summary");
		Mlist list = new Mlist (p, Datum.GroupTime, Datum.Label_GroupTime, Datum.Time, true);
		box.Add (list);

		Scrollbar scroll = new VScrollbar (list.Adjustment);
		box.Add (scroll);
		win.Add (box);
		win.ShowAll ();
		Application.Run ();
	}
}

