using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	[TestClass]
	public class SetsTest
	{
		/*
		 SETS:

		SADD - add to set
		SREM\SPOP - remove and pop from set
		SISMEMBER - is this item a member of set
		SMEMBERS - returns set

		SINTER - intersection of 2 sets (for example of waterfalls that are related two 2 countries: "Waterfall:{Canada}" and "Waterfall:{USA}" => "Niagara falls")
		
		 
		 ALSO THERE ARE SORTED SETS
		ZADD - add el-t to a sorted set
		ZREM ZPOP ZCARD ZCOUNT ZRANGE ZRANGEBYSCORE ZSCAN
		 
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
		public void SetTest()
		{
			//arrange
			var keyYuryiFollowing = "SMB:{yuryi}:following";
			var keyYuryiFollowers = "SMB:{yuryi}:followers";

			_db.SetAdd(keyYuryiFollowing, new RedisValue[] { "userA", "userB" });

			_db.SetAdd("SMB:{userA}:followers", "yuryi");
			_db.SetAdd("SMB:{userB}:followers", "yuryi");

			_db.SetAdd(keyYuryiFollowers, new RedisValue[] { "userC", "userD" });

			_db.SetAdd("SMB:{userC}:following", "yuryi");
			_db.SetAdd("SMB:{userD}:following", "yuryi");
		}

		[TestMethod]
		public void OrderedSetTest()
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

			//action
			_db.SortedSetCombineAndStore(SetOperation.Union, "homepage", key, userXPostsKey);

			//assert
			var homepagePostsCount = _db.SortedSetLength("homepage");
			Check.That(homepagePostsCount).IsEqualTo(4);

			var homepagePosts = _db.SortedSetRangeByRank("homepage", 0, 1);

		}
	}
}
