﻿using System;
using System.Collections.Generic;

using Corpus;
using InformationRetrieval.Indexing;
using InformationRetrieval.Utility;

namespace InformationRetrieval.BooleanSearch
{
    public class BooleanSearchEngine<T>
    {
        private readonly ISearchableIndex<T> index;
        private readonly Func<string, T> tokenConvertor;

        public BooleanSearchEngine(ISearchableIndex<T> index, Func<string, T> tokenConvertor)
        {
            this.index = index;
            this.tokenConvertor = tokenConvertor;
        }

        public IEnumerable<DocumentId> ExecuteQuery(BooleanQuery query)
        {
            switch(query)
            {
                case BooleanQueryTerm term:
                    return ExecuteTerm(term);
                case BooleanQueryOperationAnd opAnd:
                    return ExecuteAnd(opAnd);
                case BooleanQueryOperationOr opOr:
                    return ExecuteOr(opOr);
                case BooleanQueryOperationNot opNot:
                    return ExecuteNot(opNot);
                default:
                    throw new InvalidOperationException($"Got unexpected Boolean query: {query?.GetType()}");
            }
        }

        private IEnumerable<DocumentId> ExecuteTerm(BooleanQueryTerm term)
        {
            return index.Search(tokenConvertor(term.Word));
        }

        private IEnumerable<DocumentId> ExecuteAnd(BooleanQueryOperationAnd opAnd)
        {
            var minHeapComparer = Comparer<IEnumerator<DocumentId>>.Create((x, y) 
                => y.Current.CompareTo(x.Current));

            var queue = new PriorityQueue<IEnumerator<DocumentId>>(
                opAnd.Elements.Count,
                minHeapComparer);

            foreach (var q in opAnd.Elements)
            {
                var enumerator = ExecuteQuery(q).GetEnumerator();
                
                if (enumerator.MoveNext())
                {
                    queue.Push(enumerator);
                }
                else
                {
                    yield break;
                }
            }

            DocumentId prev = new DocumentId(0);
            int counter = 0;

            while (queue.Count > 0)
            {
                var enumerator = queue.Pop();

                if (prev.CompareTo(enumerator.Current) == 0)
                {
                    counter++;

                    if (counter == opAnd.Elements.Count)
                    {
                        yield return prev;
                    }
                }
                else if (queue.Count < opAnd.Elements.Count - 1)
                {
                    yield break;
                }
                else
                {
                    prev = enumerator.Current;
                    counter = 1;

                    if (counter == opAnd.Elements.Count)
                    {
                        yield return prev;
                    }
                }

                if (enumerator.MoveNext())
                {
                    queue.Push(enumerator);
                }
            }
        }

        private IEnumerable<DocumentId> ExecuteOr(BooleanQueryOperationOr opOr)
        {
            var minHeapComparer = Comparer<IEnumerator<DocumentId>>.Create((x, y)
                => y.Current.CompareTo(x.Current));

            var queue = new PriorityQueue<IEnumerator<DocumentId>>(
                opOr.Elements.Count,
                minHeapComparer);

            foreach (var q in opOr.Elements)
            {
                var enumerator = ExecuteQuery(q).GetEnumerator();

                if (enumerator.MoveNext())
                {
                    queue.Push(enumerator);
                }
            }

            DocumentId? prev = null;

            while (queue.Count > 0)
            {
                var enumerator = queue.Pop();

                DocumentId current = enumerator.Current;

                if (enumerator.MoveNext())
                {
                    queue.Push(enumerator);
                }

                if ((prev?.CompareTo(current) ?? -1) != 0)
                {
                    yield return current;
                }

                prev = current;
            }
        }

        private IEnumerable<DocumentId> ExecuteNot(BooleanQueryOperationNot op)
        {
            var docs = ExecuteQuery(op.Element).GetEnumerator();
            var all = index.GetAll().GetEnumerator();

            if (!all.MoveNext())
            {
                yield break;
            }

            while (docs.MoveNext())
            {
                while (all.Current.CompareTo(docs.Current) < 0)
                {
                    yield return all.Current;

                    if (!all.MoveNext())
                    {
                        yield break;
                    }
                }

                if (!all.MoveNext())
                {
                    yield break;
                }
            }

            do
            {
                yield return all.Current;
            }
            while (all.MoveNext());
        }
    }
}
