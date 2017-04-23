VERSION=0.4
SOURCES = mlist.cs test.cs camel.cs

RES = -resource:mail-new.png -resource:mail-read.png -resource:mail-replied.png -resource:plus.png -resource:minus.png

all: $(SOURCES)	
	mcs -g -o test.exe $(SOURCES) -pkg:gtk-sharp $(RES)

b: all
	mono --debug test.exe

dist:
	-mkdir mlist-$(VERSION)
	cp *.cs *png makefile README mlist-$(VERSION)
	tar czvf mlist-$(VERSION).tar.gz mlist-$(VERSION)