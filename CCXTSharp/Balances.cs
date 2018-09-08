using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCXTSharp
{
	public class Balances
	{
		public Dictionary<string, Balance> balances { get; set; } = new Dictionary<string, Balance>();
		public Dictionary<string, float?> free { get; set; } = new Dictionary<string, float?>();
		public Dictionary<string, float?> used { get; set; } = new Dictionary<string, float?>();
		public Dictionary<string, float?> total { get; set; } = new Dictionary<string, float?>();
		public Dictionary<string, object> info { get; set; } = new Dictionary<string, object>();

		public Balances(string json)
		{
			JObject jObject = JObject.Parse(json);

			foreach (var obj in jObject)
			{
				if (obj.Key == "info")
				{
					info = obj.Value.ToObject<Dictionary<string, object>>();
				}
				else if (obj.Key == "free")
				{
					foreach (var symbol in obj.Value.Children())
					{
						JProperty jProperty = symbol.ToObject<JProperty>();
						free.Add(jProperty.Name, jProperty.Value.ToObject<float?>());
					}
				}
				else if (obj.Key == "used")
				{
					foreach (var symbol in obj.Value.Children())
					{
						JProperty jProperty = symbol.ToObject<JProperty>();
						used.Add(jProperty.Name, jProperty.Value.ToObject<float?>());
					}
				}
				else if (obj.Key == "total")
				{
					foreach (var symbol in obj.Value.Children())
					{
						JProperty jProperty = symbol.ToObject<JProperty>();
						total.Add(jProperty.Name, jProperty.Value.ToObject<float?>());
					}
				}
				else
				{
					balances.Add(obj.Key, obj.Value.ToObject<Balance>());
				}
			}
		}

		public class Balance
		{
			public float? free { get; set; }
			public float? used { get; set; }
			public float? total { get; set; }
		}
	}
}
