# bin2tap - Microbee Binary to TAP file converter

A simple command line tool for converting Microbee binary images to TAP files.

## Download

Download here:

* <https://raw.github.com/toptensoftware/bin2tap/master/bin2tap.zip>

Requires:

* Windows and .NET 4.0 or later
* Linux/OSX with Mono 2.8 or later (not tested, should work)

## Usage

	bin2tap source.bin destination.tap [options] [@responsefile]

Options:

	--name:N               File name (max 6 chars), default = input file name
	--type:[M|B]           File type - M=machine code, B=Basic, default=M
	--loadaddr:N           Load address, default=0x0400
	--startaddr:N          Start address (entry point), default=0x0400
	--baud:300|1200        How to set the speed indicator in the header, default=1200
	--autostart:Y|N        Auto start or not, default=Yes for machine files, No for Basic
	--help                 Show these help instruction
	--v                    Show version information

## Example

	bin2tap robotf.bin --loadaddr:0x0400 --startaddr:0x1983