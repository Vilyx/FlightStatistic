using Pathfinding.Serialization.JsonFx;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OLDD
{
	class MyJsonUtil
	{
		public static string ObjectToJson(object obj)
		{
			System.Text.StringBuilder output = new System.Text.StringBuilder();
			JsonWriterSettings settings = new JsonWriterSettings();
			settings.PrettyPrint = true;
			settings.Tab = "  ";
			settings.TypeHintName = "type";
			//settings.AddTypeConverter(new VectorConverter());
			JsonWriter writer = new JsonWriter(output, settings);

			writer.Write(obj);
			string json = output.ToString();
			return json;
		}
		public static object JsonToObject<T>(string json)
		{
			JsonReaderSettings settings2 = new JsonReaderSettings();
			settings2.TypeHintName = "type";
			//settings2.AddTypeConverter(new VectorConverter());
			//JsonReader reader = new JsonReader(json, settings2);
			JsonReader reader = new JsonReader(json, settings2);

			T deserialized = (T)reader.Deserialize(typeof(T));

			return deserialized;
		}
	}
}
