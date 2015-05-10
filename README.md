# Windows-Entropy-Collection

WEC is a basic entropy pool for Windows I created because I was bored at twelve in the morning.
It works by collecting information from your mouse movements and keyboard tik taking, adding collected integers to entropy purgatory (An intermediate List<int>) then the agent will XOR the entropy to the ISAAC CSPRNG memory and then tumble them around so we have some nice fresh delicious random bytes.

Programs can request random bytes from the agent by communicating with it via TCP on port 65535, if the connection is successful the agent will sent an ACK (0x06). From there the program will request the amount of entropy it needs, for example if the program needed 10 bytes you would send 0x0A and if you needed the maxmimum 255 bytes you would send 0xFF.

Once the agent recieves the request it will send the requested amount of bytes followed by an EOT (0x04).
