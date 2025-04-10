using Foundation;
using UIKit;
using System;
using System.Threading.Tasks;
using HairCarePlus.Client.Patient.Common.Behaviors;
using HairCarePlus.Client.Patient.Features.Calendar.Services;
using HairCarePlus.Client.Patient.Features.Calendar.Services.Interfaces;

namespace HairCarePlus.Client.Patient;

[Register("AppDelegate")]
public class AppDelegate : MauiUIApplicationDelegate
{
	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();

	[Export("application:performFetchWithCompletionHandler:")]
	public override void PerformFetch(UIApplication application, Action<UIBackgroundFetchResult> completionHandler)
	{
		// Perform the fetch operation
		try
		{
			// Schedule background task
			Task.Run(async () => 
			{
				try
				{
					// Get services from the ServiceHelper
					var calendarService = ServiceHelper.GetService<ICalendarService>();
					if (calendarService != null)
					{
						// Example: Synchronize calendar data
						var today = DateTime.Today;
						await calendarService.GetEventsForMonthAsync(today.Year, today.Month);
						
						// Report success
						completionHandler(UIBackgroundFetchResult.NewData);
					}
					else
					{
						// No service available
						completionHandler(UIBackgroundFetchResult.NoData);
					}
				}
				catch (Exception)
				{
					completionHandler(UIBackgroundFetchResult.Failed);
				}
			});
		}
		catch (Exception)
		{
			completionHandler(UIBackgroundFetchResult.Failed);
		}
	}
}
