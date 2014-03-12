﻿using NMSD.Cronus.Transports;

namespace NMSD.Cronus.Pipelining.Transport.Config
{
    public abstract class PipelineTransportSettings : TransportSettings
    {
        public abstract void Build(PipelineSettings pipelineSettings);

        public IEndpointFactory EndpointFactory { get; protected set; }

        public IPipelineFactory<IPipeline> PipelineFactory { get; protected set; }
    }
}
