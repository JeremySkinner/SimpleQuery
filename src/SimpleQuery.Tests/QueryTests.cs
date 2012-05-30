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
using System.Linq;
using NUnit.Framework;
using Should;

namespace SimpleQuery.Tests {
	[TestFixture]
	public class QueryTests : BaseTest {
		private string connString = "Data Source='Test.sdf'";

		[SetUp]
		public void Setup() {
			using (var db = Connection.Open("Test")) {
				db.Execute("delete from Users");
			}
		}

		[Test]
		public void Gets_data_strongly_typed() {
			using (var db = Connection.Open("Test")) {
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
		public void Gets_property_with_remapped_name() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var results = db.Query<User6>("select * from Users").ToList();
				results.Count.ShouldEqual(1);
				results[0].OtherName.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Gets_property_case_insensitively() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var results = db.Query<User7>("select * from Users").ToList();
				results.Count.ShouldEqual(1);
				results[0].name.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Does_not_throw_when_object_does_not_have_property() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var results = db.Query<User>("select Name as Foo from Users").ToList();
				results.Single().Name.ShouldBeNull();
			}
		}

		[Test]
		public void Does_not_throw_when_property_not_settable() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				db.Query<User2>("select Name from Users").ToList();
			}
		}

		[Test]
		public void Throws_when_property_of_wrong_type() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var ex = Assert.Throws<MappingException>(() => db.Query<User3>("select Name from Users").ToList());
				ex.Message.ShouldEqual("Could not map the property 'Name' as its data type does not match the database.");
			}
		}

		[Test]
		public void Throws_when_cannot_instantiate_object() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var ex = Assert.Throws<MappingException>(() => db.Query<User4>("select * from Users").ToList());
				ex.Message.ShouldEqual(
					"Could not find a parameterless constructor on the type 'SimpleQuery.Tests.QueryTests+User4'. SimpleQuery can only be used to map types that have a public, parameterless constructor.");
			}
		}

		[Test]
		public void Does_not_map_properties_with_NotMapped_attribute() {
			using (var db = Connection.Open("Test"))
			{
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var result = db.Query<User5>("select * from Users").Single();
				(result.Id > 0).ShouldBeTrue();
				result.Name.ShouldBeNull();
			}
		}

		[Test]
		public void FindAll_gets_all() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");
				db.Execute("insert into Users (Name) values ('Jeremy')");

				var result = db.FindAll<User>();
				result.Count().ShouldEqual(2);
			}
		}

		[Test]
		public void FindById_finds_by_id() {
			using (var db = Connection.Open("Test")) {
				db.Execute("insert into Users (Name) values ('Jeremy')");
				decimal id = db.GetLastInsertId();
				var result = db.FindById<User>(id);
				result.Name.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Loads_by_composite_key() {
			using (var db = Connection.Open("Test")) {
				db.Insert(new CompositeKeyUser {Id = 1, Id2 = "foo", Name = "Jeremy"});
				var result = db.FindById<CompositeKeyUser>(new {Id = 1, Id2 = "foo"});
				result.Id.ShouldEqual(1);
				result.Id2.ShouldEqual("foo");
				result.Name.ShouldEqual("Jeremy");
			}
		}

		[Test]
		public void Correctly_handles_null()
		{
			using (var db = Connection.Open("Test"))
			{
				db.Log = Console.Out;
				db.Execute("insert into Users (Name) values(null)");
				decimal id = db.GetLastInsertId();
				db.Query<User>("select * from users").Single().Name.ShouldBeNull();
				db.FindById<User>(id).Name.ShouldBeNull();
			}
		}

		[Test]
		public void Gets_scalar_value()
		{
			using(var db = Connection.Open("Test"))
			{
				db.Execute("insert into Users (Name) values(null)");
				var result = db.Scalar<int>("select count(*) from Users");
				result.ShouldEqual(1);
			}
		}

		[Test]
		public void Gets_scalar_value_null()
		{
			using(var db = Connection.Open("Test"))
			{
				var result = db.Scalar<int>("select null as foo");
				result.ShouldEqual(0);
			}
		}

		public class CompositeKeyUser {
			[Key]
			public int Id { get; set; }

			[Key]
			public string Id2 { get; set; }

			public string Name { get; set; }
		}

		[Table("Users")]
		public class User {
			[Key]
			public int Id { get; set; }

			public string Name { get; set; }
		}

		[Table("Users")]
		public class User2 {
			public string Name {
				get { return null; }
			}
		}

		[Table("Users")]
		public class User3 {
			public int Name { get; set; }
		}

		[Table("Users")]
		public class User4 : User {
			public User4(int x) {
			}
		}

		[Table("Users")]
		public class User5 {
			public int Id { get; set; }

			[NotMapped]
			public string Name { get; set; }
		}

		[Table("Users")]
		public class User6 {
			public int Id { get; set; }

			[Column("Name")]
			public string OtherName { get; set; }
		}

		[Table("Users")]
		public class User7 {
			public int Id { get; set; }
			public string name { get; set; }
		}
	}
}