using System;
using System.Configuration;

namespace Common.Config
{
	/// <summary>
	/// Summary description for SystemParameters.
	/// </summary>
	public class SystemParameters
	{
		public SystemParameters()
		{
			//
			// TODO: Add constructor logic here
			//
		}

		public static string GetParameter(string a_key)
		{
			string result = "";

			try
			{
				result = ConfigurationSettings.AppSettings[a_key];
			}
			catch(Exception e)
			{
				System.Console.Out.WriteLine(e.StackTrace);
				result = "";
			}

			return result;
		}
	}
}