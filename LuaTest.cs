using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NFluent;
using StackExchange.Redis;

namespace RedisTests
{
	[TestClass]
	public class LuaTest
	{
		/*
			guarantees that a script is executed is an atomic way: no other script on Redis command will be executed while a script is being executed.

		EVAL "local name='name' return name" 0
		EVAL 'local val=ARGV[1] return val' 1 kName "anton"


		USEFUL when we need load and cache script as a procedures
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

		[TestMethod]
		public void LuaTest1()
		{
			var luaScript = @"
							redis.call('SET', KEYS[1], ARGV[1])
							redis.call('SET', KEYS[2], ARGV[2])
							local firstname = redis.call('GET', KEYS[1])
							local lastname = redis.call('GET', KEYS[2])
							return firstname..' '..lastname";

			var result = _db.ScriptEvaluate(luaScript, new RedisKey[] { "KeyFN", "KeyLN" }, new RedisValue[] {"yuryi", "kringe"});

			Check.That(result.ToString()).IsEqualTo("yuryi kringe");
		}

		[TestMethod]
		public void LuaTest2()
		{
			var luaScript = @"
							local firstname = @FirstName
							local lastname = @LastName
							return firstname..' '..lastname";

			var ls = LuaScript.Prepare(luaScript);

			var result = _db.ScriptEvaluate(ls, new { FirstName = "yuryi", LastName = "kringe" });

			Check.That(result.ToString()).IsEqualTo("yuryi kringe");
		}
	}
}
