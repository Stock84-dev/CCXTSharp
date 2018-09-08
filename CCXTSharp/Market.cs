using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CCXTSharp
{
	public class Market
	{
		public string id { get; set; }
		public string symbol { get; set; }
		[JsonProperty("base")]
		public string Base { get; set; }
		public string quote { get; set; }
		public string baseId { get; set; }
		public string quoteId { get; set; }
		public bool? active { get; set; }
		public Precision precision { get; set; }
		public Limits limits { get; set; }

		public bool? fee_loaded { get; set; }
		public Tiers tiers { get; set; }
		public float? taker { get; set; }
		public bool? tierBased { get; set; }
		public bool? percentage { get; set; }
		public float? maker { get; set; }

		[JsonExtensionData]
		public Dictionary<string, object> info { get; set; }

		public class Precision
		{
			[JsonProperty("base")]
			public float? Base { get; set; }
			public float? quote { get; set; }
			public float? price { get; set; }
			public float? amount { get; set; }
		}

		public class Tiers
		{
			public float?[][] taker { get; set; }
			public float?[][] maker { get; set; }
		}

		public class Limits
		{
			public Amount amount { get; set; }
			public Cost cost { get; set; }
			public Price price { get; set; }
		}

		public class Amount
		{
			public float? max { get; set; }
			public float? min { get; set; }
		}

		public class Cost
		{
			public float? max { get; set; }
			public float? min { get; set; }
		}

		public class Price
		{
			public float? max { get; set; }
			public float? min { get; set; }
		}
	}
}
