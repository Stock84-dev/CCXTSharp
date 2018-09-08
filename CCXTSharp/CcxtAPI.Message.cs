using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CCXTSharp
{
	partial class CcxtAPI
	{
		private class Message
		{
			public int callNumber { get; set; }
			public int handlerType { get; set; }
			public string exchange { get; set; }
			public Variable variable { get; set; }
			public Method method { get; set; }

			public Message(string name, string parentName, int callNumber, object setVariableData = null, bool isMethod = true, params object[] methodParameters)
			{
				// creating message for method
				if (isMethod)
				{
					handlerType = 1;
					method = new Method();
					method.name = name;
					PropertyInfo[] properties = method.GetType().GetProperties();
					for (int i = 0; i < methodParameters.Length; i++)
					{
						properties[i + 1].SetValue(method, methodParameters[i]);
					}
				}
				else // creating message for variable
				{
					handlerType = 0;
					variable = new Variable();
					variable.name = name;
					if (setVariableData != null)
					{
						variable.type = 1;
						variable.data = setVariableData;
					}
					else
						variable.type = 0;
				}

				this.exchange = parentName;
				this.callNumber = callNumber;
			}

			public class Variable
			{
				public string name { get; set; }
				public int type { get; set; }
				public object data { get; set; } = null;
			}

			public class Method
			{
				//NOTE: order of properties is important
				public string name { get; set; }
				public object param1 { get; set; } = null;
				public object param2 { get; set; } = null;
				public object param3 { get; set; } = null;
				public object param4 { get; set; } = null;
				public object param5 { get; set; } = null;
				public object param6 { get; set; } = null;
				public object param7 { get; set; } = null;
				public object param8 { get; set; } = null;
				public object param9 { get; set; } = null;
			}
		}
	}
}
