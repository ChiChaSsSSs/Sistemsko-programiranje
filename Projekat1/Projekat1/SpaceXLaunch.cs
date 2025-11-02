using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Projekat1
{
    public class SpaceXLaunch
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("rocket")]
        public string? Rocket { get; set; }

        [JsonPropertyName("success")]
        public bool? Success { get; set; }
        public bool Matches(SpaceXLaunch launch)
        {
            if (this.Rocket != null)
                if (launch.Rocket == null || !Rocket.Contains(launch.Rocket, StringComparison.OrdinalIgnoreCase))
                    return false;

            if (this.Success.HasValue)
                if (!launch.Success.HasValue || Success.Value != launch.Success.Value)
                    return false;

            if (this.Name != null)
                if (launch.Name == null || !Name.Contains(launch.Name, StringComparison.OrdinalIgnoreCase))
                    return false;

            return true;
        }

    }
}
