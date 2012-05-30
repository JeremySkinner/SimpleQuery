namespace SimpleQuery {
	using System;

	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public class KeyAttribute : Attribute {
		private bool _generated = true;

		public bool Generated {
			get { return _generated; }
			set { _generated = value; }
		}

	}
}