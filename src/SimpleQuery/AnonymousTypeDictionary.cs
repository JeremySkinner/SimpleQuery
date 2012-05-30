using System.Collections.Generic;
using System.ComponentModel;

namespace SimpleQuery {
	internal class AnonymousTypeDictionary : Dictionary<string,object> {
		public AnonymousTypeDictionary() {
			
		}

		public AnonymousTypeDictionary(object obj) {
			if(obj != null) {
				foreach(PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj)) {
					Add(descriptor.Name, descriptor.GetValue(obj));
				}
			}
		}

	}
}