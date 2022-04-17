using System;
using System.Net;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	/// <summary>
	/// Notes:
	/// search across the keys can be relatively high
	/// volatile keys = automatic deletion of the key after timeout expires
	///
	/// commands:
	/// KEYS *     -> fetch all the keys (BLOCKED ON AZURE), the alternative for speeding up is command SCAN, that is O(1) for every command, always
	/// SCAN
	/// 
	/// SET "KEY" "VALUE"
	/// PERSIST   -> if stated before set command make it key persistent (won't be deleted) EX:  SET "somekey" "somevalue" EX 10    =>   PERSIST "somekey"   => won't be deleted until server is running
	/// SET "somekey" "somevalue" EX 10    - expires (will be delted) in 10 seconds 
	///
	/// GET "somekey"     - fetch info with this key
	/// 
	/// </summary>

	[TestClass]
	public class Demo_03_03
	{
		private static TestContext _context = null;

		[ClassInitialize]
		public static void ClassInitialize(TestContext ctx)
		{
			_context = ctx;
		}

		[TestInitialize]
		public void SetupTest()
		{
			Console.WriteLine("TestContext.TestName='{0}'  static _testContext.TestName='{1}'", _context.TestName, _context.TestName);
		}

		[TestMethod]
		public void Test_Demo_03_03_Connect()
		{
			var redisConnectionString = _context.Properties["azureRedisConnectionString"] as string;

			//Check.That(redisConnectionString).IsNotEmpty();

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
			var cm = ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false");
			
			//action
			var redis = cm.GetDatabase();

			//assert
			Assert.IsNotNull(redis);
		}

		[TestMethod]
		public void Set_Test()
		{
			//arrange
			var cm = ConnectionMultiplexer.Connect("127.0.0.1:6379,abortConnect=false");
			var redis = cm.GetDatabase();

			var key = "testKey";
			var value = "testValue";

			//action
			var succeed = redis.StringSet(key, value, TimeSpan.FromSeconds(3));

			Assert.IsTrue(succeed);

			var result = redis.StringGet(key);

			Assert.AreEqual(result.ToString(), value);

			Thread.Sleep(TimeSpan.FromSeconds(5));

			var resultAfterWaiting = redis.StringGet(key);
			Check.That(resultAfterWaiting.HasValue).IsFalse();
		}
	}
}
