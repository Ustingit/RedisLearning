using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	[TestClass]
	public class GISTest
	{
		/*
		  key => langitude, longitude, member

		GEOADD
		GEOHASH
		GEOPOS  -> geo position (returns lat and long)
		GEODIST -> calculates distancs between those 2 members
		GEORADIUS  -> return kind of are these points in these radius ? useful for uber-like logic and others. Or in other words @limit our serach by this radius of let say 5 km" and return all the members
		GEORADIUSBYMEMBER -> almost the same as previous but for concrete point, so we look for in a radius around our certain point

		 */

		private static TestContext _context = null;
		private static IDatabase _db;

		[ClassInitialize]
		public static void ClassInit(TestContext context)
		{
			_context = context;

			var cm = ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false,allowAdmin=true");

			var server = cm.GetServer("127.0.0.1:6379");
			server.FlushDatabase(); //clean-up (only with admin rights)

			_db = cm.GetDatabase();
		}

		[TestMethod]
		public void GISTest1()
		{
			var filename = $"{System.AppContext.BaseDirectory}..\\..\\..\\TestData\\WaterfallsAt.csv";
			var absPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);

			Check.That(File.Exists(filename)).IsTrue();

			uint count = 0;
			foreach (var readLine in File.ReadAllLines(filename))
			{
				Debug.WriteLine(readLine);

				if (readLine[0] == 'L')
				{
					continue;
				}

				count++;

				var cols = readLine.Split(',');

				Check.That(cols.Length).IsGreaterThan(0);

				var countryKey = cols[7].Trim('\"');
				double longitude = double.Parse(cols[1]);
				double latitude = double.Parse(cols[0]);
				var locationName = cols[14].Trim('\"');

				_db.GeoAdd(countryKey, longitude, latitude, locationName);
			}

			Check.That(count).IsGreaterThan(0);
		}
	}
}
