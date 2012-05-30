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

using System.Configuration;
using System.Data.SqlServerCe;
using System.IO;
using NUnit.Framework;

namespace SimpleQuery.Tests {
	public abstract class BaseTest {
		[TestFixtureSetUp]
		public void TestFixtureSetup() {
			// Initialize the database.

			if (File.Exists("Test.sdf")) {
				File.Delete("Test.sdf");
			}

			using (var engine = new SqlCeEngine(ConfigurationManager.ConnectionStrings["Test"].ConnectionString)) {
				engine.CreateDatabase();
			}

			using (var conn = new SqlCeConnection(ConfigurationManager.ConnectionStrings["Test"].ConnectionString)) {
				var cmd = conn.CreateCommand();
				conn.Open();

				cmd.CommandText = "create table Users (Id int identity, Name nvarchar(250))";
				cmd.ExecuteNonQuery();

				cmd.CommandText = "create table ManualIdUser (Id int, Name nvarchar(250))";
				cmd.ExecuteNonQuery();

				cmd.CommandText =
					"create table CompositeKeyUser (Id int not null, Id2 nvarchar(250) not null, Name nvarchar(250), primary key (Id, Id2)) ";
				cmd.ExecuteNonQuery();
			}
		}
	}
}