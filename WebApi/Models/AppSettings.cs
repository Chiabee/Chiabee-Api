﻿#nullable enable

namespace WebApi.Models
{
    using System.Linq;

    public class AppSettings
    {
        public string? ConnectionString { get; set; }
        public string? LogTablePrefix { get; set; }
        public SshEntity? MachineDefault { get; set; }
        public SshEntity? PlotterDefault { get; set; }
        public SshEntity[]? Plotters { get; set; }
        public SshEntity? FarmerDefault { get; set; }
        public SshEntity[]? Farmers { get; set; }
        public SshEntity? HarvesterDefault { get; set; }
        public SshEntity[]? Harvesters { get; set; }

        internal SshEntity[] GetPlotters() => Plotters.BasedOn(PlotterDefault).BasedOn(MachineDefault).ToArray();

        internal SshEntity[] GetFarmers() => Farmers.BasedOn(FarmerDefault).BasedOn(MachineDefault).ToArray();

        internal SshEntity[] GetHarvesters() => Harvesters.BasedOn(HarvesterDefault).BasedOn(MachineDefault).ToArray();
    }
}