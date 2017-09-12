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
	public class DbConnectionExtensionsExecuteTests : BaseTest {

		private IDbConnection GetConnection() {
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
		public void Inserts_record() {
			using (var db = GetConnection()) {
				db.Execute("insert into Users (Name) values(@Name)", new User { Name = "Foo" });
				var result = db.Query<User>("select * from users").Single();
				result.Id.ShouldEqual(1);
				result.Name.ShouldEqual("Foo");
			}
		}


		[Test]
		public void Updates_record() {
			using (var db = GetConnection()) {
				var user = new User {Name = "Foo"};
				db.Execute("insert into Users (Name) values(@Name)", user);
				int id = db.GetLastInsertId();
				user.Id = id;
				user.Name = "Bar";
				db.Execute("update Users set Name = @Name where id = @Id", user);

				var result = db.Query<User>("select * from users").Single();
				result.Name.ShouldEqual("Bar");
			}
		}

		public class User {
			public int Id { get; set; }
			public string Name { get; set; }
		}

	}
}