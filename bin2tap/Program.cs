using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bin2tap
{
	class Program
	{
		string _inputFile;
		string _outputFile;
		string _name;
		byte _type = (byte)'M';
		ushort _loadAddr;
		ushort _startAddr;
		int _baud = 1200;
		bool? _autoStart;

		public bool ProcessArg(string arg)
		{
			if (arg == null)
				return true;

			if (arg.StartsWith("#"))
				return true;

			// Response file
			if (arg.StartsWith("@"))
			{
				// Get the fully qualified response file name
				string strResponseFile = System.IO.Path.GetFullPath(arg.Substring(1));

				// Load and parse the response file
				var args = Utils.ParseCommandLine(System.IO.File.ReadAllText(strResponseFile));

				// Set the current directory
				string OldCurrentDir = System.IO.Directory.GetCurrentDirectory();
				System.IO.Directory.SetCurrentDirectory(System.IO.Path.GetDirectoryName(strResponseFile));

				// Load the file
				bool bRetv = ProcessArgs(args);

				// Restore current directory
				System.IO.Directory.SetCurrentDirectory(OldCurrentDir);

				return bRetv;
			}

			// Args are in format [/-]<switchname>[:<value>];
			if (arg.StartsWith("/") || arg.StartsWith("-"))
			{
				string SwitchName = arg.Substring(arg.StartsWith("--") ? 2 : 1);
				string Value = null;

				int colonpos = SwitchName.IndexOf(':');
				if (colonpos >= 0)
				{
					// Split it
					Value = SwitchName.Substring(colonpos + 1);
					SwitchName = SwitchName.Substring(0, colonpos);
				}

				switch (SwitchName)
				{
					case "help":
					case "h":
					case "?":
						ShowLogo();
						ShowHelp();
						return false;

					case "v":
						ShowLogo();
						return false;

					case "loadaddr":
						_loadAddr = Utils.ParseUShort(Value);
						if (_startAddr == 0)
							_startAddr = _loadAddr;
						break;

					case "startaddr":
						_startAddr = Utils.ParseUShort(Value);
						break;

					case "name":
						if (Value.Length > 6)
							throw new InvalidOperationException("Filename too long");
						_name = Value;
						break;

					case "type":
						if (Value == "M" || Value == "B")
							_type = (byte)Value[0];
						else
							throw new InvalidOperationException("Invalid file type '{0}' must be 'M' or 'B'");
						break;

					case "baud":
						_baud = Utils.ParseUShort(Value);
						break;

					case "autostart":
						if (Value.ToLower().StartsWith("y") || Value.ToLower().StartsWith("T") || Value.ToLower().StartsWith("1"))
							_autoStart = true;
						else if (Value.ToLower().StartsWith("n") || Value.ToLower().StartsWith("f") || Value.ToLower().StartsWith("0"))
							_autoStart = false;
						else
							throw new InvalidOperationException("Invalid value for autostart, please specify 'Y' or 'N'");
						break;

					default:
						throw new InvalidOperationException(string.Format("Unknown switch '{0}'", arg));
				}
			}
			else
			{
				if (_inputFile == null)
					_inputFile = arg;
				else if (_outputFile == null)
					_outputFile = arg;
				else
					throw new InvalidOperationException(string.Format("Too many command line arguments, don't know what to do with '{0}'", arg));
			}

			return true;
		}

		public bool ProcessArgs(IEnumerable<string> args)
		{
			if (args == null)
				return true;

			// Parse args
			foreach (var a in args)
			{
				if (!ProcessArg(a))
					return false;
			}

			return true;
		}


		public void ShowLogo()
		{
			System.Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
			Console.WriteLine("bin2tap v{0} - Microbee binary to tap file utility", v);
			Console.WriteLine("Copyright (C) 2012 Topten Software. All Rights Reserved.");
			Console.WriteLine("");
		}

		public void ShowHelp()
		{
			Console.WriteLine("usage: bin2tap source.bin destination.tap [options] [@responsefile]");
			Console.WriteLine();

			Console.WriteLine("Options:");
			Console.WriteLine("  --name:N               File name (max 6 chars), default = input file name");
			Console.WriteLine("  --type:[M|B]           File type - M=machine code, B=Basic, default=M");              
			Console.WriteLine("  --loadaddr:N           Load address, default=0x0400");
			Console.WriteLine("  --startaddr:N          Start address (entry point), default=0x0400");
			Console.WriteLine("  --baud:300|1200        How to set the speed indicator in the header");
			Console.WriteLine("  --autostart:Y|N        Auto start or not, default=Yes for machine files, No for Basic");
			Console.WriteLine("  --help                 Show these help instruction");
			Console.WriteLine("  --v                    Show version information");

		}

		public int Run(string[] args)
		{
			// Process command line
			if (!ProcessArgs(args))
				return 0;

			// Check input file specified
			if (_inputFile == null)
			{
				ShowLogo();
				ShowHelp();

				Console.WriteLine("\nNo input file specified");
				return 7;
			}

			// Work out output file name
			if (_outputFile == null)
			{
				_outputFile = System.IO.Path.ChangeExtension(_inputFile, "tap");
			}

			// Work out the file name
			if (_name == null)
			{
				// Use input file name
				_name = System.IO.Path.GetFileNameWithoutExtension(_inputFile).ToUpperInvariant();

				// Truncate
				if (_name.Length > 6)
					_name = _name.Substring(0,6);
			}

			// Space pad the name
			while (_name.Length<6)
				_name+=" ";

			// Convert name to ASCII
			byte[] _nameBytes = Encoding.ASCII.GetBytes(_name);
			if (_nameBytes.Length != 6 || Encoding.ASCII.GetString(_nameBytes) != _name)
			{
				throw new InvalidOperationException(string.Format("Can't handle name '{0}', doesn't convert to ASCII", _name));
			}

			// Load input file
			byte[] binary = System.IO.File.ReadAllBytes(_inputFile);
			if (binary.Length > 0xFFFF)
				throw new InvalidOperationException(string.Format("File too big - {0} bytes", binary.Length));

			// Create the output file
			Stream output = System.IO.File.Create(_outputFile);

			// Write the DGOS header
			output.WriteAscii("TAP_DGOS_MBEE");

			// Write leading zeros
			var leadingZeros = new byte[64];
			output.Write(leadingZeros, 0, leadingZeros.Length);

			// Write the leading 1 mark
			output.WriteByte(1);

			var buf = new MemoryStream();


			// Write the filename
			buf.Write(_nameBytes, 0, 6);
			buf.WriteByte(_type);
			buf.WriteWord((ushort)binary.Length);
			buf.WriteWord((ushort)_loadAddr);
			buf.WriteWord((ushort)_startAddr);

			// Write the speed setting
			if (_baud == 300)
				buf.WriteByte(0);
			else if (_baud == 1200)
				buf.WriteByte(0xFF);
			else
				buf.WriteByte((byte)_baud);

			// Write the auto start setting
			if (_autoStart.HasValue)
				buf.WriteByte((byte)(_autoStart.Value ? 0xFF : 0));
			else
				buf.WriteByte((byte)(_type == (byte)'M' ? 0xFF : 0));
			
			// Write the unused byte
			buf.WriteByte(0);
			
			// Calculate check sum
			WriteCheckSummedBlock(output, buf);

			// Now write out the data blocks
			var pos = 0;
			while (pos < binary.Length)
			{
				int bytes = Math.Min(binary.Length - pos, 256);

				buf.Seek(0, SeekOrigin.Begin);
				buf.Write(binary, pos, bytes);

				WriteCheckSummedBlock(output, buf);

				pos += bytes;
			}

			output.Close();

			return 0;
		}

		public void WriteCheckSummedBlock(Stream output, MemoryStream buf)
		{
			var pos = buf.Position;

			var data = buf.GetBuffer();
			var total = pos;
			for (int i=0; i<pos; i++)
			{
				total += data[i];
			}

			byte checksum = (byte)((-total) & 0xFF);

			output.Write(data, 0, (int)pos);
			output.WriteByte(checksum);

			if (((total + checksum) & 0xFF) != 0)
				throw new InvalidDataException("Checksum algo wrong");
		}
			
			
		static int Main(string[] args)
		{
			try
			{
				return new Program().Run(args);
			}
			catch (InvalidOperationException x)
			{
				Console.WriteLine("{0}", x.Message);
				return 7;
			}
			catch (IOException x)
			{
				Console.WriteLine("File Error - {0}", x.Message);
				return 7;
			}
		}
	}
}
