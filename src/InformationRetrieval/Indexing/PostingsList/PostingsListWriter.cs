﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

using Corpus;
using InformationRetrieval.Utility;

namespace InformationRetrieval.Indexing.PostingsList
{
    public class PostingsListWriter : IDisposable
    {
        private readonly Stream stream;
        private readonly BinaryWriter writer;

        public PostingsListWriter(Stream stream)
        {
            this.stream = stream;
            writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
        }

        public long Write(IReadOnlyCollection<DocumentId> postings)
        {
            var position = stream.Position;
            writer.Write(postings.Count);

            switch (postings)
            {
                case ListChain<DocumentId> chain:
                    WriteChained(chain);
                    break;

                case RangePostingsList range:
                    writer.Write((byte)PostingsListType.Ranged);
                    WriteRanged(range.Ranges);
                    break;

                case VarintPostingsList varint:
                    writer.Write((byte)PostingsListType.Varint);
                    WriteVarint(varint);
                    break;

                default:
                    writer.Write((byte)PostingsListType.Uncompressed);
                    WriteUncompressed(postings);
                    break;
            }
            
            writer.Flush();

            return position;
        }

        public Stream Reset()
        {
            writer.Dispose();

            stream.Flush();
            stream.Seek(0, SeekOrigin.Begin);
            return stream;
        }

        public void Dispose()
        {
            writer.Dispose();
        }

        private void WriteVarint(VarintPostingsList varint)
        {
            var data = varint.GetReadOnlySpan();
            writer.Write(data.Length);
            writer.Write(data);
        }

        private void WriteUncompressed(IReadOnlyCollection<DocumentId> postings)
        {
            foreach (var id in postings)
            {
                writer.Write(id.Id);
            }
        }

        private void WriteRanged(IList<uint> list)
        {
            writer.Write(list.Count);
            foreach (var v in list)
            {
                writer.Write(v);
            }
        }

        /// <summary>
        /// This complicated logis below was implemented for the sake of performance only.
        /// We can always process ListChain as uncompressed list of Ids.
        /// </summary>
        /// <param name="chain">List of chains</param>
        private void WriteChained(ListChain<DocumentId> chain)
        {
            switch (DetectType(chain))
            {
                case PostingsListType.Ranged:
                    writer.Write((byte)PostingsListType.Ranged);
                    WriteChainedRanges(chain);
                    break;

                case PostingsListType.Varint:
                    writer.Write((byte)PostingsListType.Varint);
                    WriteChainedVarint(chain);
                    break;

                default:
                    writer.Write((byte)PostingsListType.Uncompressed);
                    WriteUncompressed(chain);
                    break;
            }
        }

        private static PostingsListType DetectType(ListChain<DocumentId> chain)
        {
            if (chain.Chains.Count > 0 && chain.Chains[0] is VarintPostingsList)
            {
                return PostingsListType.Varint;
            }

            if (chain.Chains.Count > 0 && chain.Chains[0] is RangePostingsList)
            {
                return PostingsListType.Ranged;
            }

            if (chain.Chains.Count > 1 && chain.Chains[1] is RangePostingsList)
            {
                return PostingsListType.Ranged;
            }

            return PostingsListType.Uncompressed;
        }

        private void WriteChainedVarint(ListChain<DocumentId> chain)
        {
            var varint = new VarintPostingsList(32);
            foreach (var id in chain)
            {
                varint.Add(id);
            }

            WriteVarint(varint);
        }

        private void WriteChainedRanges(ListChain<DocumentId> chain)
        {
            var start = stream.Position;
            int numBlocks = 0;
            writer.Write(numBlocks);  // we do not know a number of block at the moment

            foreach (var c in chain.Chains)
            {
                var range = GetRange(c);
                foreach (var v in range.Ranges)
                {
                    writer.Write(v);
                    ++numBlocks;
                }
            }

            var finish = stream.Position;

            stream.Seek(start, SeekOrigin.Begin);
            writer.Write(numBlocks);                // write correct number of blocks
            stream.Seek(finish, SeekOrigin.Begin);  // go back
        }

        private RangePostingsList GetRange(IReadOnlyCollection<DocumentId> chain)
        {
            return chain switch
            {
                RangePostingsList range => range,
                _ => ConvertToRange(chain)
            };
        }

        private RangePostingsList ConvertToRange(IReadOnlyCollection<DocumentId> chain)
        {
            var newRange = new RangePostingsList();
            foreach (var id in chain)
            {
                newRange.Add(id);
            }
            return newRange;
        }
    }
}
