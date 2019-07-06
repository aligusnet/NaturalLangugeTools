﻿using System.Collections.Generic;

using NaturalLanguageTools.Tokenizers;
using NaturalLanguageTools.Transformers;

namespace NaturalLanguageTools
{
    public class DocumentTokenizer : CorpusTransformer<string, IEnumerable<string>>
    {
        public DocumentTokenizer(ITokenizer tokenizer) : base(d => tokenizer.Tokenize(d))
        {
        }
    }
}
