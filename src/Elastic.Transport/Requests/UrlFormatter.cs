// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using System;
using System.Collections.Generic;
using System.Linq;
using Elastic.Transport.Extensions;

namespace Elastic.Transport
{
	/// <summary>
	/// A formatter that can utilize <see cref="ITransportConfigurationValues" /> to resolve <see cref="IUrlParameter" />'s passed
	/// as format arguments. It also handles known string representations for e.g bool/Enums/IEnumerable<object>.
	/// </summary>
	public class UrlFormatter : IFormatProvider, ICustomFormatter
	{
		private readonly ITransportConfigurationValues _settings;

		public UrlFormatter(ITransportConfigurationValues settings) => _settings = settings;

		public string Format(string format, object arg, IFormatProvider formatProvider)
		{
			if (arg == null) throw new ArgumentNullException();

			if (format == "r") return arg.ToString();

			var value = CreateString(arg, _settings);
			if (value.IsNullOrEmpty() && !format.IsNullOrEmpty())
				throw new ArgumentException($"The parameter: {format} to the url is null or empty");

			return value.IsNullOrEmpty() ? string.Empty : Uri.EscapeDataString(value);
		}

		public object GetFormat(Type formatType) => formatType == typeof(ICustomFormatter) ? this : null;

		public string CreateString(object value) => CreateString(value, _settings);

		public static string CreateString(object value, ITransportConfigurationValues settings)
		{
			switch (value)
			{
				case null: return null;
				case string s: return s;
				case string[] ss: return string.Join(",", ss);
				case Enum e: return e.GetStringValue();
				case bool b: return b ? "true" : "false";
				case DateTimeOffset offset: return offset.ToString("o");
				case IEnumerable<object> pns:
					return string.Join(",", pns.Select(o => ResolveUrlParameterOrDefault(o, settings)));
				case TimeSpan timeSpan: return timeSpan.ToTimeUnit();
				default:
					return ResolveUrlParameterOrDefault(value, settings);
			}
		}

		private static string ResolveUrlParameterOrDefault(object value, ITransportConfigurationValues settings) =>
			value is IUrlParameter urlParam ? urlParam.GetString(settings) : value.ToString();
	}
}
