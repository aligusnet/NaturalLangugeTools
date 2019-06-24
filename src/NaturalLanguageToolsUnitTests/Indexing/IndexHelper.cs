﻿using System;
using System.Collections.Generic;

using DocumentStorage;
using NaturalLanguageTools.Indexing;

namespace NaturalLanguageToolsUnitTests.Indexing
{
    public static class IndexHelper
    {
        public static string[][] GetTestSentenceCollections()
        {
            var collection1 = new[]
            {
                "The Great Barrier Reef in Australia is the world’s largest reef system",
                "The waste hierarchy or 3 R’s are in order of importance reduce reuse and recycle",
                "Around 75% of the volcanoes on Earth are found in the Pacific Ring of Fire an area around the Pacific Ocean where tectonic plates meet",
            };

            var collection2 = new[]
            {
                "Despite it name the Killer Whale Orca is actually a type of dolphin",
                "Giant water lilies in the Amazon can grow over 6 feet in diameter",
                "The largest ocean on Earth is the Pacific Ocean",
                "The largest individual flower on Earth is from a plant called Rafflesia arnoldii Its flowers reach up to 1 metre 3 feet in diameter and weigh around 10kg",
            };

            return new[]
            {
                collection1,
                collection2,
            };
        }

        public static IDictionary<string, DocumentId[]> Results = new Dictionary<string, DocumentId[]>
        {
            {
                "largest",
                new[]
                {
                    new DocumentId(0, 0),
                    new DocumentId(1, 2), new DocumentId(1, 3),
                }
            },
            {
                "the",
                new[]
                {
                    new DocumentId(0, 0), new DocumentId(0, 1), new DocumentId(0, 2),
                    new DocumentId(1, 0), new DocumentId(1, 1), new DocumentId(1, 2), new DocumentId(1, 3),
                }
            },
            {
                "moon",
                Array.Empty<DocumentId>()
            },
        };

        public static DocumentId[] AllDocuments = new[]
        {
            new DocumentId(0, 0), new DocumentId(0, 1), new DocumentId(0, 2),
            new DocumentId(1, 0), new DocumentId(1, 1), new DocumentId(1, 2), new DocumentId(1, 3),
        };

        public static void BuildIndex(IBuildableIndex<string> index, string[][] sentenceCollections)
        {
            for (var collectionId = 0; collectionId < sentenceCollections.Length; ++collectionId)
            {
                var collection = sentenceCollections[collectionId];
                for (var localId = 0; localId < collection.Length; ++localId)
                {
                    var doc = collection[localId].ToLower().Split();
                    var docId = new DocumentId((ushort)collectionId, (ushort)localId);
                    for (var position = 0; position < doc.Length; ++position)
                    {
                        index.IndexWord(docId, doc[position], position);
                    }
                }
            }
        }

        public static DictionaryIndex<string> CreateDictionaryIndex()
            => CreateDictionaryIndex(GetTestSentenceCollections());

        public static DictionaryIndex<string> CreateDictionaryIndex(string[][] storage)
        {
            var index = new DictionaryIndex<string>();
            IndexHelper.BuildIndex(index, storage);
            return index;
        }
    }
}