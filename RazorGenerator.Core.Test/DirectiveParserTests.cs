using System;
using System.Collections.Generic;
using System.Linq;

using Xunit;

namespace RazorGenerator.Core.Test
{
	public class DirectiveParserTests
	{
		[Fact]
		public void DirectivesParserWillReadKeyValuePairsCorrectly()
		{
			const String input =
@"
notakey
key1:value1
key2 : value2 key22: value22
key3 : value3, value31
alsonotakey";

			Dictionary<string,string> pairs = new Dictionary<string, string>();
			DirectivesParser.ParseKeyValueDirectives( pairs, input );

			KeyValuePair<string,string>[] expected = new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>( "key1", "value1" ),
				new KeyValuePair<string, string>( "key2", "value2" ),
				new KeyValuePair<string, string>( "key22", "value22" ),
				new KeyValuePair<string, string>( "key3", "value3, value31" ),
			};

			Assert.Equal( expected.ToList(), pairs.ToList() );
		}

		[Fact]
		public void DirectivesParserWillIgnoreComments()
		{
			const String input =
@"
notakey
key1:value1
key2 : value2 # key22: value22 # that was a comment
key3 : value3, value31
# comment: goes, here
alsonotakey
  # another comment key4: value4
 key5: value5";

			Dictionary<string,string> pairs = new Dictionary<string, string>();
			DirectivesParser.ParseKeyValueDirectives( pairs, input );

			KeyValuePair<string,string>[] expected = new KeyValuePair<string, string>[]
			{
				new KeyValuePair<string, string>( "key1", "value1" ),
				new KeyValuePair<string, string>( "key2", "value2" ),
				new KeyValuePair<string, string>( "key3", "value3, value31" ),
				new KeyValuePair<string, string>( "key5", "value5" ),
			};

			Assert.Equal( expected.ToList(), pairs.ToList() );
		}
	}
}
