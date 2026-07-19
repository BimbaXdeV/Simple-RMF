using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF_Server.Debugger;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace RMF_Server.Logic
{
    internal class Firewall : IFirewall
    {
        private readonly ILoggingEngine? _logger;

        private readonly Regex IpExtractor;
        private ConcurrentDictionary<string, byte> bannedIPs;
        private bool IsChanged;

        public Firewall(ILoggingEngine? logger = null)
        {
            this._logger = logger;

            this.IpExtractor = new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);
            this.bannedIPs = new ConcurrentDictionary<string, byte>();
            this.IsChanged = false;
        }

        public bool TryLoadFrom(string path)
        {
            string? directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                this._logger?.Output($"An empty file has been created to store blocked IP addresses on path: {path}");
                return true;
            }

            try
            {
                string[] rawIPs = File.ReadAllLines(path);

                for (int i = 0; i < rawIPs.Length; i++)
                {
                    string line = rawIPs[i];

                    if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith('#'))
                    {
                        continue;
                    }

                    Match match = IpExtractor.Match(line);
                    if (match.Success)
                    {
                        string ip = match.Value;
                        if (IPAddress.TryParse(ip, out _))
                        {
                            bannedIPs.TryAdd(ip, 0);
                        }
                        else
                        {
                            this._logger?.Warning($"Invalid IP address format found in the banned IPs file on line {i + 1}: \"{line}\"");
                        }
                    }
                }
                IsChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                this._logger?.Error($"Failed to load banned IPs: {ex}");
                return false;
            }
        }

        public bool TrySaveTo(string path)
        {
            if (!IsChanged)
            {
                this._logger?.Output("No changes detected in the banned IPs, skipping file update");
                return true;
            }

            try
            {
                string? directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(path);
                }

                string[] writtenIPs = File.ReadAllLines(path);
                string[] actualIPs = bannedIPs.Keys.OrderBy(ip => ip).ToArray();

                bool isEqual = writtenIPs.OrderBy(ip => ip).SequenceEqual(actualIPs, StringComparer.OrdinalIgnoreCase);
                if (!isEqual)
                {
                    File.WriteAllLines(path, actualIPs);
                    this._logger?.Output($"Updated banned IPs have been saved to \"{path}\"");
                }
                else
                {
                    this._logger?.Output("No changes detected in the banned IPs, skipping file update");
                }
                IsChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                this._logger?.Error($"An error occurred while saving banned IPs: {ex}");
                return false;
            }
        }

        public bool IsBanned(string ipAddress)
        {
            return bannedIPs.ContainsKey(ipAddress);
        }

        public string[] GetBannedIPs(int? limit = null)
        {
            ICollection<string> keys = bannedIPs.Keys;
            return limit == null ? keys.ToArray() : keys.Take(limit.Value).ToArray();
        }

        public void Ban(string? ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress) && IpExtractor.IsMatch(ipAddress))
            {
                if (bannedIPs.TryAdd(ipAddress, 0))
                {
                    IsChanged = true;
                    this._logger?.Output($"The suspicious IP \"{ipAddress}\" has been banned");
                }
                else
                {
                    this._logger?.Warning($"The suspicious IP is already on the blacklist");
                }
            }
            else
            {
                this._logger?.Warning("Failed to ban the IP, received an invalid structure");
            }
        }
    }
}
