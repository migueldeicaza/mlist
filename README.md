
Mlist widget, by Miguel de Icaza (miguel@microsoft.com)

This is an import of an old code drop that I found, written in
2003 or 2005.   Here just for reference.

# Introduction

This is a list widget similar to what you have seen on the
new Outlook screenshots.   Hopefully the code can be useful
to people who might want to learn to write widgets from 
scratch with Gtk#.

The idea of the new layout in Outlook is very simple: most of
the time the right side of your message display contains blank
space, which is not used, and the list of messages is crammed
down to a few lines above the current message view.

My moving from the traditional view to the new view, you win
a few lines:

```
+------------------------+  +-------------+---------+
| Sender   Time  Subject |  | Sender Time | Content |
| Sender   Time  Subject |  | Subject     | Content |
+------------------------+  +-------------+ Content |
| Content	         |  | Sender Time | Content |
| Content	         |  | Subject     | Content |
| Content	         |  | Sender Time | Content |
| Content	         |  | Subject     | Content |
+------------------------+  +-------------+---------+
```

The graphic does not do justice to the real space savings, but
you get the idea.  

# The widget

The widget supports grouping, and it is not very general, it
is currently designed to just render this particular kind of
data. 

The widget communicates with the data through an interface:

```csharp
interface IDataProvider {
	object GetData (int row, Datum col);
	int    Rows { get; }
}
```

The implementor of this interface acts as the model for the
widget, the widget implements the view.  The model will be
called back to retrieve information.

The widget takes firve arguments:

```csharp
public Mlist (IDataProvider provider, Datum group_col, 
	      Datum group_label, Datum sort_col, bool invert_sort)
```

The first is the model provider, which should return data on
demand, and the number of rows on it.  The `group_col`
is the column queried to perform groupings, you can choose any
grouping style you want with this function.

The actual labels rendered for the group should be provided by
the `group_label` column on the model.  You can for example
group by day, but you might want the label to show information
with labels like "Today", "Yesterday", "Last week".  Ideally
the widget is initialized and constructed in a way that the
columns make sense.

The `sort_col` is the column used to sort the elements inside a
group,and the invert_sort operation is used to tell whether
the sorting order should be reverted.

A typical use is:

```csharp
Mlist list = new Mlist (
	p, Datum.GroupTime, Datum.Label_GroupTime, Datum.Time, true);
```

This will group by time, will use a special label for grouping
by time, and will sort by the time field.   

# Using it.

To compile use the `make` command, you will need a recent
version of Gtk# (CVS at the time of this writing), the default
test program will load a file in the current directory called
`mbox.ev-summary` which is a Camel summary file for mbox files
(I took this from my Evolution setup).

You can provide your own model, but using the default, and
using the Evolution code allows you to see the data in all its
beauty. 

	