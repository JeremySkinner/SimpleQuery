#region License

// Copyright (c) Jeremy Skinner (http://www.jeremyskinner.co.uk)
// 
// Licensed under the Apache License, Version 2.0 (the "License"); 
// you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at 
// 
// http://www.apache.org/licenses/LICENSE-2.0 
// 
// Unless required by applicable law or agreed to in writing, software 
// distributed under the License is distributed on an "AS IS" BASIS, 
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied. 
// See the License for the specific language governing permissions and 
// limitations under the License.
// 
// The latest version of this file can be found at https://github.com/JeremySkinner/SimpleQuery

#endregion

using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlServerCe;
using System.Linq;
using NUnit.Framework;
using Should;

namespace SimpleQuery.Tests {
	[TestFixture]
	public class DbConnectionExtensionsQueryTests : BaseTest {

		private IDbConnection GetConnection()
		{
			var conn = new SqlCeConnection(ConfigurationManager.ConnectionStrings["Test"].ConnectionString);
			conn.Open();
			return conn;
		}

		[SetUp]
		public void Setup() {
			using (var db = GetConnection()) {
				db.Execute("delete from Users");
			}
		}

		[Test]
		public void Gets_data_strongly_typed() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");
				db.Execute("insert into Users (Name) values ('Bob')");

				var results = db.Query<User>("select * from Users").ToList();
				results.Count.ShouldEqual(2);

				results[0].Id.ShouldNotEqual(0);
				results[0].Name.ShouldEqual("Jeremy");

				results[1].Id.ShouldNotEqual(0);
				results[1].Name.ShouldEqual("Bob");
			}
		}

		[Test]
		public void Gets_property_case_insensitively() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var results = db.Query<User7>("select * from Users").ToList();
				results.Count.ShouldEqual(1);
				results[0].name.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Does_not_throw_when_object_does_not_have_property() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var results = db.Query<User>("select Name as Foo from Users").ToList();
				results.Single().Name.ShouldBeNull();
			}
		}

		[Test]
		public void Does_not_throw_when_property_not_settable() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				db.Query<User2>("select Name from Users").ToList();
			}
		}

		[Test]
		public void Throws_when_property_of_wrong_type() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var ex = Assert.Throws<DbConnectionExtensions.MappingException>(() => db.Query<User3>("select Name from Users").ToList());
				ex.Message.ShouldEqual("Could not map the property 'Name' as its data type does not match the database.");
			}
		}

		[Test]
		public void Throws_when_cannot_instantiate_object() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var ex = Assert.Throws<DbConnectionExtensions.MappingException>(() => db.Query<User4>("select * from Users").ToList());
				ex.Message.ShouldEqual(
					"Could not find a parameterless constructor on the type 'SimpleQuery.Tests.DbConnectionExtensionsQueryTests+User4'. SimpleQuery can only be used to map types that have a public, parameterless constructor.");
			}
		}

		[Test]
		public void FindAll_gets_all() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var result = db.Query<User>("select * from Users");
				result.Count().ShouldEqual(2);
			}
		}

		[Test]
		public void FindById_finds_by_id() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values ('Jeremy')");
				decimal id = db.GetLastInsertId();
				var result = db.QuerySingle<User>("select * from Users", new{id});
				result.Name.ShouldEqual("Jeremy");
			}
		}


		[Test]
		public void Correctly_handles_null()
		{
			using (var db = GetConnection())
			{
				db.Execute("insert into Users (Name) values(null)");
				decimal id = db.GetLastInsertId();
				db.Query<User>("select * from users").Single().Name.ShouldBeNull();
				db.QuerySingle<User>("select * from users", new{id}).Name.ShouldBeNull();
			}
		}

		[Test]
		public void Gets_scalar_value()
		{
			using(var db = GetConnection())
			{
				db.Execute("insert into Users (Name) values(null)");
				var result = db.QuerySingle<int>("select count(*) from Users");
				result.ShouldEqual(1);
			}
		}

		[Test]
		public void Gets_scalar_value_null()
		{
			using(var db = GetConnection())
			{
				var result = db.QuerySingle<int>("select null as foo");
				result.ShouldEqual(0);
			}
		}


		public class User {
			public int Id { get; set; }
			public string Name { get; set; }
		}

		public class User2 {
			public string Name {
				get { return null; }
			}
		}

		public class User3 {
			public int Name { get; set; }
		}

		public class User4 : User {
			public User4(int x) {
			}
		}


		public class User7 {
			public int Id { get; set; }
			public string name { get; set; }
		}
	}
}