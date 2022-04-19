using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	[TestClass]
	public class MultiOperationsTests
	{
		/*
		 Pipelinig:
		- multiple mesages sent, deferring processing of replies (implemented by StackExchange ConnectionMultiplexer)

		Fire and forget:
		means that we say to server to do smth but don't want when it's finished (kinda async op-on with await)
		- opt in with StackExchange CommanFlags

		Batch:
		-StackExchange CreateBatch
		- NOT A TRANSACTION (just a set of commands)
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
		public void MultiTest1()
		{
			var key = "MultiKey1";

			_db.HashSet(key, new HashEntry[]
			{
				new HashEntry("foo1", "bar1"), 
				new HashEntry("foo2", "bar2"), 
				new HashEntry("foo3", "bar3"), 
			}, CommandFlags.FireAndForget); //it changes pattern of the network traffic, there is no delay, gives dramatic difference
		}

		[TestMethod]
		public void BatchTest()
		{
			var filename = $"{System.AppContext.BaseDirectory}..\\..\\..\\TestData\\WaterfallsAt.csv";
			var absPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
			Check.That(File.Exists(filename)).IsTrue();

			var batch = _db.CreateBatch();
			
			foreach (var readLine in File.ReadAllLines(filename))
			{
				Debug.WriteLine(readLine);

				if (readLine[0] == 'L')
				{
					continue;
				}

				var cols = readLine.Split(',');

				Check.That(cols.Length).IsGreaterThan(0);

				var countryKey = cols[7].Trim('\"');
				double longitude = double.Parse(cols[1]);
				double latitude = double.Parse(cols[0]);
				var locationName = cols[14].Trim('\"');

				batch.GeoAddAsync(countryKey, longitude, latitude, locationName); //there is no sync methods in batch. We're accumulating all the data on client and sent the batch to server
			}

			batch.Execute();
		}


		/// <summary>
		/// Redis transaction guarantees that the command are executed as a single isolated operation
		///
		/// Syntax: MULTI <some_command> EXEC
		///
		/// 
		/// </summary>
		[TestMethod]
		public void TransactionsTest()
		{
			var stringKey = "keyTest1_1";

			_db.StringSet(stringKey, 5, when: When.NotExists);
			_db.StringIncrement(stringKey, 5);

			_db.StringSet("keyA", 1);
			_db.StringSet("keyB", 1);

			var tx = _db.CreateTransaction(); //analog of MULTI

			tx.StringIncrementAsync("keyA"); //confirmation that the command is queued, but not executed
			tx.StringIncrementAsync("keyB");

			tx.Execute(); //EXEC
		}

		[TestMethod]
		public void TransactionHomePageTest()
		{
			//arrange
			var today = DateTime.UtcNow.Ticks;
			var yesterday = DateTime.UtcNow.AddDays(-1).Ticks;

			var key = "SMB:{yuryi}:Posts";

			_db.SortedSetAdd(key, new SortedSetEntry[]
			{
				new SortedSetEntry("Hello world!",  yesterday),
				new SortedSetEntry("Welcome to SMB service!", today),
			});

			var userXPostsKey = "SMB:{userX}:Posts";
			_db.SortedSetAdd(userXPostsKey, new SortedSetEntry[]
			{
				new SortedSetEntry("userX says Hello world!", yesterday + 1),
				new SortedSetEntry("userX says Welcome to SMB service!", today + 1),
			});

			var homePagePosts = _db.SortedSetRangeByRank("homepage", 0, -1);
			
			if (homePagePosts.Length == 0)
			{
				var txHomePage = _db.CreateTransaction(); //both operations below will be excutes on the server as a transaction
				txHomePage.AddCondition(Condition.KeyNotExists("homepage"));

				txHomePage.SortedSetCombineAndStoreAsync(SetOperation.Union, "homepage", key, userXPostsKey);
				txHomePage.SortedSetRemoveRangeByRankAsync("homepage", 0, -3); // we get only latest 3 posts

				txHomePage.Execute();
			}

			homePagePosts = _db.SortedSetRangeByRank("homepage", 0, -1);
			Check.That(homePagePosts.Length).IsGreaterThan(0);
		}
	}
}
