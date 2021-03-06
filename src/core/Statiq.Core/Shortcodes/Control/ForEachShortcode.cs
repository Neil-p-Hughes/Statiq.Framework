using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Statiq.Common;

namespace Statiq.Core
{
    /// <summary>
    /// Iterates a sequence from metadata, setting a specified key for each iteration.
    /// </summary>
    /// <parameter name="Key">The key that contains the sequence to iterate.</parameter>
    /// <parameter name="ValueKey">A key to set with the value for each iteration.</parameter>
    /// <parameter name="IndexKey">A key to set with the current iteration index (zero-based, optional).</parameter>
    public class ForEachShortcode : Shortcode
    {
        public override async Task<IEnumerable<IDocument>> ExecuteAsync(
            KeyValuePair<string, string>[] args,
            string content,
            IDocument document,
            IExecutionContext context)
        {
            IMetadataDictionary dictionary = args.ToDictionary(
                "Key",
                "ValueKey",
                "IndexKey");
            dictionary.RequireKeys("Key", "ValueKey");
            string valueKey = dictionary.GetString("ValueKey");
            if (string.IsNullOrEmpty(valueKey))
            {
                throw new ShortcodeArgumentException("Invalid ValueKey");
            }
            string indexKey = dictionary.GetString("IndexKey");

            IReadOnlyList<object> items = document.GetList<object>(dictionary.GetString("Key"));
            if (items != null)
            {
                List<IDocument> results = new List<IDocument>();
                int index = 0;
                foreach (object item in items)
                {
                    MetadataItems metadata = new MetadataItems()
                    {
                        { valueKey, item }
                    };
                    if (!string.IsNullOrEmpty(indexKey))
                    {
                        metadata.Add(indexKey, index);
                    }

                    results.Add(await document.CloneAsync(metadata, content));

                    index++;
                }

                return results;
            }

            return null;
        }
    }
}