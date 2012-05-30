namespace SimpleQuery.Tests {
	using System;
	using System.Data.SqlServerCe;
	using System.IO;
	using NUnit.Framework;
	using System.Linq;
	using Should;

	[TestFixture]
	public class ExecuteTests : BaseTest {

		[SetUp]
		public void Setup() {
			using (var db = Connection.Open("Test")) {
				db.Log = Console.Out;
				db.Execute("delete from Users");
			}
		}

		[Test]
		public void Inserts_record() {
			using(var db = Connection.Open("Test")) {
				db.Log = Console.Out;

				db.Insert(new User { Name = "Foo" });

				var result = db.Query<User>("select * from users").Single();
				result.Id.ShouldEqual(1);
				result.Name.ShouldEqual("Foo");
			}
		}
		[Test]
		public void Inserts_record_with_aliased_column() {
			using(var db = Connection.Open("Test")) {
				db.Log = Console.Out;

				db.Insert(new UserWithAliasedProperty { OtherName = "FooAliased" });

				var result = db.Query<User>("select * from users").Single();
				result.Name.ShouldEqual("FooAliased");
			}
		}

		[Test]
		public void Updates_record() {
			using (var db = Connection.Open("Test")) {
				db.Log = Console.Out;

				var user = new User { Name = "Foo" };
				db.Insert(user);
				user.Name = "Bar";
				db.Update(user);

				var result = db.Query<User>("select * from users").Single();
				result.Name.ShouldEqual("Bar");
			}
		}


		[Test]
		public void Inserts_with_store_generated_id() {
			using(var db = Connection.Open("Test")) {
				db.Log = Console.Out;

				var user = new User{ Name = "Foo" };
				db.Insert(user);
				
				user.Id.ShouldNotEqual(0);
			}
		}

		[Test]
		public void Inserts_with_store_generated_id_when_id_is_implicit() {
			using (var db = Connection.Open("Test")) {
				db.Log = Console.Out;

				var user = new UserWithImplicitId { Name = "Foo" };
				db.Insert(user);

				user.Id.ShouldNotEqual(0);
			}
		}

		[Test]
		public void Saves_with_non_generated_key() {
			using (var db = Connection.Open("Test")) {
				db.Log = Console.Out;
				var user = new ManualIdUser {Id = 5, Name = "foo"};
				db.Insert(user);
				user.Id.ShouldEqual(5);

				var result = db.FindById<ManualIdUser>(5);
				result.Id.ShouldEqual(5);
			}
		}

		public class ManualIdUser {
			[Key(Generated = false)]
			public int Id { get; set; }
			public string Name { get; set; }
		}


		[Table("Users")]
		public class User {
			[Key]
			public int Id { get; set; }
			public string Name { get; set; }
		}

		[Table("Users")]
		public class UserWithImplicitId {
			public int Id { get; set; }
			public string Name { get; set; }
		}

		[Table("Users")]
		public class UserWithAliasedProperty {
			public int Id { get; set; }
			[Column("Name")]
			public string OtherName { get; set; }
		}
	}

}