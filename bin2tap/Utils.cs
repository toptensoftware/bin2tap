﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace bin2tap
{
	public static class Utils
	{
		public static ushort ParseUShort(string str)
		{
			try
			{
				if (str.StartsWith("0x", StringComparison.InvariantCultureIgnoreCase))
				{
					return Convert.ToUInt16(str.Substring(2), 16);
				}
				else
				{
					return ushort.Parse(str);
				}
			}
			catch (Exception)
			{
				throw new InvalidOperationException(string.Format("Invalid number: '{0}'", str));
			}
		}

		public static int[] ParseIntegers(string str, int Count)
		{
			var values = new List<int>();
			if (str != null)
			{
				foreach (var n in str.Split(','))
				{
					values.Add(int.Parse(n));
				}
			}

			if (Count != 0 && Count != values.Count)
			{
				throw new InvalidOperationException(string.Format("Invalid value - expected {0} comma separated values", Count));
			}


			return values.ToArray();
		}


		public static List<string> ParseCommandLine(string args)
		{
			var newargs = new List<string>();

			var temp = new StringBuilder();

			int i = 0;
			while (i < args.Length)
			{
				if (char.IsWhiteSpace(args[i]))
				{
					i++;
					continue;
				}

				bool bInQuotes = false;
				temp.Length = 0;
				while (i < args.Length && (!char.IsWhiteSpace(args[i]) || bInQuotes))
				{
					if (args[i] == '\"')
					{
						if (args[i + 1] == '\"')
						{
							temp.Append("\"");
							i++;
						}
						else
						{
							bInQuotes = !bInQuotes;
						}
					}
					else
					{
						temp.Append(args[i]);
					}

					i++;
				}

				if (temp.Length > 0)
				{
					newargs.Add(temp.ToString());
				}
			}

			return newargs;
		}

		public static void WriteAscii(this Stream stm, string str)
		{
			var bytes = Encoding.ASCII.GetBytes(str);
			stm.Write(bytes, 0, bytes.Length);
		}

		public static void WriteWord(this Stream stm, ushort w)
		{
			stm.WriteByte((byte)(w & 0xFF));
			stm.WriteByte((byte)((w >> 8) & 0xFF));
		}

	}
}
