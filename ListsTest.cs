using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	/*
	 Commands:
	- flushdb  for cleaning db

	Lists: (based on linked list)
	lpush - add el-t on the left
	rpush - add el-t on the right
	lpop - removes from left
	rpop - removes from right

	ex: lpush myList v1 v2 v3     
	llen myList    - returns len of this list
	lrange 0 -1   - returns all the items of these list


	 */

	[TestClass]
	public class ListsTest
	{
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
		public void ListTest1()
		{
			//arrange 
			var keyUserList = "SMB:Users";
			var smbYuryiPosts = "SMB:{yuryi}:Posts"; //SMB - short message blocks, how it can look with real data to consider what it is and what it's related to

			//action
			_db.ListLeftPush(keyUserList, new RedisValue[] { "yuryi" });
			_db.ListLeftPush(smbYuryiPosts, new RedisValue[] { "Hello world", "Welcome to the short message posts" });

			var numberOfPosts = _db.ListLength(smbYuryiPosts);
			var posts = _db.ListRange(smbYuryiPosts, 0, -1);

			//assert
			Check.That(numberOfPosts).IsEqualTo(2);
			Check.That(posts.Length).IsEqualTo(2);
		}
	}
}
