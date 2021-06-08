﻿namespace WebApi.Services.ServerCommands
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Renci.SshNet;
    using WebApi.Models;
    using WebApi.Services;

    public static class HarvesterDiskCommand
    {
        public static HarvesterDiskInfo[] GetHarvesterDiskInfo(this TargetMachine client)
        {
            if (!client.EnsureConnected()) return null;
            using var cmd = client.RunCommand(@"lsblk -e7 -no name,mountpoint,label,size,type,uuid");
            /*
sda                                             3.7T disk
└─sda1                    /farm/8      wy006    3.7T part 10f015c0-4b7d-4a47-850a-c75bfc441ea9
sdc                                           223.6G disk
├─sdc1                                            1M part
├─sdc2                    /boot                   1G part 53801bff-2359-432f-9565-a7e563bd2f2a
└─sdc3                                        222.6G part iIQ1cM-qGEM-rqyp-Ul8e-fMg8-1dva-gz39XC
└─ubuntu--vg-ubuntu--lv /                   111.3G lvm  feaa5e8d-abb9-4704-9f73-75593f3ee898
sdd                                             3.7T disk
└─sdd1                    /farm/zsl003 zsl003   3.7T part d74c5ecb-7013-4967-9c2e-6dcf6f853f64
sde                                             3.7T disk
sdm                                             3.7T disk
└─sdm1                    /farm/zyc008 zyc008   3.7T part e6c6f3f3-55f9-4aa9-ac5b-ad12c78520f0
             */
            var diskNames = ParseDiskName(cmd.Result).ToArray();

            static IEnumerable<string> ParseDiskName(string output) => output
                .CleanSplit()
                .Where(_ => _.Contains("disk"))
                .Select(_ => _.CleanSplit(" "))
                //sda                                             3.7T disk
                .Select(segs => (segs.Length == 3 && segs[2] == "disk") ? segs[0] : default)
                .Where(_ => _ != null);

            var devs = ParseDiskInfo(cmd.Result)
                .ToArray();

            static IEnumerable<DevicePartInfo> ParseDiskInfo(string output) => output
                .CleanSplit()
                .Where(_ => _.StartsWith("└─"))
                .Select(_ => _["└─".Length..].CleanSplit(" "))
                .Where(segs => segs.Length == 6 && segs[0].Contains("1") && segs[4] == "part")
                //└─sdm1                    /farm/zyc008 zyc008   3.7T part e6c6f3f3-55f9-4aa9-ac5b-ad12c78520f0
                .Select(segs => new DevicePartInfo(segs[0], segs[3], segs[1], segs[2], segs[5]) { BlockDevice = segs[0].TrimEnd('1'), });

            return ParseDiskSmart(diskNames, devs).ToArray();

            IEnumerable<HarvesterDiskInfo> ParseDiskSmart(string[] diskNames, DevicePartInfo[] devices)
            {
                var dd = devices.ToDictionary(_ => _.BlockDevice, _ => _);
                foreach (var disk in diskNames)
                {
                    dd.TryGetValue(disk, out var device);
                    using var cmd = client.RunCommand(@$"echo sutu | sudo -S sudo smartctl -d sat -a /dev/{disk}");
                    var smart = cmd.Result;
                    var pairs = CommandHelper.ParsePairs(smart,
                        new StatusDefinition(nameof(HarvesterDiskInfo.Sn), "Serial Number"),
                        new StatusDefinition(nameof(HarvesterDiskInfo.Model), "Device Model")
                        )
                        .ToDictionary(_ => _.Key, _ => _.Value);

                    yield return new HarvesterDiskInfo(
                        pairs.TryGetValue(nameof(HarvesterDiskInfo.Sn), out var sn) ? sn : null,
                        pairs.TryGetValue(nameof(HarvesterDiskInfo.Model), out var model) ? model : null,
                        disk,
                        device == null ? null : new[] { device with { BlockDevice = null } },
                        null
                        );
                }

            }
        }
    }

    public record HarvesterDiskInfo(string Sn, string Model, string BlockDevice, DevicePartInfo[] Parts, DiskSmartInfo Smart);
    public record DevicePartInfo(string Name, string Size, string MountPoint, string Label, string Uuid)
    {
        public string BlockDevice { get; init; }
    }
    public record DiskSmartInfo(int PowerCycleCount, int PowerOnHours, int Temperature, DiskSmartPair[] Values);
    public record DiskSmartPair(string Key, string Value);
}