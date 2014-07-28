// Program.cs
//
// Copyright 2014 Wave Software Limited.
//

using System;
using System.Diagnostics;
using System.IO;

class Program
{
	static void Main(string[] args)
	{
		Stopwatch clock = new Stopwatch();

		clock.Start();

		foreach (var pathname in args)
		{
			if (File.Exists(pathname))
			{
				Fixer fixer = new Fixer(pathname);

				fixer.Fix();
			}
		}

		clock.Stop();

		TimeSpan delta = clock.Elapsed;

		Console.WriteLine(
			"Total processing time: {0:00}:{1:00}:{2:00}.{3:00}",
			delta.Hours,
			delta.Minutes,
			delta.Seconds,
			delta.Milliseconds / 10);
	}
}
