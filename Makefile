SRCS=\
  Card.cs \
  Deck.cs \
  Player.cs \
  Program.cs

MCS=dmcs
LFLAGS=/sdk:4

Hanabi.exe: $(SRCS)
	$(MCS) $(LFLAGS) $(SRCS) -o Hanabi.exe

all: Hanabi.exe

.PHONY:

clean: .PHONY
	rm -f Hanabi.exe
