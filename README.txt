A lightweight wrapper around ADO.NET that allows you to map SQL statements to objects. 

This is based around WebMatrix.Data.StronglyTyped 
(https://github.com/JeremySkinner/WebMatrix.Data.StronglyTyped)
but without the dependency on WebMatrix.Data

Note that this requires NuGet Package Restore in order to use. 

Examples:

[Table("Users")]
public class User {
  public int Id { get; set; }
  public string Name { get; set; }
}

// Map query into a list of User objects.
using(var db = Connection.Open("ConnectionStringName")) {
  var results = db.Query<User>("select * from Users").ToList();
}

//Find record by PK
using (var db = Connection.Open("ConnectionStringName")) {
  //Assumes PK property is called ID, but this can be overriden
	User result = db.FindById<User>(100); 
}

//Insert record
using(var db = Connection.Open("ConnectionStringName")) {
  db.Insert(new User { Name = "Foo" });
}

// Update record
using (var db = Connection.Open("ConnectionStringName")) {
    var user = new User { Name = "Foo" };
    db.Insert(user);
    
    user.Name = "Bar";
    db.Update(user);
}
