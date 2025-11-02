using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Projekat3.Entities
{
    public class BookList
    {
        [JsonPropertyName("items")]
        public List<Book> ?Items { get; set; }
    }
}
