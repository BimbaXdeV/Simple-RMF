using Microsoft.Extensions.Logging;
using RMF.Core.Interfaces;
using RMF.Core.Interfaces.Network;
using RMF_Server.Configurations;
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
    internal class Firewall : IFirewall, IDisposable
    {
        private readonly ILogger<Firewall> _logger;
        private readonly FirewallConfig _firewallConfig;

        private readonly Regex _ipExtractor;
        private readonly ConcurrentDictionary<string, byte> _bannedIPs;
        private bool _isChanged;

        private string BlacklistFilePath => PathResolver.GetResolvedPath(
            this._firewallConfig.BlacklistFilePath,
            fileName: "blacklist",
            fileFormat: "txt"
        );

        public Firewall(ILogger<Firewall> logger, FirewallConfig firewallConfig)
        {
            this._logger = logger;
            this._firewallConfig = firewallConfig;

            this._ipExtractor = new Regex(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);
            this._bannedIPs = new ConcurrentDictionary<string, byte>();
            this._isChanged = false;
        }

        public bool TryLoadBlacklist()
        {
            string path = this.BlacklistFilePath;
            string? directory = Path.GetDirectoryName(this.BlacklistFilePath);
            if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                this._logger.LogInformation("An empty file has been created to store blocked IP addresses on path: {FilePath}", path);
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

                    Match match = this._ipExtractor.Match(line);
                    if (match.Success)
                    {
                        string ip = match.Value;
                        if (IPAddress.TryParse(ip, out _))
                        {
                            this._bannedIPs.TryAdd(ip, 0);
                        }
                        else
                        {
                            this._logger.LogWarning("Invalid IP address format found in the banned IPs file on line {LineNum}: \"{LineText}\"", i + 1, line);
                        }
                    }
                }

                this._logger.LogInformation("Firewall connection blacklist loaded successfully ({Total} IPs)", this._bannedIPs.Count);
                this._isChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError("Failed to load banned IPs: {Exception}", ex);
                return false;
            }
        }

        public bool TrySaveBlacklist()
        {
            if (!this._isChanged)
            {
                this._logger.LogInformation("No changes detected in the banned IPs, skipping file update");
                return true;
            }

            try
            {
                string path = this.BlacklistFilePath;
                string? directory = Path.GetDirectoryName(path);
                if (string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(path);
                }

                string[] writtenIPs = File.ReadAllLines(path);
                string[] actualIPs = _bannedIPs.Keys.OrderBy(ip => ip).ToArray();

                bool isEqual = writtenIPs.OrderBy(ip => ip).SequenceEqual(actualIPs, StringComparer.OrdinalIgnoreCase);
                if (!isEqual)
                {
                    File.WriteAllLines(path, actualIPs);
                    this._logger.LogInformation("Updated banned IPs have been saved to \"{FilePath}\"", path);
                }
                else
                {
                    this._logger.LogInformation("No changes detected in the banned IPs, skipping file update");
                }

                this._isChanged = false;
                return true;
            }
            catch (Exception ex)
            {
                this._logger.LogError("An error occurred while saving banned IPs: {Exception}", ex);
                return false;
            }
        }

        public bool IsBanned(string ipAddress)
        {
            return this._bannedIPs.ContainsKey(ipAddress);
        }

        public string[] GetBannedIPs(int limit = -1)
        {
            ICollection<string> keys = this._bannedIPs.Keys;
            return limit <= 0 ? keys.ToArray() : keys.Take(limit).ToArray();
        }

        public int GetBannedIPsCount()
        {
            return this._bannedIPs.Count;
        }

        public void Ban(string? ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress) && this._ipExtractor.IsMatch(ipAddress))
            {
                if (this._bannedIPs.TryAdd(ipAddress, 0))
                {
                    this._isChanged = true;
                    this._logger.LogInformation("The suspicious IP \"{IpAddress}\" has been banned", ipAddress);
                }
                else
                {
                    this._logger.LogWarning($"The suspicious IP is already on the blacklist");
                }
            }
            else
            {
                this._logger.LogError("Failed to ban the IP, received an invalid structure");
            }
        }

        public void Dispose()
        {
            if (this._firewallConfig.EnableBlacklistSaving)
            {
                TrySaveBlacklist();
            }
        }
    }
}
