using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Projekat3.Entities
{
    public class Book
    {
        [JsonPropertyName("volumeInfo")]
        public BookVolumeInfo ?volumeInfo { get; set; }
    }
}
