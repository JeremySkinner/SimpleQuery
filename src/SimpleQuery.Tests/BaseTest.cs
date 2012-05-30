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

				cmd.CommandText = "create table CompositeKeyUser (Id int not null, Id2 nvarchar(250) not null, Name nvarchar(250), primary key (Id, Id2)) ";
				cmd.ExecuteNonQuery();
			}
		}
	}
}