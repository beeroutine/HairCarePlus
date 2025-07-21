using ObjCRuntime;
using UIKit;
using System;

namespace HairCarePlus.Client.Clinic;

public class Program
{
	// This is the main entry point of the application.
	static void Main(string[] args)
	{
		AppDomain.CurrentDomain.UnhandledException += (_, e) =>
		{
			Console.WriteLine($"UNHANDLED_EXCEPTION: {e.ExceptionObject}");
		};
		// if you want to use a different Application Delegate class from "AppDelegate"
		// you can specify it here.
		UIApplication.Main(args, null, typeof(AppDelegate));
	}
}