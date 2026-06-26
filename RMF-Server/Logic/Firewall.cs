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
    internal static class Firewall
    {
        private static readonly Regex IpExtractor = new(@"\b(?:\d{1,3}\.){3}\d{1,3}\b", RegexOptions.Compiled);
        private static ConcurrentDictionary<string, byte> bannedIPs = new();

        public static bool TryLoadFrom(string path)
        {
            string? directory = Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(path);
            }

            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
                Logging.Output($"An empty file has been created to store blocked IP addresses on path: {path}");
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
                            Logging.Warning($"Invalid IP address format found in the banned IPs file on line {i + 1}: \"{line}\"");
                        }
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logging.Error($"Failed to load banned IPs: {ex}");
                return false;
            }
        }

        public static bool TrySaveTo(string path)
        {
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
                    Logging.Output($"Updated banned IPs have been saved to \"{path}\"");
                }
                else
                {
                    Logging.Output("No changes detected in the banned IPs, skipping file update");
                }
                return true;
            }
            catch (Exception ex)
            {
                Logging.Error($"An error occurred while saving banned IPs: {ex}");
                return false;
            }
        }

        public static bool IsBanned(string ipAddress)
        {
            return bannedIPs.ContainsKey(ipAddress);
        }

        public static string[] GetBannedIPs(int? limit = null)
        {
            ICollection<string> keys = bannedIPs.Keys;
            return limit == null ? keys.ToArray() : keys.Take(limit.Value).ToArray();
        }

        public static void Ban(string? ipAddress)
        {
            if (!string.IsNullOrEmpty(ipAddress) && IpExtractor.IsMatch(ipAddress))
            {
                if (bannedIPs.TryAdd(ipAddress, 0))
                {
                    Logging.Output($"The suspicious IP \"{ipAddress}\" has been banned");
                }
                else
                {
                    Logging.Warning($"The suspicious IP is already on the blacklist");
                }
            }
            else
            {
                Logging.Warning("Failed to ban the IP, received an invalid structure");
            }
        }
    }
}
