using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using TwitchMemeAlertsAuto.Core.Properties;

namespace TwitchMemeAlertsAuto.Core
{
	public static class ResourceManagerExtensions
	{
		public static Dictionary<string, string> GetAllLocalizedStrings(this ResourceManager rm, string key)
		{
			var localizedStrings = new Dictionary<string, string>();

			// 1. Get the ResourceManager associated with your RESX file
			rm = rm ?? Resources.ResourceManager;

			// 2. Retrieve all cultures installed/available on the operating system
			CultureInfo[] cultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
			foreach (CultureInfo culture in cultures)
			{
				try
				{
					// 3. Request the string specifically for this culture.
					// Setting the third parameter (createIfNotExists) to true tells it to load the assembly if needed.
					ResourceSet resourceSet = rm.GetResourceSet(culture, true, false);
					if (resourceSet != null)
					{
						// Retrieve the value directly for the culture
						string value = rm.GetString(key, culture);

						// Ensure the value exists and avoid duplicates caused by culture fallbacks
						// (e.g., 'en-GB' falling back to 'en' or invariant culture)
						if (!string.IsNullOrEmpty(value) && !localizedStrings.ContainsValue(value))
						{
							localizedStrings[culture.Name == "" ? "Invariant" : culture.Name] = value;
						}
					}
				}
				catch (CultureNotFoundException)
				{
					// Skip cultures that are not valid or supported
				}
			}

			return localizedStrings;
		}
	}
}