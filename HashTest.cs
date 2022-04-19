using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	[TestClass]
	public class HashTest
	{
		/*
		 HSET - add to set
		HMSET - multiply add
		HDEL  - del from set
		HIncr - incrementing by double


		HGET key 
		OR  we can get keys directly from hash like: HGET SMB:{yuryi}:UserInfo email    => returns "ust@gmail.com"
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

			_db.StringSet("next_user_id", 0);
			_db.StringSet("next_post_id", 0);
		}

		/// <summary>
		/// https://stackoverflow.com/questions/58544228/updating-particular-key-values-in-a-hash-table-redis-c-sharp-net-core-2-2
		/// </summary>
		[TestMethod]
		public void HashTest1()
		{
			var nextUserId = _db.StringIncrement("next_user_id");

			var keyUserList = "SMB:Users";
			_db.HashSet(keyUserList, new HashEntry[]
			{
				new HashEntry("yuryi", nextUserId), 
			});

			var keyInfo = "SMB:{yuryi}:UserInfo";

			_db.HashSet(keyInfo, new HashEntry[]
			{
				new HashEntry("email", "ust@gmail.com"), 
				new HashEntry("phone", "+971...."), 
				new HashEntry("id", nextUserId), 
				new HashEntry("etc.", "etc."), 
			});

			var result = _db.HashGet(keyInfo, new RedisValue[] { "email" });

			Check.That((string) result[0]).IsEqualTo("ust@gmail.com");
		}
	}
}
