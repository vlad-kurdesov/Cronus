﻿using System.Linq;
using Elders.Cronus.Logging;

namespace Elders.Cronus.EventStore
{

    //public class MissingFirstRevisionValidator : IEventStreamValidator
    //{
    //    public IValidatorResult Validate(EventStream eventStream)
    //    {
    //        //var gg = eventStream.aggregateCommits(x=>x.Revision
    //    }
    //}



    public class BruteForceDuplicateRevisionResolver
    {
        static readonly ILog log = LogProvider.GetLogger(typeof(BruteForceDuplicateRevisionResolver));

        public EventStream Resolve(EventStream eventStream)
        {
            var errors = eventStream.Commits
                .GroupBy(x => x.Revision)
                .OrderBy(x => x.Key)
                .Select(x => x.First())
                .ToList();
            return new EventStream(errors);
        }
    }
}
