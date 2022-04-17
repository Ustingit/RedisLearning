using System.Net;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	/// <summary>
	/// Notes:
	/// serach across the keys can be relatively high
	///
	/// commands:
	/// KEYS
	/// </summary>

	[TestClass]
	public class Demo_03_03
	{
		private static TestContext _context = null;

		[ClassInitialize]
		public static void Class_Init(TestContext ctx)
		{
			_context = ctx;
		}

		[TestMethod]
		public void Test_Demo_03_03_Connect()
		{
			string _redisConnectionString = _context.Properties["azureRedisConnectionString"] as string;

			Check.That(_redisConnectionString).IsNotEmpty();

			var ipAddressOfServer = Dns.GetHostAddresses("learnredis.azure.ustin.com");
			var port = 6380;

			var cfg = new ConfigurationOptions()
			{
				AllowAdmin = true,
				AbortOnConnectFail = false, // is not compatible with azure (like a preliminary test of connection)
				ClientName = nameof(Test_Demo_03_03_Connect), //useful for debugging info
				Ssl = true,
				Password = "", //keyy from vault, azure or...
				EndPoints = { new IPEndPoint(ipAddressOfServer[0], port) }
			};

			//arrange
			var cm = ConnectionMultiplexer.Connect("127.0.0.1:50800,abortConnect=false");


			//action
			var redis = cm.GetDatabase();

			//assert
			Assert.IsNotNull(redis);
		}
	}
}
