//
// olist.cs: A grouping listing widget.
//
// Author:
//   Miguel de Icaza (miguel@ximian.com)
//
// (C) 2003 Ximian, Inc.
//

using Gtk;
using Gdk;
using GtkSharp;
using System;
using System.Collections;

public enum Datum {
	Sender,
	Time,
	TimeString,
	Subject,
	ReadStatus,
	IsDeleted,
	IconToUse,
	
	GroupTime,

	Label_GroupTime
}

interface IDataProvider {
	object GetData (int row, Datum col);
	int    Rows { get; }
}

class Mlist : Gtk.DrawingArea {
	const int Width = 320;
	const int SenderWidth = 220;
	
	Gtk.Adjustment adjustment;
	IDataProvider provider;
	Pango.Layout layout, bold_layout;
	Datum group_col, sort_col, group_label;
	bool invert_sort;
	static Gdk.Pixbuf mail_new, mail_read, mail_replied, plus, minus;

	SortKeyCompare sorter;
	
	int top_displayed;
	
	static void InitImages ()
	{
		mail_new = new Gdk.Pixbuf (null, "mail-new.png");
		mail_read = new Gdk.Pixbuf (null, "mail-read.png");
		mail_replied = new Gdk.Pixbuf (null, "mail-replied.png");
		mail_replied = new Gdk.Pixbuf (null, "mail-replied.png");
		plus = new Gdk.Pixbuf (null, "plus.png");
		minus = new Gdk.Pixbuf (null, "minus.png");
	}
	
	public Mlist (IDataProvider provider, Datum group_col, Datum group_label, Datum sort_col, bool invert_sort)
	{
		if (mail_new == null)
			InitImages ();
		
		this.provider = provider;
		this.group_col = group_col;
		this.group_label = group_label;
		
		sorter = new SortKeyCompare (this, sort_col, invert_sort);
		
		adjustment = new Gtk.Adjustment (0, 0, 0, 1, 1, 1);
		adjustment.ValueChanged += new EventHandler (ValueChangedHandler);
		
		SetSizeRequest (Width, 600);

		ComputeGroups ();

		
		layout = new Pango.Layout (PangoContext);
		layout.FontDescription = Pango.FontDescription.FromString ("Tahoma 13");
		bold_layout = new Pango.Layout (PangoContext);
		bold_layout.FontDescription = Pango.FontDescription.FromString ("Tahoma Bold 13");
		
		//
		// Event handlers
		//
		ExposeEvent += new ExposeEventHandler (ExposeHandler);
		Realized += new EventHandler (RealizeHandler);
		Unrealized += new EventHandler (UnrealizeHandler);
	}

	struct GroupBlock : IComparable {
		public int       position;
		public bool      expanded;
		public ArrayList index_list;

		int IComparable.CompareTo (object other)
		{
			object other_item = ((GroupBlock) other).index_list [0];
			IComparable this_item = (IComparable) index_list [0];

			return - this_item.CompareTo (other_item);
		}
	};

	class SortKeyCompare : IComparer {
		bool invert_sort;
		Datum sort_col;
		Mlist mlist;
		
		public SortKeyCompare (Mlist mlist, Datum sort_col, bool invert_sort)
		{
			this.mlist = mlist;
			this.sort_col = sort_col;
			this.invert_sort = invert_sort;
		}
		
		int IComparer.Compare (object a, object b)
		{
			IComparable a_key = (IComparable) mlist.provider.GetData ((int) a, sort_col);
			IComparable b_key = (IComparable) mlist.provider.GetData ((int) b, sort_col);

			if (invert_sort)
				return -a_key.CompareTo (b_key);
			else
				return a_key.CompareTo (b_key);
		}
	}

	GroupBlock [] group_blocks;
	
	void ComputeGroups ()
	{
		int rows = provider.Rows;

		Hashtable groups = new Hashtable ();
		
		for (int row = 0; row < rows; row++){
			ArrayList list;
			object key = provider.GetData (row, group_col);

			list = (ArrayList) groups [key];
			if (list == null)
				groups [key] = list = new ArrayList ();
			list.Add (row);
		}
		group_blocks = new GroupBlock [groups.Count];
		int i = 0;
		foreach (object o in groups.Keys){
			group_blocks [i].expanded = true;
			ArrayList l = (ArrayList) groups [o];
			l.Sort (sorter);
			group_blocks [i].index_list = l;
			i++;
		}
		Array.Sort (group_blocks);

		// Compute the pos offsets
		int pos = 0;
		i = 0;
		foreach (GroupBlock gb in group_blocks){
			group_blocks [i].position = pos;
			pos += group_blocks [i].index_list.Count + 1;
			i++;
		}

		adjustment.SetBounds (0, rows + groups.Count, 1, 20, 20);
	}

	public Gtk.Adjustment Adjustment {
		get {
			return adjustment;
		}
	}

	void ValueChangedHandler (object obj, EventArgs e)
	{
		top_displayed = (int) Adjustment.Value;
		QueueDrawArea (0, 0, Allocation.Width, Allocation.Height);
	}
	
#region Rendering code
	
	Gdk.GC gray, clipped_gc;
	
	void RealizeHandler (object o, EventArgs sender)
	{
		gray = new Gdk.GC (GdkWindow);
		gray.Copy (Style.BackgroundGC (StateType.Normal));
		Gdk.Colormap colormap = Gdk.Colormap.System;
		
                Gdk.Color fgcol = new Gdk.Color (0x99, 0x99, 0x99);
		colormap.AllocColor (ref fgcol, true, true);
		gray.Foreground = fgcol;

		clipped_gc = new Gdk.GC (GdkWindow);
		clipped_gc.Copy (Style.BlackGC);
		Gdk.Rectangle rect = new Gdk.Rectangle ();
		rect.X = 0;
		rect.Y = 0;
		rect.Width = SenderWidth;
		rect.Height = 10000;
		clipped_gc.ClipRectangle = rect;
	}

	void UnrealizeHandler (object o, EventArgs sender)
	{
		gray.Dispose ();
	}
	
	int DrawEntry (Gdk.Window win, int y, int row)
	{
		bool read = (bool) provider.GetData (row, Datum.ReadStatus);
		bool deleted = (bool) provider.GetData (row, Datum.IsDeleted);
		string time = (string) provider.GetData (row, Datum.TimeString);
		string sender = (string) provider.GetData (row, Datum.Sender);
		string text = (string) provider.GetData (row, Datum.Subject);
		int x = 30;

		y += 4;
		
		//
		// Cute icon
		//
		Gdk.Pixbuf icon = read ? mail_read : mail_new;
		icon.RenderToDrawable (win, gray, 0, 0, 10, y + 1, 16, 16, Gdk.RgbDither.Normal, 0, 0);

		//
		// Sender and Time.
		//
		Pango.Layout r;
		if (read)
			r = layout;
		else
			r = bold_layout;

		r.SetText (sender);
		win.DrawLayout (clipped_gc, x, y, r);

		r.SetText (time);
		int width, height;
		r.GetPixelSize (out width, out height);
		win.DrawLayout (Style.BlackGC, Width-width - 3, y, r);
		r.Alignment = Pango.Alignment.Left;

		// Subject
		layout.SetMarkup (text);
		win.DrawLayout (gray, x, y + 18, layout);
		if (deleted)
			win.DrawLine (gray, x, y + 28, Width, y+28);
		
		win.DrawLine (gray, 0, y + 39, Width, y + 39);

		//
		// FIXME: Should compute size instead for two lines. 
		//
		return 40;
	}

	int DrawGroup (Gdk.Window win, int y, int group_index)
	{
		string s = (string) provider.GetData ((int) group_blocks [group_index].index_list [0], group_label);

		Gdk.Pixbuf icon = group_blocks [group_index].expanded ? minus : plus;
		icon.RenderToDrawable (win, gray, 0, 0, 3, y + 21, 16, 16, Gdk.RgbDither.Normal, 0, 0);
		
		bold_layout.SetText (s);
		win.DrawLayout (Style.BlackGC, 20, y + 20, bold_layout);

		win.DrawLine (gray, 0, y + 38, Width, y + 38);
		win.DrawLine (gray, 0, y + 39, Width, y + 39);
		return 40;
	}
	
	void ExposeHandler (object obj, ExposeEventArgs args)
	{
		Gdk.Window win = args.Event.Window;
		Gdk.Rectangle area = args.Event.Area;
		
		win.DrawRectangle (Style.WhiteGC, true, area);

		int group_index = group_blocks.Length-1;
		int item_index = 0; // FIXME, this should always be computed!
		for (int i = 0; i < group_blocks.Length-1; i++)
			if (group_blocks [i+1].position > top_displayed){
				group_index = i;
				item_index = top_displayed - group_blocks [i].position;
				break;
			}

		for (int y = 0; y < area.Height; ){
			if (item_index == 0)
				y += DrawGroup (win, y, group_index);
			else {
				int row = (int) group_blocks [group_index].index_list [item_index-1];
				y += DrawEntry (win, y, row);
			}
			item_index++;
			if (item_index >= group_blocks [group_index].index_list.Count-1){
				group_index++;
				item_index = 0;
			}
		}
		args.RetVal = true;
	}
#endregion 
}
